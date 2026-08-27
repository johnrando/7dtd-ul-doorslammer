namespace DoorSlammer
{
	/// <summary>
	/// Marks a damage response as coming from a door slam so <see cref="RageSuppression"/> can
	/// recognise it without a re-entrancy flag. Reference identity holds for the whole synchronous
	/// <c>DamageEntity -&gt; damageEntityLocal -&gt; ProcessDamageResponseLocal</c> chain.
	///
	/// The base class choice carries the "no other effects" requirement, and each half matters:
	/// <list type="bullet">
	/// <item><c>EnumDamageSource.Internal</c> makes <c>DamageSource.AffectedByArmor()</c> false, so
	/// Undead Legacy's armour system cannot round the 1 HP down to nothing.</item>
	/// <item><c>EnumDamageTypes.None</c> makes <c>DamageSource.CanStun</c> false, so the hit never
	/// accumulates into <c>bodyDamage.StunProne</c>/<c>StunKnee</c> and can never knock down.</item>
	/// </list>
	/// Leaving <c>ownerEntityId</c> at its default of -1 is what keeps the hit uncredited: the
	/// revenge-target, <c>DamagedByEntity()</c> and <c>onCombatEntered</c> block in
	/// <c>ProcessDamageResponseLocal</c> is entirely gated on that lookup resolving.
	/// </summary>
	internal sealed class DoorSlamDamageSource : DamageSource
	{
		internal DoorSlamDamageSource(EnumDamageSource _source, EnumDamageTypes _damageType)
			: base(_source, _damageType)
		{
		}
	}
}
