using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowSlave.Aspects
{
    /// <summary>
    /// Static content definition describing an immutable canon or custom character Flaw.
    /// Holds static identity and provenance data only; does not implement runtime Flaw mechanics or effects.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFlawDefinition", menuName = "ShadowSlave/Aspects/Flaw Definition")]
    public class FlawDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string flawId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField, TextArea]
        private string description = string.Empty;

        [Header("Metadata & Provenance")]
        [SerializeField]
        private List<AspectMetadataEntry> metadata = new List<AspectMetadataEntry>();

        [SerializeField]
        private string canonProvenance = string.Empty;

        public string FlawId => flawId;
        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<AspectMetadataEntry> Metadata => metadata;
        public string CanonProvenance => canonProvenance;

        public void SetFlawId(string newFlawId) => flawId = newFlawId ?? string.Empty;
        public void SetDisplayName(string newDisplayName) => displayName = newDisplayName ?? string.Empty;
        public void SetDescription(string newDescription) => description = newDescription ?? string.Empty;
        public void SetCanonProvenance(string newProvenance) => canonProvenance = newProvenance ?? string.Empty;

        public void SetMetadata(IEnumerable<AspectMetadataEntry> entries)
        {
            metadata.Clear();
            if (entries != null)
            {
                metadata.AddRange(entries);
            }
        }
    }
}
