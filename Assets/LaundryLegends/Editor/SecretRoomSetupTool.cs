using LaundryLegends.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Wires the secret room into the Laundromat scene: three clue pickups, a
    /// reveal interaction on the concealed door, a preparation interaction, room
    /// lighting, and stage props. Additive and re-runnable.
    /// </summary>
    public static class SecretRoomSetupTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string RootName = "SecretRoomSystem";

        [MenuItem("Laundry Legends/Phase 7/Build Secret Room")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject(RootName);
            SecretRoomController controller = root.AddComponent<SecretRoomController>();
            controller.Configure(3, 3);

            GameObject door = GameObject.Find("SecretDoor_Foreshadowed");
            GameObject cover = GameObject.Find("SecretAccess_ServicePanel");
            Transform anchor = door != null ? door.transform : root.transform;

            // Three clues hidden around the utility alcove.
            var clues = new GameObject("Clues");
            clues.transform.SetParent(root.transform, false);
            Clue(clues.transform, controller, "Clue_Token", anchor.position + new Vector3(-1.6f, 0.35f, 0.8f));
            Clue(clues.transform, controller, "Clue_Meter", anchor.position + new Vector3(1.4f, 1.15f, 0.6f));
            Clue(clues.transform, controller, "Clue_Receipt", anchor.position + new Vector3(0.4f, 0.25f, -0.9f));

            // Reveal and preparation interactions in front of the door.
            Interactable(root.transform, controller, "RevealPoint", SecretRoomAction.Reveal,
                "Force the service panel", anchor.position + new Vector3(0f, 1.1f, 1.1f));
            Interactable(root.transform, controller, "PreparePoint", SecretRoomAction.Prepare,
                "Prepare the hidden room", anchor.position + new Vector3(0f, 1.1f, 0.2f));

            // Room lighting: cold magenta arcade light, off until powered.
            var lights = new GameObject("RoomLights");
            lights.transform.SetParent(root.transform, false);
            Light[] lightComponents =
            {
                AddLight(lights.transform, "ArcadeLight_A", anchor.position + new Vector3(-1.2f, 2.2f, -2.2f), new Color(0.85f, 0.25f, 0.75f)),
                AddLight(lights.transform, "ArcadeLight_B", anchor.position + new Vector3(1.2f, 2.2f, -2.2f), new Color(0.25f, 0.75f, 0.9f))
            };

            // Placeholder room content, only shown when the room is usable.
            var content = new GameObject("RoomContent");
            content.transform.SetParent(root.transform, false);
            Box(content.transform, "ArcadePlaceholder", anchor.position + new Vector3(0f, 0.55f, -2.4f), new Vector3(0.7f, 1.1f, 0.6f));

            var cleaned = new GameObject("CleanedProps");
            cleaned.transform.SetParent(root.transform, false);
            Box(cleaned.transform, "Broom", anchor.position + new Vector3(1.6f, 0.5f, 0.4f), new Vector3(0.08f, 1.0f, 0.08f));

            var powered = new GameObject("PoweredProps");
            powered.transform.SetParent(root.transform, false);
            Box(powered.transform, "CableRun", anchor.position + new Vector3(-1.4f, 0.2f, -1.0f), new Vector3(1.4f, 0.05f, 0.05f));

            var repaired = new GameObject("RepairedProps");
            repaired.transform.SetParent(root.transform, false);
            Box(repaired.transform, "FloorPatch", anchor.position + new Vector3(0f, 0.02f, -1.6f), new Vector3(1.2f, 0.02f, 1.2f));

            // Assign the serialized references.
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_sealedDoor").objectReferenceValue = door;
            serialized.FindProperty("_sealedDoorCover").objectReferenceValue = cover;
            serialized.FindProperty("_cluesFoundGroup").objectReferenceValue = clues;

            SerializedProperty roomLights = serialized.FindProperty("_roomLights");
            roomLights.arraySize = lightComponents.Length;
            for (int i = 0; i < lightComponents.Length; i++)
            {
                roomLights.GetArrayElementAtIndex(i).objectReferenceValue = lightComponents[i].gameObject;
            }

            serialized.FindProperty("_roomContent").objectReferenceValue = content;
            serialized.FindProperty("_cleanedProps").objectReferenceValue = cleaned;
            serialized.FindProperty("_poweredProps").objectReferenceValue = powered;
            serialized.FindProperty("_repairedProps").objectReferenceValue = repaired;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Start hidden: lights and content off until earned.
            lights.SetActive(false);
            content.SetActive(false);
            cleaned.SetActive(false);
            powered.SetActive(false);
            repaired.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SECRET] Wired: 3 clues, reveal, prepare, 2 lights, stage props. Door='"
                      + (door != null ? door.name : "MISSING") + "' cover='" + (cover != null ? cover.name : "MISSING") + "'.");
        }

        [MenuItem("Laundry Legends/Phase 7/Validate Secret Room")]
        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                throw new System.InvalidOperationException("SecretRoomSystem missing from the scene.");
            }

            var controller = root.GetComponent<SecretRoomController>();
            if (controller == null)
            {
                throw new System.InvalidOperationException("SecretRoomController missing.");
            }

            if (controller.Stage != SecretRoomStage.Hidden)
            {
                throw new System.InvalidOperationException("Secret room must start hidden.");
            }

            Debug.Log("[SECRET] Validation passed: controller present and hidden.");
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void Clue(Transform parent, SecretRoomController controller, string name, Vector3 position)
        {
            GameObject clue = Box(parent, name, position, new Vector3(0.12f, 0.12f, 0.12f));
            clue.AddComponent<SecretRoomInteractable>().Configure(controller, SecretRoomAction.CollectClue, "Inspect clue");
        }

        private static void Interactable(
            Transform parent,
            SecretRoomController controller,
            string name,
            SecretRoomAction action,
            string prompt,
            Vector3 position)
        {
            GameObject point = Box(parent, name, position, new Vector3(0.35f, 0.35f, 0.1f));
            point.GetComponent<Renderer>().enabled = false;
            point.AddComponent<SecretRoomInteractable>().Configure(controller, action, prompt);
        }

        private static Light AddLight(Transform parent, string name, Vector3 position, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 1.8f;
            light.color = color;
            light.shadows = LightShadows.None;
            return light;
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            return box;
        }
    }
}
