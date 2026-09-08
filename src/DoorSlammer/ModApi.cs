namespace DoorSlammer
{
	public class ModApi : IModApi
	{
		public void InitMod(Mod _modInstance)
		{
			// Settings first: the rage patch logs which way its switch is set, and that should be
			// the player's setting rather than the built-in default.
			Config.Load();
			Patches.Apply();
		}
	}
}
