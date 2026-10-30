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

        [UnityTest]
        public IEnumerator InteractionExposesPromptAndDispatchesThroughTryInteract()
        {
            PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "Laundromat should contain a PlayerInteractor.");
            Camera camera = interactor.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "Player should have a camera.");

            GameObject target = CreateTarget(camera, 2f);
            SimpleInteractable simple = target.AddComponent<SimpleInteractable>();
            int interactions = 0;
            simple.Interacted += _ => interactions++;

            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.AreSame(simple, interactor.CurrentInteractable);
            Assert.AreEqual(target, interactor.CurrentTarget);
            Assert.AreEqual("Interact", interactor.CurrentPrompt);
            Assert.AreEqual(KeyCode.E, interactor.InteractKey);
            Assert.AreEqual("E", interactor.InteractKeyLabel);
            Assert.IsTrue(interactor.TryInteract(), "An eligible target should dispatch.");
            Assert.AreEqual(1, interactions);

            Object.Destroy(target);
            yield return null;
            yield return null;

            Assert.IsNull(interactor.CurrentInteractable, "Destroyed targets should clear.");
            Assert.IsNull(interactor.CurrentTarget);
            Assert.IsNull(interactor.CurrentPrompt);
        }

        [UnityTest]
        public IEnumerator TargetChangedNotificationFiresWhenTargetAppearsAndClears()
        {
            PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "Laundromat should contain a PlayerInteractor.");
            Camera camera = interactor.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "Player should have a camera.");

            int changes = 0;
            interactor.TargetChanged += () => changes++;

            GameObject target = CreateTarget(camera, 2f);
            target.AddComponent<SimpleInteractable>();

            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.GreaterOrEqual(changes, 1, "A new target should raise the change notification.");
            Assert.IsNotNull(interactor.CurrentTarget);

            int changesWhileTargeted = changes;
            Object.Destroy(target);
            yield return null;
            yield return null;

            Assert.Greater(changes, changesWhileTargeted, "Clearing a target should raise the change notification.");
            Assert.IsNull(interactor.CurrentTarget);
        }

        [UnityTest]
        public IEnumerator IneligibleInteractionIsNotReportedOrDispatched()
        {
            PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "Laundromat should contain a PlayerInteractor.");
            Camera camera = interactor.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "Player should have a camera.");

            GameObject target = CreateTarget(camera, 2f);
            SimpleInteractable simple = target.AddComponent<SimpleInteractable>();
            simple.SetInteractable(false);
            int interactions = 0;
            simple.Interacted += _ => interactions++;

            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.IsNull(interactor.CurrentInteractable, "Ineligible targets must not be reported.");
            Assert.IsNull(interactor.CurrentTarget);
            Assert.IsNull(interactor.CurrentPrompt);
            Assert.IsFalse(interactor.TryInteract(), "Ineligible targets must not dispatch.");
            Assert.AreEqual(0, interactions);

            Object.Destroy(target);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BlockedInteractionIsNotReportedOrDispatched()
        {
            PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "Laundromat should contain a PlayerInteractor.");
            Camera camera = interactor.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "Player should have a camera.");

            GameObject blocker = CreateTarget(camera, 1.5f);
            GameObject target = CreateTarget(camera, 2.5f);
            SimpleInteractable simple = target.AddComponent<SimpleInteractable>();
            int interactions = 0;
            simple.Interacted += _ => interactions++;

            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.IsNull(interactor.CurrentTarget, "A non-interactable collider should block the raycast.");
            Assert.IsFalse(interactor.TryInteract());
            Assert.AreEqual(0, interactions);

            Object.Destroy(blocker);
            Object.Destroy(target);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OutOfRangeInteractionIsNotReportedOrDispatched()
        {
            PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "Laundromat should contain a PlayerInteractor.");
            Camera camera = interactor.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "Player should have a camera.");

            GameObject target = CreateTarget(camera, 10f);
            SimpleInteractable simple = target.AddComponent<SimpleInteractable>();
            int interactions = 0;
            simple.Interacted += _ => interactions++;

            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.IsNull(interactor.CurrentTarget, "Targets beyond the interaction range must not be reported.");
            Assert.IsFalse(interactor.TryInteract());
            Assert.AreEqual(0, interactions);

            Object.Destroy(target);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PausedInteractionClearsTargetAndDoesNotDispatch()
        {
            PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "Laundromat should contain a PlayerInteractor.");
            Camera camera = interactor.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "Player should have a camera.");

            GameObject target = CreateTarget(camera, 2f);
            SimpleInteractable simple = target.AddComponent<SimpleInteractable>();
            int interactions = 0;
            simple.Interacted += _ => interactions++;

            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.IsNotNull(interactor.CurrentTarget, "The target should resolve before pausing.");

            Time.timeScale = 0f;
            yield return null;

            Assert.IsNull(interactor.CurrentTarget, "Pausing should clear the reported target.");
            Assert.IsNull(interactor.CurrentPrompt);
            Assert.IsFalse(interactor.TryInteract(), "Paused interactions must not dispatch.");
            Assert.AreEqual(0, interactions);

            Time.timeScale = 1f;
            Object.Destroy(target);
            yield return null;
        }

        private static GameObject CreateTarget(Camera camera, float distance)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "TestInteractable";
            target.transform.position = camera.transform.position + camera.transform.forward * distance;
            return target;
        }
    }
}
