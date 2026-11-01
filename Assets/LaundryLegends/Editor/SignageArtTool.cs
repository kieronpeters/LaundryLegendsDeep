using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Replaces the floating legacy TextMesh signage with flat quads using the
    /// authored sign textures. The old text objects are disabled, not deleted, so
    /// the change is reversible.
    /// </summary>
    public static class SignageArtTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string TextureFolder = "Assets/LaundryLegends/Art/Textures/Signs";
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";

        private sealed class SignSpec
        {
            public string ObjectName;
            public string Texture;
            public float Width;
            public float Height;
        }

        private static readonly SignSpec[] Specs =
        {
            new SignSpec { ObjectName = "Facade_SignText_Outside", Texture = "sign_facade", Width = 6.0f, Height = 1.12f },
            new SignSpec { ObjectName = "Sign_MachineWall_Text", Texture = "sign_machines", Width = 4.0f, Height = 0.5f },
            new SignSpec { ObjectName = "Counter_DeskText", Texture = "sign_dropoff", Width = 0.62f, Height = 0.155f },
            new SignSpec { ObjectName = "Sign_Open", Texture = "sign_open", Width = 0.5f, Height = 0.25f },
            new SignSpec { ObjectName = "Utility_Sign_Text", Texture = "sign_staff", Width = 0.4f, Height = 0.2f }
        };

        [MenuItem("Laundry Legends/Phase 7/Apply Sign Art")]
        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                var applied = new List<string>();

                foreach (SignSpec spec in Specs)
                {
                    Transform target = FindDeep(root.transform, spec.ObjectName);
                    if (target == null)
                    {
                        Debug.LogWarning($"[SIGN] Missing '{spec.ObjectName}'");
                        continue;
                    }

                    var text = target.GetComponent<TextMesh>();
                    if (text != null)
                    {
                        text.text = string.Empty;
                    }

                    var renderer = target.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }

                    Material material = EnsureSignMaterial(spec.Texture);
                    if (material == null)
                    {
                        Debug.LogWarning($"[SIGN] Missing texture {spec.Texture}");
                        continue;
                    }

                    Transform existing = target.Find("SignQuad");
                    if (existing != null)
                    {
                        Object.DestroyImmediate(existing.gameObject);
                    }

                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = "SignQuad";
                    Object.DestroyImmediate(quad.GetComponent<Collider>());
                    quad.transform.SetParent(target, false);
                    quad.transform.localPosition = new Vector3(0f, 0f, -0.002f);
                    quad.transform.localRotation = Quaternion.identity;
                    quad.transform.localScale = new Vector3(spec.Width, spec.Height, 1f);
                    quad.GetComponent<Renderer>().sharedMaterial = material;

                    applied.Add(spec.ObjectName);
                }

                PrefabUtility.SaveAsPrefabAsset(root, RoomPrefabPath);
                Debug.Log($"[SIGN] Applied {applied.Count} sign panels: {string.Join(", ", applied)}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Material EnsureSignMaterial(string textureName)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{textureName}.png");
            if (texture == null)
            {
                return null;
            }

            string path = $"{MaterialFolder}/MAT_Sign_{textureName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find("Standard");
            material.mainTexture = texture;
            material.color = Color.white;
            material.mainTextureScale = Vector2.one;
            material.SetFloat("_Glossiness", 0.08f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.18f, 0.18f, 0.18f));
            EditorUtility.SetDirty(material);
            return material;
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
