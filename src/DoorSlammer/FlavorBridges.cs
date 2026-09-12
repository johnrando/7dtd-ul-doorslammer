using System.Collections.Generic;
using System.Text;

namespace DoorSlammer
{
	/// <summary>
	/// The optional other halves of a slam, in the order they run. FletchWounds first, because its
	/// arrow proc can kill and Stumblr's trip checks for a corpse; adding a mod is one line here
	/// plus its own <c>DoorSlamInterop</c>.
	///
	/// DoorSlammer is the hub for the linked flavor switch: a toggle here, or one pushed here by
	/// any bridged mod through <see cref="FlavorInterop.SetFlavor"/>, is pushed on to every bridge.
	/// Receivers never push back, so there is no loop, and the player can set the switch in any
	/// one mod and have all of them follow.
	/// </summary>
	internal static class FlavorBridges
	{
		internal static readonly FlavorBridge[] All =
		{
			new FlavorBridge("FletchWounds", "drives your arrows deeper into it"),
			new FlavorBridge("Stumblr", "can trip it")
		};

		/// <summary>The tail of the <c>ds flavor</c> menu line, ready to print.</summary>
		internal static string FlavorSummary = "no supported mods installed";

		internal static void Resolve()
		{
			List<string> wired = new List<string>();
			for (int i = 0; i < All.Length; i++)
			{
				All[i].Resolve();
				if (All[i].Wired)
				{
					wired.Add(All[i].Label);
				}
			}

			FlavorSummary = wired.Count == 0
				? "no supported mods installed"
				: "enhanced mod interaction with " + string.Join(", ", wired.ToArray());
		}

		/// <summary>
		/// Called on every slam that caught a zombie, including one the never-kill floor spared.
		/// </summary>
		internal static void TryProc(World _world, EntityAlive _zombie)
		{
			if (!Settings.Flavor)
			{
				return;
			}

			// updateOpenCloseState carries no player argument, but the whole door-activation path
			// runs on the machine of the player who pressed the key (see ServerAuthoritative), so
			// the primary player is the one who closed it. On a dedicated server this is null and
			// the procs are skipped.
			EntityPlayerLocal player = _world.GetPrimaryPlayer();
			if (player == null || player.IsDead())
			{
				return;
			}

			for (int i = 0; i < All.Length; i++)
			{
				All[i].TryProc(_zombie, player);
			}
		}

		/// <summary>Mirror this mod's flavor setting onto every bridged mod.</summary>
		internal static void PushFlavor(bool _on)
		{
			for (int i = 0; i < All.Length; i++)
			{
				All[i].PushFlavor(_on);
			}
		}

		/// <summary>The line <c>ds flavor</c> prints after toggling.</summary>
		internal static string Describe()
		{
			List<string> linked = new List<string>();
			List<string> unlinked = new List<string>();
			List<string> unbound = new List<string>();
			StringBuilder effects = new StringBuilder();

			for (int i = 0; i < All.Length; i++)
			{
				FlavorBridge bridge = All[i];
				if (bridge.Wired)
				{
					(bridge.Linked ? linked : unlinked).Add(bridge.Label);
					effects.Append(effects.Length == 0 ? string.Empty : " and ").Append(bridge.Effect);
				}
				else if (bridge.Found)
				{
					unbound.Add(bridge.Label);
				}
			}

			string here = linked.Count == 0
				? string.Empty
				: " here and in " + string.Join(", ", linked.ToArray());

			if (!Settings.Flavor)
			{
				return "Flavor OFF" + here + " - a slam does its own damage and nothing else.";
			}

			if (effects.Length == 0)
			{
				// Installed-but-unbound is a version mismatch somebody can act on; absent is not.
				return unbound.Count > 0
					? "Flavor ON - but " + string.Join(", ", unbound.ToArray())
						+ " could not be bound, so nothing changes. See 'ds info'."
					: "Flavor ON - but no mod that hooks into it is installed, so nothing changes.";
			}

			string text = "Flavor ON" + here + " - a slam that catches a zombie now " + effects + ".";
			if (unlinked.Count > 0)
			{
				text += " " + string.Join(", ", unlinked.ToArray()) + " needs its own flavor switch on too.";
			}
			return text;
		}

		internal static void ResetCounters()
		{
			for (int i = 0; i < All.Length; i++)
			{
				All[i].Procs = 0;
			}
		}
	}
}
