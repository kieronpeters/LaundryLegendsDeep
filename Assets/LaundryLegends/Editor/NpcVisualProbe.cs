using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Measures the NPC material and the carried-item scale so we can tell a
    /// material-assignment failure from an over-bright light, and a socket
    /// scale inheritance bug from an oversized prop.
    /// </summary>
    public static class NpcVisualProbe
    {
        [MenuItem("Laundry Legends/Phase 4/Probe NPC Visuals")]
        public static void Probe()
        {
            var scene = EditorSceneManager.OpenScene("Assets/LaundryLegends/Scenes/Laundromat.unity", OpenSceneMode.Single);
            GameObject root = GameObject.Find("AmbientActors");
            Transform attendant = root != null ? root.transform.Find("Attendant") : null;
            if (attendant == null)
            {
                Debug.LogError("[VIS] no Attendant");
                return;
            }

            foreach (Renderer renderer in attendant.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                string color = material != null && material.HasProperty("_Color")
                    ? material.color.ToString()
                    : "n/a";
                Debug.Log($"[VIS] renderer '{renderer.name}' mat={(material != null ? material.name : "null")} " +
                          $"shader={(material != null && material.shader != null ? material.shader.name : "null")} " +
                          $"color={color} bounds={renderer.bounds.size}");
            }

            var socket = attendant.GetComponent<CarrySocket>();
            Debug.Log($"[VIS] root lossyScale={attendant.lossyScale} anchor={(socket != null && socket.Anchor != null ? socket.Anchor.name : "null")} " +
                      $"anchorLossy={(socket != null && socket.Anchor != null ? socket.Anchor.lossyScale.ToString() : "n/a")}");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LaundryLegends/Prefabs/Gameplay/Laundry/LooseLaundry.prefab");
            if (prefab != null && socket != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var item = instance.GetComponent<LaundryItem>();
                Debug.Log($"[VIS] loose laundry prefab scale={instance.transform.localScale} bounds={(instance.GetComponentInChildren<Renderer>().bounds.size)}");

                if (socket.Attach(item))
                {
                    Renderer renderer = instance.GetComponentInChildren<Renderer>();
                    Debug.Log($"[VIS] carried losses={instance.transform.lossyScale} worldBounds={renderer.bounds.size} " +
                              $"anchorDistance={Vector3.Distance(instance.transform.position, socket.Anchor.position):0.000}");
                    socket.Detach();
                }

                Object.DestroyImmediate(instance);
            }

            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
