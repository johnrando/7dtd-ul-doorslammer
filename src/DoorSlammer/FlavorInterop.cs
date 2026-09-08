namespace DoorSlammer
{
	/// <summary>
	/// The one thing this mod exposes to another, and it exists only so the <c>flavor</c> switch
	/// does not have to be set twice.
	///
	/// PUBLISHED CONTRACT. FletchWounds binds <see cref="SetFlavor"/> by reflection, exactly as
	/// <see cref="FletchWoundsBridge"/> binds its <c>TryProc</c> - neither mod can reference the
	/// other, so this signature is the whole interface. Changing it does not break the build; it
	/// silently unlinks the two switches, leaving them independent rather than broken.
	///
	/// Kept apart from <see cref="FletchWoundsBridge"/> deliberately. That class is this mod
	/// reaching out; this one is the small surface it offers back, and the two are worth being able
	/// to tell apart at a glance.
	/// </summary>
	public static class FlavorInterop
	{
		/// <summary>
		/// Called when the player toggles the flavor switch in a supported mod, so both move
		/// together. Deliberately does not push back: whoever the player actually typed at owns the
		/// propagation, which is what keeps the two from calling each other forever.
		///
		/// It does save, though. The switch the player moved was a real change to this mod's
		/// settings whichever command they typed at, so it has to survive a restart on both sides.
		/// </summary>
		public static void SetFlavor(bool _on)
		{
			Settings.Flavor = _on;
			Config.Save();
		}
	}
}
