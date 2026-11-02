using System.Collections;
using LaundryLegends.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>Phase 13: the audio director exists, owns clips and respects the buses.</summary>
    public sealed class GameAudioPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [TearDown]
        public void RestoreAudio()
        {
            GameAudioSettings.ResetToDefaults();
        }

        [UnityTest]
        public IEnumerator DirectorOwnsClipsAndPlaysCues()
        {
            GameAudio audio = GameAudio.Instance;
            Assert.IsNotNull(audio);
            Assert.IsNotNull(audio.SfxSource);
            Assert.IsNotNull(audio.AmbienceSource);
            Assert.IsNotNull(audio.ArcadeSource);
            Assert.IsNotNull(audio.AmbienceSource.clip);
            Assert.IsNotNull(audio.ArcadeSource.clip);

            Assert.DoesNotThrow(() => GameAudio.Play(Sfx.Click));
            Assert.DoesNotThrow(() => GameAudio.Play(Sfx.Cash));
            yield return null;

            GameAudioSettings.SetMaster(0.5f);
            Assert.AreEqual(0.5f, AudioListener.volume, 0.0001f);
        }

        [UnityTest]
        public IEnumerator ArcadeBedCanBeForced()
        {
            GameAudio audio = GameAudio.Instance;
            GameAudio.SetArcade(true);
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(audio.ArcadeSource.volume, 0f, "The arcade bed should fade in.");
            GameAudio.SetArcade(false);
            yield return new WaitForSeconds(0.4f);
            Assert.Less(audio.ArcadeSource.volume, 0.1f, "The arcade bed should fade out.");
        }
    }
}
