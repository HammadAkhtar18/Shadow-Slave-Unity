using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ShadowSlave.Attributes;
using ShadowSlave.Core;
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

        [Header("Flaw Configuration")]
        [SerializeField]
        private FlawDefinition activeFlawDefinition;

        [Header("Runtime Ability State")]
        [SerializeField]
        private List<AspectAbilityInstance> abilityInstances = new List<AspectAbilityInstance>();

        [Header("State Transition Guard")]
        [SerializeField]
        private bool _isProcessingAbilityTransition = false;

        private ReadOnlyCollection<AspectAbilityInstance> _readOnlyAbilityInstances;

        /// <summary>
        /// Fires when the active Aspect Definition changes. Arguments are (newAspectDef, oldAspectDef).
        /// Does not fire when setting the same definition instance.
        /// </summary>
        public event Action<AspectDefinition, AspectDefinition> OnAspectChanged;

        /// <summary>
        /// Fires when the active Flaw Definition changes. Arguments are (newFlawDef, oldFlawDef).
        /// Does not fire when setting the same Flaw definition reference.
        /// Matches UE5 FOnFlawChangedSignature.
        /// </summary>
        public event Action<FlawDefinition, FlawDefinition> OnFlawChanged;

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
        /// Fires when an ability instance is activated. Argument is the newly activated runtime instance.
        /// Fires only on actual state transition (does not fire if already active).
        /// </summary>
        public event Action<AspectAbilityInstance> OnAbilityActivated;

        /// <summary>
        /// Returns whether an ability state transition (activation, deactivation, or aspect replacement) is currently in progress.
        /// Guards against re-entrant delegate callbacks.
        /// </summary>
        public bool IsProcessingAbilityTransition => _isProcessingAbilityTransition;

        /// <summary>
        /// Returns whether this component currently has an Aspect assigned.
        /// </summary>
        public bool HasAspect() => aspectDefinition != null;

        /// <summary>
        /// Returns the currently bound static Aspect Definition asset, or null if none is bound.
        /// </summary>
        public AspectDefinition GetAspectDefinition() => aspectDefinition;

        private void Awake()
        {
            ReconcileRuntimeState();
        }

        /// <summary>
        /// Reconciles runtime ability instances and active flaw with the currently assigned AspectDefinition.
        /// Used during component initialization (Awake) or when an Aspect is assigned via Unity Inspector/serialization.
        /// Ensures instances are created in initial locked/inactive state with clean dynamic properties,
        /// without consuming Essence or activating abilities.
        /// Discards and rebuilds any serialized instance list containing stale runtime state (unlocked, active, or non-empty dynamic properties).
        /// </summary>
        public void ReconcileRuntimeState()
        {
            if (aspectDefinition == null)
            {
                if (abilityInstances != null && abilityInstances.Count > 0)
                {
                    abilityInstances.Clear();
                    _readOnlyAbilityInstances = null;
                }
                return;
            }

            if (aspectDefinition.HasDuplicateAbilityIds())
            {
                SSLog.Warning(SSLog.CategoryAspects, $"Rejecting AspectDefinition '{aspectDefinition.name}' during reconciliation due to duplicate ability IDs.");
                if (abilityInstances != null)
                {
                    abilityInstances.Clear();
                    _readOnlyAbilityInstances = null;
                }
                return;
            }

            if (IsRuntimeStateCleanAndInSync())
            {
                if (activeFlawDefinition == null && aspectDefinition.HasFlaw())
                {
                    activeFlawDefinition = aspectDefinition.FlawDefinition;
                }
                return;
            }

            if (abilityInstances == null)
            {
                abilityInstances = new List<AspectAbilityInstance>();
            }
            else
            {
                abilityInstances.Clear();
            }
            _readOnlyAbilityInstances = null;

            if (aspectDefinition.AbilityDefinitions != null)
            {
                IReadOnlyList<AspectAbilityDefinition> defs = aspectDefinition.AbilityDefinitions;
                for (int i = 0; i < defs.Count; i++)
                {
                    AspectAbilityDefinition abilityDef = defs[i];
                    if (abilityDef != null)
                    {
                        abilityInstances.Add(new AspectAbilityInstance(abilityDef, false));
                    }
                }
            }

            if (activeFlawDefinition == null && aspectDefinition.HasFlaw())
            {
                activeFlawDefinition = aspectDefinition.FlawDefinition;
            }
        }

        private bool IsRuntimeStateCleanAndInSync()
        {
            if (aspectDefinition == null)
            {
                return abilityInstances == null || abilityInstances.Count == 0;
            }

            if (aspectDefinition.HasDuplicateAbilityIds())
            {
                return false;
            }

            IReadOnlyList<AspectAbilityDefinition> defs = aspectDefinition.AbilityDefinitions;
            int expectedCount = 0;
            if (defs != null)
            {
                for (int i = 0; i < defs.Count; i++)
                {
                    if (defs[i] != null)
                    {
                        expectedCount++;
                    }
                }
            }

            if (abilityInstances == null || abilityInstances.Count != expectedCount)
            {
                return false;
            }

            int instIdx = 0;
            if (defs != null)
            {
                for (int i = 0; i < defs.Count; i++)
                {
                    AspectAbilityDefinition def = defs[i];
                    if (def == null)
                    {
                        continue;
                    }

                    AspectAbilityInstance inst = abilityInstances[instIdx++];
                    if (inst == null || inst.AbilityDefinition != def)
                    {
                        return false;
                    }

                    // Pre-existing serialized instances with dirty runtime state (unlocked, active, or with dynamic properties)
                    // are not clean and must trigger a rebuild to guarantee initial contract.
                    if (inst.IsUnlocked || inst.IsActive || inst.DynamicProperties.Count > 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Sets or changes the active Aspect Definition.
        /// Discards previous runtime ability instances (deactivating any active instances),
        /// and populates new instances in locked and inactive initial states.
        /// Returns true if updated. Suppresses duplicate assignment without firing events.
        /// Rejects aspect definitions containing duplicate ability IDs.
        /// Commits all state changes atomically before dispatching events to prevent inconsistent state if subscribers throw.
        /// </summary>
        public bool SetAspectDefinition(AspectDefinition newAspectDefinition)
        {
            if (aspectDefinition == newAspectDefinition)
            {
                return true;
            }

            if (_isProcessingAbilityTransition)
            {
                return false;
            }

            if (newAspectDefinition != null && newAspectDefinition.HasDuplicateAbilityIds())
            {
                SSLog.Warning(SSLog.CategoryAspects, $"Rejecting AspectDefinition '{newAspectDefinition.name}' due to duplicate ability IDs.");
                return false;
            }

            _isProcessingAbilityTransition = true;
            try
            {
                // 1. Gather active instances to deactivate and deactivate them
                List<AspectAbilityInstance> deactivatedInstances = null;
                for (int i = 0; i < abilityInstances.Count; i++)
                {
                    AspectAbilityInstance instance = abilityInstances[i];
                    if (instance != null && instance.IsActive)
                    {
                        instance.IsActive = false;
                        deactivatedInstances ??= new List<AspectAbilityInstance>();
                        deactivatedInstances.Add(instance);
                    }
                }

                // 2. Prepare new runtime instances
                List<AspectAbilityInstance> newInstances = new List<AspectAbilityInstance>();
                if (newAspectDefinition != null && newAspectDefinition.AbilityDefinitions != null)
                {
                    IReadOnlyList<AspectAbilityDefinition> defs = newAspectDefinition.AbilityDefinitions;
                    for (int i = 0; i < defs.Count; i++)
                    {
                        AspectAbilityDefinition abilityDef = defs[i];
                        if (abilityDef != null)
                        {
                            newInstances.Add(new AspectAbilityInstance(abilityDef, false));
                        }
                    }
                }

                // 3. Atomically commit structural state before any events are fired
                AspectDefinition oldAspectDef = aspectDefinition;
                FlawDefinition oldFlaw = activeFlawDefinition;

                aspectDefinition = newAspectDefinition;
                abilityInstances = newInstances;
                _readOnlyAbilityInstances = null;
                activeFlawDefinition = newAspectDefinition != null ? newAspectDefinition.FlawDefinition : null;

                // 4. Dispatch events; state is already committed if any listener throws
                if (deactivatedInstances != null)
                {
                    for (int i = 0; i < deactivatedInstances.Count; i++)
                    {
                        try
                        {
                            OnAbilityDeactivated?.Invoke(deactivatedInstances[i]);
                        }
                        catch (Exception ex)
                        {
                            SSLog.Error(SSLog.CategoryAspects, $"Exception in OnAbilityDeactivated subscriber during SetAspectDefinition: {ex}");
                        }
                    }
                }

                try
                {
                    OnAspectChanged?.Invoke(newAspectDefinition, oldAspectDef);
                }
                catch (Exception ex)
                {
                    SSLog.Error(SSLog.CategoryAspects, $"Exception in OnAspectChanged subscriber during SetAspectDefinition: {ex}");
                }

                if (activeFlawDefinition != oldFlaw)
                {
                    try
                    {
                        OnFlawChanged?.Invoke(activeFlawDefinition, oldFlaw);
                    }
                    catch (Exception ex)
                    {
                        SSLog.Error(SSLog.CategoryAspects, $"Exception in OnFlawChanged subscriber during SetAspectDefinition: {ex}");
                    }
                }

                return true;
            }
            finally
            {
                _isProcessingAbilityTransition = false;
            }
        }

        /// <summary>
        /// Returns the Aspect Rank from the bound definition.
        /// Returns AspectRank.Unknown if no Aspect is assigned.
        /// </summary>
        public AspectRank GetAspectRank()
        {
            return aspectDefinition != null ? aspectDefinition.AspectRank : AspectRank.Unknown;
        }

        /* --- Flaw API --- */

        /// <summary>
        /// Returns the active Flaw Definition bound to this character, or null if none is bound.
        /// Pure query with no side effects.
        /// </summary>
        public FlawDefinition GetFlawDefinition() => activeFlawDefinition;

        /// <summary>
        /// Sets or overrides the active Flaw Definition directly.
        /// Returns true if the assignment was processed. Suppresses duplicate assignment without firing events.
        /// Fires <see cref="OnFlawChanged"/> only when the Flaw reference actually changes.
        /// Matches UE5 UShadowSlaveAspectComponent::SetFlawDefinition semantics.
        /// Does not use the ability transition guard.
        /// </summary>
        public bool SetFlawDefinition(FlawDefinition newFlawDefinition)
        {
            if (activeFlawDefinition == newFlawDefinition)
            {
                return true;
            }

            FlawDefinition oldFlaw = activeFlawDefinition;
            activeFlawDefinition = newFlawDefinition;
            try
            {
                OnFlawChanged?.Invoke(newFlawDefinition, oldFlaw);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryAspects, $"Exception in OnFlawChanged subscriber during SetFlawDefinition: {ex}");
            }
            return true;
        }

        /// <summary>
        /// Returns whether this character currently has an active Flaw bound.
        /// Pure query with no side effects.
        /// </summary>
        public bool HasFlaw() => activeFlawDefinition != null;

        /* --- Ability Queries & State Management --- */

        /// <summary>
        /// Returns all runtime Ability Instances owned by this component for the currently bound Aspect.
        /// Public view is wrapped in a ReadOnlyCollection to prevent external callers from mutating the backing list.
        /// </summary>
        public IReadOnlyList<AspectAbilityInstance> GetAbilityInstances()
        {
            if (abilityInstances == null)
            {
                return Array.Empty<AspectAbilityInstance>();
            }

            if (_readOnlyAbilityInstances == null)
            {
                _readOnlyAbilityInstances = abilityInstances.AsReadOnly();
            }

            return _readOnlyAbilityInstances;
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
            try
            {
                OnAbilityUnlocked?.Invoke(instance);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryAspects, $"Exception in OnAbilityUnlocked subscriber during UnlockAbility: {ex}");
            }
            return true;
        }

        /// <summary>
        /// Deactivates the specified Aspect ability by its unique identifier.
        /// Fails safely and returns false if the ability does not exist, abilityId is null/empty, or a transition is in progress.
        /// If the ability is currently active, clears IsActive and broadcasts <see cref="OnAbilityDeactivated"/>.
        /// Safely succeeds and returns true when the ability exists and is already inactive.
        /// </summary>
        public bool DeactivateAbility(string abilityId)
        {
            if (_isProcessingAbilityTransition || string.IsNullOrEmpty(abilityId))
            {
                return false;
            }

            AspectAbilityInstance instance = FindAbilityInstance(abilityId);
            if (instance == null)
            {
                return false;
            }

            if (!instance.IsActive)
            {
                return true;
            }

            _isProcessingAbilityTransition = true;
            try
            {
                instance.IsActive = false;
                try
                {
                    OnAbilityDeactivated?.Invoke(instance);
                }
                catch (Exception ex)
                {
                    SSLog.Error(SSLog.CategoryAspects, $"Exception in OnAbilityDeactivated subscriber during DeactivateAbility: {ex}");
                }
                return true;
            }
            finally
            {
                _isProcessingAbilityTransition = false;
            }
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

        /* --- Ability Activation State Transition --- */

        /// <summary>
        /// Activates the generic runtime state for an unlocked Aspect ability.
        /// Rejects invalid IDs, re-entrant calls, missing or locked instances, and failed prerequisites.
        /// If already active, safely returns true without consuming resources or firing events.
        /// Configured Essence cost is consumed from <see cref="AttributeComponent"/> before transition commit.
        /// On success, sets IsActive = true and broadcasts <see cref="OnAbilityActivated"/>.
        /// Catches and logs subscriber exceptions via <see cref="SSLog.Error"/> so listener failures do not abort state commit or report false failures.
        /// Does not implement gameplay effects, VFX, combat actions, or cooldowns.
        /// </summary>
        public bool ActivateAbility(string abilityId)
        {
            if (_isProcessingAbilityTransition || string.IsNullOrEmpty(abilityId))
            {
                return false;
            }

            AspectAbilityInstance instance = FindAbilityInstance(abilityId);
            if (instance == null)
            {
                return false;
            }

            if (instance.IsActive)
            {
                return true;
            }

            if (!CanActivateAbility(abilityId))
            {
                return false;
            }

            _isProcessingAbilityTransition = true;
            try
            {
                float cost = instance.AbilityDefinition.BaseEssenceCost;
                if (cost > 0f)
                {
                    AttributeComponent attributes = GetAttributeComponent();
                    if (attributes == null || !attributes.ConsumeEssence(cost))
                    {
                        return false;
                    }
                }

                instance.IsActive = true;

                try
                {
                    OnAbilityActivated?.Invoke(instance);
                }
                catch (Exception ex)
                {
                    SSLog.Error(SSLog.CategoryAspects, $"Exception in OnAbilityActivated subscriber during ActivateAbility: {ex}");
                }

                return true;
            }
            finally
            {
                _isProcessingAbilityTransition = false;
            }
        }

        /* --- Dynamic Instance Properties --- */

        /// <summary>
        /// Sets a dynamic runtime property on an individual ability instance.
        /// Stores data on the runtime <see cref="AspectAbilityInstance"/> without mutating the static definition.
        /// Rejects invalid/empty abilityId or key, non-existent abilities, or invalid instances.
        /// Uses deterministic ordinal key comparison. Null values are normalized to string.Empty matching UE5 FString semantics.
        /// Does not trigger gameplay effects, ability activation, or resource consumption.
        /// </summary>
        public bool SetAbilityDynamicProperty(string abilityId, string key, string value)
        {
            if (string.IsNullOrEmpty(abilityId) || string.IsNullOrEmpty(key))
            {
                return false;
            }

            AspectAbilityInstance instance = FindAbilityInstance(abilityId);
            if (instance == null || !instance.IsValid)
            {
                return false;
            }

            return instance.SetDynamicProperty(key, value);
        }

        /// <summary>
        /// Retrieves a dynamic runtime property from an individual ability instance.
        /// Safely initializes out value to null. Rejects invalid/empty abilityId or key,
        /// non-existent abilities, invalid instances, or missing keys.
        /// Pure side-effect-free query using deterministic ordinal key comparison.
        /// </summary>
        public bool GetAbilityDynamicProperty(string abilityId, string key, out string value)
        {
            value = null;

            if (string.IsNullOrEmpty(abilityId) || string.IsNullOrEmpty(key))
            {
                return false;
            }

            AspectAbilityInstance instance = FindAbilityInstance(abilityId);
            if (instance == null || !instance.IsValid)
            {
                return false;
            }

            return instance.TryGetDynamicProperty(key, out value);
        }
    }
}
