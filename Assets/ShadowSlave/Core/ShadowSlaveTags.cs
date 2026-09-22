namespace ShadowSlave.Core
{
    /// <summary>
    /// Centralised known gameplay tags matching UE ShadowSlaveGameplayTags.
    /// Generic technical categories only — no canon content.
    /// </summary>
    public static class ShadowSlaveTags
    {
        /* --- State --- */
        public static readonly GameplayTag State = new GameplayTag("State");
        public static readonly GameplayTag State_Active = new GameplayTag("State.Active");
        public static readonly GameplayTag State_Disabled = new GameplayTag("State.Disabled");
        public static readonly GameplayTag State_Pending = new GameplayTag("State.Pending");

        /* --- Event --- */
        public static readonly GameplayTag Event = new GameplayTag("Event");
        public static readonly GameplayTag Event_Trigger = new GameplayTag("Event.Trigger");
        public static readonly GameplayTag Event_Complete = new GameplayTag("Event.Complete");

        /* --- Ability --- */
        public static readonly GameplayTag Ability = new GameplayTag("Ability");
        public static readonly GameplayTag Ability_Action = new GameplayTag("Ability.Action");

        /* --- Combat --- */
        public static readonly GameplayTag Combat = new GameplayTag("Combat");
        public static readonly GameplayTag Combat_Engaged = new GameplayTag("Combat.Engaged");

        /* --- Interaction --- */
        public static readonly GameplayTag Interaction = new GameplayTag("Interaction");
        public static readonly GameplayTag Interaction_Interactable = new GameplayTag("Interaction.Interactable");
    }
}
