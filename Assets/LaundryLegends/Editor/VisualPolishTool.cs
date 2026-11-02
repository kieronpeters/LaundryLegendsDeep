using UnityEditor;
using UnityEditor.SceneManagement;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Visual repair pass for the owner-reported prop, signage, and laundry
    /// defects. Every step is idempotent and re-runnable, and each one records
    /// what it changed so the pass can be verified from the log.
    /// <para>
    /// Team-facing rule: signs are authored as a dark backing box plus a
    /// <c>SignQuad</c>/<c>SignFace</c> with an authored sign texture. The legacy
    /// TextMesh objects stay disabled because they render without materials under
    /// URP. A backing box with no face is the "floating black slab" defect.
    /// </para>
    /// </summary>
    public static class VisualPolishTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string Materials = "Assets/LaundryLegends/Art/Materials";
        private const string LoosePrefab = "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/LooseLaundry.prefab";

        [MenuItem("Laundry Legends/Phase 8/Polish Visuals")]
        public static void Polish()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            FixSigns();
            FixLaundry();
            FixBrokenShaders();
            AttachControlsHint();
            CleanUpClutter();
            RemoveFirstPersonHands();
            FixShadows();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[POLISH] Visual pass complete.");
        }

        /// <summary>
        /// Removes the blank panels that read as floating black slabs: the OPEN
        /// sign hanging inside the doorway, the text-less blade sign, and the
        /// duplicate counter board. The remaining drop-off board gets an authored
        /// sign face.
        /// </summary>
        private static void FixSigns()
        {
            Transform root = FindOrCreateFaceRoot();

            SetActive("OpenSign", false);
            SetActive("Blade_Sign", false);
            SetActive("Blade_Text", false);

            Transform counterPanel = Find("Sign_Counter_Panel");
            if (counterPanel != null)
            {
                var renderer = counterPanel.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    renderer.enabled = false;
                    Debug.Log("[POLISH] hid the duplicate counter board; the drop-off sign above it stays legible.");
                }
            }

            Transform dropOff = Find("DropOff_Panel");
            if (dropOff != null)
            {
                PlaceFace(
                    root,
                    dropOff,
                    "MAT_Sign_sign_dropoff",
                    1.55f,
                    0.35f,
                    0.04f,
                    true);
            }

            Debug.Log("[POLISH] signage repaired.");
        }

        /// <summary>Adds a readable sign face to the front of a backing panel.</summary>
        private static void PlaceFace(
            Transform root,
            Transform panel,
            string materialName,
            float width,
            float height,
            float offset,
            bool faceRoom)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/{materialName}.mat");
            if (material == null)
            {
                Debug.LogWarning($"[POLISH] missing sign material {materialName}");
                return;
            }

            string faceName = $"{panel.name}_Face";
            Transform existing = root.Find(faceName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = faceName;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(root, false);

            // The quad reads from its -Z side. The panel's room side is +Z, so a
            // room-facing sign is turned around and pushed back toward the room.
            float direction = faceRoom ? 1f : -1f;
            quad.transform.position = panel.position + new Vector3(0f, 0f, direction * offset);
            quad.transform.rotation = faceRoom
                ? panel.rotation * Quaternion.Euler(0f, 180f, 0f)
                : panel.rotation;
            quad.transform.localScale = new Vector3(width, height, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            Debug.Log($"[POLISH] sign face '{faceName}' on '{panel.name}' ({width:0.00} x {height:0.00}).");
        }

        private static void FixLaundry()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(LoosePrefab);
            try
            {
                // The pile was authored at roughly twice a believable bundle size,
                // which is why it read as a giant shard in the staff's arms.
                contents.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                PrefabUtility.SaveAsPrefabAsset(contents, LoosePrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            SetFabricColor("MAT_LooseLaundry", new Color(0.78f, 0.82f, 0.86f, 1f));
            SetFabricColor("MAT_BundleCloth", new Color(0.72f, 0.76f, 0.80f, 1f));
            Debug.Log("[POLISH] laundry resized and recoloured to fabric tones.");
        }

        private static void SetFabricColor(string materialName, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/{materialName}.mat");
            if (material == null)
            {
                Debug.LogWarning($"[POLISH] missing material {materialName}");
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
        }

        /// <summary>
        /// Converts leftover Built-in "Standard" materials to URP/Lit. The ambient
        /// phone prop was created with Shader.Find("Standard") and rendered as a
        /// magenta square under URP.
        /// </summary>
        private static void FixBrokenShaders()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                Debug.LogWarning("[POLISH] URP/Lit shader not found");
                return;
            }

            int converted = 0;
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null)
                    {
                        continue;
                    }

                    string shaderName = material.shader.name;
                    if (!shaderName.StartsWith("Standard", System.StringComparison.Ordinal)
                        && !shaderName.StartsWith("Legacy Shaders", System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (material.HasProperty("_Color") && material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", material.GetColor("_Color"));
                    }

                    if (material.HasProperty("_MainTex") && material.HasProperty("_BaseMap"))
                    {
                        Texture texture = material.GetTexture("_MainTex");
                        if (texture != null)
                        {
                            material.SetTexture("_BaseMap", texture);
                        }
                    }

                    material.shader = lit;
                    material.SetFloat("_Smoothness", 0.2f);
                    material.SetFloat("_Metallic", 0f);
                    EditorUtility.SetDirty(material);
                    converted++;
                }
            }

            Debug.Log($"[POLISH] converted {converted} Built-in material(s) to URP/Lit.");
        }

        /// <summary>Adds the key legend to the HUD object and binds the live player.</summary>
        private static void AttachControlsHint()
        {
            Transform hud = Find("HUD");
            if (hud == null)
            {
                Debug.LogWarning("[POLISH] HUD object not found; controls legend not attached.");
                return;
            }

            var hint = hud.GetComponent<LaundryLegends.UI.ControlsHintView>();
            if (hint == null)
            {
                hint = hud.gameObject.AddComponent<LaundryLegends.UI.ControlsHintView>();
            }

            var interactor = Object.FindFirstObjectByType<LaundryLegends.Interaction.PlayerInteractor>();
            var carry = Object.FindFirstObjectByType<LaundryLegends.Player.CarryController>();
            hint.Configure(interactor, carry);
            Debug.Log($"[POLISH] controls legend on '{hud.name}' (interact={(interactor != null)} drop={(carry != null)}).");
        }

        /// <summary>
        /// Removes the placeholder debris the owner flagged as "random items on
        /// the floor": dirt decals, clue cubes, and leftover build placeholders.
        /// They are deactivated, not deleted, so the objects stay available to
        /// the tools that reference them.
        /// </summary>
        private static void CleanUpClutter()
        {
            string[] clutter =
            {
                "Dirt_Entrance", "Dirt_MachineWall", "Dirt_Counter",
                "Clue_Token", "Clue_Receipt", "Clue_Meter",
                "Broom", "FloorPatch", "ArcadePlaceholder", "CableRun", "InstallPointVisual"
            };

            int hidden = 0;
            foreach (string name in clutter)
            {
                Transform target = Find(name);
                if (target != null && target.gameObject.activeSelf)
                {
                    target.gameObject.SetActive(false);
                    hidden++;
                }
            }

            foreach (LaundryItem item in Object.FindObjectsByType<LaundryItem>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item.name.StartsWith("LooseLaundry", System.StringComparison.Ordinal) && item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(false);
                    hidden++;
                }
            }

            Debug.Log($"[POLISH] hid {hidden} clutter object(s).");
        }

        /// <summary>
        /// The placeholder first-person hand meshes read as broken shapes on
        /// screen. Remove them from the player prefab; the carried item is still
        /// visible in front of the camera. The visibility component stays valid
        /// with a null reference, and its tests build their own hand object.
        /// </summary>
        private static void RemoveFirstPersonHands()
        {
            const string playerPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/Player.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(playerPrefab);
            try
            {
                var views = contents.GetComponentsInChildren<LaundryLegends.Player.FirstPersonHandsView>(true);
                foreach (var view in views)
                {
                    var serialized = new SerializedObject(view);
                    SerializedProperty hands = serialized.FindProperty("_hands");
                    if (hands != null)
                    {
                        hands.objectReferenceValue = null;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                Transform[] transforms = contents.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (candidate.name == "FirstPersonHands")
                    {
                        Object.DestroyImmediate(candidate.gameObject);
                        Debug.Log("[POLISH] removed the placeholder first-person hands.");
                        break;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, playerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// The storefront sawtooth shadow on the facade is directional-light
        /// shadow acne; a normal bias raise removes it without softening the
        /// interior point-light shadows.
        /// </summary>
        private static void FixShadows()
        {
            Light directional = null;
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    directional = light;
                    break;
                }
            }

            if (directional == null)
            {
                Debug.LogWarning("[POLISH] no directional light found");
                return;
            }

            directional.shadowBias = 0.08f;
            directional.shadowNormalBias = 1f;
            Debug.Log("[POLISH] directional shadow bias raised to remove facade acne.");
        }

        private static Transform FindOrCreateFaceRoot()
        {
            GameObject existing = GameObject.Find("SignFaces");
            if (existing == null)
            {
                existing = new GameObject("SignFaces");
            }

            return existing.transform;
        }

        private static void SetActive(string name, bool active)
        {
            Transform target = Find(name);
            if (target == null)
            {
                return;
            }

            if (target.gameObject.activeSelf != active)
            {
                target.gameObject.SetActive(active);
                Debug.Log($"[POLISH] '{name}' active -> {active}.");
            }
        }

        private static Transform Find(string name)
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform found = FindDeep(root.transform, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
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
