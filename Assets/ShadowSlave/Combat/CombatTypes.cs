using System;
using UnityEngine;

namespace ShadowSlave.Combat
{
    /// <summary>
    /// Combat states representing the actor's current combat activity.
    /// Mirrors UE ECombatState.
    /// </summary>
    public enum ECombatState
    {
        Neutral = 0,
        Attacking = 1,
        Recovering = 2,
        Dodging = 3,
        Stunned = 4,
        Dead = 5
    }

    /// <summary>
    /// Basic melee attack classifications. Mirrors UE EAttackType.
    /// </summary>
    public enum EAttackType
    {
        Light = 0,
        Heavy = 1
    }

    /// <summary>
    /// Cardinal directions for dodge / evasion. Mirrors UE EDodgeDirection.
    /// Full dodge execution is deferred; type retained for future parity.
    /// </summary>
    public enum EDodgeDirection
    {
        Forward = 0,
        Backward = 1,
        Left = 2,
        Right = 3
    }

    /// <summary>
    /// Data-driven configuration for an attack action.
    /// Mirrors UE FShadowSlaveAttackData. AnimationClip is optional (null-ok);
    /// montage-driven hit windows are deferred on Unity.
    /// </summary>
    [Serializable]
    public struct AttackData
    {
        public EAttackType AttackType;
        [Min(0f)] public float Damage;
        [Min(5f)] public float TraceRadius;
        [Min(20f)] public float TraceDistance;
        [Min(0.05f)] public float HitWindowDuration;
        [Min(0f)] public float RecoveryDuration;
        [Min(1)] public int MaxHitsPerTarget;
        /// <summary>Optional; montage/notify-driven windows deferred. Null is valid.</summary>
        public AnimationClip AttackClip;

        public static AttackData DefaultLight => new AttackData
        {
            AttackType = EAttackType.Light,
            Damage = 25f,
            TraceRadius = 45f,
            TraceDistance = 160f,
            HitWindowDuration = 0.35f,
            RecoveryDuration = 0.20f,
            MaxHitsPerTarget = 1,
            AttackClip = null
        };

        public static AttackData DefaultHeavy => new AttackData
        {
            AttackType = EAttackType.Heavy,
            Damage = 60f,
            TraceRadius = 55f,
            TraceDistance = 180f,
            HitWindowDuration = 0.45f,
            RecoveryDuration = 0.35f,
            MaxHitsPerTarget = 1,
            AttackClip = null
        };
    }

    /// <summary>
    /// Data-driven configuration for a dodge action.
    /// Mirrors UE FShadowSlaveDodgeData. Full dodge movement is deferred.
    /// </summary>
    [Serializable]
    public struct DodgeData
    {
        [Min(0f)] public float StaminaCost;
        [Min(0.05f)] public float DodgeDuration;
        [Min(0f)] public float DodgeSpeed;

        public static DodgeData Default => new DodgeData
        {
            StaminaCost = 20f,
            DodgeDuration = 0.35f,
            DodgeSpeed = 950f
        };
    }
}
