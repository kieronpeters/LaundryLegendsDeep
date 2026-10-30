using LaundryLegends.Interaction;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LaundryLegends.Tests.EditMode
{
    public sealed class InteractionEligibilityTests
    {
        private GameObject _target;

        [TearDown]
        public void DestroyTarget()
        {
            if (_target != null)
            {
                Object.DestroyImmediate(_target);
            }
        }

        [Test]
        public void SimpleInteractableIsEligibleByDefault()
        {
            SimpleInteractable interactable = CreateInteractable();

            Assert.IsTrue(interactable.CanInteract(_target));
            Assert.AreEqual("Interact", interactable.InteractionPrompt);
        }

        [Test]
        public void SimpleInteractableIsIneligibleWhenFlagCleared()
        {
            SimpleInteractable interactable = CreateInteractable();

            interactable.SetInteractable(false);

            Assert.IsFalse(interactable.CanInteract(_target));
        }

        [Test]
        public void SimpleInteractableIsIneligibleWhenComponentDisabled()
        {
            SimpleInteractable interactable = CreateInteractable();

            interactable.enabled = false;

            Assert.IsFalse(interactable.CanInteract(_target));
        }

        [Test]
        public void InteractRaisesEventOnlyWhenEligible()
        {
            SimpleInteractable interactable = CreateInteractable();
            int interactions = 0;
            interactable.Interacted += _ => interactions++;

            interactable.Interact(_target);
            Assert.AreEqual(1, interactions, "An eligible interaction should raise the event once.");

            interactable.SetInteractable(false);
            interactable.Interact(_target);
            Assert.AreEqual(1, interactions, "An ineligible interaction must not raise the event.");
        }

        [Test]
        public void InteractDoesNotRaiseEventWhenComponentDisabled()
        {
            SimpleInteractable interactable = CreateInteractable();
            int interactions = 0;
            interactable.Interacted += _ => interactions++;

            interactable.enabled = false;
            interactable.Interact(_target);

            Assert.AreEqual(0, interactions);
        }

        private SimpleInteractable CreateInteractable()
        {
            _target = new GameObject("TestInteractable");
            return _target.AddComponent<SimpleInteractable>();
        }
    }
}
