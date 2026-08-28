namespace DoorSlammer
{
	/// <summary>
	/// Runtime knobs, all switchable from the <c>ds</c> console command. Deliberately plain
	/// statics rather than a config file: everything here is cheap to re-tune mid-session, and
	/// the interesting ones are toggles you want to flip while standing in front of a door.
	/// </summary>
	internal static class Settings
	{
		/// <summary>Master switch. When off, the door-close hook returns immediately.</summary>
		internal static bool Enabled = true;

		/// <summary>
		/// Suppress Undead Legacy's rage roll for slam damage only. Off by default - the roll is
		/// UL's own behaviour and staying out of its way is the safer default. See
		/// <see cref="RageSuppression"/> for what turning this on actually does.
		/// </summary>
		internal static bool SuppressRage;

		/// <summary>
		/// Which of the game's own sounds a damaging slam plays on the door. Cycled with
		/// <c>ds sound</c>; see <see cref="SlamSound"/> for where each name comes from.
		/// </summary>
		internal static SlamSoundMode SoundMode = SlamSoundMode.Impact;

		/// <summary>HP the slam takes off the zombie.</summary>
		internal static int DamageToZombie = 1;

		/// <summary>HP the slam takes off the door.</summary>
		internal static int DamageToDoor = 10;

		/// <summary>
		/// A slam never damages a target at or below this many HP remaining, so it can never land
		/// a killing blow on a zombie or break a door open. Applied to each target independently.
		/// </summary>
		internal static int MinRemainingHp = 10;

		/// <summary>
		/// Minimum seconds between two damaging slams of the same door. Stops a held or macro'd
		/// activate key from turning into a damage-per-frame grinder.
		/// </summary>
		internal static float CooldownSeconds = 1f;

		/// <summary>
		/// How far outside the door's own block column a zombie still counts as "in the doorway",
		/// in metres. Zombies press right up against the frame rather than standing inside it.
		/// </summary>
		internal static float SearchPadding = 0.35f;
	}
}
