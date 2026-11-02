using System.IO;
using LaundryLegends.Arcade;
using LaundryLegends.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Phase 12 visual evidence: activates the expansion bay in the editor, then
    /// renders the bay floor (carpet), the cabinet side art and the bay poster to
    /// TestResults/phase12 so the art can be reviewed without a playthrough.
    /// </summary>
    public static class Phase12CaptureTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string OutputFolder = "TestResults/phase12";
        private const int Width = 1280;
        private const int Height = 720;

        [MenuItem("Laundry Legends/Phase 12/Capture Expansion Bay")]
        public static void CaptureBay()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(OutputFolder);

            GameObject bay = FindInactive("ExpansionBay");
            GameObject seal = FindInactive("ExpansionSeal");
            if (bay == null)
            {
                Debug.LogError("[P12-CAP] ExpansionBay not found.");
                return;
            }

            // The bay lives under the inactive back-room root; activate it without
            // saving so the authored scene state is unchanged.
            GameObject content = FindInactive("RoomContent");
            if (content != null)
            {
                content.SetActive(true);
            }

            bay.SetActive(true);
            if (seal != null)
            {
                seal.SetActive(false);
            }

            // The carpet is a post-expansion purchase; reveal it for the art shot.
            GameObject carpet = FindInactive("Decor_ArcadeCarpet");
            if (carpet != null)
            {
                carpet.SetActive(true);
            }

            Renderer floorRenderer = FindRenderer(bay.transform, "BayFloor");
            if (floorRenderer == null)
            {
                Debug.LogError("[P12-CAP] BayFloor not found.");
                return;
            }

            Bounds bounds = floorRenderer.bounds;
            Vector3 center = bounds.center;
            Debug.Log($"[P12-CAP] bay bounds center={center} size={bounds.size}");

            // 1. Entry view: standing just inside the opening, looking at the back wall.
            var entry = center + new Vector3(0f, 1.7f, -bounds.extents.z + 0.6f);
            Render("12-bay-entry.png", entry, center + new Vector3(0f, 1.0f, bounds.extents.z), 55f);

            // 2. Floor/carpet view: high angle looking down at the carpet pattern.
            Render("12-bay-carpet.png",
                center + new Vector3(-1.4f, 2.6f, -1.2f),
                center + new Vector3(0.2f, 0f, 0.4f),
                65f);

            // 3. Cabinet side art: look straight at the starter cabinet's side quad.
            GameObject sideArt = FindInactive("SideRacer") ?? FindInactive("SideSpin");
            if (sideArt != null)
            {
                Vector3 art = sideArt.transform.position;
                Vector3 normal = sideArt.transform.forward;
                Render("12-cabinet-side.png", art + normal * 1.35f + Vector3.up * 0.1f, art, 40f);
                Debug.Log($"[P12-CAP] side art '{sideArt.name}' at {art} normal {normal}");
            }

            // 4. Bay poster on the back wall.
            GameObject poster = FindInactive("Poster_RacerRiot");
            if (poster != null)
            {
                var posterCamera = poster.transform.position + new Vector3(0f, -0.1f, -2.2f);
                Render("12-bay-poster.png", posterCamera, poster.transform.position, 45f);
            }

            // 5. Secret doorway seen from inside the shop, after the room unlock.
            GameObject door = FindInactive("SecretDoor_Foreshadowed");
            if (door != null)
            {
                SecretRoomController controller = Object.FindFirstObjectByType<SecretRoomController>(
                    FindObjectsInactive.Include);
                if (controller != null)
                {
                    controller.UnlockByPurchase();
                }

                Vector3 doorPosition = door.transform.position;
                Render("12-secret-door.png",
                    doorPosition + new Vector3(-3.0f, 0.6f, 0f),
                    doorPosition + new Vector3(0f, -0.2f, 0f),
                    55f);
            }

            // 6. Waiting pocket: phone regular and the ambient route end point.
            GameObject phone = FindInactive("PhoneRegular");
            if (phone != null)
            {
                Vector3 phonePosition = phone.transform.position;
                Render("12-waiting-area.png",
                    phonePosition + new Vector3(-2.2f, 1.6f, 1.6f),
                    phonePosition + new Vector3(0f, 0.9f, 0f),
                    55f);
            }

            Debug.Log("[P12-CAP] Bay captures written to " + OutputFolder);
        }

        private static void Render(string file, Vector3 position, Vector3 lookAt, float fov)
        {
            var cameraObject = new GameObject("P12CaptureCamera");
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.LookRotation((lookAt - position).normalized, Vector3.up);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = fov;
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.Skybox;

            var renderTexture = new RenderTexture(Width, Height, 24);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            string path = Path.Combine(OutputFolder, file);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(cameraObject);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(texture);
            Debug.Log($"[P12-CAP] {path} from {position} looking at {lookAt}");
        }

        private static Renderer FindRenderer(Transform root, string name)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name == name)
                {
                    return renderer;
                }
            }

            return null;
        }

        private static GameObject FindInactive(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }
    }
}
