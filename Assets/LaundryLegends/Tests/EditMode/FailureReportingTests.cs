using System;
using NUnit.Framework;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// Verifies that the batch test runner reports a deliberate failure as failed.
    /// Set LL_FAIL_PROBE=1 before launching Unity to activate it. The normal suite
    /// leaves the probe inactive so ordinary runs stay green.
    /// </summary>
    public sealed class FailureReportingTests
    {
        [Test]
        public void DeliberateFailureProbe()
        {
            string probe = Environment.GetEnvironmentVariable("LL_FAIL_PROBE");
            if (string.IsNullOrEmpty(probe))
            {
                Assert.Pass("Failure probe inactive. Set LL_FAIL_PROBE=1 to verify failure reporting.");
            }

            Assert.Fail($"Deliberate failure probe active: {probe}");
        }
    }
}
