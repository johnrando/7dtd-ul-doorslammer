using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoorSlammer
{
	/// <summary>
	/// The effect itself: a door has just been slammed shut, so find at most one zombie caught in
	/// the doorway and chip health off it and off the door. <see cref="DoorCloseTrigger"/> decides
	/// when a slam happened; everything about what a slam does lives here.
	/// </summary>
	internal static class DoorSlam
	{
		/// <summary>Reused across calls: the list-returning query overloads hand back a shared
		/// buffer that the next caller clears, so we own ours.</summary>
		private static readonly List<Entity> candidates = new List<Entity>();

		/// <param name="_parentPos">The door's parent block position, already resolved.</param>
		internal static void Apply(World _world, int _clrIdx, Vector3i _parentPos)
		{
			Counters.ClosesChecked++;

			EntityAlive zombie = FindSingleZombie(_world, _parentPos);
			if (zombie == null)
			{
				return;
			}

			Counters.Slams++;
			DamageZombie(zombie, _parentPos);

			// Outside DamageZombie on purpose: the never-kill floor is a rule about what a door may
			// do, not about what the player's own arrow may do once the door drives it deeper. This
			// is the only part of a slam that can land a killing blow. Inert without FletchWounds.
			FletchWoundsBridge.TryProc(_world, zombie);

			DamageDoor(_world, _clrIdx, _parentPos);
		}

		/// <summary>The single nearest zombie standing in the doorway, or null.</summary>
		private static EntityAlive FindSingleZombie(World _world, Vector3i _parentPos)
		{
			Bounds bb = DoorwayBounds(_world, _parentPos);
			bb.Expand(Settings.SearchPadding * 2f);
			Vector3 center = bb.center;

			candidates.Clear();

			// (entityFlags & mask) == flags. EntityFlags comes from entityclasses.xml, so unlike
			// 'is EntityZombie' it covers zombie dogs, excludes bandits, and picks up UL's zombies.
			_world.GetEntitiesAround(EntityFlags.Zombie, EntityFlags.Zombie, center,
				bb.extents.magnitude, candidates);

			EntityAlive best = null;
			float bestDistanceSq = float.MaxValue;
			for (int i = 0; i < candidates.Count; i++)
			{
				if (!(candidates[i] is EntityAlive entity) || entity.IsDead())
				{
					continue;
				}
				if (!bb.Intersects(entity.boundingBox))
				{
					continue;
				}
				float distanceSq = (entity.position - center).sqrMagnitude;
				if (distanceSq < bestDistanceSq)
				{
					bestDistanceSq = distanceSq;
					best = entity;
				}
			}

			candidates.Clear();
			return best;
		}

		/// <summary>
		/// The world-space box every block of the door occupies, in its placed rotation. A plain door
		/// is 1x2x1 above its parent, but closet and commercial double doors are two wide, cellar
		/// doors two deep and garage doors wider still; a fixed single column missed anything caught
		/// in the other leaf.
		/// </summary>
		private static Bounds DoorwayBounds(World _world, Vector3i _parentPos)
		{
			Vector3i min = _parentPos;
			Vector3i max = _parentPos;

			BlockValue blockValue = _world.GetBlock(_parentPos);
			Block block = blockValue.isair ? null : blockValue.Block;
			if (block != null && block.isMultiBlock && block.multiBlockPos != null)
			{
				// Get() applies the shape's rotation to the layout offset, the same way AddChilds
				// places the child blocks, so this is exactly the set of cells the door fills.
				for (int i = 0; i < block.multiBlockPos.Length; i++)
				{
					Vector3i cell = _parentPos + block.multiBlockPos.Get(i, blockValue.type, blockValue.rotation);
					min = Vector3i.Min(min, cell);
					max = Vector3i.Max(max, cell);
				}
			}
			else
			{
				// Block unreadable or not a multiblock: fall back to the classic 1x2x1 door column.
				max.y += 1;
			}

			Bounds bb = new Bounds();
			bb.SetMinMax(min.ToVector3(), (max + Vector3i.one).ToVector3());
			return bb;
		}

		private static void DamageZombie(EntityAlive _zombie, Vector3i _parentPos)
		{
			// The never-kill floor is a cap on the hit, not a gate in front of it: the zombie lands
			// on the floor rather than through it at any 'ds dmg', and the next slam finds nothing
			// left to take.
			int headroom = _zombie.Health - Settings.MinRemainingHp;
			if (headroom <= 0)
			{
				Counters.ZombiesSpared++;
				return;
			}

			DoorSlamDamageSource damageSource =
				new DoorSlamDamageSource(EnumDamageSource.Internal, EnumDamageTypes.None)
				{
					BlockPosition = _parentPos,
					DismemberChance = 0f
				};

			// _impulseScale 0 suppresses knockback. With Strength 1 and a non-Bashing type the
			// PainHit test (Strength + ArmorDamage / 2 >= 6) fails too, so no hit reaction either.
			//
			// Deliberately no SetIgnoreConsecutiveDamages: its throttle is keyed on EnumDamageSource
			// alone and would collide with unrelated damage. The per-door cooldown does that job.
			int applied = _zombie.DamageEntity(damageSource, Math.Min(Settings.DamageToZombie, headroom),
				_criticalHit: false, _impulseScale: 0f);

			// -1 means rejected outright rather than merely absorbed.
			if (applied >= 0)
			{
				Counters.ZombiesHit++;
			}
		}

		private static void DamageDoor(World _world, int _clrIdx, Vector3i _parentPos)
		{
			// Read the block back rather than trusting the value passed into the close, so the
			// damage lands on top of the state the close just wrote.
			BlockValue blockValue = _world.GetBlock(_parentPos);
			if (blockValue.isair)
			{
				return;
			}

			Block block = blockValue.Block;
			if (block == null)
			{
				return;
			}

			int headroom = block.MaxDamage - blockValue.damage - Settings.MinRemainingHp;
			if (headroom <= 0)
			{
				Counters.DoorsSpared++;
				return;
			}

			// Mirrors what vanilla BlockDamage does before damaging a block.
			if (_world.IsWithinTraderArea(_parentPos))
			{
				Counters.DoorsSpared++;
				return;
			}

			// _entityIdThatDamaged -1 keeps the hit unattributed, which also suppresses UL's floating
			// damage number. Block.OnBlockDamaged handles the multiblock child-to-parent redirect
			// and replicates the new damage value itself.
			block.DamageBlock(_world, _clrIdx, _parentPos, blockValue,
				Math.Min(Settings.DamageToDoor, headroom),
				_entityIdThatDamaged: -1, _attackHitInfo: null, _bUseHarvestTool: false,
				_bBypassMaxDamage: false);
			Counters.DoorsDamaged++;

			// Only once the door has really taken HP, so a spared slam stays silent.
			SlamSound.Play(_parentPos, block);
		}
	}
}
