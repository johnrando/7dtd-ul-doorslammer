using System.Collections.Generic;

namespace DoorSlammer
{
	/// <summary>
	/// <c>ds</c> (or <c>doorslammer</c>). The bare command prints the settings block and changes
	/// nothing; every line of the block names the command that changes it, so it doubles as the
	/// menu. <c>ds info</c> adds the diagnostics and counters that answer "is this thing working".
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
				OutputMenu("DoorSlammer is " + OnOff(Settings.Enabled));
				return;

			case "on":
			case "off":
				SetEnabled(command == "on");
				return;

			case "sound":
				SlamSound.Cycle();
				Config.Save();
				Output(SlamSound.Describe());
				return;

			case "rage":
				Settings.SuppressRage = !Settings.SuppressRage;
				Config.Save();
				Output(Settings.SuppressRage
					? "Rage suppression ON - slam damage no longer rolls for rage."
					: "Rage suppression OFF - slam damage can enrage zombies like any other damage.");
				return;

			case "flavor":
				Settings.Flavor = !Settings.Flavor;
				Config.Save();
				FlavorBridges.PushFlavor(Settings.Flavor);
				Output(FlavorBridges.Describe());
				return;

			case "dmg":
				SetDamage(_params);
				return;

			case "floor":
				SetFloor(_params);
				return;

			case "tuning":
				SetTuning(_params);
				return;

			case "info":
				OutputInfo();
				return;

			case "reset":
				Counters.Reset();
				Output("Counters reset.");
				return;

			default:
				Output("Unknown option '" + _params[0]
					+ "'. Try: ds [on|off|sound|rage|flavor|dmg|floor|tuning|info|reset]");
				return;
			}
		}

		private static void OutputMenu(string _header)
		{
			Output(_header);
			Switch("ds on|off", EnabledChoices(), "damage a zombie caught in a slammed door");
			Line("ds dmg {z} {d}", DamageLine());
			Switch("ds sound", SoundChoices(), "play a material-relevant sound on slam");
			Switch("ds rage", RageChoices(), "suppress UL's chance to rage from slam damage");
			Switch("ds flavor", FlavorChoices(), FlavorBridges.FlavorSummary);
			Line("ds floor {hp}", FloorLine());
			Line("ds tuning {cd} {dist}", TuningLine());
		}

		/// <summary>The header says whether anything moved: typing the state you were already in
		/// should not read like a change.</summary>
		private static void SetEnabled(bool _on)
		{
			bool changed = Settings.Enabled != _on;
			Settings.Enabled = _on;
			if (changed)
			{
				Config.Save();
			}
			OutputMenu("DoorSlammer is " + (changed ? "now " : "already ") + OnOff(_on));
		}

		private static string OnOff(bool _on)
		{
			return _on ? "ON" : "OFF";
		}

		/// <summary>The menu, with the read-only lines appended in the same column.</summary>
		private static void OutputInfo()
		{
			OutputMenu("DoorSlammer is " + OnOff(Settings.Enabled));
			Line("settings file", Config.Status);
			Line("Undead Legacy", UndeadLegacyInfo.Status);
			Line("door-close hook", Patches.DoorCloseHookStatus);
			Line("rage roll patch", Patches.RageSuppressionStatus);
			for (int i = 0; i < FlavorBridges.All.Length; i++)
			{
				Line(FlavorBridges.All[i].Label, FlavorBridges.All[i].Status);
			}
			Line("last slam sound", SlamSound.LastPlayed);
			Line("door closes checked", Counters.ClosesChecked
				+ " (" + Counters.Slams + " caught a zombie)");
			Line("zombies", Counters.ZombiesHit + " hit, "
				+ Counters.ZombiesSpared + " spared by the floor");
			Line("doors", Counters.DoorsDamaged + " damaged, "
				+ Counters.DoorsSpared + " spared");
			Line("rage rolls suppressed", Counters.RageSuppressed.ToString());
			for (int i = 0; i < FlavorBridges.All.Length; i++)
			{
				Line("via " + FlavorBridges.All[i].Label, FlavorBridges.All[i].Procs + " slams acted on");
			}

			if (Counters.ClosesChecked == 0)
			{
				Output("Note: no door close has reached the hook yet. Closing any door by hand should");
				Output("move that number - if it stays at zero, the hook is not live.");
			}
		}

		/// <summary>Labels padded to the longest one ("ds tuning {cd} {dist}") so the block shares a column.</summary>
		private static void Line(string _label, string _value)
		{
			Output("  " + _label.PadRight(22) + ": " + _value);
		}

		/// <summary>A switch line: the choices padded to the widest set, then what the switch is for.</summary>
		private static void Switch(string _label, string _choices, string _note)
		{
			Line(_label, _choices.PadRight(26) + " - " + _note);
		}

		private static void SetDamage(List<string> _params)
		{
			if (_params.Count != 3)
			{
				Output("Usage: ds dmg {z} {d} - currently: " + DamageLine());
				return;
			}

			if (!TryCount(_params[1], "zombie damage", out int zombie)
				|| !TryCount(_params[2], "door damage", out int door))
			{
				return;
			}

			Settings.DamageToZombie = zombie;
			Settings.DamageToDoor = door;
			Config.Save();
			Output("Slam damage: " + DamageLine());
		}

		private static void SetFloor(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: ds floor {hp} - currently: " + FloorLine());
				return;
			}

			if (!TryCount(_params[1], "health floor", out int floor))
			{
				return;
			}

			Settings.MinRemainingHp = floor;
			Config.Save();
			Output("Health floor: " + FloorLine());
		}

		private static void SetTuning(List<string> _params)
		{
			if (_params.Count != 3)
			{
				Output("Usage: ds tuning {cd} {dist} - currently: " + TuningLine());
				return;
			}

			if (!TryMeasure(_params[1], "cooldown", out float cooldown)
				|| !TryMeasure(_params[2], "range", out float range))
			{
				return;
			}

			Settings.CooldownSeconds = cooldown;
			Settings.SearchPadding = range;
			Config.Save();
			Output("Tuning: " + TuningLine());
		}

		/// <summary>Zero is allowed: it disables that gate.</summary>
		private static bool TryCount(string _value, string _what, out int _parsed)
		{
			if (Config.TryCount(_value, out _parsed))
			{
				return true;
			}
			Output("'" + _value + "' is not a valid " + _what + " - whole numbers from 0 up.");
			return false;
		}

		private static bool TryMeasure(string _value, string _what, out float _parsed)
		{
			if (Config.TryMeasure(_value, out _parsed))
			{
				return true;
			}
			Output("'" + _value + "' is not a valid " + _what + " - numbers from 0 up, like 0.35.");
			return false;
		}

		/// <summary>The choice list for a cycle or toggle, with the live value marked.</summary>
		private static string Choices(params string[] _options)
		{
			return "[ " + string.Join(" | ", _options) + " ]";
		}

		private static string Mark(string _option, bool _live)
		{
			return _live ? ">" + _option + "<" : _option;
		}

		private static string EnabledChoices()
		{
			return Choices(Mark("on", Settings.Enabled), Mark("off", !Settings.Enabled));
		}

		private static string SoundChoices()
		{
			return Choices(
				Mark("off", Settings.SoundMode == SlamSoundMode.Off),
				Mark("impact", Settings.SoundMode == SlamSoundMode.Impact),
				Mark("break", Settings.SoundMode == SlamSoundMode.Break));
		}

		private static string RageChoices()
		{
			return Choices(Mark("off", !Settings.SuppressRage), Mark("on", Settings.SuppressRage));
		}

		private static string FlavorChoices()
		{
			return Choices(Mark("on", Settings.Flavor), Mark("off", !Settings.Flavor));
		}

		private static string DamageLine()
		{
			return Settings.DamageToZombie + " to zombie / " + Settings.DamageToDoor + " to door";
		}

		private static string FloorLine()
		{
			return "enemies below " + Settings.MinRemainingHp
				+ " health will not be affected by slam damage";
		}

		private static string TuningLine()
		{
			return Config.Number(Settings.CooldownSeconds) + " sec cooldown / "
				+ Config.Number(Settings.SearchPadding) + " range";
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
			return "Reports DoorSlammer's settings; 'ds on' and 'ds off' switch it.";
		}

		public override string getHelp()
		{
			return "Usage: ds [on|off|sound|rage|flavor|dmg {z} {d}|floor {hp}|tuning {cd} {dist}"
				+ "|info|reset]"
				+ "\r\n\r\nClosing a door on a zombie takes a little health off "
				+ "both the zombie and the door. It catches at most one zombie - the nearest one "
				+ "standing in the doorway - and deliberately does nothing else: no knockdown, no "
				+ "stun, no knockback, no hit reaction, and the damage is credited to nobody, so it "
				+ "never sets the zombie on you.\r\n\r\nA slam cannot land a killing blow. Each hit "
				+ "is capped to whatever health the target has above the never-kill floor, so a "
				+ "target at or below the floor is left alone and one above it lands exactly on the "
				+ "floor rather than through it. Slamming therefore whittles a zombie down and then "
				+ "stops, and wears a door down without breaking it open, at any 'ds dmg'."
				+ "\r\n\r\n'ds' on "
				+ "its own prints the settings and changes nothing - it is the status read, so it is "
				+ "safe to type when you only want to look. Each line names the command that changes "
				+ "it, so the settings block is also the menu, and each switch says what it is for."
				+ "\r\n\r\n'ds on' and 'ds off' are the master switch. With it off the door-close "
				+ "hook returns immediately, so a slam does nothing at all and every other setting "
				+ "here is inert. Saying which state you want rather than toggling it means the "
				+ "command reads the same whichever state you were in, and repeating it is harmless."
				+ "\r\n\r\n'ds sound' cycles the "
				+ "sound a damaging slam plays on the door: impact (what a zombie's fist sounds "
				+ "like on that door), break (what it sounds like when it breaks), off. Both are "
				+ "the game's own sounds, picked automatically from the door's material, so every "
				+ "door type sounds like itself, and neither makes any noise the AI can hear. "
				+ "Break by default.\r\n\r\n'ds rage' toggles "
				+ "suppression of the rage roll on slam damage. Undead Legacy rolls for rage on every "
				+ "damage response no matter how small, so over the many slams it takes to whittle a "
				+ "zombie the odds add up. Off by default. Without Undead Legacy installed this "
				+ "suppresses vanilla's own rage roll instead.\r\n\r\n'ds dmg {z} {d}' sets the HP a "
				+ "slam takes off the zombie and off the door: 10 and 10 by default.\r\n\r\n'ds floor "
				+ "{hp}' sets the never-kill floor, 20 by default. A slam takes at most the health "
				+ "the target has above it, which is what stops a slam killing a zombie or breaking "
				+ "a door open. It holds at any 'ds dmg': a 100 HP slam on a zombie with 50 left "
				+ "takes 40 and leaves it standing on the floor. 0 removes the protection entirely, "
				+ "and is the only setting at which a slam itself can kill.\r\n\r\n"
				+ "'ds flavor' toggles the extra behaviour supported mods offer, on by default. It "
				+ "does nothing unless one of them is installed. Currently that is FletchWounds and "
				+ "Stumblr. FletchWounds: a slam that catches a zombie with one of your arrows still "
				+ "in it drives that arrow deeper, once per slam however many are in it. That is "
				+ "FletchWounds' arrow rather than the door's damage, so unlike a slam it is credited "
				+ "to you and can land a killing blow. Stumblr: a slam that catches a zombie can "
				+ "trip it, in one of the game's own stumble animations; 'sb door' sets the chance. "
				+ "Every linked mod carries this switch and toggling it in any one of them moves all "
				+ "of them."
				+ "\r\n\r\n"
				+ "'ds tuning {cd} {dist}' sets the seconds between "
				+ "two damaging slams of the same door, and how far past the frame a zombie still "
				+ "counts as standing in the doorway, in metres: 1 and 0.35 by default. The cooldown "
				+ "is what stops a held or macro'd activate key grinding out damage frame by frame."
				+ "\r\n\r\nEvery setting here takes effect immediately and is written straight to a "
				+ "settings file, so it survives a restart - and survives updating the mod, because "
				+ "the file lives in the game's user data folder next to Saves rather than in Mods. "
				+ "'ds info' prints its full path. It is plain 'key = value' text and can be edited "
				+ "by hand with the game closed; a line that will not parse is ignored rather than "
				+ "fatal, and deleting the file goes back to the built-in defaults in Settings.cs."
				+ "\r\n\r\n'ds info' prints the same block with the "
				+ "patch state and the counters added. The key line is 'door closes checked': the startup log "
				+ "only proves the hook was installed, that number proves door closes are reaching "
				+ "it. If it climbs but 'caught a zombie' does not, the zombie is not being seen as "
				+ "inside the doorway.\r\n\r\n'ds reset' zeroes the counters "
				+ "so one scenario can be measured on its own.\r\n\r\n'doorslammer' is an alias for "
				+ "'ds'.";
		}
	}
}
