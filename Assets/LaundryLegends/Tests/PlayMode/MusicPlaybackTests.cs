using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    public sealed class MusicPlaybackTests
    {
        [UnitySetUp]
        public IEnumerator LoadLaundromat()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator MusicSourceIsConfiguredForLoopingAmbience()
        {
            GameObject music = GameObject.Find("Music");
            Assert.IsNotNull(music, "Laundromat should contain the Music root.");
            AudioSource source = music.GetComponent<AudioSource>();
            Assert.IsNotNull(source, "The Music root needs an AudioSource.");
            Assert.IsNotNull(source.clip, "The music source needs a clip.");
            Assert.IsTrue(source.loop, "The music must loop.");
            Assert.IsTrue(source.playOnAwake, "The music must start automatically.");
            Assert.That(source.volume, Is.InRange(0.2f, 0.6f), "Ambient volume should be audible but not dominant.");
            Assert.AreEqual(0f, source.spatialBlend, "Music should be non-spatial.");
            yield return null;
        }
    }
}
