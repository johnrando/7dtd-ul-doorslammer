namespace DoorSlammer
{
	/// <summary>
	/// The mods this one links up with, in the order a slam hands a zombie to them. FletchWounds
	/// first, because its arrow proc can kill and the others check for a corpse; WhackLash before
	/// Stumblr, so a knockdown the player earned by working the zombie over takes precedence and
	/// Stumblr's trip then declines a zombie already down. Adding a mod is one line here plus its
	/// own <c>FlavorInterop</c>.
	///
	/// A slam reaches a partner only when this side's switch for it is on; the partner checks its
	/// own switch for us inside its <c>TryProc</c>. Toggling a pair here mirrors to that partner
	/// alone, through <see cref="FlavorPartner.PushFlavor"/>, and a mirrored toggle arriving
	/// through <see cref="FlavorInterop.SetFlavor"/> is never pushed on, so nothing loops.
	/// </summary>
	internal static class FlavorPartners
	{
		/// <summary>This mod's console command, for the menu and the log.</summary>
		internal const string Command = "ds";

		internal static readonly FlavorPartner[] All =
		{
			new FlavorPartner("FletchWounds", "fw", true,
				"a slam drives your arrows deeper",
				"a slam that catches a zombie with one of your arrows in it drives that arrow deeper"),
			new FlavorPartner("WhackLash", "wl", true,
				"a slam can floor a zombie you have been working on",
				"a slam that catches a zombie whose WhackLash focus meter is up can knock it down"),
			new FlavorPartner("Stumblr", "sb", true,
				"a slam can trip the zombie",
				"a slam that catches a zombie can trip it")
		};

		/// <summary>The partner a token names, by label or alias, or null.</summary>
		internal static FlavorPartner Find(string _token)
		{
			for (int i = 0; i < All.Length; i++)
			{
				if (All[i].Matches(_token))
				{
					return All[i];
				}
			}
			return null;
		}

		/// <summary>The short name a label is shown under: the alias for a known partner.</summary>
		internal static string AliasOf(string _label)
		{
			FlavorPartner partner = Find(_label);
			return partner != null ? partner.Alias : _label.ToLowerInvariant();
		}

		internal static void Resolve()
		{
			for (int i = 0; i < All.Length; i++)
			{
				All[i].Resolve();
			}
		}

		/// <summary>
		/// Called on every slam that caught a zombie, including one the never-kill floor spared.
		/// </summary>
		internal static void TryProc(World _world, EntityAlive _zombie)
		{
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
				if (FlavorSwitches.IsOn(All[i].Label))
				{
					All[i].TryProc(_zombie, player);
				}
			}
		}

		/// <summary>Mirror this side's switch for one partner onto that partner.</summary>
		internal static void Push(string _label, bool _on)
		{
			FlavorPartner partner = Find(_label);
			if (partner != null)
			{
				partner.PushFlavor(_on);
			}
		}

		internal static void PushAll(bool _on)
		{
			for (int i = 0; i < All.Length; i++)
			{
				All[i].PushFlavor(_on);
			}
		}

		/// <summary>The line the flavor command prints after toggling one pair.</summary>
		internal static string Describe(string _label)
		{
			bool on = FlavorSwitches.IsOn(_label);
			FlavorPartner partner = Find(_label);
			string head = "Flavor with " + _label + ": " + (on ? "ON" : "OFF");

			if (partner == null)
			{
				return head + " here. Nothing to link on this side; " + _label
					+ " keeps its own switch for " + FlavorSwitches.Self + ".";
			}

			string where = partner.Linked ? " on both sides" : " here";
			if (!on)
			{
				return head + where + " - " + (partner.Linked
					? "neither side takes part."
					: "this side takes no part; set " + _label + "'s own switch too if you want it fully off.");
			}

			if (!partner.Found)
			{
				return head + " - but " + _label + " is not installed, so nothing changes.";
			}

			if (partner.Active && !partner.Wired)
			{
				// Installed-but-unbound is a version mismatch somebody can act on.
				return head + " - but " + _label + " could not be bound, so nothing changes. See '"
					+ Command + " info'.";
			}

			string text = head + where + " - " + partner.Effect + ".";
			if (!partner.Linked)
			{
				text += " " + _label + " needs its own flavor switch for " + FlavorSwitches.Self + " on too.";
			}
			return text;
		}

		/// <summary>The note after the choices on a menu line.</summary>
		internal static string MenuNote(string _label)
		{
			FlavorPartner partner = Find(_label);
			if (partner == null)
			{
				return _label + ": another mod's interaction";
			}
			return partner.Label + ": " + partner.Menu + (partner.Found ? string.Empty : " (not installed)");
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
