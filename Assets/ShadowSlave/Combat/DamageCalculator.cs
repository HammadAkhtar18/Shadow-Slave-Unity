using UnityEngine;

namespace ShadowSlave.Combat
{
    /// <summary>
    /// Pure-logic damage finalization. UE has no separate damage pipeline —
    /// AttributeComponent clamps to remaining health. This helper mirrors that
    /// pass-through / clamp behaviour for callers that need a final amount
    /// before ApplyDamage.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Returns a non-negative damage amount. Does not invent resist/armor
        /// systems that UE does not have.
        /// </summary>
        public static float FinalizeAmount(in DamageInfo info)
        {
            return Mathf.Max(0f, info.DamageAmount);
        }

        /// <summary>
        /// Clamps proposed damage to remaining health (same clamp AttributeComponent uses).
        /// </summary>
        public static float ClampToRemainingHealth(float amount, float currentHealth)
        {
            if (amount <= 0f || currentHealth <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp(amount, 0f, currentHealth);
        }

        /// <summary>
        /// Convenience: finalize then clamp against remaining health.
        /// </summary>
        public static float CalculateApplied(in DamageInfo info, float currentHealth)
        {
            return ClampToRemainingHealth(FinalizeAmount(info), currentHealth);
        }
    }
}
