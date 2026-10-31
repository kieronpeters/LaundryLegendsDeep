using LaundryLegends.Progression;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// Logic tests for the secret room controller, run on a bare GameObject with
    /// no scene references so the gating rules are proven independently of the room.
    /// </summary>
    public sealed class SecretRoomControllerTests
    {
        private GameObject _go;
        private SecretRoomController _controller;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SecretRoomProbe");
            _controller = _go.AddComponent<SecretRoomController>();
            _controller.Configure(requiredClues: 2, requiredOrders: 2);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void StartsHiddenAndIneligible()
        {
            Assert.AreEqual(SecretRoomStage.Hidden, _controller.Stage);
            Assert.IsFalse(_controller.IsEligible);
            Assert.IsFalse(_controller.IsRevealed);
            Assert.AreEqual(0, _controller.ClueCount);
        }

        [Test]
        public void RevealRequiresCluesAndOrders()
        {
            _controller.TryCollectClue();
            _controller.TryCollectClue();
            Assert.IsFalse(_controller.IsEligible, "Orders are still missing.");

            _controller.RecordCompletedOrder();
            Assert.IsFalse(_controller.IsEligible, "Only one order recorded.");

            _controller.RecordCompletedOrder();
            Assert.IsTrue(_controller.IsEligible);
            Assert.IsTrue(_controller.TryReveal());
            Assert.AreEqual(SecretRoomStage.Revealed, _controller.Stage);
        }

        [Test]
        public void RevealHappensOnlyOnce()
        {
            _controller.Configure(1, 1);
            _controller.TryCollectClue();
            _controller.RecordCompletedOrder();

            Assert.IsTrue(_controller.TryReveal());
            Assert.IsFalse(_controller.TryReveal());
        }

        [Test]
        public void CannotPrepareBeforeReveal()
        {
            Assert.IsFalse(_controller.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Hidden, _controller.Stage);
        }

        [Test]
        public void PreparationRunsInOrder()
        {
            _controller.Configure(1, 1);
            _controller.TryCollectClue();
            _controller.RecordCompletedOrder();
            _controller.TryReveal();

            Assert.IsTrue(_controller.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Cleaned, _controller.Stage);
            Assert.IsTrue(_controller.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Powered, _controller.Stage);
            Assert.IsTrue(_controller.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Repaired, _controller.Stage);
            Assert.IsTrue(_controller.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Usable, _controller.Stage);
            Assert.IsFalse(_controller.TryPrepare());
        }

        [Test]
        public void CluesStopAtTheRequiredCount()
        {
            _controller.Configure(2, 1);
            Assert.IsTrue(_controller.TryCollectClue());
            Assert.IsTrue(_controller.TryCollectClue());
            Assert.IsFalse(_controller.TryCollectClue());
            Assert.AreEqual(2, _controller.ClueCount);
        }

        [Test]
        public void SnapshotRestoresProgress()
        {
            _controller.Configure(1, 1);
            _controller.TryCollectClue();
            _controller.RecordCompletedOrder();
            _controller.TryReveal();
            _controller.TryPrepare();

            SecretRoomSnapshot snapshot = _controller.Snapshot();
            _controller.Restore(snapshot);

            Assert.AreEqual(SecretRoomStage.Cleaned, _controller.Stage);
            Assert.AreEqual(1, _controller.ClueCount);
        }
    }
}
