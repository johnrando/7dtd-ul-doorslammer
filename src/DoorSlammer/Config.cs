using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace DoorSlammer
{
	/// <summary>
	/// Reads <see cref="Settings"/> back at startup and writes it out whenever a <c>ds</c> command
	/// changes something. The file lives in the game's user data folder rather than in the mod
	/// folder, so it survives a mod update. Plain <c>key = value</c> text; every line names the
	/// console command that writes it. Nothing here can stop the mod working: any failure degrades
	/// to a log line and the defaults.
	/// </summary>
	internal static class Config
	{
		private const string FolderName = "DoorSlammer";

		private const string FileName = "settings.txt";

		/// <summary>What the last load or save did, as reported by <c>ds info</c>.</summary>
		internal static string Status = "not loaded - mod init has not run";

		/// <summary>Where the file is, once resolved. Null means it never was.</summary>
		private static string filePath;

		/// <summary>
		/// Called once from <see cref="ModApi.InitMod"/>, before the patches go in, so the startup
		/// log reports the player's settings rather than the defaults. A missing file is a first
		/// run: writing the defaults out is what makes the file discoverable.
		/// </summary>
		internal static void Load()
		{
			if (!Resolve())
			{
				return;
			}

			if (!File.Exists(filePath))
			{
				Save();
				return;
			}

			try
			{
				int applied = 0;
				int rejected = 0;
				foreach (string line in File.ReadAllLines(filePath))
				{
					switch (Parse(line))
					{
					case LineResult.Applied:
						applied++;
						break;
					case LineResult.Rejected:
						rejected++;
						break;
					}
				}

				Status = applied + " settings loaded"
					+ (rejected > 0 ? ", " + rejected + " line(s) ignored" : "")
					+ " - " + filePath;
				Log.Out(Patches.LogPrefix + "Settings loaded from " + filePath + ".");
			}
			catch (Exception e)
			{
				Status = "NOT LOADED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not read " + filePath + ", so the built-in "
					+ "defaults are in force: " + e.Message);
			}
		}

		/// <summary>
		/// Writes the whole file, which is what keeps the comments and ordering intact. Called by
		/// every <c>ds</c> command that changes a setting and by <see cref="FlavorInterop.SetFlavor"/>.
		/// </summary>
		internal static void Save()
		{
			if (!Resolve())
			{
				return;
			}

			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				File.WriteAllText(filePath, Compose());
				Status = "saved - " + filePath;
			}
			catch (Exception e)
			{
				Status = "NOT SAVED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not write " + filePath + ", so this change "
					+ "will not survive a restart: " + e.Message);
			}
		}

		/// <summary>Works out where the file goes, once.</summary>
		private static bool Resolve()
		{
			if (filePath != null)
			{
				return true;
			}

			try
			{
				string dir = GameIO.GetUserGameDataDir();
				if (string.IsNullOrEmpty(dir))
				{
					Status = "unavailable - the game reported no user data folder";
					return false;
				}
				filePath = Path.Combine(Path.Combine(dir, FolderName), FileName);
				return true;
			}
			catch (Exception e)
			{
				Status = "unavailable - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not work out where to keep settings, so they "
					+ "will not persist: " + e.Message);
				return false;
			}
		}

		private static string Compose()
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine("# DoorSlammer settings.");
			text.AppendLine("#");
			text.AppendLine("# Read once when the game starts and rewritten whenever a 'ds' command changes");
			text.AppendLine("# something, so edit this with the game closed. Every line names the command that");
			text.AppendLine("# sets it; anything after a '#' is a comment, and a line that will not parse is");
			text.AppendLine("# ignored rather than fatal.");
			text.AppendLine();
			Setting(text, "enabled", OnOff(Settings.Enabled), "ds on|off");
			Setting(text, "damage.zombie", Settings.DamageToZombie.ToString(), "ds dmg {z} {d}");
			Setting(text, "damage.door", Settings.DamageToDoor.ToString(), "ds dmg {z} {d}");
			Setting(text, "sound", Settings.SoundMode.ToString().ToLowerInvariant(),
				"ds sound - off, impact or break");
			Setting(text, "rage", OnOff(Settings.SuppressRage), "ds rage");
			foreach (string label in FlavorSwitches.Labels)
			{
				Setting(text, "flavor." + label.ToLowerInvariant(), OnOff(FlavorSwitches.IsOn(label)),
					"ds flavor " + FlavorPartners.AliasOf(label));
			}
			Setting(text, "floor", Settings.MinRemainingHp.ToString(), "ds floor {hp}");
			Setting(text, "cooldown", Number(Settings.CooldownSeconds), "ds tuning {cd} {dist}");
			Setting(text, "range", Number(Settings.SearchPadding), "ds tuning {cd} {dist}");
			return text.ToString();
		}

		/// <summary>One setting, padded so the values and the commands each share a column.</summary>
		private static void Setting(StringBuilder _text, string _key, string _value, string _command)
		{
			_text.AppendLine(_key.PadRight(20) + "= " + _value.PadRight(8) + " # " + _command);
		}

		private enum LineResult
		{
			/// <summary>Blank or a comment.</summary>
			Skipped,

			Applied,

			Rejected
		}

		/// <summary>
		/// One line of the file. An unknown key is a warning rather than an error: that is what a
		/// file written by a newer version of the mod looks like to an older one.
		/// </summary>
		private static LineResult Parse(string _line)
		{
			int comment = _line.IndexOf('#');
			string text = (comment >= 0 ? _line.Substring(0, comment) : _line).Trim();
			if (text.Length == 0)
			{
				return LineResult.Skipped;
			}

			int split = text.IndexOf('=');
			if (split <= 0)
			{
				Log.Warning(Patches.LogPrefix + "Ignoring a settings line that is not 'key = value': "
					+ _line.Trim());
				return LineResult.Rejected;
			}

			string key = text.Substring(0, split).Trim().ToLowerInvariant();
			string value = text.Substring(split + 1).Trim();
			if (Apply(key, value))
			{
				return LineResult.Applied;
			}

			Log.Warning(Patches.LogPrefix + "Ignoring settings line '" + _line.Trim()
				+ "' - unknown setting or unusable value.");
			return LineResult.Rejected;
		}

		private static bool Apply(string _key, string _value)
		{
			switch (_key)
			{
			case "enabled":
				return TryBool(_value, ref Settings.Enabled);
			case "damage.zombie":
				return LoadCount(_value, ref Settings.DamageToZombie);
			case "damage.door":
				return LoadCount(_value, ref Settings.DamageToDoor);
			case "sound":
				return TrySound(_value);
			case "rage":
				return TryBool(_value, ref Settings.SuppressRage);
			case "flavor":
				// The single switch older builds wrote: apply it to every partner.
				return TryFlavor(null, _value);
			case "floor":
				return LoadCount(_value, ref Settings.MinRemainingHp);
			case "cooldown":
				return LoadMeasure(_value, ref Settings.CooldownSeconds);
			case "range":
				return LoadMeasure(_value, ref Settings.SearchPadding);
			default:
				// flavor.<mod>: one partner's switch. Any label is accepted, so a switch a mod
				// this build does not know about created is kept.
				return _key.StartsWith("flavor.") && _key.Length > 7
					&& TryFlavor(_key.Substring(7), _value);
			}
		}

		/// <summary>One partner's switch, or every partner's when the label is null.</summary>
		private static bool TryFlavor(string _label, string _value)
		{
			bool on = false;
			if (!TryBool(_value, ref on))
			{
				return false;
			}
			if (_label == null)
			{
				FlavorSwitches.SetAll(on);
			}
			else
			{
				FlavorSwitches.Set(_label, on);
			}
			return true;
		}

		/// <summary>Accepts what the file writes plus the obvious hand-edit synonyms.</summary>
		private static bool TryBool(string _value, ref bool _target)
		{
			switch (_value.ToLowerInvariant())
			{
			case "on":
			case "true":
			case "yes":
			case "1":
				_target = true;
				return true;
			case "off":
			case "false":
			case "no":
			case "0":
				_target = false;
				return true;
			default:
				return false;
			}
		}

		/// <summary>Whole numbers of HP, zero or more. Shared with the console command.</summary>
		internal static bool TryCount(string _value, out int _parsed)
		{
			return int.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _parsed)
				&& _parsed >= 0;
		}

		private static bool LoadCount(string _value, ref int _target)
		{
			if (!TryCount(_value, out int parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		/// <summary>
		/// Seconds or metres, zero or more, against the invariant culture so a file written on one
		/// machine means the same on one whose decimal separator is a comma. Shared with the console
		/// command. <c>!(x &gt;= 0)</c> rather than <c>x &lt; 0</c> so NaN is rejected too.
		/// </summary>
		internal static bool TryMeasure(string _value, out float _parsed)
		{
			return float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out _parsed)
				&& _parsed >= 0f && !float.IsInfinity(_parsed);
		}

		private static bool LoadMeasure(string _value, ref float _target)
		{
			if (!TryMeasure(_value, out float parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		private static bool TrySound(string _value)
		{
			switch (_value.ToLowerInvariant())
			{
			case "off":
				Settings.SoundMode = SlamSoundMode.Off;
				return true;
			case "impact":
				Settings.SoundMode = SlamSoundMode.Impact;
				return true;
			case "break":
				Settings.SoundMode = SlamSoundMode.Break;
				return true;
			default:
				return false;
			}
		}

		private static string OnOff(bool _on)
		{
			return _on ? "on" : "off";
		}

		/// <summary>Written the way it is parsed, so a reported value can be typed back in.</summary>
		internal static string Number(float _value)
		{
			return _value.ToString(CultureInfo.InvariantCulture);
		}
	}
}
