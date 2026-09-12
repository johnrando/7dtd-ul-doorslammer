using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoorSlammer
{
	/// <summary>
	/// Detects "a player just closed this door" by postfixing <c>BlockDoor.updateOpenCloseState</c>,
	/// the single point every door state change is written through. Vanilla's door classes and all
	/// six of Undead Legacy's BlockDoor subclasses inherit it without overriding.
	///
	/// A postfix rather than a prefix: updateOpenCloseState finishes by calling <c>SetBlockRPC</c>
	/// with its own local copy of the BlockValue, so damage applied from a prefix would be
	/// overwritten by that write.
	/// </summary>
	internal static class DoorCloseTrigger
	{
		/// <summary>Last time each door was slammed for damage, keyed on parent position.</summary>
		private static readonly Dictionary<Vector3i, float> lastSlam = new Dictionary<Vector3i, float>();

		/// <summary>Scratch list for <see cref="Prune"/>.</summary>
		private static readonly List<Vector3i> stale = new List<Vector3i>();

		/// <summary>Prune the cooldown map once it passes this many doors.</summary>
		private const int PruneAbove = 64;

		/// <summary>Never forget a door sooner than this, whatever the cooldown is set to.</summary>
		private const float MinRetainSeconds = 30f;

		internal static void Postfix(bool _bOpen, WorldBase _world, Vector3i _blockPos, int _cIdx,
			BlockValue _blockValue, bool _bOnlyLocal)
		{
			if (!Settings.Enabled)
			{
				return;
			}

			// OnBlockActivated computes _bOpen as !IsDoorOpen(meta), so false already means a
			// genuine open-to-closed transition.
			if (_bOpen)
			{
				return;
			}

			// BlockDoor.OnBlockAdded calls this with _bOnlyLocal true on every chunk load and block
			// placement - without this gate, loading a save would slam every door in the world.
			if (_bOnlyLocal)
			{
				return;
			}

			// Only act where we are authoritative. On a dedicated server this hook runs on the
			// acting player's client, not the server; see ServerAuthoritative for that path.
			if (!(_world is World world) || world.IsRemote())
			{
				return;
			}

			Vector3i parentPos = _blockValue.ischild
				? _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue)
				: _blockPos;

			if (!CooldownElapsed(parentPos))
			{
				return;
			}

			DoorSlam.Apply(world, _cIdx, parentPos);
		}

		/// <summary>Per-door rate limit, so a held or macro'd activate key cannot grind damage out
		/// frame by frame.</summary>
		private static bool CooldownElapsed(Vector3i _parentPos)
		{
			float now = Time.time;
			if (lastSlam.TryGetValue(_parentPos, out float previous)
				&& now - previous < Settings.CooldownSeconds)
			{
				return false;
			}

			if (lastSlam.Count > PruneAbove)
			{
				Prune(now);
			}

			lastSlam[_parentPos] = now;
			return true;
		}

		/// <summary>Drop doors nobody has touched recently, so the map cannot grow without bound.</summary>
		private static void Prune(float _now)
		{
			float retain = Math.Max(MinRetainSeconds, Settings.CooldownSeconds);
			stale.Clear();
			foreach (KeyValuePair<Vector3i, float> entry in lastSlam)
			{
				if (_now - entry.Value > retain)
				{
					stale.Add(entry.Key);
				}
			}
			for (int i = 0; i < stale.Count; i++)
			{
				lastSlam.Remove(stale[i]);
			}
			stale.Clear();
		}
	}
}
