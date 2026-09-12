using System;
using System.Reflection;
using HarmonyLib;

namespace DoorSlammer
{
	/// <summary>
	/// One supported mod's half of a slam. Every mod that links up publishes the same two methods
	/// on a <c>&lt;Mod&gt;.DoorSlamInterop</c> type - <c>TryProc(EntityAlive zombie, EntityAlive
	/// slammer) : bool</c> and <c>SetFlavor(bool)</c> - and this binds them by reflection, so either
	/// side works with the other absent. Each is bound independently, in game types only, into a
	/// delegate so a slam pays no reflection cost. Every failure degrades to a log line and an
	/// inert bridge.
	///
	/// <see cref="FlavorBridges"/> holds the instances; this class knows nothing about the others.
	/// </summary>
	internal sealed class FlavorBridge
	{
		private const string TypeSuffix = ".DoorSlamInterop";

		private const string MethodName = "TryProc";

		/// <summary>The mod's name, which is also its assembly's simple name.</summary>
		internal readonly string Label;

		/// <summary>What the mod does with a caught zombie, for the log and the menu.</summary>
		private readonly string effect;

		/// <summary>Outcome of the lookup, as reported by <c>ds info</c>.</summary>
		internal string Status = "not checked";

		/// <summary>Slams this mod acted on. At most one per slam.</summary>
		internal int Procs;

		private Func<EntityAlive, EntityAlive, bool> proc;

		/// <summary>Whether the assembly was there at all, as opposed to there but unusable.</summary>
		internal bool Found { get; private set; }

		/// <summary>Whether the proc is bound and a slam will reach it.</summary>
		internal bool Wired => proc != null;

		/// <summary>The other mod's own flavor setter, bound the same way it binds ours.</summary>
		private Action<bool> setTheirs;

		internal bool Linked => setTheirs != null;

		/// <param name="_label">Mod and assembly name, e.g. "FletchWounds".</param>
		/// <param name="_effect">Finishes "a slam that catches a zombie ...", e.g. "drives your arrows deeper".</param>
		internal FlavorBridge(string _label, string _effect)
		{
			Label = _label;
			effect = _effect;
		}

		internal void Resolve()
		{
			Assembly assembly = UndeadLegacyInfo.FindAssembly(Label);
			Found = assembly != null;
			if (assembly == null)
			{
				Status = "not installed";
				return;
			}

			string typeName = Label + TypeSuffix;
			try
			{
				Type type = assembly.GetType(typeName, false);
				MethodInfo method = type == null
					? null
					: AccessTools.DeclaredMethod(type, MethodName,
						new[] { typeof(EntityAlive), typeof(EntityAlive) });

				if (method == null || method.ReturnType != typeof(bool))
				{
					Status = "installed, but " + typeName + "." + MethodName + " did not match";
					Log.Warning(Patches.LogPrefix + Label + " is installed but " + typeName + "."
						+ MethodName + " could not be bound, so a slam will not reach it. "
						+ "Both mods still work; they just do not talk to each other.");
					return;
				}

				proc = (Func<EntityAlive, EntityAlive, bool>)Delegate.CreateDelegate(
					typeof(Func<EntityAlive, EntityAlive, bool>), method);

				setTheirs = BindFlavorSetter(type);

				Status = "installed - wired up";
				Log.Out(Patches.LogPrefix + Label + " detected: a slam that catches a zombie now "
					+ effect + ". Toggle with 'ds flavor'.");
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
		private Action<bool> BindFlavorSetter(Type _type)
		{
			MethodInfo method = AccessTools.DeclaredMethod(_type, "SetFlavor", new[] { typeof(bool) });
			if (method == null)
			{
				Log.Warning(Patches.LogPrefix + Label + " has no flavor switch to link, so 'ds flavor' "
					+ "does not reach it. Set its own flavor switch too.");
				return null;
			}
			return (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), method);
		}

		/// <summary>Mirror the flavor setting onto this mod. Receivers never push back.</summary>
		internal void PushFlavor(bool _on)
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

		/// <summary>Hand a caught zombie over. Returns whether the mod acted on it.</summary>
		internal bool TryProc(EntityAlive _zombie, EntityPlayerLocal _player)
		{
			if (proc == null)
			{
				return false;
			}

			try
			{
				if (!proc(_zombie, _player))
				{
					return false;
				}
				Procs++;
				return true;
			}
			catch (Exception e)
			{
				// One throw retires the bridge rather than repeating on every future slam.
				proc = null;
				Status = "installed, but the call threw - bridge retired for this session";
				Log.Error(Patches.LogPrefix + Label + " threw during a slam; its part of a slam is "
					+ "off for the rest of this session. Slams themselves are unaffected.");
				Log.Exception(e);
				return false;
			}
		}

		/// <summary>What a slam does through this mod, for <c>ds flavor</c>'s line.</summary>
		internal string Effect => effect;
	}
}
