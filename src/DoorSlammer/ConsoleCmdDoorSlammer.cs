using System.Collections.Generic;
using System.Globalization;

namespace DoorSlammer
{
	/// <summary>
	/// <c>ds</c> (or <c>doorslammer</c>) - prints the settings block and changes nothing.
	/// <c>ds on</c> and <c>ds off</c> are the master switch; <c>ds sound</c> cycles the slam sound;
	/// <c>ds rage</c> and <c>ds flavor</c> toggle; <c>ds dmg</c>, <c>ds floor</c> and
	/// <c>ds tuning</c> set the numbers. <c>ds info</c> prints the diagnostics and counters,
	/// <c>ds reset</c> zeroes them.
	///
	/// The bare command reports rather than acts, and that is the point of it: it is what a player
	/// types to find out where things stand, so it must never be the thing that changed the answer.
	/// Everything that changes something has to name the change.
	///
	/// The settings block doubles as the menu: every line names the command that changes it, and
	/// shows what that command left behind. The switches list their choices with the live one
	/// marked and say what they do, so the block is also the answer to "what can I set this to".
	///
	/// It is kept short on purpose - it is what you read while standing in front of a door.
	/// Everything that answers "is this thing working" lives in <c>ds info</c> instead: the startup
	/// log proves the patches were installed, but only a non-zero counter proves a door close is
	/// reaching them, and the breakdown is what tells you which gate a slam died on when it looks
	/// like nothing is happening.
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
				Output(SlamSound.Describe());
				return;

			case "rage":
				Settings.SuppressRage = !Settings.SuppressRage;
				Output(Settings.SuppressRage
					? "Rage suppression ON - slam damage no longer rolls for rage."
					: "Rage suppression OFF - slam damage can enrage zombies like any other damage.");
				return;

			case "flavor":
				Settings.Flavor = !Settings.Flavor;
				FletchWoundsBridge.PushFlavor(Settings.Flavor);
				Output(FletchWoundsBridge.Describe());
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

		/// <summary>
		/// The menu. <paramref name="_header"/> is what separates <c>ds on</c> from a bare
		/// <c>ds</c>: one has just changed something and the other has not.
		/// </summary>
		private static void OutputMenu(string _header)
		{
			Output(_header);
			Switch("ds on|off", EnabledChoices(), "damage a zombie caught in a slammed door");
			Line("ds dmg {z} {d}", DamageLine());
			Switch("ds sound", SoundChoices(), "play a material-relevant sound on slam");
			Switch("ds rage", RageChoices(), "suppress UL's chance to rage from slam damage");
			Switch("ds flavor", FlavorChoices(), FletchWoundsBridge.FlavorSummary);
			Line("ds floor {hp}", FloorLine());
			Line("ds tuning {cd} {dist}", TuningLine());
		}

		/// <summary>
		/// <c>ds on</c> / <c>ds off</c>. The header says whether anything actually moved, because
		/// typing the state you were already in is not an error and should not read like a change.
		/// </summary>
		private static void SetEnabled(bool _on)
		{
			bool changed = Settings.Enabled != _on;
			Settings.Enabled = _on;
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
			Line("Undead Legacy", UndeadLegacyInfo.Status);
			Line("door-close hook", Patches.DoorCloseHookStatus);
			Line("rage roll patch", Patches.RageSuppressionStatus);
			Line("FletchWounds", FletchWoundsBridge.Status);
			Line("last slam sound", SlamSound.LastPlayed);
			Line("door closes checked", Counters.ClosesChecked
				+ " (" + Counters.Slams + " caught a zombie)");
			Line("zombies", Counters.ZombiesHit + " hit, "
				+ Counters.ZombiesSpared + " spared by the floor");
			Line("doors", Counters.DoorsDamaged + " damaged, "
				+ Counters.DoorsSpared + " spared");
			Line("rage rolls suppressed", Counters.RageSuppressed.ToString());
			Line("arrows driven in", Counters.ArrowProcs.ToString());

			if (Counters.ClosesChecked == 0)
			{
				Output("Note: no door close has reached the hook yet. Closing any door by hand should");
				Output("move that number - if it stays at zero, the hook is not live.");
			}
		}

		/// <summary>
		/// One line of the block. Every label is padded to the width of the longest one -
		/// "ds tuning {cd} {dist}" - so the settings and the read-only lines share a column and
		/// <c>ds info</c> reads as one block rather than two.
		/// </summary>
		private static void Line(string _label, string _value)
		{
			Output("  " + _label.PadRight(22) + ": " + _value);
		}

		/// <summary>
		/// A line for a switch: the choices, then what the switch is for. The choice list is padded
		/// to the width of the longest one - "ds sound"'s three - so the notes line up in a column
		/// of their own instead of starting wherever the marked option happened to end.
		///
		/// A setter has no note, because its value already reads as one: "1 to zombie / 10 to door"
		/// says what "ds dmg" does. A switch shows "[ off | >impact< | break ]", which says what it
		/// can be set to and nothing at all about what setting it does.
		/// </summary>
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
			Output("Tuning: " + TuningLine());
		}

		/// <summary>A whole number of HP, zero or more. Zero is allowed: it disables that gate.</summary>
		private static bool TryCount(string _value, string _what, out int _parsed)
		{
			if (!int.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _parsed)
				|| _parsed < 0)
			{
				Output("'" + _value + "' is not a valid " + _what + " - whole numbers from 0 up.");
				_parsed = 0;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Seconds or metres, zero or more. Parsed against the invariant culture rather than the
		/// player's, so "0.35" means the same thing on a machine whose decimal separator is a comma.
		/// </summary>
		private static bool TryMeasure(string _value, string _what, out float _parsed)
		{
			// !(x >= 0f) rather than x < 0f, because NaN parses successfully and then fails every
			// comparison - a plain "less than zero" test would wave it through.
			if (!float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out _parsed)
				|| !(_parsed >= 0f) || float.IsInfinity(_parsed))
			{
				Output("'" + _value + "' is not a valid " + _what + " - numbers from 0 up, like 0.35.");
				_parsed = 0f;
				return false;
			}
			return true;
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

		/// <summary>
		/// The one menu line whose note names something outside this mod, so its note is not a
		/// fixed string but what the lookup found: an interaction is not worth toggling if there
		/// is nothing to interact with. See <see cref="FletchWoundsBridge.FlavorSummary"/>.
		/// </summary>
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
			return Number(Settings.CooldownSeconds) + " sec cooldown / "
				+ Number(Settings.SearchPadding) + " range";
		}

		/// <summary>Printed the same way it is parsed, so a reported value can be typed back in.</summary>
		private static string Number(float _value)
		{
			return _value.ToString(CultureInfo.InvariantCulture);
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
				+ "Impact by default.\r\n\r\n'ds rage' toggles "
				+ "suppression of the rage roll on slam damage. Undead Legacy rolls for rage on every "
				+ "damage response no matter how small, so over the many slams it takes to whittle a "
				+ "zombie the odds add up. Off by default. Without Undead Legacy installed this "
				+ "suppresses vanilla's own rage roll instead.\r\n\r\n'ds dmg {z} {d}' sets the HP a "
				+ "slam takes off the zombie and off the door: 1 and 10 by default.\r\n\r\n'ds floor "
				+ "{hp}' sets the never-kill floor, 10 by default. A slam takes at most the health "
				+ "the target has above it, which is what stops a slam killing a zombie or breaking "
				+ "a door open. It holds at any 'ds dmg': a 100 HP slam on a zombie with 50 left "
				+ "takes 40 and leaves it standing on the floor. 0 removes the protection entirely, "
				+ "and is the only setting at which a slam itself can kill.\r\n\r\n"
				+ "'ds flavor' toggles the extra behaviour supported mods offer, on by default. It "
				+ "does nothing unless one of them is installed. Currently that is FletchWounds: a "
				+ "slam that catches a zombie with one of your arrows still in it drives that arrow "
				+ "deeper, once per slam however many are in it. That is FletchWounds' arrow rather "
				+ "than the door's damage, so unlike a slam it is credited to you and can land a "
				+ "killing blow. Both mods carry this switch and toggling either one moves both."
				+ "\r\n\r\n"
				+ "'ds tuning {cd} {dist}' sets the seconds between "
				+ "two damaging slams of the same door, and how far past the frame a zombie still "
				+ "counts as standing in the doorway, in metres: 1 and 0.35 by default. The cooldown "
				+ "is what stops a held or macro'd activate key grinding out damage frame by frame."
				+ "\r\n\r\nThe three setters take effect immediately and last until the game is "
				+ "restarted; the defaults live in Settings.cs.\r\n\r\n'ds info' prints the same block with the "
				+ "patch state and the counters added. The key line is 'door closes checked': the startup log "
				+ "only proves the hook was installed, that number proves door closes are reaching "
				+ "it. If it climbs but 'caught a zombie' does not, the zombie is not being seen as "
				+ "inside the doorway.\r\n\r\n'ds reset' zeroes the counters "
				+ "so one scenario can be measured on its own.\r\n\r\n'doorslammer' is an alias for "
				+ "'ds'.";
		}
	}
}
