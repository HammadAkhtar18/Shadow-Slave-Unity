using System;
using UnityEngine;

namespace ShadowSlave.Combat
{
    /// <summary>
    /// Payload for a damage event. Mirrors UE FShadowSlaveDamageInfo field-for-field.
    /// UE has no damage-type field; none is added here.
    /// </summary>
    [Serializable]
    public struct DamageInfo
    {
        public float DamageAmount;
        public GameObject Attacker;
        public GameObject DamageCauser;
        public Vector3 HitLocation;
        public Vector3 HitNormal;
        public Vector3 HitDirection;
        public int AttackInstanceId;

        public DamageInfo(
            float damageAmount,
            GameObject attacker = null,
            GameObject damageCauser = null,
            Vector3 hitLocation = default,
            Vector3 hitNormal = default,
            Vector3 hitDirection = default,
            int attackInstanceId = 0)
        {
            DamageAmount = damageAmount;
            Attacker = attacker;
            DamageCauser = damageCauser;
            HitLocation = hitLocation;
            HitNormal = hitNormal;
            HitDirection = hitDirection;
            AttackInstanceId = attackInstanceId;
        }
    }
}
