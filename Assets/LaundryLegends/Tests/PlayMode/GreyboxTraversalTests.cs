using System.Collections;
using LaundryLegends.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    public sealed class GreyboxTraversalTests
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
        public IEnumerator PlayerWalksIntoRoomAndIsBlockedByMachineWall()
        {
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.IsNotNull(player, "Laundromat should contain a player.");

            yield return null;

            Vector3 start = player.transform.position;
            Assert.Greater(start.y, -0.1f, "Player should spawn above the floor.");

            bool foundFloor = false;
            RaycastHit[] hits = Physics.RaycastAll(start + Vector3.up * 2f, Vector3.down, 3f);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject != player.gameObject && hit.point.y <= 0.2f)
                {
                    foundFloor = true;
                    break;
                }
            }

            Assert.IsTrue(foundFloor, "Player spawn should have floor support.");

            float elapsed = 0f;
            float minimumY = start.y;
            while (elapsed < 2.6f)
            {
                player.Move(Vector2.up);
                yield return null;
                elapsed += Time.deltaTime;
                minimumY = Mathf.Min(minimumY, player.transform.position.y);
            }

            Assert.Greater(minimumY, -0.5f, "Player fell through geometry while walking forward.");

            Assert.Greater(player.transform.position.z, start.z + 2f,
                "Player should be able to walk into the room.");
            Assert.Less(player.transform.position.z, 5.1f,
                "The machine wall should block forward movement.");
        }

        [UnityTest]
        public IEnumerator PlayerCannotWalkThroughEastWall()
        {
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.IsNotNull(player, "Laundromat should contain a player.");

            player.transform.position = new Vector3(0f, 0.1f, 0f);
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return null;

            float elapsed = 0f;
            float minimumY = player.transform.position.y;
            while (elapsed < 2.6f)
            {
                player.Move(Vector2.up);
                yield return null;
                elapsed += Time.deltaTime;
                minimumY = Mathf.Min(minimumY, player.transform.position.y);
            }

            Assert.Greater(minimumY, -0.5f,
                "Player fell through geometry while walking toward the east wall.");
            Assert.Less(player.transform.position.x, 8.0f,
                "The east wall should block the player.");
        }

        [UnityTest]
        public IEnumerator PlayerCanEnterAndLeaveThroughTheEntrance()
        {
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.IsNotNull(player, "Laundromat should contain a player.");

            // Start outside the room, facing the storefront, and walk in.
            player.transform.position = new Vector3(0f, 0.1f, -7.2f);
            player.transform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            yield return null;

            float elapsed = 0f;
            while (elapsed < 1.4f)
            {
                player.Move(Vector2.up);
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.Greater(player.transform.position.z, -5.5f,
                "Player should be able to enter through the storefront entrance.");
            Assert.Greater(player.transform.position.y, -0.5f,
                "Player should not fall through the exterior ground while entering.");

            // Turn around and walk back out.
            player.transform.position = new Vector3(0f, 0.1f, -4.5f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Physics.SyncTransforms();
            yield return null;

            elapsed = 0f;
            while (elapsed < 1.4f)
            {
                player.Move(Vector2.up);
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.Less(player.transform.position.z, -6.0f,
                "Player should be able to leave through the storefront entrance.");
            Assert.Greater(player.transform.position.y, -0.5f,
                "Player should not fall through the exterior ground while leaving.");
        }

        [UnityTest]
        public IEnumerator PublicZonesArePresentAndClear()
        {
            yield return null;

            GameObject room = GameObject.Find("GreyboxRoom");
            Assert.IsNotNull(room, "Laundromat should contain the greybox room.");

            string[] required =
            {
                "WaitingArea/Waiting_Chair_1",
                "WaitingArea/Waiting_Chair_2",
                "WaitingArea/Waiting_SideTable",
                "StorageWest/Storage_Shelf_1",
                "StorageWest/Storage_Shelf_2",
                "UtilityAlcove/Utility_Divider",
                "UtilityAlcove/Utility_Cabinet",
                "Signage/Sign_MachineWall",
                "Signage/Sign_Counter"
            };

            foreach (string path in required)
            {
                Transform target = room.transform.Find(path);
                Assert.IsNotNull(target, $"Missing public-zone object: {path}.");
                Assert.IsNotNull(target.GetComponentInChildren<Collider>(true),
                    $"Public-zone object has no collider: {path}.");
            }

            Assert.IsNotNull(room.transform.Find("FrontageRoot/Facade_SignPanel"),
                "Storefront frontage should have a mounted fascia sign.");

            TextMesh machineSign = room.transform
                .Find("Signage/Sign_MachineWall")
                .GetComponentInChildren<TextMesh>(true);
            Assert.IsNotNull(machineSign, "Machine-wall sign should have readable text.");
            StringAssert.Contains("WASH", machineSign.text);

            Assert.IsFalse(
                Physics.Linecast(new Vector3(0f, 1.1f, -4.1f), new Vector3(0f, 1.1f, 4.3f)),
                "Public props should not block the central aisle.");
            Assert.IsFalse(
                Physics.Linecast(new Vector3(0f, 1.1f, -4.1f), new Vector3(-2.7f, 1.1f, -2.4f)),
                "Public props should not block the entrance-to-counter route.");
        }

        [UnityTest]
        public IEnumerator SecretDoorIsPresentAndClosed()
        {
            yield return null;

            GameObject door = GameObject.Find("SecretDoor_Foreshadowed");
            Assert.IsNotNull(door, "The foreshadowed secret door should exist.");
            Collider collider = door.GetComponent<Collider>();
            Assert.IsNotNull(collider, "The secret door needs a collider to stay closed.");
            Assert.IsTrue(collider.enabled, "The secret door collider should be enabled.");

            Transform counter = GameObject.Find("Counter")?.transform;
            Transform machine = GameObject.Find("Machine_1")?.transform;
            Assert.IsNotNull(counter, "Counter should exist.");
            Assert.IsNotNull(machine, "Machine wall should exist.");
            Assert.AreEqual(1.1f, counter.lossyScale.y, 0.02f,
                "Counter height should read as a person-scale counter.");
            Assert.AreEqual(1.1f, machine.lossyScale.y, 0.02f,
                "Washer height should read as a person-scale machine.");
        }

        [UnityTest]
        public IEnumerator SecretDoorIsConcealedButForeshadowed()
        {
            yield return null;

            GameObject cover = GameObject.Find("SecretAccess");
            Assert.IsNotNull(cover, "The secret access should have a service cover.");

            GameObject door = GameObject.Find("SecretDoor_Foreshadowed");
            Transform shelf = cover.transform.Find("SecretAccess_Shelf");
            Transform panel = cover.transform.Find("SecretAccess_ServicePanel");
            Transform marker = cover.transform.Find("SecretAccess_Marker");
            Assert.IsNotNull(door, "Secret door should exist.");
            Assert.IsNotNull(shelf, "Secret access should have a supply shelf.");
            Assert.IsNotNull(panel, "Secret access should have a service panel.");
            Assert.IsNotNull(marker, "Secret access should have a foreshadowing marker.");

            Assert.IsNotNull(shelf.GetComponentInChildren<Collider>(true), "Shelf should block the access.");
            Assert.IsNotNull(panel.GetComponentInChildren<Collider>(true), "Service panel should block the access.");

            Bounds doorBounds = GetBounds(door);
            Bounds shelfBounds = GetBounds(shelf.gameObject);
            Assert.Less(shelfBounds.min.x, doorBounds.min.x, "Shelf should sit in front of the door.");
            Assert.LessOrEqual(shelfBounds.min.z, doorBounds.min.z + 0.05f,
                "Shelf should cover the left side of the door.");
            Assert.GreaterOrEqual(shelfBounds.max.z, doorBounds.max.z - 0.05f,
                "Shelf should cover the right side of the door.");
            Assert.Less(shelfBounds.max.y, doorBounds.max.y - 0.05f,
                "Shelf should leave the top of the door readable.");

            TextMesh markerText = marker.GetComponentInChildren<TextMesh>(true);
            Assert.IsNotNull(markerText, "Service marker should use readable text.");
            StringAssert.Contains("SERVICE", markerText.text);

            Assert.IsTrue(
                Physics.Raycast(new Vector3(6f, 1.2f, 3f), Vector3.right, out RaycastHit hit, 3f),
                "The aisle-to-door path should hit the concealment.");
            Assert.IsTrue(hit.collider.transform.IsChildOf(cover.transform),
                "The first thing between the aisle and door should be the cover, not the door.");
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

        [UnityTest]
        public IEnumerator PublicNavigationAndCameraClearanceAreValid()
        {
            yield return null;

            GameObject room = GameObject.Find("GreyboxRoom");
            Assert.IsNotNull(room, "Laundromat should contain the greybox room.");

            AssertPathClear("entrance-to-counter", new Vector3(0f, 1.1f, -4.1f), new Vector3(-2.7f, 1.1f, -2.4f));
            AssertPathClear("central aisle", new Vector3(0f, 1.1f, -3.8f), new Vector3(0f, 1.1f, 3.8f));
            AssertPathClear("machine aisle", new Vector3(0f, 1.1f, 3.8f), new Vector3(5.8f, 1.1f, 3.8f));
            AssertPathClear("east circulation", new Vector3(6.4f, 1.1f, 3.8f), new Vector3(6.7f, 1.1f, -3.6f));
            AssertPathClear("waiting-to-central", new Vector3(6.7f, 1.1f, -3.6f), new Vector3(0f, 1.1f, -3.6f));
            AssertPathClear("central-to-entrance", new Vector3(0f, 1.1f, -3.6f), new Vector3(0f, 1.1f, -4.1f));
            AssertPathClear("counter-to-entrance", new Vector3(-2.7f, 1.1f, -2.4f), new Vector3(0f, 1.1f, -4.1f));

            string[] critical =
            {
                "Counter",
                "FoldingTable",
                "Machine_1",
                "WaitingArea/Waiting_Chair_1",
                "WaitingArea/Waiting_Chair_2",
                "WaitingArea/Waiting_SideTable",
                "StorageWest/Storage_Shelf_1",
                "StorageWest/Storage_Shelf_2",
                "UtilityAlcove/Utility_Divider",
                "UtilityAlcove/Utility_Cabinet",
                "SecretAccess/SecretAccess_ServicePanel",
                "SecretAccess/SecretAccess_Shelf"
            };

            foreach (string path in critical)
            {
                Transform target = room.transform.Find(path);
                Assert.IsNotNull(target, $"Critical object is missing: {path}.");
                Assert.IsNotNull(target.GetComponentInChildren<Collider>(true), $"Critical object has no collider: {path}.");
            }

            foreach (Renderer renderer in room.GetComponentsInChildren<Renderer>(true))
            {
                Assert.IsNotNull(renderer.sharedMaterial, $"Renderer has no material: {renderer.gameObject.name}.");
            }

            Vector3[] samples =
            {
                new Vector3(0f, 0f, -3.8f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 3.6f),
                new Vector3(-2.6f, 0f, -2.3f),
                new Vector3(2f, 0f, -3.6f),
                new Vector3(5.8f, 0f, 3.6f),
                new Vector3(6.4f, 0f, -3.6f),
                new Vector3(4.5f, 0f, 3.6f)
            };

            float[] eyeHeights = { 1.5f, 1.7f };
            foreach (Vector3 sample in samples)
            {
                foreach (float eyeHeight in eyeHeights)
                {
                    Vector3 eye = new Vector3(sample.x, eyeHeight, sample.z);
                    Assert.IsFalse(Physics.CheckSphere(eye, 0.2f, ~0, QueryTriggerInteraction.Ignore),
                        $"Camera clearance is blocked at ({sample.x:0.0}, {eyeHeight:0.0}, {sample.z:0.0}).");
                }
            }
        }

        private static void AssertPathClear(string label, Vector3 start, Vector3 end)
        {
            if (Physics.Linecast(start, end, out RaycastHit hit))
            {
                Assert.Fail($"{label} is blocked by {hit.collider.gameObject.name} at {hit.distance:0.00} m.");
            }
        }

        [UnityTest]
        public IEnumerator RoomHasPointAndDirectionalLighting()
        {
            yield return null;

            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            int pointLights = 0;
            bool hasDirectional = false;
            foreach (Light light in lights)
            {
                if (light.type == LightType.Point)
                {
                    pointLights++;
                }
                else if (light.type == LightType.Directional)
                {
                    hasDirectional = true;
                }
            }

            Assert.GreaterOrEqual(pointLights, 4, "Room should have at least four ceiling point lights.");
            Assert.IsTrue(hasDirectional, "Room should have a directional fill light.");
        }
    }
}
