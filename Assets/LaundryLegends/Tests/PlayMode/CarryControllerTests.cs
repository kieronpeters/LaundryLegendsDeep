using System.Collections;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    public sealed class CarryControllerTests
    {
        [UnitySetUp]
        public IEnumerator LoadLaundromat()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [TearDown]
        public void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator PickupAttachesItemAndRejectsASEcondItem()
        {
            CarryController carry = FindCarryController();
            LaundryItem first = CreateItem(carry, "loose-a", LaundryItemKind.LooseLaundry);
            LaundryItem second = CreateItem(carry, "loose-b", LaundryItemKind.LooseLaundry);
            yield return null;

            Assert.IsTrue(carry.TryPickUp(first));
            Assert.AreSame(first, carry.HeldItem);
            Assert.AreEqual(carry.HoldAnchor, first.transform.parent);
            Assert.IsFalse(first.GetComponent<Collider>().enabled, "Held colliders must be disabled.");
            Assert.IsTrue(first.GetComponent<Rigidbody>().isKinematic);
            Assert.IsFalse(first.GetComponent<Rigidbody>().useGravity);

            Assert.IsFalse(carry.TryPickUp(second), "Only one item may be held.");
            Assert.AreSame(first, carry.HeldItem);

            Assert.IsTrue(carry.TryDrop());
            DestroyItem(first);
            DestroyItem(second);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DropRestoresWorldStateAndClearsTheHeldItem()
        {
            CarryController carry = FindCarryController();
            LaundryItem item = CreateItem(carry, "loose", LaundryItemKind.LooseLaundry, false, true);
            yield return null;

            Assert.IsTrue(carry.TryPickUp(item));
            Assert.IsTrue(carry.TryDrop());

            Assert.IsTrue(carry.HeldItem == null);
            Assert.IsFalse(carry.IsCarrying);
            Assert.IsNull(item.transform.parent);
            Assert.IsTrue(item.GetComponent<Collider>().enabled, "Dropped colliders must be restored.");
            Assert.IsFalse(item.GetComponent<Rigidbody>().isKinematic, "The original rigidbody mode must be restored.");
            Assert.IsTrue(item.GetComponent<Rigidbody>().useGravity);
            Assert.Less(Vector3.Distance(item.transform.position, carry.transform.position), 2f);

            DestroyItem(item);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacementSucceedsIntoEmptyCompatibleSlot()
        {
            CarryController carry = FindCarryController();
            CarrySlot slot = CreateSlot();
            slot.SetAcceptedKinds(LaundryItemKind.LooseLaundry);
            LaundryItem item = CreateItem(carry, "loose", LaundryItemKind.LooseLaundry);
            yield return null;

            Assert.IsTrue(carry.TryPickUp(item));
            Assert.IsTrue(carry.TryPlaceInto(slot));

            Assert.IsTrue(carry.HeldItem == null);
            Assert.AreSame(item, slot.Occupant);
            Assert.AreEqual(slot.Anchor, item.transform.parent);
            Assert.IsTrue(item.GetComponent<Rigidbody>().isKinematic);

            Assert.AreSame(item, slot.Take());
            Assert.IsTrue(slot.IsEmpty);
            Assert.IsNull(item.transform.parent);

            DestroyItem(item);
            Object.Destroy(slot.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacementRejectsAnOccupiedSlot()
        {
            CarryController carry = FindCarryController();
            CarrySlot slot = CreateSlot();
            slot.SetAcceptedKinds(LaundryItemKind.LooseLaundry);
            LaundryItem first = CreateItem(carry, "loose-a", LaundryItemKind.LooseLaundry);
            LaundryItem second = CreateItem(carry, "loose-b", LaundryItemKind.LooseLaundry);
            yield return null;

            Assert.IsTrue(carry.TryPickUp(first));
            Assert.IsTrue(carry.TryPlaceInto(slot));

            Assert.IsTrue(carry.TryPickUp(second));
            Assert.IsFalse(carry.TryPlaceInto(slot), "An occupied slot must reject placement.");
            Assert.AreSame(second, carry.HeldItem);
            Assert.AreSame(first, slot.Occupant);

            Assert.IsTrue(carry.TryDrop());
            slot.Take();
            DestroyItem(first);
            DestroyItem(second);
            Object.Destroy(slot.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacementRejectsAnIncompatibleKind()
        {
            CarryController carry = FindCarryController();
            CarrySlot slot = CreateSlot();
            slot.SetAcceptedKinds(LaundryItemKind.LooseLaundry);
            LaundryItem basket = CreateItem(carry, "basket", LaundryItemKind.Basket);
            yield return null;

            Assert.IsTrue(carry.TryPickUp(basket));
            Assert.IsFalse(carry.TryPlaceInto(slot));
            Assert.AreSame(basket, carry.HeldItem, "A rejected placement must not release the item.");
            Assert.IsTrue(slot.IsEmpty);

            Assert.IsTrue(carry.TryDrop());
            DestroyItem(basket);
            Object.Destroy(slot.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyedHeldItemDoesNotLeaveAStuckState()
        {
            CarryController carry = FindCarryController();
            LaundryItem item = CreateItem(carry, "loose", LaundryItemKind.LooseLaundry);
            yield return null;

            Assert.IsTrue(carry.TryPickUp(item));
            Object.Destroy(item.gameObject);
            yield return null;
            yield return null;

            Assert.IsTrue(carry.HeldItem == null);
            Assert.IsFalse(carry.IsCarrying);

            LaundryItem replacement = CreateItem(carry, "loose-2", LaundryItemKind.LooseLaundry);
            yield return null;
            Assert.IsTrue(carry.TryPickUp(replacement), "The controller must recover after a held item is destroyed.");
            carry.TryDrop();
            DestroyItem(replacement);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisablingTheControllerDropsTheHeldItem()
        {
            CarryController carry = FindCarryController();
            LaundryItem item = CreateItem(carry, "loose", LaundryItemKind.LooseLaundry);
            yield return null;

            Assert.IsTrue(carry.TryPickUp(item));
            carry.enabled = false;
            yield return null;

            Assert.IsTrue(carry.HeldItem == null);
            Assert.IsNull(item.transform.parent, "Disabling carry must release the item.");
            Assert.IsTrue(item.GetComponent<Collider>().enabled);

            carry.enabled = true;
            DestroyItem(item);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BlockedDropUsesANearbySafePoint()
        {
            CarryController carry = FindCarryController();
            LaundryItem item = CreateItem(carry, "loose", LaundryItemKind.LooseLaundry);
            yield return null;

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "DropBlocker";
            wall.transform.localScale = new Vector3(2f, 2f, 0.2f);
            wall.transform.rotation = Quaternion.LookRotation(-carry.transform.forward, Vector3.up);
            wall.transform.position = carry.transform.position + carry.transform.forward * 1.1f + Vector3.up * 0.8f;
            Physics.SyncTransforms();
            yield return null;

            Assert.IsTrue(carry.TryPickUp(item));
            Assert.IsTrue(carry.TryDrop());

            Vector3 offset = item.transform.position - carry.transform.position;
            offset.y = 0f;
            Assert.Less(offset.magnitude, 1f, "A blocked drop must use a safe point closer than the blocker.");
            Assert.Greater(offset.magnitude, 0.05f, "A blocked drop must not sit exactly inside the player.");

            DestroyItem(item);
            Object.Destroy(wall);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DoubleInteractionDoesNotDuplicateTheItem()
        {
            CarryController carry = FindCarryController();
            LaundryItem item = CreateItem(carry, "loose", LaundryItemKind.LooseLaundry);
            yield return null;

            Assert.IsTrue(carry.TryPickUp(item));
            Assert.IsFalse(carry.TryPickUp(item), "A second pickup in the same frame must be rejected.");
            Assert.AreSame(item, carry.HeldItem);

            Assert.IsTrue(carry.TryDrop());
            Assert.IsFalse(carry.TryDrop(), "A second drop must be rejected.");

            DestroyItem(item);
            yield return null;
        }

        private static CarryController FindCarryController()
        {
            CarryController carry = Object.FindFirstObjectByType<CarryController>();
            Assert.IsNotNull(carry, "The Player prefab must have a CarryController.");
            Assert.IsTrue(carry.HasHoldAnchor, "The CarryController must have an explicit hold anchor.");
            return carry;
        }

        private static LaundryItem CreateItem(
            CarryController carry,
            string templateId,
            LaundryItemKind kind,
            bool kinematic = true,
            bool useGravity = false)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = templateId;
            target.transform.position = carry.transform.position + carry.transform.forward * 0.8f + Vector3.up * 0.5f;
            target.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            Rigidbody body = target.AddComponent<Rigidbody>();
            body.isKinematic = kinematic;
            body.useGravity = useGravity;

            LaundryItem item = target.AddComponent<LaundryItem>();
            item.Initialize(templateId, kind);
            return item;
        }

        private static CarrySlot CreateSlot()
        {
            var target = new GameObject("TestCarrySlot");
            return target.AddComponent<CarrySlot>();
        }

        private static void DestroyItem(LaundryItem item)
        {
            if (item != null)
            {
                Object.Destroy(item.gameObject);
            }
        }
    }
}
