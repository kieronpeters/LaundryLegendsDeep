using System.Collections;
using LaundryLegends.Interaction;
using LaundryLegends.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    public sealed class InteractionPromptTests
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
        public IEnumerator PromptStartsHiddenWithoutTarget()
        {
            InteractionPromptView prompt = FindPrompt(out _, out _);

            yield return null;

            Assert.IsFalse(prompt.IsVisible, "No target should mean no prompt.");
            Assert.AreEqual(string.Empty, prompt.CurrentLabel);
        }

        [UnityTest]
        public IEnumerator PromptShowsPromptAndKeyForEligibleTarget()
        {
            InteractionPromptView prompt = FindPrompt(out PlayerInteractor interactor, out Camera camera);

            GameObject target = CreateTarget(camera, 2f);
            target.AddComponent<SimpleInteractable>();

            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.IsTrue(prompt.IsVisible, "An eligible target should show the prompt.");
            Assert.AreEqual(
                $"Interact [{interactor.InteractKeyLabel}]",
                prompt.CurrentLabel,
                "The label should combine the prompt with the interactor key label.");

            Object.Destroy(target);
            yield return null;
            yield return null;

            Assert.IsFalse(prompt.IsVisible, "Clearing the target should hide the prompt.");
            Assert.AreEqual(string.Empty, prompt.CurrentLabel);
        }

        [UnityTest]
        public IEnumerator PromptHidesWhenTargetBecomesIneligible()
        {
            InteractionPromptView prompt = FindPrompt(out _, out Camera camera);

            GameObject target = CreateTarget(camera, 2f);
            SimpleInteractable simple = target.AddComponent<SimpleInteractable>();

            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.IsTrue(prompt.IsVisible, "The prompt should be visible before eligibility changes.");

            simple.SetInteractable(false);
            yield return null;
            yield return null;

            Assert.IsFalse(prompt.IsVisible, "An ineligible target should hide the prompt.");
            Assert.AreEqual(string.Empty, prompt.CurrentLabel);

            Object.Destroy(target);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptHidesWhilePausedAndReturnsAfterResume()
        {
            InteractionPromptView prompt = FindPrompt(out _, out Camera camera);

            GameObject target = CreateTarget(camera, 2f);
            target.AddComponent<SimpleInteractable>();

            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.IsTrue(prompt.IsVisible, "The prompt should be visible before pausing.");

            Time.timeScale = 0f;
            yield return null;

            Assert.IsFalse(prompt.IsVisible, "Time.timeScale == 0 should hide the prompt.");
            Assert.AreEqual(string.Empty, prompt.CurrentLabel);

            Time.timeScale = 1f;
            yield return null;
            yield return null;

            Assert.IsTrue(prompt.IsVisible, "Resuming should restore the prompt for a still-valid target.");

            Object.Destroy(target);
            yield return null;
        }

        private static InteractionPromptView FindPrompt(
            out PlayerInteractor interactor,
            out Camera camera)
        {
            interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "Laundromat should contain a PlayerInteractor.");

            InteractionPromptView prompt = Object.FindFirstObjectByType<InteractionPromptView>();
            Assert.IsNotNull(prompt, "Laundromat should contain an InteractionPromptView.");
            Assert.AreSame(interactor, prompt.Interactor,
                "The prompt view should be wired to the player interactor.");

            camera = interactor.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "Player should have a camera.");
            return prompt;
        }

        private static GameObject CreateTarget(Camera camera, float distance)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "TestPromptInteractable";
            target.transform.position = camera.transform.position + camera.transform.forward * distance;
            return target;
        }
    }
}
