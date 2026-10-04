using System;
using UnityEngine;

namespace ShadowSlave.Aspects
{
    /// <summary>
    /// Reusable component managing an entity's Aspect binding and identity.
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

        /// <summary>
        /// Fires when the active Aspect Definition changes. Arguments are (newAspectDef, oldAspectDef).
        /// Does not fire when setting the same definition instance.
        /// </summary>
        public event Action<AspectDefinition, AspectDefinition> OnAspectChanged;

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
        /// Returns true if updated. Suppresses duplicate assignment without firing events.
        /// </summary>
        public bool SetAspectDefinition(AspectDefinition newAspectDefinition)
        {
            if (aspectDefinition == newAspectDefinition)
            {
                return true;
            }

            AspectDefinition oldAspectDef = aspectDefinition;
            aspectDefinition = newAspectDefinition;

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
    }
}
