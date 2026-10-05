using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ShadowSlave.Aspects
{
    /// <summary>
    /// Static content definition describing an immutable canon or custom character Flaw.
    /// Holds static identity and provenance data only; does not implement runtime Flaw mechanics or effects.
    /// Authored via Unity serialized asset workflow; read-only at runtime.
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

        private ReadOnlyCollection<AspectMetadataEntry> _readOnlyMetadata;
        private List<AspectMetadataEntry> _cachedMetadataSource;

        public string FlawId => flawId;
        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<AspectMetadataEntry> Metadata
        {
            get
            {
                if (metadata == null)
                {
                    return Array.Empty<AspectMetadataEntry>();
                }

                if (_readOnlyMetadata == null || _cachedMetadataSource != metadata)
                {
                    _cachedMetadataSource = metadata;
                    _readOnlyMetadata = metadata.AsReadOnly();
                }

                return _readOnlyMetadata;
            }
        }
        public string CanonProvenance => canonProvenance;

        /// <summary>
        /// Compatibility identity accessor matching UE5 SetFlawId.
        /// </summary>
        public void SetFlawId(string newFlawId) => flawId = newFlawId ?? string.Empty;
    }
}
