using System;
using System.Reflection;
using HarmonyLib;

namespace DoorSlammer
{
	/// <summary>
	/// Installs the mod's Harmony patches. Every patch is resolved late and gated on its own
	/// prerequisites, so a game update that moves something degrades to a log line naming the
	/// behaviour that is therefore missing, rather than an exception during mod init.
	///
	/// Load order needs no declaration. UL applies its own patches as a BepInEx plugin roughly a
	/// second before the game calls any IModApi.InitMod, so by the time this runs both are fully
	/// present regardless of mod folder ordering.
	/// </summary>
	internal static class Patches
	{
		internal const string LogPrefix = "[DoorSlammer] ";

		private const string HarmonyId = "DoorSlammer";

		private const string NotRunYet = "not applied - mod init has not run";

		/// <summary>Outcome of each patch, as reported by the <c>ds</c> console command.</summary>
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
			FletchWoundsBridge.Resolve();

			Harmony harmony = new Harmony(HarmonyId);
			ApplyDoorCloseHook(harmony);
			ApplyRageSuppression(harmony);
		}

		/// <summary>
		/// The one patch the mod cannot work without: without it nothing ever detects a door closing.
		/// </summary>
		private static void ApplyDoorCloseHook(Harmony _harmony)
		{
			// Declared protected in source, but the shipped Assembly-CSharp is publicized, so this
			// resolves. No vanilla or Undead Legacy subclass overrides it, which is what lets a
			// single patch cover every door in both.
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
		/// Optional, and inert until <c>ds rage</c> switches it on - but installed up front so the
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

			// Priority.First so this sorts ahead of Undead Legacy's rage prefix. Returning false
			// then skips it - but only ever for our own damage source.
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
