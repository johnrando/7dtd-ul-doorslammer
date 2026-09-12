using System;
using System.Reflection;
using HarmonyLib;

namespace DoorSlammer
{
	/// <summary>
	/// Installs the Harmony patches. Each is resolved late and gated on its own prerequisites, so a
	/// game update that moves something degrades to a log line rather than an exception at init.
	///
	/// No load order needs declaring: UL applies its patches as a BepInEx plugin before any
	/// IModApi.InitMod runs, and ModManager loads every mod assembly before calling any InitMod.
	/// </summary>
	internal static class Patches
	{
		internal const string LogPrefix = "[DoorSlammer] ";

		private const string HarmonyId = "DoorSlammer";

		private const string NotRunYet = "not applied - mod init has not run";

		/// <summary>Outcome of each patch, as reported by <c>ds info</c>.</summary>
		internal static string DoorCloseHookStatus = NotRunYet;

		internal static string RageSuppressionStatus = NotRunYet;

		private static bool applied;

		internal static void Apply()
		{
			if (applied)
			{
				return;
			}
			applied = true;
			try
			{
				ApplyPatches();
			}
			catch (Exception e)
			{
				Log.Error(LogPrefix + "Failed to apply patches; door slams will do nothing.");
				Log.Exception(e);
			}
		}

		private static void ApplyPatches()
		{
			UndeadLegacyInfo.Report();
			FlavorBridges.Resolve();

			Harmony harmony = new Harmony(HarmonyId);
			ApplyDoorCloseHook(harmony);
			ApplyRageSuppression(harmony);
		}

		/// <summary>The one patch the mod cannot work without.</summary>
		private static void ApplyDoorCloseHook(Harmony _harmony)
		{
			// Protected in source but the shipped Assembly-CSharp is publicized. No vanilla or UL
			// subclass overrides it, so a single patch covers every door in both.
			MethodInfo target = AccessTools.DeclaredMethod(typeof(BlockDoor), "updateOpenCloseState");
			if (target == null)
			{
				DoorCloseHookStatus = "NOT APPLIED - BlockDoor.updateOpenCloseState not found";
				Log.Error(LogPrefix + "Door-close hook NOT applied: BlockDoor.updateOpenCloseState "
					+ "could not be found, so slamming a door will never damage anything.");
				return;
			}

			_harmony.Patch(target, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(DoorCloseTrigger), nameof(DoorCloseTrigger.Postfix))));

			DoorCloseHookStatus = "applied - postfix on BlockDoor.updateOpenCloseState";
			Log.Out(LogPrefix + "Door-close hook applied: closing a door now checks the doorway for a "
				+ "zombie to catch.");
		}

		/// <summary>
		/// Optional and inert until <c>ds rage</c> switches it on, but installed up front so the
		/// toggle does not have to re-patch a live method mid-session.
		/// </summary>
		private static void ApplyRageSuppression(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(EntityHuman), "ProcessDamageResponseLocal");
			if (target == null)
			{
				RageSuppressionStatus = "NOT APPLIED - EntityHuman.ProcessDamageResponseLocal not found";
				Log.Warning(LogPrefix + "Rage suppression NOT applied: "
					+ "EntityHuman.ProcessDamageResponseLocal could not be found. Slams still work; "
					+ "'ds rage' will have no effect.");
				return;
			}

			if (!RageSuppression.BindReversePatch(_harmony))
			{
				RageSuppressionStatus = "NOT APPLIED - could not copy the vanilla damage response";
				Log.Warning(LogPrefix + "Rage suppression NOT applied: could not build a copy of "
					+ "EntityAlive.ProcessDamageResponseLocal. Slams still work; 'ds rage' will "
					+ "have no effect.");
				return;
			}

			// Priority.First sorts this ahead of Undead Legacy's rage prefix so returning false can
			// skip it - only ever for our own damage source.
			_harmony.Patch(target, prefix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(RageSuppression), nameof(RageSuppression.Prefix)))
			{
				priority = Priority.First
			});

			RageSuppressionStatus = "applied - currently " + (Settings.SuppressRage ? "on" : "off");
			Log.Out(LogPrefix + "Rage suppression installed and switched "
				+ (Settings.SuppressRage ? "on" : "off") + " (toggle with 'ds rage').");
		}
	}
}
