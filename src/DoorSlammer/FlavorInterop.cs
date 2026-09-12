namespace DoorSlammer
{
	/// <summary>
	/// PUBLISHED CONTRACT. FletchWounds and Stumblr bind <see cref="SetFlavor"/> by reflection, the
	/// way <see cref="FlavorBridge"/> binds their <c>TryProc</c>, so this signature is the whole
	/// interface. Changing it does not break the build; it silently unlinks the switches.
	/// </summary>
	public static class FlavorInterop
	{
		/// <summary>
		/// Called when the player toggles flavor in another mod. This mod is the hub, so the value
		/// is saved here and pushed on to every bridged mod, the caller included - which is harmless,
		/// because a receiver only sets and saves and never pushes back. That is what lets the
		/// player set the switch in any one mod and have all of them follow, without a loop.
		/// </summary>
		public static void SetFlavor(bool _on)
		{
			Settings.Flavor = _on;
			Config.Save();
			FlavorBridges.PushFlavor(_on);
		}
	}
}
