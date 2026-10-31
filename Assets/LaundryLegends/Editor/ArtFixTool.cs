using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Repairs the issues visible after the first art pass: sign materials that
    /// were authored with the Built-in shader (magenta under URP), leftover
    /// TextMesh objects, and the stretched ceiling texture.
    /// </summary>
    public static class ArtFixTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string Signs = "Assets/LaundryLegends/Art/Textures/Signs";
        private const string Materials = "Assets/LaundryLegends/Art/Materials";

        [MenuItem("Laundry Legends/Phase 7/Fix Art Pass")]
        public static void Fix()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");

            string[] signNames = { "sign_facade", "sign_machines", "sign_dropoff", "sign_open", "sign_staff" };
            foreach (string name in signNames)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MAT_Sign_{name}.mat");
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Signs}/{name}.png");
                if (material == null || texture == null)
                {
                    continue;
                }

                material.shader = lit;
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", 0.08f);
                material.SetFloat("_Metallic", 0f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.05f, 0.05f, 0.05f));
                EditorUtility.SetDirty(material);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                int hidden = 0;
                foreach (TextMesh text in root.GetComponentsInChildren<TextMesh>(true))
                {
                    var renderer = text.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.enabled)
                    {
                        renderer.enabled = false;
                        hidden++;
                    }
                }

                Transform ceiling = FindDeep(root.transform, "Ceiling");
                if (ceiling != null)
                {
                    var renderer = ceiling.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var flat = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/MAT_CeilingFlat.mat");
                        if (flat == null)
                        {
                            flat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            AssetDatabase.CreateAsset(flat, $"{Materials}/MAT_CeilingFlat.mat");
                        }

                        flat.shader = Shader.Find("Universal Render Pipeline/Lit");
                        flat.SetColor("_BaseColor", new Color(0.17f, 0.17f, 0.18f));
                        flat.SetFloat("_Smoothness", 0.05f);
                        flat.SetFloat("_Metallic", 0f);
                        EditorUtility.SetDirty(flat);
                        renderer.sharedMaterial = flat;
                    }
                }

                Transform counterSign = FindDeep(root.transform, "Counter_DeskText");
                if (counterSign != null)
                {
                    Transform quad = counterSign.Find("SignQuad");
                    if (quad != null)
                    {
                        // The parent text object was flipped 180 degrees earlier, so the
                        // child quad faced into the counter and read as a blank panel.
                        quad.localRotation = Quaternion.Euler(0f, 180f, 0f) * quad.localRotation;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, RoomPrefabPath);
                Debug.Log($"[ARTFIX] Sign materials converted to URP, {hidden} TextMesh renderers hidden, ceiling tiling reset.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
