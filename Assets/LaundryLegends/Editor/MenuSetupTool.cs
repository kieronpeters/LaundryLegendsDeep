using LaundryLegends.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds the main menu: storefront background, menu music, title and two
    /// mouse-driven buttons (Start Game, Exit).
    /// </summary>
    public static class MenuSetupTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/MainMenu.unity";
        private const string BackgroundPath = "Assets/LaundryLegends/UI/MenuBackground.png";
        private const string MusicPath = "Assets/LaundryLegends/Audio/Music/Healing.mp3";

        [MenuItem("Laundry Legends/Phase 8/Build Main Menu")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (GameObject existing in scene.GetRootGameObjects())
            {
                if (existing.name == "MainMenu"
                    || existing.name == "MainMenuCamera"
                    || existing.name.StartsWith("MenuSetup"))
                {
                    Object.DestroyImmediate(existing);
                }
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var cameraObject = new GameObject("MainMenuCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            // The menu music needs a listener or the scene is silent in a build.
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
            cameraObject.transform.position = new Vector3(0f, 1.6f, -6f);

            var menuRoot = new GameObject("MenuSetup");
            var controller = menuRoot.AddComponent<MainMenuController>();

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(menuRoot.transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Background photo taken from the game itself.
            var background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            background.transform.SetParent(canvasObject.transform, false);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var raw = background.GetComponent<RawImage>();
            raw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            raw.color = Color.white;

            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(canvasObject.transform, false);
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.62f);

            Text title = MakeText(canvasObject.transform, "Title", "LAUNDRY LEGENDS", font, 104, TextAnchor.MiddleCenter);
            Place(title.rectTransform, 0.60f, 0.78f);
            title.color = new Color(0.96f, 0.92f, 0.82f);

            Text subtitle = MakeText(canvasObject.transform, "Subtitle", "A Bali neighbourhood laundromat", font, 34, TextAnchor.MiddleCenter);
            Place(subtitle.rectTransform, 0.52f, 0.60f);
            subtitle.color = new Color(0.82f, 0.84f, 0.9f);

            MakeButton(canvasObject.transform, "PlayButton", "START GAME", font, 0.42f, controller.StartGame);
            MakeButton(canvasObject.transform, "QuitButton", "EXIT", font, 0.30f, controller.QuitGame);

            Text hint = MakeText(canvasObject.transform, "Hint", "WASD move  ·  E interact  ·  Q drop  ·  U upgrade  ·  Esc pause", font, 22, TextAnchor.MiddleCenter);
            Place(hint.rectTransform, 0.08f, 0.16f);
            hint.color = new Color(0.75f, 0.77f, 0.82f);

            // Menu music, long enough to cover a few minutes without repeating.
            var musicObject = new GameObject("MenuMusic");
            musicObject.transform.SetParent(menuRoot.transform, false);
            AudioSource music = musicObject.AddComponent<AudioSource>();
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            if (music.clip == null)
            {
                Debug.LogError($"[MENU] Missing menu music at {MusicPath}; the menu will be silent.");
            }
            music.loop = true;
            music.playOnAwake = true;
            music.spatialBlend = 0f;
            music.volume = 0.45f;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(menuRoot.transform, false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MENU] Built: background, music, title, Start/Exit buttons, EventSystem.");
        }

        private static void Place(RectTransform rect, float minY, float maxY)
        {
            rect.anchorMin = new Vector2(0.08f, minY);
            rect.anchorMax = new Vector2(0.92f, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text MakeText(Transform parent, string name, string content, Font font, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            return text;
        }

        private static void MakeButton(Transform parent, string name, string label, Font font, float centreY, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, centreY);
            rect.anchorMax = new Vector2(0.5f, centreY);
            rect.sizeDelta = new Vector2(360f, 82f);
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = name == "PlayButton"
                ? new Color(0.9f, 0.23f, 0.51f, 0.95f)
                : new Color(0.18f, 0.2f, 0.26f, 0.95f);

            Text text = MakeText(go.transform, "Label", label, font, 36, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            // A persistent listener is written into the scene file. Button.onClick.AddListener
            // only registers a runtime callback, which is not serialized, so a scene built
            // with it ships with an empty click list and the button silently does nothing.
            Button button = go.GetComponent<Button>();
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }
    }
}
