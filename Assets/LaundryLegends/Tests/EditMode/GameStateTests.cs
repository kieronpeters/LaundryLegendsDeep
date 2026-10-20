using LaundryLegends.Core;
using NUnit.Framework;

namespace LaundryLegends.Tests.EditMode
{
    public sealed class GameStateTests
    {
        [Test]
        public void ConstructorStoresStartingMoney()
        {
            var state = new GameState(250);

            Assert.AreEqual(250, state.Money);
        }

        [Test]
        public void ConstructorRejectsNegativeMoney()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GameState(-1));
        }

        [Test]
        public void MoneyHasNoPublicSetter()
        {
            var property = typeof(GameState).GetProperty(nameof(GameState.Money));

            Assert.IsNotNull(property);
            Assert.IsNull(property.GetSetMethod(), "Money must remain read-only outside EconomyService.");
        }

        [Test]
        public void SetFlagAddsFlagOnce()
        {
            var state = new GameState();

            state.SetFlag("tutorial-complete");
            state.SetFlag("tutorial-complete");

            Assert.IsTrue(state.HasFlag("tutorial-complete"));
            Assert.AreEqual(1, state.ProgressionFlags.Count);
        }

        [Test]
        public void HasFlagRejectsNullAndWhitespace()
        {
            var state = new GameState();

            Assert.IsFalse(state.HasFlag(null));
            Assert.IsFalse(state.HasFlag(string.Empty));
            Assert.IsFalse(state.HasFlag("   "));
        }

        [Test]
        public void SetFlagRejectsEmptyName()
        {
            var state = new GameState();

            Assert.Throws<System.ArgumentException>(() => state.SetFlag(""));
        }

        [Test]
        public void AdvanceDayIncrementsDay()
        {
            var state = new GameState();
            int startDay = state.Day;

            state.AdvanceDay();

            Assert.AreEqual(startDay + 1, state.Day);
        }
    }
}
