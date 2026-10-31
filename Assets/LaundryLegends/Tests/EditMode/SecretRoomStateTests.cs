using LaundryLegends.Progression;
using NUnit.Framework;

namespace LaundryLegends.Tests.EditMode
{
    public sealed class SecretRoomStateTests
    {
        [Test]
        public void CannotRevealBeforeTheThreshold()
        {
            var state = new SecretRoomState(requiredClues: 3, requiredOrders: 3);
            state.TryAddClue();
            state.TryAddClue();

            Assert.IsFalse(state.IsEligible);
            Assert.IsFalse(state.TryReveal());
            Assert.AreEqual(SecretRoomStage.Hidden, state.Stage);
        }

        [Test]
        public void RevealsWhenCluesAndOrdersAreMet()
        {
            var state = new SecretRoomState(requiredClues: 2, requiredOrders: 2);
            state.TryAddClue();
            state.TryAddClue();
            state.RecordCompletedOrder();

            Assert.IsFalse(state.TryReveal(), "Orders are still short.");

            state.RecordCompletedOrder();
            Assert.IsTrue(state.IsEligible);
            Assert.IsTrue(state.TryReveal());
            Assert.AreEqual(SecretRoomStage.Revealed, state.Stage);
        }

        [Test]
        public void RevealHappensOnlyOnce()
        {
            var state = new SecretRoomState(requiredClues: 1, requiredOrders: 1);
            state.TryAddClue();
            state.RecordCompletedOrder();

            Assert.IsTrue(state.TryReveal());
            Assert.IsFalse(state.TryReveal());
        }

        [Test]
        public void PreparationIsSequential()
        {
            var state = new SecretRoomState(requiredClues: 1, requiredOrders: 1);
            state.TryAddClue();
            state.RecordCompletedOrder();
            state.TryReveal();

            Assert.IsTrue(state.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Cleaned, state.Stage);
            Assert.IsTrue(state.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Powered, state.Stage);
            Assert.IsTrue(state.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Repaired, state.Stage);
            Assert.IsTrue(state.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Usable, state.Stage);
            Assert.IsFalse(state.TryPrepare(), "Usable is the final stage.");
        }

        [Test]
        public void CannotPrepareBeforeReveal()
        {
            var state = new SecretRoomState();
            Assert.IsFalse(state.TryPrepare());
            Assert.AreEqual(SecretRoomStage.Hidden, state.Stage);
        }

        [Test]
        public void CluesStopAtTheRequiredCount()
        {
            var state = new SecretRoomState(requiredClues: 2);
            Assert.IsTrue(state.TryAddClue());
            Assert.IsTrue(state.TryAddClue());
            Assert.IsFalse(state.TryAddClue());
            Assert.AreEqual(2, state.Clues);
        }

        [Test]
        public void SnapshotRestorePreservesProgress()
        {
            var state = new SecretRoomState(requiredClues: 2, requiredOrders: 1);
            state.TryAddClue();
            state.TryAddClue();
            state.RecordCompletedOrder();
            state.TryReveal();
            state.TryPrepare();

            SecretRoomSnapshot snapshot = state.Snapshot();
            SecretRoomState restored = SecretRoomState.Restore(snapshot, 2, 1);

            Assert.AreEqual(state.Clues, restored.Clues);
            Assert.AreEqual(state.CompletedOrders, restored.CompletedOrders);
            Assert.AreEqual(state.Stage, restored.Stage);
            Assert.AreEqual(SecretRoomStage.Cleaned, restored.Stage);
        }
    }
}
