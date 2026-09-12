using System;
using System.Reflection;
using HarmonyLib;

namespace DoorSlammer
{
	/// <summary>
	/// The optional other half of a slam: if FletchWounds is installed, a slam that catches a
	/// zombie with one of the player's arrows in it drives that arrow deeper.
	///
	/// Resolved by reflection rather than referenced, so either mod works with the other absent.
	/// FletchWounds publishes two methods - the proc and a flavor setter - bound independently, in
	/// game types only, each into a delegate so a slam pays no reflection cost. Every failure
	/// degrades to a log line and an inert bridge.
	/// </summary>
	internal static class FletchWoundsBridge
	{
		private const string AssemblyName = "FletchWounds";

		private const string TypeName = "FletchWounds.DoorSlamInterop";

		private const string MethodName = "TryProc";

		private const string Label = "FletchWounds";

		/// <summary>Outcome of the lookup, as reported by <c>ds info</c>.</summary>
		internal static string Status = "not checked";

		/// <summary>The tail of the <c>ds flavor</c> menu line, ready to print.</summary>
		internal static string FlavorSummary = "no supported mods installed";

		private static Func<EntityAlive, EntityAlive, bool> proc;

		/// <summary>Whether the assembly was there at all, as opposed to there but unusable.</summary>
		private static bool found;

		/// <summary>FletchWounds' own flavor setter, bound the same way it binds ours.</summary>
		private static Action<bool> setTheirs;

		internal static void Resolve()
		{
			Assembly assembly = UndeadLegacyInfo.FindAssembly(AssemblyName);
			found = assembly != null;
			if (assembly == null)
			{
				Status = "not installed";
				return;
			}

			try
			{
				Type type = assembly.GetType(TypeName, false);
				MethodInfo method = type == null
					? null
					: AccessTools.DeclaredMethod(type, MethodName,
						new[] { typeof(EntityAlive), typeof(EntityAlive) });

				if (method == null || method.ReturnType != typeof(bool))
				{
					Status = "installed, but " + TypeName + "." + MethodName + " did not match";
					Log.Warning(Patches.LogPrefix + Label + " is installed but " + TypeName + "."
						+ MethodName + " could not be bound, so a slam will not drive arrows in. "
						+ "Both mods still work; they just do not talk to each other.");
					return;
				}

				proc = (Func<EntityAlive, EntityAlive, bool>)Delegate.CreateDelegate(
					typeof(Func<EntityAlive, EntityAlive, bool>), method);

				setTheirs = BindFlavorSetter(type);

				Status = "installed - arrow procs wired up";
				FlavorSummary = "enhanced mod interaction with " + Label;
				Log.Out(Patches.LogPrefix + Label + " detected: a slam that catches a zombie with one "
					+ "of your arrows in it now drives that arrow deeper. Toggle with 'ds flavor'.");
			}
			catch (Exception e)
			{
				Status = "installed, but the lookup threw";
				Log.Warning(Patches.LogPrefix + "Could not bind " + Label + ": " + e.Message);
			}
		}

		/// <summary>
		/// Optional, and bound separately: a build whose TryProc binds but whose SetFlavor does not
		/// still gets the interaction, the player just has to set the other switch themselves.
		/// </summary>
		private static Action<bool> BindFlavorSetter(Type _type)
		{
			MethodInfo method = AccessTools.DeclaredMethod(_type, "SetFlavor", new[] { typeof(bool) });
			if (method == null)
			{
				Log.Warning(Patches.LogPrefix + Label + " has no flavor switch to link, so 'ds flavor' "
					+ "only sets this side. Set 'fw flavor' too.");
				return null;
			}
			return (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), method);
		}

		/// <summary>
		/// Mirror this mod's flavor setting onto FletchWounds. Called only from the console command:
		/// whoever the player typed at owns the propagation, which is what stops the two mods
		/// calling each other forever.
		/// </summary>
		internal static void PushFlavor(bool _on)
		{
			if (setTheirs == null)
			{
				return;
			}

			try
			{
				setTheirs(_on);
			}
			catch (Exception e)
			{
				setTheirs = null;
				Log.Warning(Patches.LogPrefix + "Could not mirror the flavor switch to " + Label
					+ "; set it there by hand. " + e.Message);
			}
		}

		/// <summary>
		/// Called on every slam that caught a zombie, including one the never-kill floor spared.
		/// </summary>
		internal static void TryProc(World _world, EntityAlive _zombie)
		{
			if (proc == null || !Settings.Flavor)
			{
				return;
			}

			// updateOpenCloseState carries no player argument, but the whole door-activation path
			// runs on the machine of the player who pressed the key (see ServerAuthoritative), so
			// the primary player is the one who closed it. On a dedicated server this is null and
			// the proc is skipped.
			EntityPlayerLocal player = _world.GetPrimaryPlayer();
			if (player == null || player.IsDead())
			{
				return;
			}

			try
			{
				if (proc(_zombie, player))
				{
					Counters.ArrowProcs++;
				}
			}
			catch (Exception e)
			{
				// One throw retires the bridge rather than repeating on every future slam.
				proc = null;
				Status = "installed, but the call threw - bridge retired for this session";
				Log.Error(Patches.LogPrefix + Label + " threw during a slam; arrow procs are off for "
					+ "the rest of this session. Slams themselves are unaffected.");
				Log.Exception(e);
			}
		}

		/// <summary>The line <c>ds flavor</c> prints after toggling.</summary>
		internal static string Describe()
		{
			bool linked = setTheirs != null;

			if (!Settings.Flavor)
			{
				return linked
					? "Flavor OFF here and in " + Label + " - a slam does its own damage and nothing else."
					: "Flavor OFF - a slam does its own damage and nothing else.";
			}

			if (proc == null)
			{
				// Installed-but-unbound is a version mismatch somebody can act on; absent is not.
				return found
					? "Flavor ON - but " + Label + " could not be bound, so nothing changes. See 'ds info'."
					: "Flavor ON - but no mod that hooks into it is installed, so nothing changes.";
			}

			return linked
				? "Flavor ON here and in " + Label + " - a slam now drives your arrows deeper into "
					+ "whatever it catches."
				: "Flavor ON - a slam drives your arrows deeper into whatever it catches. " + Label
					+ " needs 'fw flavor' on too.";
		}
	}
}
