namespace ShadowSlave.Combat
{
    /// <summary>
    /// Interface for any actor capable of receiving damage in the Shadow Slave combat framework.
    /// Mirrors UE IShadowSlaveDamageableInterface (C# interface instead of UInterface).
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Applies incoming combat damage. Returns actual damage taken.</summary>
        float TakeDamage(DamageInfo damageInfo);

        /// <summary>Whether the target is alive and valid for combat targeting.</summary>
        bool IsAlive();
    }
}
