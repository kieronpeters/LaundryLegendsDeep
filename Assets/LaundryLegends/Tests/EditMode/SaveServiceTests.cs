using System;
using System.IO;
using LaundryLegends.Save;
using NUnit.Framework;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>Phase 14: the save DTO round-trips and corrupt files are rejected.</summary>
    public sealed class SaveServiceTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), $"ll-save-{Guid.NewGuid():N}.json");
            SaveService.OverridePath(_path);
        }

        [TearDown]
        public void TearDown()
        {
            SaveService.OverridePath(null);
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        [Test]
        public void RoundTripPreservesProgression()
        {
            var data = new SaveData
            {
                Money = 321,
                Day = 12,
                MinuteOfDay = 615,
                Reputation = 44,
                RoomUnlocked = true,
                ArcadeExpanded = true,
                Detergent = 7,
                Fragrance = 3,
                Plastic = 9,
                MasterVolume = 0.5f,
                MusicVolume = 0.4f,
                SfxVolume = 0.9f
            };

            Assert.IsTrue(SaveService.Save(data));
            Assert.IsTrue(SaveService.HasSave);

            SaveData loaded = SaveService.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.Version);
            Assert.AreEqual(321, loaded.Money);
            Assert.AreEqual(12, loaded.Day);
            Assert.AreEqual(615, loaded.MinuteOfDay);
            Assert.AreEqual(44, loaded.Reputation);
            Assert.IsTrue(loaded.RoomUnlocked);
            Assert.IsTrue(loaded.ArcadeExpanded);
            Assert.AreEqual(7, loaded.Detergent);
            Assert.AreEqual(9, loaded.Plastic);
            Assert.AreEqual(0.5f, loaded.MasterVolume, 0.0001f);
        }

        [Test]
        public void CorruptFileLoadsAsNull()
        {
            File.WriteAllText(_path, "{ not valid json");
            Assert.IsNull(SaveService.Load());
        }

        [Test]
        public void MissingFileLoadsAsNull()
        {
            Assert.IsFalse(SaveService.HasSave);
            Assert.IsNull(SaveService.Load());
        }
    }
}
