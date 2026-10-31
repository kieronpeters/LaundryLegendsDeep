using System.Collections;
using LaundryLegends.NPC;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    public sealed class AmbientNpcTests
    {
        [UnitySetUp]
        public IEnumerator LoadLaundromat()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator StreetCustomerHasRouteCarriedLaundryAndVoice()
        {
            AmbientNpc customer = Object.FindFirstObjectByType<AmbientNpc>();
            Assert.IsNotNull(customer, "Laundromat should contain the ambient street customer.");
            Assert.GreaterOrEqual(customer.WaypointCount, 4, "Customer needs an enter/leave route.");
            Assert.IsNotNull(customer.CarriedProp, "Customer should carry laundry.");
            Assert.IsNotNull(customer.Voice, "Customer should have a voice source.");

            Vector3 start = customer.transform.position;
            float elapsed = 0f;
            while (elapsed < 1.5f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.Greater(Vector3.Distance(start, customer.transform.position), 0.1f,
                "Customer should move along the route.");
        }
    }
}
