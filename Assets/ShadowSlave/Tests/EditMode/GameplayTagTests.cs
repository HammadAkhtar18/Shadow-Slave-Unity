using NUnit.Framework;
using ShadowSlave.Core;

namespace ShadowSlave.Tests.EditMode
{
    public class GameplayTagTests
    {
        [Test]
        public void ExactTag_MatchesItself()
        {
            var tag = new GameplayTag("State.Active");
            Assert.IsTrue(tag.Matches(new GameplayTag("State.Active")));
            Assert.IsTrue(tag.Equals(ShadowSlaveTags.State_Active));
        }

        [Test]
        public void ChildTag_MatchesParentQuery()
        {
            var child = ShadowSlaveTags.State_Active;
            Assert.IsTrue(child.Matches(ShadowSlaveTags.State));
            Assert.IsFalse(ShadowSlaveTags.State.Matches(ShadowSlaveTags.State_Active));
        }

        [Test]
        public void Container_HasTag_Hierarchical()
        {
            var container = new GameplayTagContainer();
            container.Add(ShadowSlaveTags.Combat_Engaged);

            Assert.IsTrue(container.HasTag(ShadowSlaveTags.Combat_Engaged));
            Assert.IsTrue(container.HasTag(ShadowSlaveTags.Combat));
            Assert.IsFalse(container.HasTag(ShadowSlaveTags.State_Active));
        }

        [Test]
        public void Container_HasAny_And_HasAll()
        {
            var container = new GameplayTagContainer();
            container.Add(ShadowSlaveTags.State_Active);
            container.Add(ShadowSlaveTags.Event_Trigger);

            Assert.IsTrue(container.HasAny(ShadowSlaveTags.State_Disabled, ShadowSlaveTags.State_Active));
            Assert.IsFalse(container.HasAny(ShadowSlaveTags.Ability_Action));

            Assert.IsTrue(container.HasAll(ShadowSlaveTags.State, ShadowSlaveTags.Event_Trigger));
            Assert.IsFalse(container.HasAll(ShadowSlaveTags.State_Active, ShadowSlaveTags.Combat_Engaged));
        }

        [Test]
        public void Container_AddRemove_Idempotent()
        {
            var container = new GameplayTagContainer();
            container.Add("Interaction.Interactable");
            container.Add(ShadowSlaveTags.Interaction_Interactable);
            Assert.AreEqual(1, container.Count);

            Assert.IsTrue(container.Remove(ShadowSlaveTags.Interaction_Interactable));
            Assert.AreEqual(0, container.Count);
            Assert.IsFalse(container.Remove(ShadowSlaveTags.Interaction_Interactable));
        }

        [Test]
        public void KnownTags_Match_UE_ShadowSlaveGameplayTags()
        {
            Assert.AreEqual("State", ShadowSlaveTags.State.Value);
            Assert.AreEqual("State.Active", ShadowSlaveTags.State_Active.Value);
            Assert.AreEqual("State.Disabled", ShadowSlaveTags.State_Disabled.Value);
            Assert.AreEqual("State.Pending", ShadowSlaveTags.State_Pending.Value);
            Assert.AreEqual("Event", ShadowSlaveTags.Event.Value);
            Assert.AreEqual("Event.Trigger", ShadowSlaveTags.Event_Trigger.Value);
            Assert.AreEqual("Event.Complete", ShadowSlaveTags.Event_Complete.Value);
            Assert.AreEqual("Ability", ShadowSlaveTags.Ability.Value);
            Assert.AreEqual("Ability.Action", ShadowSlaveTags.Ability_Action.Value);
            Assert.AreEqual("Combat", ShadowSlaveTags.Combat.Value);
            Assert.AreEqual("Combat.Engaged", ShadowSlaveTags.Combat_Engaged.Value);
            Assert.AreEqual("Interaction", ShadowSlaveTags.Interaction.Value);
            Assert.AreEqual("Interaction.Interactable", ShadowSlaveTags.Interaction_Interactable.Value);
        }
    }
}
