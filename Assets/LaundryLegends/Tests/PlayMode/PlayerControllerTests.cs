using System.Collections;
using LaundryLegends.Interaction;
using LaundryLegends.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    public sealed class PlayerControllerTests
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
        public IEnumerator PlayerMovesAndLooks()
        {
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.IsNotNull(player, "Laundromat should contain a player.");

            yield return null;

            Vector3 startPosition = player.transform.position;
            Vector3 forward = player.transform.forward;

            // Batch PlayMode runs at an uncapped frame rate, so one frame is not
            // a reliable amount of movement. Accumulate a short fixed duration.
            float elapsed = 0f;
            while (elapsed < 0.1f)
            {
                player.Move(new Vector2(0f, 1f));
                yield return null;
                elapsed += Time.deltaTime;
            }

            Vector3 displacement = player.transform.position - startPosition;
            Assert.Greater(Vector3.Dot(displacement, forward), 0.05f,
                "Forward input should move the player forward.");

            float yawBefore = player.transform.eulerAngles.y;
            player.Look(new Vector2(20f, 0f));
            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(yawBefore, player.transform.eulerAngles.y));
            Assert.Greater(yawDelta, 1f, "Mouse X input should rotate the player.");
        }

        [UnityTest]
        public IEnumerator PauseStopsTimeAndResumeRestoresIt()
        {
            PauseController pause = Object.FindFirstObjectByType<PauseController>();
            Assert.IsNotNull(pause, "Laundromat should contain a PauseController.");

            pause.Pause();
            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);

            yield return null;

            pause.Resume();
            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator InteractionRaycastFindsTargetInRange()
        {
            PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "Laundromat should contain a PlayerInteractor.");
            Camera camera = interactor.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "Player should have a camera.");

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "TestInteractable";
            target.transform.position = camera.transform.position + camera.transform.forward * 2f;
            target.AddComponent<SimpleInteractable>();

            // The new collider must be synced before PlayerInteractor's next Update.
            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.AreEqual(target, interactor.CurrentTarget,
                "Raycast should report the cube as the current target.");
            Assert.AreEqual("Interact", interactor.CurrentPrompt);

            Object.Destroy(target);
            yield return null;
        }
    }
}
