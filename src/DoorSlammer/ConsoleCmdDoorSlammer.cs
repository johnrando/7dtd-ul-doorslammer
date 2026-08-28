using System.Collections.Generic;

namespace DoorSlammer
{
	/// <summary>
	/// <c>ds</c> (or <c>doorslammer</c>) - toggles the mod and prints the settings block.
	/// <c>ds sound</c> cycles the slam sound, <c>ds rage</c> toggles rage suppression,
	/// <c>ds info</c> prints the diagnostics and counters, <c>ds reset</c> zeroes the counters.
	///
	/// Everything is a toggle rather than an explicit on/off, so the state has to be reported back:
	/// each toggle prints what it left behind.
	///
	/// The settings block is kept short on purpose - it is what you read while standing in front of
	/// a door. Everything that answers "is this thing working" lives in <c>ds info</c> instead: the
	/// startup log proves the patches were installed, but only a non-zero counter proves a door
	/// close is reaching them, and the breakdown is what tells you which gate a slam died on when it
	/// looks like nothing is happening.
	/// </summary>
	public class ConsoleCmdDoorSlammer : ConsoleCmdAbstract
	{
		public override bool IsExecuteOnClient => false;

		public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
		{
			string command = _params.Count > 0 ? _params[0].ToLower() : string.Empty;

			switch (command)
			{
			case "":
				Settings.Enabled = !Settings.Enabled;
				OutputStatus();
				return;

			case "sound":
				SlamSound.Cycle();
				Output(SlamSound.Describe());
				return;

			case "rage":
				Settings.SuppressRage = !Settings.SuppressRage;
				Output(Settings.SuppressRage
					? "Rage suppression ON - slam damage no longer rolls for rage."
					: "Rage suppression OFF - slam damage can enrage zombies like any other damage.");
				return;

			case "info":
				OutputInfo();
				return;

			case "reset":
				Counters.Reset();
				Output("Counters reset.");
				return;

			default:
				Output("Unknown option '" + _params[0] + "'. Try: ds [sound|rage|info|reset]");
				return;
			}
		}

		private static void OutputStatus()
		{
			Output("Door Slammer is now " + (Settings.Enabled ? "ON" : "OFF"));
			Output("  sound        : " + SlamSound.Status());
			Output("  prevent rage : " + (Settings.SuppressRage ? "on" : "off"));
			Output("  slam damage  : " + Settings.DamageToZombie + " to zombie / "
				+ Settings.DamageToDoor + " to door");
			Output("  HP floor     : " + Settings.MinRemainingHp);
			Output("  tuning       : " + Settings.CooldownSeconds + "s cooldown, "
				+ Settings.SearchPadding + "m distance");
		}

		private static void OutputInfo()
		{
			Output("Door Slammer info");
			Output("  Undead Legacy          : " + UndeadLegacyInfo.Status);
			Output("  door-close hook        : " + Patches.DoorCloseHookStatus);
			Output("  rage suppression patch : " + Patches.RageSuppressionStatus);
			Output("  last slam sound        : " + SlamSound.LastPlayed);
			Output("  door closes checked    : " + Counters.ClosesChecked
				+ " (" + Counters.Slams + " caught a zombie)");
			Output("  zombies                : " + Counters.ZombiesHit + " hit, "
				+ Counters.ZombiesSpared + " spared by the floor");
			Output("  doors                  : " + Counters.DoorsDamaged + " damaged, "
				+ Counters.DoorsSpared + " spared");
			Output("  rage rolls suppressed  : " + Counters.RageSuppressed);

			if (Counters.ClosesChecked == 0)
			{
				Output("Note: no door close has reached the hook yet. Closing any door by hand should");
				Output("move that number - if it stays at zero, the hook is not live.");
			}
		}

		private static void Output(string _line)
		{
			SdtdConsole.Instance.Output(_line);
		}

		public override string[] getCommands()
		{
			return new string[2] { "ds", "doorslammer" };
		}

		public override string getDescription()
		{
			return "Toggles the Door Slammer mod and reports its status.";
		}

		public override string getHelp()
		{
			return "Usage: ds [sound|rage|info|reset]\r\n\r\nClosing a door on a zombie takes a little health off "
				+ "both the zombie and the door. It catches at most one zombie - the nearest one "
				+ "standing in the doorway - and deliberately does nothing else: no knockdown, no "
				+ "stun, no knockback, no hit reaction, and the damage is credited to nobody, so it "
				+ "never sets the zombie on you.\r\n\r\nA slam never lands a killing blow. Anything at "
				+ "or below the never-kill floor is left alone, so slamming whittles a zombie down "
				+ "and then stops, and can wear a door down but never break it open.\r\n\r\n'ds' on "
				+ "its own toggles the mod and prints the settings.\r\n\r\n'ds sound' cycles the "
				+ "sound a damaging slam plays on the door: impact (what a zombie's fist sounds "
				+ "like on that door), break (what it sounds like when it breaks), off. Both are "
				+ "the game's own sounds, picked automatically from the door's material, so every "
				+ "door type sounds like itself, and neither makes any noise the AI can hear. "
				+ "Impact by default.\r\n\r\n'ds rage' toggles "
				+ "suppression of the rage roll on slam damage. Undead Legacy rolls for rage on every "
				+ "damage response no matter how small, so over the many slams it takes to whittle a "
				+ "zombie the odds add up. Off by default. Without Undead Legacy installed this "
				+ "suppresses vanilla's own rage roll instead.\r\n\r\n'ds info' prints the patch "
				+ "state and the counters. The key line is 'door closes checked': the startup log "
				+ "only proves the hook was installed, that number proves door closes are reaching "
				+ "it. If it climbs but 'caught a zombie' does not, the zombie is not being seen as "
				+ "inside the doorway.\r\n\r\n'ds reset' zeroes the counters "
				+ "so one scenario can be measured on its own.\r\n\r\n'doorslammer' is an alias for "
				+ "'ds'.";
		}
	}
}
