namespace DoorSlammer
{
	/// <summary>
	/// Live counters behind the <c>ds</c> command. The startup log only proves the patches were
	/// installed; these are what prove they are being reached, and which gate a slam died on.
	///
	/// No locking: all writes happen on the main thread, from the door-activation path.
	/// </summary>
	internal static class Counters
	{
		/// <summary>Door closes that passed every gate, including the per-door cooldown.</summary>
		internal static int ClosesChecked;

		/// <summary>Of those, the ones that actually caught a zombie in the doorway.</summary>
		internal static int Slams;

		internal static int ZombiesHit;

		/// <summary>Zombies caught but at or below the never-kill floor, so left alone.</summary>
		internal static int ZombiesSpared;

		internal static int DoorsDamaged;

		/// <summary>Doors left alone: at or below the floor, or inside a trader area.</summary>
		internal static int DoorsSpared;

		/// <summary>Slam hits whose damage response skipped the rage roll.</summary>
		internal static int RageSuppressed;

		/// <summary>Slams that drove an arrow deeper through Fletch Wounds. At most one per slam,
		/// and always zero without that mod installed.</summary>
		internal static int ArrowProcs;

		internal static void Reset()
		{
			ClosesChecked = 0;
			Slams = 0;
			ZombiesHit = 0;
			ZombiesSpared = 0;
			DoorsDamaged = 0;
			DoorsSpared = 0;
			RageSuppressed = 0;
			ArrowProcs = 0;
		}
	}
}
