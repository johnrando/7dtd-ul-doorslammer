using System.Collections.Generic;
using UnityEngine;

namespace DoorSlammer
{
	/// <summary>
	/// Detects "a player just closed this door" by postfixing
	/// <c>BlockDoor.updateOpenCloseState</c>, the single point every door state change is written
	/// through. Vanilla's own door classes and all six of Undead Legacy's BlockDoor subclasses
	/// inherit it without overriding, so one patch covers both.
	///
	/// A postfix rather than a prefix, and that matters: updateOpenCloseState finishes by calling
	/// <c>SetBlockRPC</c> with its own local copy of the BlockValue. Block damage applied from a
	/// prefix would carry the pre-damage value and be overwritten by that write.
	/// </summary>
	internal static class DoorCloseTrigger
	{
		/// <summary>Last time each door was slammed for damage, keyed on parent position.</summary>
		private static readonly Dictionary<Vector3i, float> lastSlam = new Dictionary<Vector3i, float>();

		/// <summary>Prune the cooldown map once it passes this many doors.</summary>
		private const int PruneAbove = 64;

		internal static void Postfix(bool _bOpen, WorldBase _world, Vector3i _blockPos, int _cIdx,
			BlockValue _blockValue, bool _bOnlyLocal)
		{
			if (!Settings.Enabled)
			{
				return;
			}

			// Only a close is a slam. OnBlockActivated computes this as !IsDoorOpen(meta), so
			// _bOpen == false already means a genuine open-to-closed transition; there is no need
			// to re-read the meta bit (which this method has already overwritten by now anyway).
			if (_bOpen)
			{
				return;
			}

			// The gate that matters most. BlockDoor.OnBlockAdded calls this with _bOnlyLocal true
			// on every chunk load and block placement - without this check, loading a save would
			// slam every door in the world at once.
			if (_bOnlyLocal)
			{
				return;
			}

			if (!(_world is World world) || world.IsRemote())
			{
				// Only act where we are authoritative. On a dedicated server this hook runs on the
				// acting player's client, not the server; see ServerAuthoritative for that path.
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

		/// <summary>
		/// Per-door rate limit, so holding or macro-ing the activate key cannot grind damage out
		/// frame by frame.
		/// </summary>
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

		/// <summary>Drop doors nobody has touched recently, so a long session cannot grow this
		/// map without bound.</summary>
		private static void Prune(float _now)
		{
			List<Vector3i> stale = new List<Vector3i>();
			foreach (KeyValuePair<Vector3i, float> entry in lastSlam)
			{
				if (_now - entry.Value > 30f)
				{
					stale.Add(entry.Key);
				}
			}
			for (int i = 0; i < stale.Count; i++)
			{
				lastSlam.Remove(stale[i]);
			}
		}
	}
}
