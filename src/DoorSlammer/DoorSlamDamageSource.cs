namespace DoorSlammer
{
	/// <summary>
	/// Marks a damage response as a door slam so <see cref="RageSuppression"/> can recognise it.
	/// Reference identity holds for the whole synchronous DamageEntity chain.
	///
	/// The base-class arguments carry the "no other effects" requirement:
	/// <list type="bullet">
	/// <item><c>EnumDamageSource.Internal</c> makes <c>AffectedByArmor()</c> false, so UL's armour
	/// cannot round the slam down to nothing.</item>
	/// <item><c>EnumDamageTypes.None</c> makes <c>CanStun</c> false, so the hit never accumulates
	/// into a knockdown.</item>
	/// </list>
	/// Leaving <c>ownerEntityId</c> at -1 keeps the hit uncredited: the revenge-target and
	/// combat-entered block in <c>ProcessDamageResponseLocal</c> is gated on that lookup resolving.
	/// </summary>
	internal sealed class DoorSlamDamageSource : DamageSource
	{
		internal DoorSlamDamageSource(EnumDamageSource _source, EnumDamageTypes _damageType)
			: base(_source, _damageType)
		{
		}
	}
}
