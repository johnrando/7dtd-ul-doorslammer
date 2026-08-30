using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoorSlammer
{
	/// <summary>
	/// The effect itself: one door has just been slammed shut, so find at most one zombie caught in
	/// the doorway and chip a little health off it and off the door.
	///
	/// This is deliberately the only place the damage lives. <see cref="DoorCloseTrigger"/> decides
	/// *when* a slam happened; everything about *what* a slam does is here, so the scaffolded
	/// server-authoritative trigger in <see cref="ServerAuthoritative"/> can reuse it unchanged.
	///
	/// That includes the one thing a slam does not do by itself: <see cref="FletchWoundsBridge"/>
	/// hands the caught zombie to FletchWounds, if it is installed, so an arrow already stuck in
	/// that zombie gets driven deeper too.
	/// </summary>
	internal static class DoorSlam
	{
		/// <summary>Reused across calls; the query overloads that return a list hand back a shared
		/// buffer that the next caller clears, so we own ours.</summary>
		private static readonly List<Entity> candidates = new List<Entity>();

		/// <param name="_parentPos">The door's parent block position, already resolved.</param>
		internal static void Apply(World _world, int _clrIdx, Vector3i _parentPos)
		{
			Counters.ClosesChecked++;

			EntityAlive zombie = FindSingleZombie(_world, _parentPos);
			if (zombie == null)
			{
				// Nothing caught, so nothing happened: the door does not wear down from ordinary use.
				return;
			}

			Counters.Slams++;
			DamageZombie(zombie, _parentPos);

			// Outside DamageZombie rather than inside it, and that is the decision: DamageZombie
			// caps itself at the never-kill floor, and an arrow is allowed past it. The floor is a
			// rule about what a door may do, not about what your own ammunition may do once the door
			// drives it deeper. So this is the one part of a slam that can land a killing blow, and
			// the only way a zombie ever dies to one. Inert unless FletchWounds is installed.
			FletchWoundsBridge.TryProc(_world, zombie);

			DamageDoor(_world, _clrIdx, _parentPos);
		}

		/// <summary>
		/// The one zombie standing in the doorway, or null. "At most one" is a hard requirement, so
		/// this returns the single nearest rather than everything overlapping.
		/// </summary>
		private static EntityAlive FindSingleZombie(World _world, Vector3i _parentPos)
		{
			// Doors default to MultiBlockDim 1,2,1 with the parent as the lower block, so the column
			// spans parentPos.y and parentPos.y + 1 - centre is one block up from the parent's floor.
			Vector3 center = new Vector3(_parentPos.x + 0.5f, _parentPos.y + 1f, _parentPos.z + 0.5f);
			Bounds bb = new Bounds(center, new Vector3(1f, 2f, 1f));
			bb.Expand(Settings.SearchPadding * 2f);

			candidates.Clear();

			// (entityFlags & mask) == flags, so passing Zombie for both filters to zombies only.
			// EntityFlags comes from entityclasses.xml, which makes this the right discriminator
			// rather than 'is EntityZombie': it covers zombie dogs (an EntityEnemyAnimal, not an
			// EntityZombie), excludes bandits, and picks up UL-added zombies for free.
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

		private static void DamageZombie(EntityAlive _zombie, Vector3i _parentPos)
		{
			// The never-kill floor, applied as a cap on the hit rather than as a gate in front of
			// it. As a gate it only held while the damage was no larger than the floor: 'ds dmg 100'
			// against 'ds floor 10' let anything above 10 HP take the whole 100 and die, which is
			// the one thing the floor is named for stopping. Capping to the headroom instead makes
			// it mean what it says at any 'ds dmg' - the zombie lands on the floor rather than
			// through it, and the next slam finds nothing left to take.
			//
			// A slam whittles a zombie down and then stops; finishing it takes a real hit.
			// Uncredited damage would grant no XP for the kill anyway.
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

			// _impulseScale 0 suppresses knockback. With Strength 1 and a non-Bashing damage type
			// the PainHit test (Strength + ArmorDamage / 2 >= 6) also fails, so there is no
			// hit-reaction animation either.
			//
			// Note we deliberately do NOT call SetIgnoreConsecutiveDamages: its throttle is keyed
			// only on EnumDamageSource, of which there are two values, so it would collide with
			// unrelated damage in both directions. DoorCloseTrigger's per-door cooldown does that job.
			int applied = _zombie.DamageEntity(damageSource, Math.Min(Settings.DamageToZombie, headroom),
				_criticalHit: false, _impulseScale: 0f);

			// -1 means the hit was rejected outright rather than merely absorbed, so the counter
			// only moves when the damage really reached the zombie.
			if (applied >= 0)
			{
				Counters.ZombiesHit++;
			}
		}

		private static void DamageDoor(World _world, int _clrIdx, Vector3i _parentPos)
		{
			// Read the block back rather than trusting the value passed into the close, so the
			// damage is applied on top of the state the close just wrote.
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

			// Same floor as the zombie, capped the same way: slamming wears a door down to the floor
			// and never through it, whatever 'ds dmg' asks for.
			int headroom = block.MaxDamage - blockValue.damage - Settings.MinRemainingHp;
			if (headroom <= 0)
			{
				Counters.DoorsSpared++;
				return;
			}

			if (_world.IsWithinTraderArea(_parentPos))
			{
				// Mirrors what vanilla BlockDamage does before damaging a block.
				Counters.DoorsSpared++;
				return;
			}

			// _entityIdThatDamaged -1 keeps the hit unattributed, which also suppresses Undead
			// Legacy's floating damage number. Block.OnBlockDamaged handles the multiblock
			// child-to-parent redirect and replicates the new damage value itself.
			block.DamageBlock(_world, _clrIdx, _parentPos, blockValue,
				Math.Min(Settings.DamageToDoor, headroom),
				_entityIdThatDamaged: -1, _attackHitInfo: null, _bUseHarvestTool: false,
				_bBypassMaxDamage: false);
			Counters.DoorsDamaged++;

			// Only once the door has really taken HP, so a slam that is spared by the floor or by
			// the trader-area check stays as silent as it is harmless.
			SlamSound.Play(_parentPos, block);
		}
	}
}
