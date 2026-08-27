using System;
using HarmonyLib;

namespace DoorSlammer
{
	/// <summary>
	/// Optional: stops Undead Legacy's rage roll from firing on slam damage.
	///
	/// UL replaces <c>EntityHuman.ProcessDamageResponseLocal</c> with a prefix that always returns
	/// false, and rolls <c>rageChance * (Strength / 40)</c> on *every* damage response. At the
	/// default 25% setting a 1 HP slam is roughly 0.63% to enrage and about 1 in 4,500 to trigger
	/// super rage - double speed for 30 seconds plus an alert scream. Spread over the many slams it
	/// takes to whittle a zombie down, that adds up to a coin flip, which is squarely one of the
	/// "other effects" this mod is supposed to avoid.
	///
	/// It cannot be dodged by simply skipping the method: the damage itself lands inside
	/// ProcessDamageResponseLocal (<c>Health -= num4</c>), not before it. So this does what UL
	/// itself does one layer down - runs a pristine copy of the vanilla EntityAlive body via a
	/// reverse patch, then returns false to skip everything else.
	///
	/// Off by default. Staying out of UL's way is the safer default, so this is opt-in via
	/// <c>ds rage</c>.
	/// </summary>
	internal static class RageSuppression
	{
		/// <summary>
		/// Stand-in replaced by Harmony with a copy of the *unpatched* IL of
		/// <c>EntityAlive.ProcessDamageResponseLocal</c>. Because a reverse patch copies the
		/// original method, UL's own replacement of that method does not leak in - which matches
		/// what zombies already get under UL, whose rage prefix routes through an identical copy.
		/// </summary>
		internal static void VanillaProcessDamageResponseLocal(EntityAlive __instance,
			DamageResponse _dmResponse)
		{
			throw new NotImplementedException("Harmony reverse patch stub was not replaced.");
		}

		/// <summary>
		/// Runs at <c>Priority.First</c> on <c>EntityHuman.ProcessDamageResponseLocal</c>. Returning
		/// false skips every remaining prefix - UL's rage roll and UL's cosmetic floating-damage
		/// prefix included - but only ever for our own damage source, so ordinary combat is
		/// completely untouched.
		///
		/// The toggle is read here rather than at patch time so <c>ds rage</c> can flip it
		/// mid-session without re-patching.
		/// </summary>
		internal static bool Prefix(EntityHuman __instance, DamageResponse _dmResponse)
		{
			if (!Settings.Enabled || !Settings.SuppressRage)
			{
				return true;
			}
			if (!(_dmResponse.Source is DoorSlamDamageSource))
			{
				return true;
			}

			VanillaProcessDamageResponseLocal(__instance, _dmResponse);
			Counters.RageSuppressed++;
			return false;
		}

		/// <summary>
		/// Creates the reverse patch. Separate from installing the prefix so a failure here can
		/// disable the feature rather than leave a prefix that would throw on first use.
		/// </summary>
		internal static bool BindReversePatch(Harmony _harmony)
		{
			System.Reflection.MethodInfo original =
				AccessTools.DeclaredMethod(typeof(EntityAlive), "ProcessDamageResponseLocal");
			if (original == null)
			{
				Log.Error(Patches.LogPrefix + "EntityAlive.ProcessDamageResponseLocal not found.");
				return false;
			}

			System.Reflection.MethodInfo standin = AccessTools.DeclaredMethod(
				typeof(RageSuppression), nameof(VanillaProcessDamageResponseLocal));
			_harmony.CreateReversePatcher(original, new HarmonyMethod(standin))
				.Patch(HarmonyReversePatchType.Original);
			return true;
		}
	}
}
