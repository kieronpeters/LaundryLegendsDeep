using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Checks whether the animation library FBX ships a usable skinned
    /// character, which would let NPCs use the exact rig the clips were
    /// authored for.
    /// </summary>
    public static class AnimationModelProbe
    {
        private const string Path = "Assets/LaundryLegends/Art/Animations/UAL1_Standard.fbx";

        [MenuItem("Laundry Legends/Phase 4/Probe Animation Model")]
        public static void Probe()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            if (model == null)
            {
                Debug.LogError("[PROBE2] No GameObject at animation path.");
                return;
            }

            Debug.Log($"[PROBE2] root '{model.name}' children={model.transform.childCount}");
            foreach (Transform child in model.transform)
            {
                Debug.Log($"[PROBE2] child '{child.name}' type={child.GetType().Name}");
            }

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Debug.Log($"[PROBE2] renderer {renderer.GetType().Name} '{renderer.name}' material={(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null")}");
            }

            var animator = model.GetComponentInChildren<Animator>(true);
            Debug.Log($"[PROBE2] animator={(animator != null)}");

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Path))
            {
                if (asset is Avatar avatar)
                {
                    Debug.Log($"[PROBE2] avatar {avatar.name} human={avatar.isHuman} valid={avatar.isValid}");
                }
            }
        }
    }
}
