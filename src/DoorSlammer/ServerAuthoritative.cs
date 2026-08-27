using HarmonyLib;

namespace DoorSlammer
{
	/// <summary>
	/// SCAFFOLD - not installed. Documents the dedicated-server path so enabling it later is a
	/// wiring change rather than a redesign.
	///
	/// The shipped hook (<see cref="DoorCloseTrigger"/>) patches
	/// <c>BlockDoor.updateOpenCloseState</c>, which only ever runs on the machine of the player who
	/// pressed the activate key - the whole door-activation path is client-side, driven from
	/// <c>PlayerMoveController.Update</c> through the radial menu. That covers single-player and a
	/// host, but on a dedicated server a client's door close never reaches the server through this
	/// method.
	///
	/// <c>BlockDoor.OnBlockValueChanged</c> is the only door hook that does fire on a dedicated
	/// server, because it sits downstream of <c>GameManager.ChangeBlocks</c>, which every block
	/// change is applied through no matter who originated it.
	///
	/// Two problems have to be solved before turning this on:
	///
	/// 1. ATTRIBUTION. <c>_changedByEntityId</c> is carried as far as <c>Chunk.SetBlock</c> but is
	///    not forwarded into OnBlockValueChanged, so this hook cannot tell who closed the door.
	///    Capturing it needs a second prefix on
	///    <c>GameManager.ChangeBlocks(PlatformUserIdentifierAbs, List&lt;BlockChangeInfo&gt;)</c>
	///    reading <c>BlockChangeInfo.changedByEntityId</c>. Not needed while slam damage is
	///    deliberately uncredited, but it is the piece that would be missing.
	///
	/// 2. THE RAGE MARKER. <c>NetPackageDamageEntity</c> reconstructs a plain DamageSource on the
	///    receiving side, so <c>is DoorSlamDamageSource</c> does not survive a network round trip.
	///    A server build needs a marker that does - a per-entity tick stamp set immediately before
	///    DamageEntity would do it.
	///
	/// Also note this is a prefix target only. Do not try to reuse the body: it early-returns
	/// unless <c>shape is BlockShapeModelEntity</c>, and it casts the world to <c>World</c> and
	/// reaches into <c>ChunkClusters[_clrIdx].GetBlockEntity(...)</c>.
	/// </summary>
	internal static class ServerAuthoritative
	{
		/// <summary>
		/// Deliberately a static readonly rather than a const, so flipping it by hand while
		/// experimenting does not turn every line below into unreachable code at compile time.
		/// </summary>
		internal static readonly bool Enabled = false;

		/// <summary>
		/// Not called. <see cref="Patches"/> installs <see cref="DoorCloseTrigger"/> instead; wiring
		/// this up means calling it from there, and dropping DoorCloseTrigger so a host does not run
		/// both and damage twice.
		/// </summary>
		internal static void Install(Harmony _harmony)
		{
			if (!Enabled)
			{
				return;
			}

			System.Reflection.MethodInfo target =
				AccessTools.DeclaredMethod(typeof(BlockDoor), "OnBlockValueChanged");
			if (target == null)
			{
				Log.Error(Patches.LogPrefix + "BlockDoor.OnBlockValueChanged not found.");
				return;
			}

			_harmony.Patch(target, prefix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(ServerAuthoritative), nameof(Prefix))));
		}

		internal static void Prefix(WorldBase _world, int _clrIdx, Vector3i _blockPos,
			BlockValue _oldBlockValue, BlockValue _newBlockValue)
		{
			if (!Enabled || !Settings.Enabled)
			{
				return;
			}

			// Fires on the server AND on every client that has the chunk loaded, so this gate is
			// what keeps the damage server-authoritative and applied exactly once.
			if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
			{
				return;
			}

			// Unlike updateOpenCloseState there is no _bOpen argument, so the transition has to be
			// read off the two block values. This also naturally ignores non-state-changing writes,
			// such as the damage write that DoorSlam itself performs.
			if (!BlockDoor.IsDoorOpen(_oldBlockValue.meta) || BlockDoor.IsDoorOpen(_newBlockValue.meta))
			{
				return;
			}

			if (_newBlockValue.ischild || !(_world is World world))
			{
				return;
			}

			DoorSlam.Apply(world, _clrIdx, _blockPos);
		}
	}
}
