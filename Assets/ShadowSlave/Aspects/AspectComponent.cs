using System;
using System.Collections.Generic;
using ShadowSlave.Attributes;
using ShadowSlave.Progression;
using UnityEngine;

namespace ShadowSlave.Aspects
{
    /// <summary>
    /// Reusable component managing an entity's Aspect binding, runtime ability instances, and identity.
    /// Operates purely event-driven without tick overhead.
    /// Resource storage and modification (Health, Stamina, Essence) remains strictly within AttributeComponent.
    /// Progression (Character Rank, Soul Cores, Progression Metadata) remains strictly within ProgressionComponent.
    /// </summary>
    [DisallowMultipleComponent]
    public class AspectComponent : MonoBehaviour
    {
        [Header("Aspect Configuration")]
        [SerializeField]
        private AspectDefinition aspectDefinition;

        [Header("Runtime Ability State")]
        [SerializeField]
        private List<AspectAbilityInstance> abilityInstances = new List<AspectAbilityInstance>();

        /// <summary>
        /// Fires when the active Aspect Definition changes. Arguments are (newAspectDef, oldAspectDef).
        /// Does not fire when setting the same definition instance.
        /// </summary>
        public event Action<AspectDefinition, AspectDefinition> OnAspectChanged;

        /// <summary>
        /// Fires when an ability instance is unlocked. Argument is the newly unlocked runtime instance.
        /// Fires only on actual state transition (does not fire if already unlocked).
        /// </summary>
        public event Action<AspectAbilityInstance> OnAbilityUnlocked;

        /// <summary>
        /// Fires when an active ability instance is deactivated. Argument is the deactivated runtime instance.
        /// Fires only on actual state transition (does not fire if already inactive).
        /// </summary>
        public event Action<AspectAbilityInstance> OnAbilityDeactivated;

        /// <summary>
        /// Returns whether this component currently has an Aspect assigned.
        /// </summary>
        public bool HasAspect() => aspectDefinition != null;

        /// <summary>
        /// Returns the currently bound static Aspect Definition asset, or null if none is bound.
        /// </summary>
        public AspectDefinition GetAspectDefinition() => aspectDefinition;

        /// <summary>
        /// Sets or changes the active Aspect Definition.
        /// Discards previous runtime ability instances (deactivating any active instances),
        /// and populates new instances in locked and inactive initial states.
        /// Returns true if updated. Suppresses duplicate assignment without firing events.
        /// </summary>
        public bool SetAspectDefinition(AspectDefinition newAspectDefinition)
        {
            if (aspectDefinition == newAspectDefinition)
            {
                return true;
            }

            // Deactivate any currently active ability instances before discarding
            for (int i = 0; i < abilityInstances.Count; i++)
            {
                AspectAbilityInstance instance = abilityInstances[i];
                if (instance != null && instance.IsActive)
                {
                    instance.IsActive = false;
                    OnAbilityDeactivated?.Invoke(instance);
                }
            }

            abilityInstances.Clear();

            AspectDefinition oldAspectDef = aspectDefinition;
            aspectDefinition = newAspectDefinition;

            if (newAspectDefinition != null && newAspectDefinition.AbilityDefinitions != null)
            {
                IReadOnlyList<AspectAbilityDefinition> defs = newAspectDefinition.AbilityDefinitions;
                for (int i = 0; i < defs.Count; i++)
                {
                    AspectAbilityDefinition abilityDef = defs[i];
                    if (abilityDef != null)
                    {
                        abilityInstances.Add(new AspectAbilityInstance(abilityDef, false));
                    }
                }
            }

            OnAspectChanged?.Invoke(newAspectDefinition, oldAspectDef);
            return true;
        }

        /// <summary>
        /// Returns the Aspect Rank from the bound definition.
        /// Returns AspectRank.Unknown if no Aspect is assigned.
        /// </summary>
        public AspectRank GetAspectRank()
        {
            return aspectDefinition != null ? aspectDefinition.AspectRank : AspectRank.Unknown;
        }

        /* --- Ability Queries & State Management --- */

        /// <summary>
        /// Returns all runtime Ability Instances owned by this component for the currently bound Aspect.
        /// </summary>
        public IReadOnlyList<AspectAbilityInstance> GetAbilityInstances()
        {
            return abilityInstances;
        }

        /// <summary>
        /// Returns whether the ability with the given identifier is currently unlocked.
        /// Fails safely and returns false if the ability does not exist or abilityId is null/empty.
        /// </summary>
        public bool IsAbilityUnlocked(string abilityId)
        {
            AspectAbilityInstance instance = FindAbilityInstance(abilityId);
            return instance != null && instance.IsUnlocked;
        }

        /// <summary>
        /// Returns whether the ability with the given identifier is currently active.
        /// Fails safely and returns false if the ability does not exist or abilityId is null/empty.
        /// </summary>
        public bool IsAbilityActive(string abilityId)
        {
            AspectAbilityInstance instance = FindAbilityInstance(abilityId);
            return instance != null && instance.IsActive;
        }

        /// <summary>
        /// Finds an ability runtime instance by its AbilityId using exact ordinal comparison.
        /// Returns null if not found or if abilityId is null/empty.
        /// </summary>
        public AspectAbilityInstance FindAbilityInstance(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId) || abilityInstances == null)
            {
                return null;
            }

            for (int i = 0; i < abilityInstances.Count; i++)
            {
                AspectAbilityInstance instance = abilityInstances[i];
                if (instance != null && string.Equals(instance.AbilityId, abilityId, StringComparison.Ordinal))
                {
                    return instance;
                }
            }

            return null;
        }

        /// <summary>
        /// Unlocks the specified Aspect ability by its unique identifier.
        /// Fails safely and returns false if the ability does not exist or is already unlocked.
        /// When the state changes, sets IsUnlocked = true, broadcasts <see cref="OnAbilityUnlocked"/>, and returns true.
        /// </summary>
        public bool UnlockAbility(string abilityId)
        {
            AspectAbilityInstance instance = FindAbilityInstance(abilityId);
            if (instance == null)
            {
                return false;
            }

            if (instance.IsUnlocked)
            {
                return false;
            }

            instance.IsUnlocked = true;
            OnAbilityUnlocked?.Invoke(instance);
            return true;
        }

        /// <summary>
        /// Deactivates the specified Aspect ability by its unique identifier.
        /// Fails safely and returns false if the ability does not exist.
        /// If the ability is currently active, clears IsActive and broadcasts <see cref="OnAbilityDeactivated"/>.
        /// Safely succeeds and returns true when the ability exists.
        /// </summary>
        public bool DeactivateAbility(string abilityId)
        {
            AspectAbilityInstance instance = FindAbilityInstance(abilityId);
            if (instance == null)
            {
                return false;
            }

            if (instance.IsActive)
            {
                instance.IsActive = false;
                OnAbilityDeactivated?.Invoke(instance);
            }

            return true;
        }

        /* --- Attribute Integration --- */

        /// <summary>
        /// Safe helper to locate the owning GameObject's AttributeComponent without duplicate ownership.
        /// Returns the AttributeComponent attached to the same GameObject, or null if missing.
        /// Lookup only; does not cache, create, or mutate attribute state.
        /// </summary>
        public AttributeComponent GetAttributeComponent()
        {
            return GetComponent<AttributeComponent>();
        }

        /* --- Ability Activation Prerequisites --- */

        /// <summary>
        /// Evaluates whether the specified Aspect ability is currently eligible for activation.
        /// Pure eligibility check; does not activate the ability, consume resources, or produce side effects.
        /// Verifies valid ID, unlocked instance, valid definition, valid Essence cost, Character Rank prerequisite,
        /// and sufficient Essence (if cost > 0).
        /// </summary>
        public bool CanActivateAbility(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
            {
                return false;
            }

            AspectAbilityInstance instance = FindAbilityInstance(abilityId);
            if (instance == null || !instance.IsValid || !instance.IsUnlocked)
            {
                return false;
            }

            AspectAbilityDefinition abilityDef = instance.AbilityDefinition;
            if (abilityDef == null)
            {
                return false;
            }

            float cost = abilityDef.BaseEssenceCost;
            if (!float.IsFinite(cost) || cost < 0f)
            {
                return false;
            }

            if (abilityDef.HasRankRequirement())
            {
                ProgressionComponent progression = GetComponent<ProgressionComponent>();
                if (progression == null || !progression.HasKnownRank() || progression.GetCharacterRank() < abilityDef.RequiredCharacterRank)
                {
                    return false;
                }
            }

            if (cost > 0f)
            {
                AttributeComponent attributes = GetAttributeComponent();
                if (attributes == null || attributes.CurrentEssence < cost)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
