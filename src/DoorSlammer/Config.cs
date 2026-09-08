using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace DoorSlammer
{
	/// <summary>
	/// Reads <see cref="Settings"/> back at startup and writes it out again whenever a <c>ds</c>
	/// command changes something, so a tuned mod stays tuned across a restart.
	///
	/// The file lives in the game's own user data folder rather than in <c>Mods/DoorSlammer/</c>,
	/// which is the difference between settings that survive an update of the mod and settings that
	/// get overwritten by one. It is plain <c>key = value</c> text on purpose: no XML reference to
	/// add, nothing to get wrong in an editor, and every line names the console command that writes
	/// it, so the file reads like the menu it came from.
	///
	/// Nothing here can stop the mod working. A folder that will not resolve, a file that will not
	/// parse and a disk that will not take the write all degrade to a log line and the defaults,
	/// which is what <c>ds info</c> reports on its "settings file" line.
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
		/// Set for as long as <see cref="Load"/> is applying lines, so the setters it goes through
		/// do not write the file back out one line at a time while reading it.
		/// </summary>
		private static bool loading;

		/// <summary>
		/// Called once from <see cref="ModApi.InitMod"/>, before the patches go in - the rage patch
		/// logs which way its switch is set, and that should be the player's setting rather than
		/// the built-in one. A missing file is not an error: it is a first run, and writing the
		/// defaults out is what makes the file discoverable at all.
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
				loading = true;
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
			finally
			{
				loading = false;
			}
		}

		/// <summary>
		/// Called by every <c>ds</c> command that changes a setting, and by
		/// <see cref="FlavorInterop.SetFlavor"/> when the other mod moves the flavor switch. Writes
		/// the whole file rather than the one line that changed, which is what keeps the comments
		/// and the ordering intact.
		/// </summary>
		internal static void Save()
		{
			if (loading || !Resolve())
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

		/// <summary>
		/// Works out where the file goes, once. Failure here is the one case that leaves the mod
		/// with no persistence at all, so it says so plainly rather than retrying every command.
		/// </summary>
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

		/// <summary>The file, exactly as it is written every time.</summary>
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
			Setting(text, "flavor", OnOff(Settings.Flavor), "ds flavor");
			Setting(text, "floor", Settings.MinRemainingHp.ToString(), "ds floor {hp}");
			Setting(text, "cooldown", Number(Settings.CooldownSeconds), "ds tuning {cd} {dist}");
			Setting(text, "range", Number(Settings.SearchPadding), "ds tuning {cd} {dist}");
			return text.ToString();
		}

		/// <summary>One setting, padded so the values and the commands each share a column.</summary>
		private static void Setting(StringBuilder _text, string _key, string _value, string _command)
		{
			_text.AppendLine(_key.PadRight(14) + "= " + _value.PadRight(8) + " # " + _command);
		}

		private enum LineResult
		{
			/// <summary>Blank or a comment - not a setting, and not a complaint either.</summary>
			Skipped,

			Applied,

			Rejected
		}

		/// <summary>
		/// One line of the file. An unknown key is a warning rather than an error: it is what a
		/// file written by a newer version of the mod looks like to an older one, and dropping the
		/// line it does not understand is better than refusing the eight it does.
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
				return TryCount(_value, ref Settings.DamageToZombie);
			case "damage.door":
				return TryCount(_value, ref Settings.DamageToDoor);
			case "sound":
				return TrySound(_value);
			case "rage":
				return TryBool(_value, ref Settings.SuppressRage);
			case "flavor":
				return TryBool(_value, ref Settings.Flavor);
			case "floor":
				return TryCount(_value, ref Settings.MinRemainingHp);
			case "cooldown":
				return TryMeasure(_value, ref Settings.CooldownSeconds);
			case "range":
				return TryMeasure(_value, ref Settings.SearchPadding);
			default:
				return false;
			}
		}

		/// <summary>
		/// Accepts what the file itself writes, and the obvious synonyms a player would reach for
		/// editing it by hand.
		/// </summary>
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

		/// <summary>The same range the console command accepts: whole numbers of HP, zero or more.</summary>
		private static bool TryCount(string _value, ref int _target)
		{
			if (!int.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
				|| parsed < 0)
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		/// <summary>
		/// Seconds or metres, zero or more, read against the invariant culture so a file written on
		/// one machine means the same thing on a machine whose decimal separator is a comma.
		/// </summary>
		private static bool TryMeasure(string _value, ref float _target)
		{
			// !(x >= 0f) rather than x < 0f, because NaN parses successfully and then fails every
			// comparison - a plain "less than zero" test would wave it through.
			if (!float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
				|| !(parsed >= 0f) || float.IsInfinity(parsed))
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

		/// <summary>Written the way the console command parses it, so the two agree.</summary>
		private static string Number(float _value)
		{
			return _value.ToString(CultureInfo.InvariantCulture);
		}
	}
}
