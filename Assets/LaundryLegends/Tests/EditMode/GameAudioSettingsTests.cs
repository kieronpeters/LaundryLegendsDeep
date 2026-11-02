using LaundryLegends.Audio;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>Phase 13: audio bus values clamp and persist.</summary>
    public sealed class GameAudioSettingsTests
    {
        [TearDown]
        public void RestoreDefaults()
        {
            GameAudioSettings.ResetToDefaults();
        }

        [Test]
        public void ValuesClampSaveAndReload()
        {
            GameAudioSettings.ResetToDefaults();

            GameAudioSettings.SetMaster(1.4f);
            GameAudioSettings.SetMusic(-0.5f);
            GameAudioSettings.SetSfx(0.25f);

            Assert.AreEqual(1f, GameAudioSettings.Master, 0.0001f);
            Assert.AreEqual(0f, GameAudioSettings.Music, 0.0001f);
            Assert.AreEqual(0.25f, GameAudioSettings.Sfx, 0.0001f);
            Assert.AreEqual(1f, AudioListener.volume, 0.0001f);

            Assert.AreEqual(1f, PlayerPrefs.GetFloat(GameAudioSettings.MasterKey), 0.0001f);
            Assert.AreEqual(0.25f, PlayerPrefs.GetFloat(GameAudioSettings.SfxKey), 0.0001f);

            GameAudioSettings.SetSfx(0.6f);
            GameAudioSettings.Load();
            Assert.AreEqual(0.6f, GameAudioSettings.Sfx, 0.0001f);
        }

        [Test]
        public void ResetRestoresDocumentedDefaults()
        {
            GameAudioSettings.SetMaster(0.1f);
            GameAudioSettings.ResetToDefaults();

            Assert.AreEqual(GameAudioSettings.DefaultMaster, GameAudioSettings.Master, 0.0001f);
            Assert.AreEqual(GameAudioSettings.DefaultMusic, GameAudioSettings.Music, 0.0001f);
            Assert.AreEqual(GameAudioSettings.DefaultSfx, GameAudioSettings.Sfx, 0.0001f);
        }
    }
}
