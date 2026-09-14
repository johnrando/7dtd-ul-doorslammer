namespace DoorSlammer
{
	/// <summary>
	/// Runtime knobs, all switchable from the <c>ds</c> console command. The values here are the
	/// built-in defaults; <see cref="Config"/> reads the player's file over them at startup.
	/// </summary>
	internal static class Settings
	{
		/// <summary>Master switch. When off, the door-close hook returns immediately.</summary>
		internal static bool Enabled = true;

		/// <summary>Suppress the rage roll for slam damage only. Off by default: staying out of
		/// UL's way is the safer default. See <see cref="RageSuppression"/>.</summary>
		internal static bool SuppressRage;

		/// <summary>Which of the game's own sounds a damaging slam plays. See <see cref="SlamSound"/>.</summary>
		internal static SlamSoundMode SoundMode = SlamSoundMode.Break;

		internal static int DamageToZombie = 10;

		internal static int DamageToDoor = 10;

		/// <summary>
		/// A slam never takes a target below this many HP. A cap on the hit, not a gate in front of
		/// it, so a slam can never kill a zombie or break a door open however large the damage is
		/// set. Applied to each target independently.
		/// </summary>
		internal static int MinRemainingHp = 20;

		/// <summary>Minimum seconds between two damaging slams of the same door.</summary>
		internal static float CooldownSeconds = 1f;

		/// <summary>How far outside the door's block column a zombie still counts as "in the
		/// doorway", in metres. Zombies press up against the frame rather than standing inside it.</summary>
		internal static float SearchPadding = 0.35f;

		// The per-partner flavor switches live in FlavorSwitches: one per mod this one links up
		// with, all on by default, and mirrored pairwise rather than as one shared value.
	}
}
