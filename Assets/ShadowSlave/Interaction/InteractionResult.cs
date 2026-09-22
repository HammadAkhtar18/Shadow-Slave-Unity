using System;
using System.Collections.Generic;

namespace ShadowSlave.Interaction
{
    /// <summary>
    /// Lightweight result payload describing the outcome of an interaction.
    /// Mirrors UE FShadowSlaveInteractionResult; optional metadata is a simple string dictionary only.
    /// </summary>
    /// <remarks>
    /// UE static Success() cannot share the name of the <see cref="Success"/> field in C# (CS0102).
    /// Use <see cref="Succeeded"/> for success and <see cref="Failure"/> for failure.
    /// </remarks>
    [Serializable]
    public struct InteractionResult
    {
        public bool Success;
        public string FailureReason;
        public string InteractionId;
        public Dictionary<string, string> Metadata;

        public InteractionResult(
            bool success,
            string failureReason = null,
            string interactionId = null,
            Dictionary<string, string> metadata = null)
        {
            Success = success;
            FailureReason = failureReason ?? string.Empty;
            InteractionId = interactionId ?? string.Empty;
            Metadata = metadata;
        }

        /// <summary>UE FShadowSlaveInteractionResult::Success — successful interaction result.</summary>
        public static InteractionResult Succeeded(string id = null)
        {
            return new InteractionResult(true, string.Empty, id, null);
        }

        /// <summary>UE FShadowSlaveInteractionResult::Failure — failed interaction result.</summary>
        public static InteractionResult Failure(string reason, string id = null)
        {
            return new InteractionResult(false, reason ?? string.Empty, id, null);
        }
    }
}
