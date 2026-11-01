using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Optional slow-motion for visible PlayMode runs. Enable by setting the
    /// LL_SLOW_PLAYMODE environment variable (e.g. 0.25) before launching the
    /// editor; tests then run at that time scale so a human can watch them.
    /// </summary>
    [InitializeOnLoad]
    public static class SlowPlayModeHook
    {
        private static readonly float Scale;

        static SlowPlayModeHook()
        {
            string value = System.Environment.GetEnvironmentVariable("LL_SLOW_PLAYMODE");
            if (float.TryParse(value, out float parsed) && parsed > 0f && parsed < 1f)
            {
                Scale = parsed;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                UnityEngine.Debug.Log($"[SLOW] PlayMode slow motion enabled at timeScale {Scale:0.00}.");
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Time.timeScale = Scale;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
