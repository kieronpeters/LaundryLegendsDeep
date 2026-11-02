using System.IO;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Replaces the eight blocky appliance visuals with the detailed Washer/Dryer
    /// FBX models and converts their imported materials to URP/Lit.
    /// </summary>
    public static class ApplianceIntegrationTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string WasherPath = "Assets/LaundryLegends/Art/Models/Appliances/WasherDetailed.fbx";
        private const string DryerPath = "Assets/LaundryLegends/Art/Models/Appliances/DryerDetailed.fbx";
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials/Appliances";

        [MenuItem("Laundry Legends/Phase 12/Audit Appliance Orientation")]
        public static void AuditOrientation()
        {
            Audit("Assets/LaundryLegends/Art/Models/Appliance.fbx");
            Audit(WasherPath);
            Audit(DryerPath);
        }

        private static void Audit(string path)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
            {
                Debug.LogWarning($"[APPLIANCE-AUDIT] missing {path}");
                return;
            }

            Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
            bool first = true;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            Debug.Log($"[APPLIANCE-AUDIT] {path}: rootRot={model.transform.rotation.eulerAngles} rootScale={model.transform.localScale} bounds={bounds.size}");
            int logged = 0;
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
            {
                if (logged++ >= 14)
                {
                    break;
                }

                Debug.Log($"[APPLIANCE-AUDIT]   child {child.name} localRot={child.localRotation.eulerAngles}");
            }
        }

        [MenuItem("Laundry Legends/Phase 12/Integrate Detailed Appliances")]
        public static void Integrate()
        {
            GameObject room = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Directory.CreateDirectory(MaterialFolder);
                AssetDatabase.Refresh();

                GameObject washer = AssetDatabase.LoadAssetAtPath<GameObject>(WasherPath);
                GameObject dryer = AssetDatabase.LoadAssetAtPath<GameObject>(DryerPath);
                if (washer == null || dryer == null)
                {
                    throw new System.InvalidOperationException($"Missing detailed appliance FBX: washer={washer != null} dryer={dryer != null}");
                }

                for (int i = 1; i <= 8; i++)
                {
                    Transform machine = FindChild(room.transform, $"Machine_{i}");
                    if (machine == null)
                    {
                        throw new System.InvalidOperationException($"GreyboxRoom has no Machine_{i}.");
                    }

                    Renderer body = machine.GetComponent<Renderer>();
                    if (body != null)
                    {
                        body.enabled = false;
                    }

                    Transform old = machine.Find("ApplianceModel");
                    if (old != null)
                    {
                        Object.DestroyImmediate(old.gameObject);
                    }

                    GameObject source = i % 2 == 1 ? washer : dryer;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, machine);
                    instance.name = "ApplianceModel";
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * source.transform.rotation;
                    Vector3 scale = machine.localScale;
                    instance.transform.localScale = new Vector3(
                        scale.x != 0f ? 1f / scale.x : 1f,
                        scale.y != 0f ? 1f / scale.y : 1f,
                        scale.z != 0f ? 1f / scale.z : 1f);

                    ConvertMaterials(instance, i);
                    UpdateInteractionCollider(machine, instance);
                }

                PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(room);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[APPLIANCE] Replaced all eight visuals with the detailed washer/dryer models.");
        }

        [MenuItem("Laundry Legends/Phase 12/Validate Detailed Appliances")]
        public static void Validate()
        {
            GameObject room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            int models = 0;
            int urpMaterials = 0;
            for (int i = 1; i <= 8; i++)
            {
                Transform machine = FindChild(room.transform, $"Machine_{i}");
                Transform model = machine != null ? machine.Find("ApplianceModel") : null;
                if (model == null)
                {
                    continue;
                }

                models++;
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && material.shader != null
                            && material.shader.name.Contains("Universal Render Pipeline"))
                        {
                            urpMaterials++;
                        }
                    }
                }
            }

            if (models != 8)
            {
                throw new System.InvalidOperationException($"Expected 8 detailed appliance models, found {models}.");
            }

            if (urpMaterials == 0)
            {
                throw new System.InvalidOperationException("Detailed appliances have no URP materials.");
            }

            Debug.Log($"[APPLIANCE] Validation passed: {models} detailed models, {urpMaterials} URP material slots.");
        }

        /// <summary>
        /// The detailed model is taller than the old greybox body, so the authored
        /// collider only covered the lower half and the eye-level interaction ray
        /// passed over it. Resize the root collider to the model bounds.
        /// </summary>
        private static void UpdateInteractionCollider(Transform machine, GameObject model)
        {
            Bounds bounds = default;
            bool first = true;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (first)
            {
                return;
            }

            bounds.Expand(0.08f);
            BoxCollider box = machine.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = machine.gameObject.AddComponent<BoxCollider>();
            }

            Vector3 scale = machine.lossyScale;
            box.center = machine.InverseTransformPoint(bounds.center);
            box.size = new Vector3(
                bounds.size.x / Mathf.Max(0.0001f, scale.x),
                bounds.size.y / Mathf.Max(0.0001f, scale.y),
                bounds.size.z / Mathf.Max(0.0001f, scale.z));
            box.isTrigger = false;
        }

        private static void ConvertMaterials(GameObject instance, int machineIndex)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    string name = source != null ? source.name : $"Appliance_{machineIndex}_{i}";
                    materials[i] = EnsureUrpMaterial(source, name);
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static Material EnsureUrpMaterial(Material source, string name)
        {
            string safe = Sanitize(name);
            string path = $"{MaterialFolder}/MAT_{safe}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(lit) { name = $"MAT_{safe}" };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = lit;
            }

            Color color = Color.white;
            if (source != null)
            {
                if (source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
                else if (source.HasProperty("_Color")) color = source.color;
            }

            bool glass = name.IndexOf("Glass", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool led = name.IndexOf("LED", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool chrome = name.IndexOf("Chrome", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool rubber = name.IndexOf("Rubber", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool metal = name.IndexOf("Metal", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool washerBody = name.IndexOf("Washer_Body", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool dryerBody = name.IndexOf("Dryer_Body", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (washerBody)
            {
                color = new Color(0.84f, 0.90f, 0.98f, 1f);
            }
            else if (dryerBody)
            {
                color = new Color(0.98f, 0.92f, 0.84f, 1f);
            }

            if (glass && color.a >= 0.95f)
            {
                color.a = 0.3f;
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", chrome ? 0.85f : metal ? 0.55f : 0f);
            material.SetFloat("_Smoothness", glass ? 0.9f : chrome ? 0.75f : rubber ? 0.1f : 0.4f);

            if (glass)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON");
                material.renderQueue = 3000;
            }

            if (led)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return name.Replace(' ', '_');
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChild(root.GetChild(i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
