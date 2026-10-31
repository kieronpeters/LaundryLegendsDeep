using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Fixes the oversized and duplicated counter signage. The counter desk
    /// sign was authored at characterSize 0.05 with fontSize 64, which renders
    /// roughly 2.5 m wide, and a second "back" copy 8 cm behind it showed as
    /// mirrored text. This resizes the front sign, removes the duplicate, and
    /// brings the hanging sign down to a sane width.
    /// </summary>
    public static class SignageFixTool
    {
        private const string RoomPrefabPath = "Assets/LaundryLegends/Prefabs/Environment/GreyboxRoom.prefab";

        [MenuItem("Laundry Legends/Phase 4/Fix Counter Signage")]
        public static void FixCounterSignage()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                int changed = 0;

                Transform back = FindDeep(root.transform, "Counter_DeskText_Back");
                if (back != null)
                {
                    Object.DestroyImmediate(back.gameObject);
                    changed++;
                    Debug.Log("[SIGN] Removed duplicated counter back text.");
                }

                Transform desk = FindDeep(root.transform, "Counter_DeskText");
                if (desk != null)
                {
                    TextMesh text = desk.GetComponent<TextMesh>();
                    if (text != null)
                    {
                        text.characterSize = 0.012f;
                        changed++;
                        Debug.Log($"[SIGN] Counter_DeskText characterSize -> {text.characterSize} (~0.6 m wide).");
                    }

                    // The text faced away from the room, so it read mirrored from
                    // the customer side; turn it around.
                    desk.localRotation = Quaternion.Euler(0f, 180f, 0f) * desk.localRotation;
                    changed++;
                    Debug.Log("[SIGN] Counter_DeskText turned to face the room.");
                }

                Transform hanging = FindDeep(root.transform, "DropOff_Text");
                if (hanging != null)
                {
                    TextMesh text = hanging.GetComponent<TextMesh>();
                    if (text != null)
                    {
                        text.characterSize = 0.03f;
                        changed++;
                        Debug.Log($"[SIGN] DropOff_Text characterSize -> {text.characterSize} (~1.5 m wide).");
                    }

                    hanging.localRotation = Quaternion.Euler(0f, 180f, 0f) * hanging.localRotation;
                    changed++;
                    Debug.Log("[SIGN] DropOff_Text turned to face the room.");
                }

                if (changed == 0)
                {
                    throw new System.InvalidOperationException("No signage was changed; object names may have moved.");
                }

                PrefabUtility.SaveAsPrefabAsset(root, RoomPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SIGN] Counter signage fixed.");
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
