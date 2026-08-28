using System.Collections.Generic;
using System.Globalization;

namespace DoorSlammer
{
	/// <summary>
	/// <c>ds</c> (or <c>doorslammer</c>) - toggles the mod and prints the settings block.
	/// <c>ds sound</c> cycles the slam sound and <c>ds rage</c> toggles rage suppression;
	/// <c>ds dmg</c>, <c>ds floor</c> and <c>ds tuning</c> set the numbers. <c>ds info</c> prints
	/// the diagnostics and counters, <c>ds reset</c> zeroes them.
	///
	/// The settings block doubles as the menu: every line names the command that changes it, and
	/// shows what that command left behind. The two toggles list their choices with the live one
	/// marked, so the block is also the answer to "what can I set this to".
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
					+ "'. Try: ds [sound|rage|dmg|floor|tuning|info|reset]");
				return;
			}
		}

		/// <summary>
		/// The menu. <paramref name="_header"/> differs because <c>ds</c> has just changed
		/// something and <c>ds info</c> has not.
		/// </summary>
		private static void OutputMenu(string _header)
		{
			Output(_header);
			Line("ds sound", SoundChoices());
			Line("ds rage", RageChoices());
			Line("ds dmg {z} {d}", DamageLine());
			Line("ds floor {hp}", FloorLine());
			Line("ds tuning {cd} {dist}", TuningLine());
		}

		private static void OutputStatus()
		{
			OutputMenu("Door Slammer is now " + (Settings.Enabled ? "ON" : "OFF"));
		}

		/// <summary>The menu, with the read-only lines appended in the same column.</summary>
		private static void OutputInfo()
		{
			OutputMenu("Door Slammer is " + (Settings.Enabled ? "ON" : "OFF"));
			Line("Undead Legacy", UndeadLegacyInfo.Status);
			Line("door-close hook", Patches.DoorCloseHookStatus);
			Line("rage roll patch", Patches.RageSuppressionStatus);
			Line("last slam sound", SlamSound.LastPlayed);
			Line("door closes checked", Counters.ClosesChecked
				+ " (" + Counters.Slams + " caught a zombie)");
			Line("zombies", Counters.ZombiesHit + " hit, "
				+ Counters.ZombiesSpared + " spared by the floor");
			Line("doors", Counters.DoorsDamaged + " damaged, "
				+ Counters.DoorsSpared + " spared");
			Line("rage rolls suppressed", Counters.RageSuppressed.ToString());

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

		private static string DamageLine()
		{
			return Settings.DamageToZombie + " to zombie / " + Settings.DamageToDoor + " to door";
		}

		private static string FloorLine()
		{
			return "will not cause damage below " + Settings.MinRemainingHp + " health";
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
			return "Toggles the Door Slammer mod and reports its status.";
		}

		public override string getHelp()
		{
			return "Usage: ds [sound|rage|dmg {z} {d}|floor {hp}|tuning {cd} {dist}|info|reset]"
				+ "\r\n\r\nClosing a door on a zombie takes a little health off "
				+ "both the zombie and the door. It catches at most one zombie - the nearest one "
				+ "standing in the doorway - and deliberately does nothing else: no knockdown, no "
				+ "stun, no knockback, no hit reaction, and the damage is credited to nobody, so it "
				+ "never sets the zombie on you.\r\n\r\nA slam never lands a killing blow. Anything at "
				+ "or below the never-kill floor is left alone, so slamming whittles a zombie down "
				+ "and then stops, and can wear a door down but never break it open.\r\n\r\n'ds' on "
				+ "its own toggles the mod and prints the settings. Each line names the command that "
				+ "changes it, so the settings block is the menu.\r\n\r\n'ds sound' cycles the "
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
				+ "{hp}' sets the never-kill floor, 10 by default. A target at or below it is left "
				+ "alone, which is what stops a slam killing a zombie or breaking a door open. 0 "
				+ "removes that protection.\r\n\r\n'ds tuning {cd} {dist}' sets the seconds between "
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
