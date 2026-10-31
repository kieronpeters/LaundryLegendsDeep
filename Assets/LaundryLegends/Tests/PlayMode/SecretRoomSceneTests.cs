using System.Collections;
using LaundryLegends.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// Smoke test for the secret-room scene wiring: starts hidden, cannot be
    /// revealed early, reveals once eligible, and can be prepared to usable.
    /// </summary>
    public sealed class SecretRoomSceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SecretRoomIsGatedThenRevealedAndPrepared()
        {
            SecretRoomController controller = Object.FindFirstObjectByType<SecretRoomController>();
            Assert.IsNotNull(controller, "Laundromat should contain the secret room controller.");
            Assert.AreEqual(SecretRoomStage.Hidden, controller.Stage);
            Assert.IsFalse(controller.TryReveal(), "Reveal must fail before the threshold.");

            controller.TryCollectClue();
            controller.TryCollectClue();
            controller.TryCollectClue();
            controller.RecordCompletedOrder();
            controller.RecordCompletedOrder();

            Assert.IsFalse(controller.TryReveal(), "Two orders is not enough.");

            controller.RecordCompletedOrder();
            Assert.IsTrue(controller.IsEligible);
            Assert.IsTrue(controller.TryReveal());
            yield return null;
            Assert.AreEqual(SecretRoomStage.Revealed, controller.Stage);

            Assert.IsTrue(controller.TryPrepare());
            Assert.IsTrue(controller.TryPrepare());
            Assert.IsTrue(controller.TryPrepare());
            Assert.IsTrue(controller.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Usable, controller.Stage);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SecretRoomStartsWithLightsAndContentDisabled()
        {
            SecretRoomController controller = Object.FindFirstObjectByType<SecretRoomController>();
            Assert.IsNotNull(controller);

            var root = GameObject.Find("SecretRoomSystem");
            Assert.IsNotNull(root, "SecretRoomSystem should exist in the scene.");
            Transform lights = root.transform.Find("RoomLights");
            Transform content = root.transform.Find("RoomContent");
            Assert.IsNotNull(lights, "RoomLights should exist under SecretRoomSystem.");
            Assert.IsNotNull(content, "RoomContent should exist under SecretRoomSystem.");
            Assert.IsFalse(lights.gameObject.activeSelf, "Lights stay off until powered.");
            Assert.IsFalse(content.gameObject.activeSelf, "Room content stays hidden until usable.");

            yield return null;
        }
    }
}
