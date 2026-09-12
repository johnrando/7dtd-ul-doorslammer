namespace DoorSlammer
{
	/// <summary>
	/// Live counters behind <c>ds info</c>: the startup log proves the patches were installed,
	/// these prove they are being reached and which gate a slam died on. No locking: all writes
	/// happen on the main thread.
	/// </summary>
	internal static class Counters
	{
		/// <summary>Door closes that passed every gate, including the per-door cooldown.</summary>
		internal static int ClosesChecked;

		/// <summary>Of those, the ones that caught a zombie in the doorway.</summary>
		internal static int Slams;

		internal static int ZombiesHit;

		/// <summary>Zombies caught but at or below the never-kill floor.</summary>
		internal static int ZombiesSpared;

		internal static int DoorsDamaged;

		/// <summary>Doors at or below the floor, or inside a trader area.</summary>
		internal static int DoorsSpared;

		internal static int RageSuppressed;

		/// <summary>Slams that drove an arrow deeper through FletchWounds. At most one per slam.</summary>
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
