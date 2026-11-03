using UnityEditor;
using UnityEngine;
using LaundryLegends.Player;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds simple gloved first-person hands under the player camera and wires
    /// them to FirstPersonHandsView so they only appear while carrying.
    /// </summary>
    public static class FirstPersonHandsSetupTool
    {
        private const string PlayerPrefab = "Assets/LaundryLegends/Prefabs/Gameplay/Player.prefab";
        private const string GlovePath = "Assets/LaundryLegends/Art/Materials/MAT_Glove.mat";

        [MenuItem("Laundry Legends/Phase 16/Build First-Person Hands")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                Transform camera = root.transform.Find("PlayerCamera");
                if (camera == null)
                {
                    throw new System.InvalidOperationException("Player.prefab has no PlayerCamera.");
                }

                Transform old = root.transform.Find("Hands");
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                Material glove = EnsureGlove();
                var hands = new GameObject("Hands");
                hands.transform.SetParent(camera, false);
                hands.transform.localPosition = Vector3.zero;
                hands.transform.localRotation = Quaternion.identity;
                hands.SetActive(false);

                BuildHand(hands.transform, "HandLeft", new Vector3(-0.32f, -0.26f, 0.45f), glove);
                BuildHand(hands.transform, "HandRight", new Vector3(0.32f, -0.26f, 0.45f), glove);

                FirstPersonHandsView view = root.GetComponent<FirstPersonHandsView>();
                if (view == null)
                {
                    view = root.AddComponent<FirstPersonHandsView>();
                }

                view.Configure(hands, root.GetComponent<CarryController>());
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
                Debug.Log("[HANDS] First-person gloved hands built and wired to CarryController.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildHand(Transform parent, string name, Vector3 localPosition, Material material)
        {
            var hand = new GameObject(name);
            hand.transform.SetParent(parent, false);
            hand.transform.localPosition = localPosition;
            hand.transform.localRotation = Quaternion.identity;

            Part(hand.transform, "Palm", new Vector3(0f, 0f, 0f),
                new Vector3(0.14f, 0.09f, 0.18f), material);
            for (int i = 0; i < 4; i++)
            {
                Part(hand.transform, $"Finger_{i}", new Vector3(-0.045f + i * 0.03f, 0.01f, 0.13f),
                    new Vector3(0.028f, 0.04f, 0.11f), material);
            }

            Part(hand.transform, "Thumb", new Vector3(0.09f, -0.01f, 0.04f),
                new Vector3(0.04f, 0.04f, 0.09f), material);
        }

        private static void Part(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = size;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material EnsureGlove()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GlovePath);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(lit) { name = "MAT_Glove" };
                AssetDatabase.CreateAsset(material, GlovePath);
            }
            else
            {
                material.shader = lit;
            }

            material.SetColor("_BaseColor", new Color(0.10f, 0.22f, 0.32f, 1f));
            material.SetFloat("_Smoothness", 0.35f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
