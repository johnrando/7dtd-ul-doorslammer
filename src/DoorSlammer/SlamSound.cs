using UnityEngine;

namespace DoorSlammer
{
	/// <summary>Which of the game's own sounds a damaging slam plays, if any.</summary>
	internal enum SlamSoundMode
	{
		/// <summary>Nothing extra. The door's own CloseSound still plays, as it always does.</summary>
		Off,

		/// <summary>What a zombie's fist sounds like on this door.</summary>
		Impact,

		/// <summary>What this door sounds like when it breaks.</summary>
		Break
	}

	/// <summary>
	/// Audio feedback for a slam that actually took HP off a door.
	///
	/// Nothing here is a bundled clip or a hand-written sound name: both modes look up a sound the
	/// game already ships and already plays on this exact door, keyed on the door's own material.
	/// So a modded door the mod has never seen still gets the right sound - and if its material
	/// has none, silence rather than a wrong one.
	/// </summary>
	internal static class SlamSound
	{
		/// <summary>
		/// What the last damaging slam resolved to, reported by <c>ds info</c>. Worth reporting
		/// because an unknown sound name fails silently by design (see Play), so this is the only
		/// way to see which name a given door actually asked for.
		/// </summary>
		internal static string LastPlayed = "nothing yet";

		/// <param name="_parentPos">The door's parent block position.</param>
		/// <param name="_block">The door, already read back out of the world by the caller.</param>
		internal static void Play(Vector3i _parentPos, Block _block)
		{
			if (Settings.SoundMode == SlamSoundMode.Off)
			{
				return;
			}

			string soundName = Resolve(_block);
			if (string.IsNullOrEmpty(soundName))
			{
				LastPlayed = "nothing - " + _block.blockName + " has no sound for this mode";
				return;
			}

			// BroadcastPlay rather than BroadcastPlayByLocalPlayer, and the difference is the whole
			// point: Audio.Manager.Play only calls SignalAI when the entity it is handed is an
			// EntityPlayer, and this overload leaves _entityId at -1. So the slam is audible to the
			// player and replicated to any other clients, but contributes no AI noise and no
			// screamer heat - which the break sounds carry in quantity (metaldestroy is noise 20,
			// heat_map_strength 1.42). Vanilla's own door open/close sounds do signal the AI; a
			// slam deliberately does not, in keeping with the rest of what a slam does not do.
			//
			// An unknown sound name is not an error and not a log line: Manager.Play returns at its
			// audioData lookup. That is what makes guessing a name off a material safe.
			Audio.Manager.BroadcastPlay(_parentPos.ToVector3() + Vector3.one * 0.5f, soundName);
			LastPlayed = soundName;
		}

		/// <summary>
		/// The sound name for this door under the current mode, or null if it has none.
		/// </summary>
		private static string Resolve(Block _block)
		{
			if (Settings.SoundMode == SlamSoundMode.Break)
			{
				// A door may name its own break sound, and many do: every commercial, chainlink and
				// sliding door carries DestroyFX "door_damage_metal,metaldestroy". Block.SpawnDestroyFX
				// prefers it over the material for exactly this reason, so we do too.
				string fromFX = SoundFromFX(_block.DestroyFX);
				if (fromFX != null)
				{
					return fromFX;
				}
			}

			// The game's own "what is this block made of, for audio" discriminator: one of wood,
			// metal, stone, glass, cloth, earth, organic, plant, water. Undead Legacy adds no block
			// material without one. blockMaterial rather than GetMaterialForSide because a slam has
			// no hit face, and because it is what Block.SpawnDestroyParticleEffect uses for sound.
			string surface = _block.blockMaterial?.SurfaceCategory;
			if (string.IsNullOrEmpty(surface))
			{
				return null;
			}

			if (Settings.SoundMode == SlamSoundMode.Break)
			{
				// Block.SpawnDestroyParticleEffect plays exactly this string when a block of this
				// material is destroyed. It is also the downgrade sound: SpawnDowngradeFX falls
				// through to the same call whenever DowngradeFX is unset, which it is on every
				// vanilla and Undead Legacy door.
				return surface + "destroy";
			}

			// ItemActionAttack.Hit composes a block-hit sound as "{attackerMadeOf}hit{surface}", and
			// zombie hands are meleeHandMaster with Material Morganic. So this is not an
			// approximation of the sound a zombie makes hitting this door - it is that sound.
			return "organichit" + surface;
		}

		/// <summary>
		/// The sound half of a block's "particle,sound" FX property, or null if it has none.
		/// </summary>
		private static string SoundFromFX(string _fx)
		{
			if (string.IsNullOrEmpty(_fx))
			{
				return null;
			}

			// Block.SpawnFX splits on the comma and indexes [1] without checking, so a malformed
			// value throws there. Guard rather than inherit that.
			string[] parts = _fx.Split(',');
			if (parts.Length < 2 || parts[1].Length == 0)
			{
				return null;
			}

			return parts[1];
		}

		/// <summary>Off -> impact -> break -> off, behind <c>ds sound</c>.</summary>
		internal static void Cycle()
		{
			switch (Settings.SoundMode)
			{
			case SlamSoundMode.Off:
				Settings.SoundMode = SlamSoundMode.Impact;
				return;

			case SlamSoundMode.Impact:
				Settings.SoundMode = SlamSoundMode.Break;
				return;

			default:
				Settings.SoundMode = SlamSoundMode.Off;
				return;
			}
		}

		/// <summary>The line <c>ds sound</c> prints after cycling.</summary>
		internal static string Describe()
		{
			switch (Settings.SoundMode)
			{
			case SlamSoundMode.Impact:
				return "Slam sound: IMPACT - 'organichit<material>', what a zombie's fist sounds "
					+ "like on this door.";

			case SlamSoundMode.Break:
				return "Slam sound: BREAK - '<material>destroy', or the door's own DestroyFX. What "
					+ "it sounds like when it breaks.";

			default:
				return "Slam sound: OFF - only the door's own close sound plays.";
			}
		}

		/// <summary>Just the mode name, for the <c>ds</c> settings block.</summary>
		internal static string Status()
		{
			switch (Settings.SoundMode)
			{
			case SlamSoundMode.Impact:
				return "impact";

			case SlamSoundMode.Break:
				return "break";

			default:
				return "off";
			}
		}
	}
}
