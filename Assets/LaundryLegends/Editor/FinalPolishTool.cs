using System.IO;
using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Final polish helpers: makes the counter sign readable from both sides and
    /// renders a dedicated close-up of the MakeHuman pilot NPC.
    /// </summary>
    public static class FinalPolishTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";

        [MenuItem("Laundry Legends/Phase 8/Fix Counter Sign")]
        public static void FixCounterSign()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform text = FindDeep(root.transform, "Counter_DeskText");
                if (text == null)
                {
                    throw new System.InvalidOperationException("Counter_DeskText missing.");
                }

                Transform front = text.Find("SignQuad");
                if (front == null)
                {
                    throw new System.InvalidOperationException("SignQuad missing on the counter sign.");
                }

                // Mirror a second quad so the sign reads from either side.
                Transform back = text.Find("SignQuadBack");
                if (back == null)
                {
                    var copy = Object.Instantiate(front.gameObject, text);
                    copy.name = "SignQuadBack";
                    copy.transform.localRotation = front.localRotation * Quaternion.Euler(0f, 180f, 0f);
                    copy.transform.localPosition = front.localPosition + new Vector3(0f, 0f, -0.004f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, RoomPrefabPath);
                Debug.Log("[POLISH] Counter sign is now double-sided.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Laundry Legends/Phase 8/Capture Pilot")]
        public static void CapturePilot()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("AmbientActors");
            Transform regular = root != null ? root.transform.Find("PhoneRegular") : null;
            if (regular == null)
            {
                throw new System.InvalidOperationException("PhoneRegular missing.");
            }

            var cameraObject = new GameObject("PilotCaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.11f, 0.13f);

            Vector3 towardRoom = Vector3.zero - regular.position;
            towardRoom.y = 0f;
            if (towardRoom.sqrMagnitude < 0.01f)
            {
                towardRoom = Vector3.back;
            }

            Quaternion original = regular.rotation;
            regular.rotation = Quaternion.LookRotation(towardRoom.normalized, Vector3.up);

            Vector3 position = regular.position + towardRoom.normalized * 2.4f + Vector3.up * 1.35f;
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(regular.position + Vector3.up * 1.1f);

            var renderTexture = new RenderTexture(1280, 720, 24);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;

            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();

            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults", "captures"));
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, "09-pilot-human.png"), texture.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);
            regular.rotation = original;

            Debug.Log("[POLISH] Pilot capture written to TestResults/captures/09-pilot-human.png");
            EditorSceneManager.CloseScene(scene, true);
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform found = FindDeep(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
