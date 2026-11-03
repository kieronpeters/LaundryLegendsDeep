using System.IO;
using LaundryLegends.Arcade;
using LaundryLegends.Interaction;
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

        [MenuItem("Laundry Legends/Phase 12/Probe Player Interaction")]
        public static void ProbePlayerInteraction()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var interactor = Object.FindFirstObjectByType<LaundryLegends.Interaction.PlayerInteractor>();
            if (interactor == null)
            {
                Debug.LogError("[P12-PLAYER] no PlayerInteractor");
                return;
            }

            var serialized = new SerializedObject(interactor);
            SerializedProperty cameraProperty = serialized.FindProperty("_camera");
            SerializedProperty rangeProperty = serialized.FindProperty("_range");
            SerializedProperty maskProperty = serialized.FindProperty("_interactionMask");
            var camera = cameraProperty != null ? cameraProperty.objectReferenceValue as Camera : null;
            Debug.Log($"[P12-PLAYER] interactor on {interactor.gameObject.name} enabled={interactor.enabled} " +
                      $"camera={(camera != null ? camera.name : "null")} " +
                      $"cameraEnabled={(camera != null && camera.enabled)} " +
                      $"range={(rangeProperty != null ? rangeProperty.floatValue : -1)} " +
                      $"mask={(maskProperty != null ? maskProperty.intValue : 0)} main={(Camera.main != null ? Camera.main.name : "null")}");

            if (camera != null)
            {
                Debug.Log($"[P12-PLAYER] camera pos={camera.transform.position} forward={camera.transform.forward}");
                var machine = GameObject.Find("Machine_1");
                if (machine != null)
                {
                    Debug.Log($"[P12-PLAYER] machine pos={machine.transform.position} forward={machine.transform.forward}");
                }
            }
        }

        [MenuItem("Laundry Legends/Phase 16/Fix Loose Props")]
        public static void FixLooseProps()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MoveProp("Basket_01", new Vector3(3.55f, 0.05f, 1.45f));
            MoveProp("Bag_01", new Vector3(4.15f, 0.05f, 1.45f));
            MoveProp("LooseLaundry_01", new Vector3(3.80f, 0.62f, 0.95f));
            MoveProp("LooseLaundry_02", new Vector3(4.10f, 0.62f, 0.95f));
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[P16] Laundry props grouped around the folding table.");
        }

        private static void MoveProp(string name, Vector3 position)
        {
            GameObject target = GameObject.Find(name);
            if (target != null)
            {
                target.transform.position = position;
            }
        }

        [MenuItem("Laundry Legends/Phase 16/Fix Player Spawn")]
        public static void FixPlayerSpawn()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = Object.FindFirstObjectByType<LaundryLegends.Player.FirstPersonController>();
            if (player == null)
            {
                Debug.LogError("[P16] No player in the scene.");
                return;
            }

            player.transform.position = new Vector3(-1.8f, 0.1f, -4.4f);
            player.transform.rotation = Quaternion.identity;
            Debug.Log($"[P16] Player spawn moved to {player.transform.position} to clear the customer lane.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Laundry Legends/Phase 16/Probe Placement")]
        public static void ProbePlacement()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var rack = Object.FindFirstObjectByType<LaundryLegends.Business.DropOffRack>(
                FindObjectsInactive.Include);
            if (rack != null)
            {
                for (int i = 0; i < rack.Slots.Count; i++)
                {
                    CarrySlot slot = rack.Slots[i];
                    if (slot != null)
                    {
                        Debug.Log($"[P16-RACK] {slot.name} pos={slot.transform.position} " +
                                  $"occupant={(slot.Occupant != null ? slot.Occupant.name : "empty")}");
                    }
                }
            }

            GameObject counter = GameObject.Find("Counter");
            if (counter != null)
            {
                foreach (Renderer renderer in counter.GetComponentsInChildren<Renderer>(true))
                {
                    Debug.Log($"[P16-COUNTER] {renderer.name} bounds={renderer.bounds}");
                }
            }

            var player = Object.FindFirstObjectByType<LaundryLegends.Player.FirstPersonController>();
            if (player != null)
            {
                Debug.Log($"[P16-PLAYER] {player.name} pos={player.transform.position} rot={player.transform.eulerAngles}");
            }

            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                Bounds bounds = renderer.bounds;
                if (bounds.max.y > 0.55f)
                {
                    continue;
                }

                if (bounds.center.x > -1f && bounds.center.x < 5f
                    && bounds.center.z > -1f && bounds.center.z < 3.5f
                    && bounds.size.magnitude < 4f)
                {
                    Debug.Log($"[P16-BOX] {renderer.name} center={bounds.center} size={bounds.size} " +
                              $"mat={(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null")}");
                }
            }

            GameObject phase3 = GameObject.Find("Phase3Items");
            if (phase3 != null)
            {
                foreach (Transform child in phase3.GetComponentsInChildren<Transform>(true))
                {
                    Debug.Log($"[P16-PHASE3] {child.name} pos={child.position}");
                }
            }
        }

        [MenuItem("Laundry Legends/Phase 12/Probe Machines")]
        public static void ProbeMachines()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            for (int i = 1; i <= 8; i++)
            {
                GameObject machine = GameObject.Find($"Machine_{i}");
                if (machine == null)
                {
                    Debug.LogWarning($"[P12-MACHINE] Machine_{i} missing");
                    continue;
                }

                var hook = machine.GetComponent<LaundryLegends.Laundry.MachineLoadHook>();
                string slot = "none";
                if (hook != null)
                {
                    var serialized = new SerializedObject(hook);
                    SerializedProperty slotProperty = serialized.FindProperty("_slot");
                    slot = slotProperty != null && slotProperty.objectReferenceValue != null
                        ? slotProperty.objectReferenceValue.name
                        : "null";
                }

                string colliders = string.Empty;
                foreach (Collider collider in machine.GetComponentsInChildren<Collider>(true))
                {
                    colliders += $"{collider.GetType().Name}({collider.gameObject.name},enabled={collider.enabled}) ";
                }

                Debug.Log($"[P12-MACHINE] {machine.name} active={machine.activeInHierarchy} layer={machine.layer} " +
                          $"hook={(hook != null ? hook.enabled.ToString() : "missing")} slot={slot} colliders=[{colliders.Trim()}]");

                Vector3 origin = machine.transform.position + machine.transform.forward * -1.4f + Vector3.up * 1.2f;
                Vector3 target = machine.transform.position + Vector3.up * 1.1f;
                Vector3 direction = (target - origin).normalized;
                bool hit = Physics.Raycast(origin, direction, out RaycastHit info, 4f);
                Debug.Log($"[P12-MACHINE] ray {origin} -> {target} hit={hit} " +
                          $"{(hit ? info.collider.gameObject.name + " at " + info.distance.ToString("0.00") : "nothing")}");
            }

            GameObject detailsRoot = GameObject.Find("MachineFrontDetails");
            if (detailsRoot != null)
            {
                foreach (Renderer renderer in detailsRoot.GetComponentsInChildren<Renderer>(true))
                {
                    string material = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "<null>";
                    Debug.Log($"[P12-DETAIL] {renderer.transform.name} pos={renderer.transform.position} " +
                              $"scale={renderer.transform.lossyScale} mat={material}");
                }
            }

            GameObject firstMachine = GameObject.Find("Machine_1");
            if (firstMachine != null)
            {
                foreach (Renderer renderer in firstMachine.GetComponentsInChildren<Renderer>(true))
                {
                    string material = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "<null>";
                    Debug.Log($"[P12-CHILD] {renderer.transform.name} pos={renderer.transform.position} " +
                              $"scale={renderer.transform.lossyScale} mat={material}");
                }
            }
        }

        [MenuItem("Laundry Legends/Phase 12/Probe Arcade Cabinet")]
        public static void ProbeCabinet()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ArcadeCabinet cabinet = Object.FindFirstObjectByType<ArcadeCabinet>(FindObjectsInactive.Include);
            if (cabinet == null)
            {
                Debug.LogError("[P12-PROBE] no cabinet");
                return;
            }

            foreach (Renderer renderer in cabinet.GetComponentsInChildren<Renderer>(true))
            {
                string materials = string.Empty;
                foreach (Material material in renderer.sharedMaterials)
                {
                    materials += (material != null ? material.name : "<null>") + ";";
                }

                Debug.Log($"[P12-PROBE] {renderer.transform.name} parent={renderer.transform.parent.name} mats={materials}");
            }
        }

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

            // 7. Themed starter cabinet front (marquee, tinted body, lit sign).
            foreach (ArcadeCabinet candidate in Object.FindObjectsByType<ArcadeCabinet>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name != "ArcadeCabinet")
                {
                    continue;
                }

                Vector3 position = candidate.transform.position;
                Vector3 towardRoom = new Vector3(12.2f, 0f, 0f) - new Vector3(position.x, 0f, position.z);
                if (towardRoom.sqrMagnitude < 0.01f)
                {
                    towardRoom = Vector3.left;
                }

                towardRoom.Normalize();
                Render("12-cabinet-front.png",
                    new Vector3(position.x, 1.5f, position.z) + towardRoom * 2.2f,
                    new Vector3(position.x, 1.05f, position.z),
                    45f);
                break;
            }

            // 8. Back-room vending machines near the new poster wall.
            GameObject vending = FindInactive("Vending_Soda");
            if (vending != null)
            {
                Vector3 position = vending.transform.position;
                Render("12-vending.png",
                    position + new Vector3(1.9f, 1.5f, 1.3f),
                    position + Vector3.up * 1.0f,
                    50f);
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
