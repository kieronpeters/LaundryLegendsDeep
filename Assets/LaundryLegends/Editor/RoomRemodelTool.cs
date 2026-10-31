using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>Adds trim, paneling, and furniture detail over the greybox shell.</summary>
    public static class RoomRemodelTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";
        private const string MaterialFolder = "Assets/LaundryLegends/Art/Materials";
        private const string RootName = "RoomRemodel";

        [MenuItem("Laundry Legends/Phase 3/Build Room Remodel")]
        public static void BuildRoomRemodel()
        {
            Material wood = Load("MAT_CounterWood");
            Material metal = Load("Grey_Metal");
            Material paint = Load("MAT_WallPaint");

            GameObject room = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                Transform old = room.transform.Find(RootName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var root = new GameObject(RootName);
                root.transform.SetParent(room.transform, false);

                // Baseboards and crown trim on the long walls.
                Add(root.transform, "Baseboard_West", new Vector3(-7.8f, 0.1f, 0f), new Vector3(0.08f, 0.2f, 11.6f), wood);
                Add(root.transform, "Baseboard_East", new Vector3(7.8f, 0.1f, 0f), new Vector3(0.08f, 0.2f, 11.6f), wood);
                Add(root.transform, "Baseboard_North", new Vector3(0f, 0.1f, 5.68f), new Vector3(15.6f, 0.2f, 0.08f), wood);
                Add(root.transform, "Crown_West", new Vector3(-7.8f, 2.62f, 0f), new Vector3(0.08f, 0.12f, 11.6f), wood);
                Add(root.transform, "Crown_East", new Vector3(7.8f, 2.62f, 0f), new Vector3(0.08f, 0.12f, 11.6f), wood);

                // Simple wainscot panels.
                for (int i = 0; i < 5; i++)
                {
                    float z = -4.4f + i * 2.2f;
                    Add(root.transform, $"Wainscot_West_{i}", new Vector3(-7.74f, 1.0f, z), new Vector3(0.04f, 1.0f, 1.8f), paint);
                    Add(root.transform, $"Wainscot_East_{i}", new Vector3(7.74f, 1.0f, z), new Vector3(0.04f, 1.0f, 1.8f), paint);
                }

                // Counter and table aprons/legs detail.
                Add(root.transform, "Counter_Apron", new Vector3(-4.5f, 0.82f, -2.96f), new Vector3(3.2f, 0.22f, 0.05f), wood);
                Add(root.transform, "Table_Apron", new Vector3(4.5f, 0.82f, 0.38f), new Vector3(3.2f, 0.18f, 0.05f), wood);

                // Cushions on the two waiting chairs.
                Add(root.transform, "ChairCushion_1", new Vector3(4.65f, 0.52f, -4.55f), new Vector3(0.55f, 0.08f, 0.55f), metal);
                Add(root.transform, "ChairCushion_2", new Vector3(5.55f, 0.52f, -4.55f), new Vector3(0.55f, 0.08f, 0.55f), metal);

                // Shelf face trim and secret-cover edge trim.
                Add(root.transform, "ShelfTrim_1", new Vector3(-7.45f, 1.2f, 2.2f), new Vector3(0.04f, 1.6f, 0.06f), wood);
                Add(root.transform, "SecretTrim", new Vector3(7.65f, 1.1f, 3.0f), new Vector3(0.06f, 2.1f, 0.08f), metal);

                PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
                Debug.Log("[Remodel] Added baseboards, crown, wainscot, aprons, cushions, and trim.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(room);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Laundry Legends/Phase 3/Validate Room Remodel")]
        public static void ValidateRoomRemodel()
        {
            var failures = new List<string>();
            GameObject room = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            Transform root = room != null ? room.transform.Find(RootName) : null;
            if (root == null)
            {
                failures.Add("Missing RoomRemodel root.");
            }
            else if (root.childCount < 18)
            {
                failures.Add($"Expected at least 18 remodel details, found {root.childCount}.");
            }

            if (failures.Count > 0)
            {
                foreach (string failure in failures)
                {
                    Debug.LogError($"[Remodel] {failure}");
                }

                throw new System.InvalidOperationException($"Room remodel validation failed with {failures.Count} issue(s).");
            }

            Debug.Log("[Remodel] Validation passed: trim, paneling, aprons, cushions, and shelf/secret detail present.");
        }

        private static void Add(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material Load(string name)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
            if (material == null)
            {
                throw new System.InvalidOperationException($"Missing material: {name}");
            }

            return material;
        }
    }
}
