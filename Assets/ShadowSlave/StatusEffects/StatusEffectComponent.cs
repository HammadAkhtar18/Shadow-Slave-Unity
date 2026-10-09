using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ShadowSlave.Core;
using UnityEngine;

namespace ShadowSlave.StatusEffects
{
    /// <summary>
    /// Component managing active status effects / conditions on an actor.
    /// Generic technical foundation with zero hardcoded canon mechanics.
    /// Purely event-driven and timer-driven: NO Update() polling loops.
    /// Strictly respects AttributeComponent authority: does NOT duplicate health/stamina/essence.
    /// Protected by a reentrancy transition guard against recursive mutations.
    /// Matches UE5 UShadowSlaveStatusEffectComponent.
    /// </summary>
    [DisallowMultipleComponent]
    public class StatusEffectComponent : MonoBehaviour
    {
        private readonly List<StatusEffectInstance> _activeEffects = new List<StatusEffectInstance>();
        private readonly ReadOnlyCollection<StatusEffectInstance> _activeEffectsReadOnly;

        private bool _isProcessingEffectTransition;
        private int _generationCounter;
        private Func<float> _timeProvider;

        /* --- Delegates --- */
        public event Action<StatusEffectInstance> OnStatusEffectApplied;
        public event Action<StatusEffectInstance> OnStatusEffectRemoved;
        public event Action<StatusEffectInstance> OnStatusEffectExpired;
        public event Action<StatusEffectInstance, int> OnStatusEffectStackChanged;
        public event Action OnStatusEffectCollectionChanged;

        public IReadOnlyList<StatusEffectInstance> ActiveEffects => _activeEffectsReadOnly;
        public int EffectCount => _activeEffects.Count;

        public StatusEffectComponent()
        {
            _activeEffectsReadOnly = _activeEffects.AsReadOnly();
        }

        /* --- Time Provider & Lifecycle Hooks --- */

        private float GetCurrentTime()
        {
            if (_timeProvider != null)
            {
                return _timeProvider();
            }

            return Time.time;
        }

        internal void SetTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        internal void OnEnable()
        {
            ReconcileTimedEffects();
        }

        internal void OnDisable()
        {
            StopEffectCoroutines();
        }

        private void OnDestroy()
        {
            StopEffectCoroutines();
            _activeEffects.Clear();
        }

        /* --- Application & Removal API --- */

        /// <summary>
        /// Applies a status effect based on its definition archetype.
        /// Handles stacking policy (IgnoreNew, RefreshDuration, AddStacks, Replace).
        /// Handles duration policy (Instant, Timed, Persistent).
        /// Returns the unique instance GUID if applied or updated, or Guid.Empty if rejected.
        /// </summary>
        public Guid ApplyEffect(
            StatusEffectDefinition effectDefinition,
            StatusEffectSource source = default,
            IDictionary<string, string> dynamicProperties = null)
        {
            if (effectDefinition == null || !effectDefinition.IsValidDefinition(out _))
            {
                SSLog.Warning(SSLog.CategoryStatusEffects, "StatusEffectComponent.ApplyEffect - Invalid effect definition.");
                return Guid.Empty;
            }

            if (_isProcessingEffectTransition)
            {
                SSLog.Warning(SSLog.CategoryStatusEffects, "StatusEffectComponent.ApplyEffect - Re-entrant call prevented.");
                return Guid.Empty;
            }

            _isProcessingEffectTransition = true;
            try
            {
                // Instant duration policy: executes notifications, does not persist in ActiveEffects
                if (effectDefinition.DurationPolicy == StatusEffectDurationPolicy.Instant)
                {
                    StatusEffectInstance instantInstance = new StatusEffectInstance(effectDefinition, source, dynamicProperties);
                    FireApplied(instantInstance);
                    FireRemoved(instantInstance);
                    return instantInstance.InstanceId;
                }

                // Check existing instance with the same definition
                int existingIndex = _activeEffects.FindIndex(inst => inst.EffectDefinition == effectDefinition);

                if (existingIndex >= 0)
                {
                    StatusEffectInstance existingInstance = _activeEffects[existingIndex];

                    switch (effectDefinition.StackingPolicy)
                    {
                        case StatusEffectStackingPolicy::IgnoreNew:
                        {
                            return existingInstance.InstanceId;
                        }

                        case StatusEffectStackingPolicy::RefreshDuration:
                        {
                            if (effectDefinition.DurationPolicy == StatusEffectDurationPolicy.Timed)
                            {
                                RefreshInstanceDuration(existingInstance);
                            }
                            return existingInstance.InstanceId;
                        }

                        case StatusEffectStackingPolicy::AddStacks:
                        {
                            int oldStacks = existingInstance.CurrentStacks;
                            if (oldStacks < effectDefinition.MaxStacks)
                            {
                                existingInstance.CurrentStacks = Mathf.Clamp(oldStacks + 1, 1, effectDefinition.MaxStacks);
                            }

                            if (effectDefinition.DurationPolicy == StatusEffectDurationPolicy.Timed)
                            {
                                RefreshInstanceDuration(existingInstance);
                            }

                            if (existingInstance.CurrentStacks != oldStacks)
                            {
                                FireStackChanged(existingInstance, oldStacks);
                            }
                            return existingInstance.InstanceId;
                        }

                        case StatusEffectStackingPolicy::Replace:
                        {
                            ClearInstanceTimer(existingInstance);
                            _activeEffects.RemoveAt(existingIndex);
                            FireRemoved(existingInstance);
                            break;
                        }
                    }
                }

                // Create new active effect instance
                StatusEffectInstance newInstance = new StatusEffectInstance(effectDefinition, source, dynamicProperties)
                {
                    CurrentStacks = 1
                };

                if (effectDefinition.DurationPolicy == StatusEffectDurationPolicy.Timed)
                {
                    newInstance.TotalDuration = effectDefinition.Duration;
                    newInstance.ExpirationTime = GetCurrentTime() + newInstance.TotalDuration;
                    newInstance.Generation = ++_generationCounter;

                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        newInstance.TimerCoroutine = StartCoroutine(EffectExpiryRoutine(newInstance.InstanceId, newInstance.Generation, newInstance.TotalDuration));
                    }
                }

                _activeEffects.Add(newInstance);

                FireApplied(newInstance);
                FireCollectionChanged();

                return newInstance.InstanceId;
            }
            finally
            {
                _isProcessingEffectTransition = false;
            }
        }

        public Guid ApplyEffectSimple(StatusEffectDefinition effectDefinition)
        {
            return ApplyEffect(effectDefinition, default, null);
        }

        public bool RemoveEffect(Guid instanceId)
        {
            if (instanceId == Guid.Empty || _isProcessingEffectTransition)
            {
                return false;
            }

            int index = _activeEffects.FindIndex(inst => inst.InstanceId == instanceId);
            if (index < 0)
            {
                return false;
            }

            _isProcessingEffectTransition = true;
            try
            {
                StatusEffectInstance removed = _activeEffects[index];
                ClearInstanceTimer(removed);
                _activeEffects.RemoveAt(index);

                FireRemoved(removed);
                FireCollectionChanged();

                return true;
            }
            finally
            {
                _isProcessingEffectTransition = false;
            }
        }

        public bool RemoveEffectByDefinition(StatusEffectDefinition effectDefinition)
        {
            if (effectDefinition == null || _isProcessingEffectTransition)
            {
                return false;
            }

            int index = _activeEffects.FindIndex(inst => inst.EffectDefinition == effectDefinition);
            if (index < 0)
            {
                return false;
            }

            return RemoveEffect(_activeEffects[index].InstanceId);
        }

        public int RemoveAllEffectsByDefinition(StatusEffectDefinition effectDefinition)
        {
            if (effectDefinition == null || _isProcessingEffectTransition)
            {
                return 0;
            }

            List<Guid> idsToRemove = new List<Guid>();
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].EffectDefinition == effectDefinition)
                {
                    idsToRemove.Add(_activeEffects[i].InstanceId);
                }
            }

            int removedCount = 0;
            for (int i = 0; i < idsToRemove.Count; i++)
            {
                if (RemoveEffect(idsToRemove[i]))
                {
                    removedCount++;
                }
            }

            return removedCount;
        }

        public int RemoveAllEffects()
        {
            if (_isProcessingEffectTransition)
            {
                return 0;
            }

            int count = _activeEffects.Count;
            if (count == 0)
            {
                return 0;
            }

            _isProcessingEffectTransition = true;
            try
            {
                List<StatusEffectInstance> removedList = new List<StatusEffectInstance>(_activeEffects);
                _activeEffects.Clear();

                for (int i = 0; i < removedList.Count; i++)
                {
                    ClearInstanceTimer(removedList[i]);
                    FireRemoved(removedList[i]);
                }

                FireCollectionChanged();
                return count;
            }
            finally
            {
                _isProcessingEffectTransition = false;
            }
        }

        public void ClearEffects()
        {
            RemoveAllEffects();
        }

        /* --- Query API --- */

        public bool HasEffect(StatusEffectDefinition effectDefinition)
        {
            if (effectDefinition == null) return false;
            return _activeEffects.Exists(inst => inst.EffectDefinition == effectDefinition);
        }

        public bool HasEffectById(string effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return false;
            return _activeEffects.Exists(inst => inst.EffectDefinition != null && string.Equals(inst.EffectDefinition.EffectId, effectId, StringComparison.Ordinal));
        }

        public bool HasEffectByInstanceId(Guid instanceId)
        {
            if (instanceId == Guid.Empty) return false;
            return _activeEffects.Exists(inst => inst.InstanceId == instanceId);
        }

        public bool FindEffect(StatusEffectDefinition effectDefinition, out StatusEffectInstance effect)
        {
            effect = null;
            if (effectDefinition == null) return false;

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].EffectDefinition == effectDefinition)
                {
                    effect = _activeEffects[i];
                    return true;
                }
            }
            return false;
        }

        public bool FindEffectById(string effectId, out StatusEffectInstance effect)
        {
            effect = null;
            if (string.IsNullOrEmpty(effectId)) return false;

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].EffectDefinition != null &&
                    string.Equals(_activeEffects[i].EffectDefinition.EffectId, effectId, StringComparison.Ordinal))
                {
                    effect = _activeEffects[i];
                    return true;
                }
            }
            return false;
        }

        public bool FindEffectByInstanceId(Guid instanceId, out StatusEffectInstance effect)
        {
            effect = null;
            if (instanceId == Guid.Empty) return false;

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].InstanceId == instanceId)
                {
                    effect = _activeEffects[i];
                    return true;
                }
            }
            return false;
        }

        public void GetAllEffectsByDefinition(StatusEffectDefinition effectDefinition, List<StatusEffectInstance> outEffects)
        {
            if (outEffects == null) return;
            outEffects.Clear();
            if (effectDefinition == null) return;

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].EffectDefinition == effectDefinition)
                {
                    outEffects.Add(_activeEffects[i]);
                }
            }
        }

        public int GetStackCount(StatusEffectDefinition effectDefinition)
        {
            if (effectDefinition == null) return 0;
            int total = 0;
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].EffectDefinition == effectDefinition)
                {
                    total += _activeEffects[i].CurrentStacks;
                }
            }
            return total;
        }

        public float GetRemainingDuration(Guid instanceId)
        {
            if (instanceId == Guid.Empty) return 0f;

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].InstanceId == instanceId)
                {
                    StatusEffectInstance inst = _activeEffects[i];
                    if (inst.EffectDefinition == null) return 0f;

                    if (inst.EffectDefinition.DurationPolicy == StatusEffectDurationPolicy.Persistent)
                    {
                        return -1.0f;
                    }

                    if (inst.EffectDefinition.DurationPolicy == StatusEffectDurationPolicy.Instant)
                    {
                        return 0f;
                    }

                    return Mathf.Max(0f, inst.ExpirationTime - GetCurrentTime());
                }
            }

            return 0f;
        }

        /* --- Persistence / Restoration Support --- */

        public void RestoreEffects(IEnumerable<StatusEffectInstance> inEffects)
        {
            if (_isProcessingEffectTransition)
            {
                return;
            }

            _isProcessingEffectTransition = true;
            try
            {
                StopEffectCoroutines();
                _activeEffects.Clear();

                if (inEffects != null)
                {
                    float now = GetCurrentTime();
                    foreach (StatusEffectInstance restored in inEffects)
                    {
                        if (restored != null && restored.IsValid)
                        {
                            if (restored.EffectDefinition.DurationPolicy == StatusEffectDurationPolicy.Timed)
                            {
                                float remaining = restored.ExpirationTime - now;
                                if (remaining <= 0f)
                                {
                                    // Expired while saved; do not restore
                                    continue;
                                }

                                restored.Generation = ++_generationCounter;
                                if (isActiveAndEnabled && Application.isPlaying)
                                {
                                    restored.TimerCoroutine = StartCoroutine(EffectExpiryRoutine(restored.InstanceId, restored.Generation, remaining));
                                }
                            }

                            _activeEffects.Add(restored);
                        }
                    }
                }

                FireCollectionChanged();
            }
            finally
            {
                _isProcessingEffectTransition = false;
            }
        }

        /* --- Internal Lifecycle & Expiration --- */

        internal void ReconcileTimedEffects()
        {
            if (_activeEffects.Count == 0 || _isProcessingEffectTransition)
            {
                return;
            }

            _isProcessingEffectTransition = true;
            List<StatusEffectInstance> expired = null;
            try
            {
                float now = GetCurrentTime();

                for (int i = _activeEffects.Count - 1; i >= 0; i--)
                {
                    StatusEffectInstance inst = _activeEffects[i];
                    if (inst.EffectDefinition != null && inst.EffectDefinition.DurationPolicy == StatusEffectDurationPolicy.Timed)
                    {
                        if (now >= inst.ExpirationTime)
                        {
                            if (expired == null)
                            {
                                expired = new List<StatusEffectInstance>();
                            }
                            expired.Add(inst);
                            ClearInstanceTimer(inst);
                            _activeEffects.RemoveAt(i);
                        }
                        else
                        {
                            if (isActiveAndEnabled && Application.isPlaying)
                            {
                                ClearInstanceTimer(inst);
                                float remaining = inst.ExpirationTime - now;
                                inst.TimerCoroutine = StartCoroutine(EffectExpiryRoutine(inst.InstanceId, inst.Generation, remaining));
                            }
                        }
                    }
                }
                if (expired != null && expired.Count > 0)
                {
                    for (int i = 0; i < expired.Count; i++)
                    {
                        FireExpired(expired[i]);
                        FireRemoved(expired[i]);
                    }
                    FireCollectionChanged();
                }
            }
            finally
            {
                _isProcessingEffectTransition = false;
            }
        }

        internal void HandleEffectExpired(Guid instanceId, int generation = -1)
        {
            if (instanceId == Guid.Empty || _isProcessingEffectTransition)
            {
                return;
            }

            int index = _activeEffects.FindIndex(inst => inst.InstanceId == instanceId);
            if (index < 0)
            {
                return;
            }

            StatusEffectInstance inst = _activeEffects[index];
            if (generation != -1 && inst.Generation != generation)
            {
                // Stale coroutine callback; ignore safely
                return;
            }

            _isProcessingEffectTransition = true;
            try
            {
                ClearInstanceTimer(inst);
                _activeEffects.RemoveAt(index);

                FireExpired(inst);
                FireRemoved(inst);
                FireCollectionChanged();
            }
            finally
            {
                _isProcessingEffectTransition = false;
            }
        }

        private System.Collections.IEnumerator EffectExpiryRoutine(Guid instanceId, int generation, float duration)
        {
            yield return new WaitForSeconds(duration);
            HandleEffectExpired(instanceId, generation);
        }

        private void RefreshInstanceDuration(StatusEffectInstance instance)
        {
            ClearInstanceTimer(instance);
            instance.TotalDuration = instance.EffectDefinition.Duration;
            instance.ExpirationTime = GetCurrentTime() + instance.TotalDuration;
            instance.Generation = ++_generationCounter;

            if (isActiveAndEnabled && Application.isPlaying)
            {
                instance.TimerCoroutine = StartCoroutine(EffectExpiryRoutine(instance.InstanceId, instance.Generation, instance.TotalDuration));
            }
        }

        private void ClearInstanceTimer(StatusEffectInstance instance)
        {
            if (instance.TimerCoroutine != null)
            {
                StopCoroutine(instance.TimerCoroutine);
                instance.TimerCoroutine = null;
            }
        }

        private void StopEffectCoroutines()
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                ClearInstanceTimer(_activeEffects[i]);
            }
        }

        /* --- Event Helper Invocations (Exception-Safe) --- */

        private void FireApplied(StatusEffectInstance instance)
        {
            try
            {
                OnStatusEffectApplied?.Invoke(instance);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryStatusEffects, $"Exception in OnStatusEffectApplied subscriber: {ex}");
            }
        }

        private void FireRemoved(StatusEffectInstance instance)
        {
            try
            {
                OnStatusEffectRemoved?.Invoke(instance);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryStatusEffects, $"Exception in OnStatusEffectRemoved subscriber: {ex}");
            }
        }

        private void FireExpired(StatusEffectInstance instance)
        {
            try
            {
                OnStatusEffectExpired?.Invoke(instance);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryStatusEffects, $"Exception in OnStatusEffectExpired subscriber: {ex}");
            }
        }

        private void FireStackChanged(StatusEffectInstance instance, int oldStacks)
        {
            try
            {
                OnStatusEffectStackChanged?.Invoke(instance, oldStacks);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryStatusEffects, $"Exception in OnStatusEffectStackChanged subscriber: {ex}");
            }
        }

        private void FireCollectionChanged()
        {
            try
            {
                OnStatusEffectCollectionChanged?.Invoke();
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryStatusEffects, $"Exception in OnStatusEffectCollectionChanged subscriber: {ex}");
            }
        }
    }
}
