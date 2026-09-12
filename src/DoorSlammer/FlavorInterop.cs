namespace DoorSlammer
{
	/// <summary>
	/// PUBLISHED CONTRACT. FletchWounds binds <see cref="SetFlavor"/> by reflection, the way
	/// <see cref="FletchWoundsBridge"/> binds its <c>TryProc</c>, so this signature is the whole
	/// interface. Changing it does not break the build; it silently unlinks the two switches.
	/// </summary>
	public static class FlavorInterop
	{
		/// <summary>
		/// Called when the player toggles flavor in the other mod. Deliberately does not push back:
		/// whoever the player typed at owns the propagation. It does save, because the switch moved
		/// on this side too.
		/// </summary>
		public static void SetFlavor(bool _on)
		{
			Settings.Flavor = _on;
			Config.Save();
		}
	}
}
