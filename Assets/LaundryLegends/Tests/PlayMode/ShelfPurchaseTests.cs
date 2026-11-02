using System.Collections;
using LaundryLegends.Customers;
using LaundryLegends.Player;
using LaundryLegends.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// Regression cover for the Phase 8 playtest blocker "the shelf never offers
    /// a prompt". The purchase point used to sit on the thin service panel while
    /// the rolling shelf stood in front of it with colliders that had no
    /// interactable, so looking at the shelf from the aisle hit nothing useful.
    /// <para>
    /// This test proves the player's view of the concealment resolves to the
    /// purchase point, then buys it through <see cref="IInteractable.Interact"/>
    /// and checks the whole cover is removed from the doorway.
    /// </para>
    /// </summary>
    public sealed class ShelfPurchaseTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShelfPurchaseIsReachableAndClearsTheDoorway()
        {
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            HiddenRoomPurchasePoint purchase = Object.FindFirstObjectByType<HiddenRoomPurchasePoint>();
            SecretRoomController room = Object.FindFirstObjectByType<SecretRoomController>();
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.IsNotNull(spawner, "The purchase point reads its wallet from the spawner.");
            Assert.IsNotNull(purchase, "The shelf must carry the HiddenRoomPurchasePoint.");
            Assert.IsNotNull(room, "The secret room controller should be in the scene.");
            Assert.IsNotNull(player, "The scene should contain the player.");
            Assert.AreEqual(200, purchase.Cost, "The shelf price should stay at 200.");

            GameObject cover = purchase.gameObject;
            Assert.AreEqual("SecretAccess", cover.name,
                "The purchase point belongs on the whole concealment group, not one panel.");
            Assert.IsTrue(cover.activeInHierarchy, "The shelf starts closed.");
            Assert.IsTrue(purchase.CanInteract(player.gameObject), "The closed shelf should be interactable.");
            StringAssert.Contains("$200", purchase.InteractionPrompt);

            // A natural eye-level look from the aisle must resolve to the purchase
            // point itself. This is the exact check that failed during playtest.
            Transform shelf = cover.transform.Find("SecretAccess_Shelf");
            Assert.IsNotNull(shelf, "The concealment should still have its rolling shelf.");

            // Anchor on the shelf itself, not the group root: the concealment
            // group sits under the door, while the shelf stands in the aisle.
            Transform anchor = shelf;
            foreach (Collider collider in cover.GetComponentsInChildren<Collider>(true))
            {
                if (collider.name == "SecretAccess_Shelf")
                {
                    anchor = collider.transform;
                    break;
                }
            }

            Vector3 target = new Vector3(anchor.position.x, 1.2f, anchor.position.z);
            Vector3 approach = new Vector3(anchor.position.x - 1.4f, 1.6f, anchor.position.z);
            var directRay = new Ray(approach, (target - approach).normalized);
            Assert.IsTrue(
                Physics.Raycast(directRay, out RaycastHit directHit, 3f, ~0, QueryTriggerInteraction.Ignore),
                $"Looking at the shelf from 1.4 m should hit the concealment at {target}.");
            Assert.AreSame(
                purchase,
                directHit.collider.GetComponentInParent<HiddenRoomPurchasePoint>(),
                $"Looking at the shelf hit '{directHit.collider.name}', which has no purchase point.");

            // Every collider inside the concealment must resolve to it, so no
            // shelf board can swallow the interaction ray.
            Bounds shelfBounds = GetBounds(shelf.gameObject);
            Vector3 shelfCenter = new Vector3(shelfBounds.center.x, 1.2f, shelfBounds.center.z);
            int coverHits = 0;
            for (float z = shelfBounds.center.z - 0.5f; z <= shelfBounds.center.z + 0.5f; z += 0.25f)
            {
                for (float y = 1.3f; y <= 1.9f; y += 0.2f)
                {
                    var eye = new Vector3(shelfBounds.min.x - 1.5f, y, z);
                    if (!Physics.Raycast(eye, (shelfCenter - eye).normalized, out RaycastHit hit, 3f, ~0,
                            QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    if (!hit.collider.transform.IsChildOf(cover.transform))
                    {
                        continue;
                    }

                    coverHits++;
                    Assert.AreSame(
                        purchase,
                        hit.collider.GetComponentInParent<HiddenRoomPurchasePoint>(),
                        $"Shelf collider '{hit.collider.name}' does not resolve to the purchase point.");
                }
            }

            Assert.Greater(coverHits, 0, "At least one aisle look should hit the concealment.");

            // Pay through the real interaction entry point.
            Assert.IsTrue(
                spawner.Economy.TryEarn(purchase.Cost + 100, "Shelf test float", Time.time),
                "The test needs money in the spawner's wallet to buy the shelf.");
            purchase.Interact(player.gameObject);

            Assert.IsTrue(purchase.IsPurchased, "Paying 200 should reveal the back room.");
            Assert.AreEqual(SecretRoomStage.Usable, room.Stage, "Buying the shelf should prepare the room.");
            Assert.IsFalse(cover.activeSelf, "Paying must remove the whole shelf so the doorway is clear.");
            Assert.IsFalse(purchase.CanInteract(player.gameObject), "A bought shelf is no longer interactable.");

            yield return null;

            // Nothing from the concealment may remain on the aisle-to-door path.
            if (Physics.Raycast(new Vector3(6f, 1.2f, 3f), Vector3.right, out RaycastHit after, 4f, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                Assert.IsFalse(
                    after.collider.transform.IsChildOf(cover.transform),
                    $"'{after.collider.name}' still blocks the doorway after purchase.");
            }

            yield return null;
        }

        private static Bounds GetBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            Assert.IsNotEmpty(renderers, $"{target.name} should have renderers.");

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }
    }
}
