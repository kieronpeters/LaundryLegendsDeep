using System;
using UnityEngine;

namespace LaundryLegends.Laundry
{
    /// <summary>
    /// Identity and carry metadata for one laundry prop. TemplateId is authored
    /// on the prefab; InstanceId is generated per spawned instance and is not
    /// persisted in Phase 3.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LaundryItem : MonoBehaviour
    {
        [SerializeField] private string _templateId = string.Empty;
        [SerializeField] private LaundryItemKind _kind = LaundryItemKind.LooseLaundry;
        [SerializeField] private bool _carryable = true;

        [NonSerialized] private string _instanceId;

        [SerializeField] private LaundryStage _stage = LaundryStage.Dirty;
        [SerializeField] private GameObject _wrappedVisualPrefab;

        private GameObject _wrappedVisual;
        private Renderer[] _baseRenderers;

        public LaundryStage Stage => _stage;

        /// <summary>Changes the visible/process stage; keeps identity and ownership.</summary>
        public void SetStage(LaundryStage stage)
        {
            _stage = stage;
            if (_baseRenderers == null || _baseRenderers.Length == 0)
            {
                _baseRenderers = GetComponentsInChildren<Renderer>(true);
            }

            Color tint = stage == LaundryStage.Dirty ? new Color(0.75f, 0.85f, 0.85f, 1f)
                : stage == LaundryStage.Washed ? new Color(0.9f, 0.95f, 1f, 1f)
                : stage == LaundryStage.Dried ? new Color(1f, 0.98f, 0.9f, 1f)
                : new Color(0.85f, 0.95f, 1f, 1f);

            foreach (Renderer renderer in _baseRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                foreach (Material material in renderer.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        material.color = tint;
                    }
                }
            }

            ApplyWrappedVisual(stage == LaundryStage.Wrapped);
        }

        /// <summary>
        /// Swaps the folded-cloth body for the authored clear-plastic bundle. The
        /// bundle is a prefab reference so the mesh stays a single asset.
        /// </summary>
        private void ApplyWrappedVisual(bool wrapped)
        {
            if (wrapped)
            {
                foreach (Renderer renderer in _baseRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                }

                if (_wrappedVisual == null)
                {
                    // The authored bundle is a flat sheet; a small cloth parcel
                    // reads as wrapped laundry on the table and in the staff's arms.
                    _wrappedVisual = CreateParcelVisual();
                    _wrappedVisual.name = "WrappedVisual";
                    _wrappedVisual.transform.SetParent(transform, false);
                    _wrappedVisual.transform.localPosition = Vector3.zero;
                    _wrappedVisual.transform.localRotation = Quaternion.identity;
                    _wrappedVisual.transform.localScale = Vector3.one;
                }

                if (_wrappedVisual != null)
                {
                    _wrappedVisual.SetActive(true);
                    GroundVisual(_wrappedVisual);
                }

                return;
            }

            if (_wrappedVisual != null)
            {
                _wrappedVisual.SetActive(false);
            }

            foreach (Renderer renderer in _baseRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }

        /// <summary>
        /// Lifts the wrapped visual so its lowest rendered point rests on the
        /// item pivot instead of sinking halfway into the folding table.
        /// </summary>
        private void GroundVisual(GameObject visual)
        {
            Bounds local = new Bounds();
            bool hasBounds = false;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                Bounds world = renderer.bounds;
                Vector3 min = transform.InverseTransformPoint(world.min);
                Vector3 max = transform.InverseTransformPoint(world.max);
                var candidate = new Bounds((min + max) * 0.5f, max - min);
                if (!hasBounds)
                {
                    local = candidate;
                    hasBounds = true;
                }
                else
                {
                    local.Encapsulate(candidate);
                }
            }

            if (!hasBounds || local.size.y <= 0.0001f)
            {
                return;
            }

            Vector3 offset = visual.transform.localPosition;
            visual.transform.localPosition = new Vector3(offset.x, offset.y - local.min.y, offset.z);
        }

        /// <summary>
        /// Minimal transparent wrap for items that have no authored bundle
        /// prefab (runtime-built items in tests and spawned props).
        /// </summary>
        /// <summary>
        /// A small cloth parcel in a clear film bag. The authored bundle mesh is a
        /// flat sheet that reads as paper; this reads as wrapped laundry.
        /// </summary>
        private GameObject CreateParcelVisual()
        {
            var root = new GameObject("Parcel");
            AddParcelBox(
                root.transform,
                "ParcelCloth",
                new Vector3(0.34f, 0.20f, 0.28f),
                new Color(0.78f, 0.82f, 0.88f, 1f),
                0.12f,
                true);
            AddParcelBox(
                root.transform,
                "ParcelFilm",
                new Vector3(0.38f, 0.24f, 0.32f),
                new Color(0.85f, 0.92f, 1f, 0.22f),
                0.55f,
                false);
            return root;
        }

        private static void AddParcelBox(Transform parent, string name, Vector3 size, Color colour,
            float smoothness, bool opaque)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = size;

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            if (!opaque)
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

            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>Stable prefab-template identifier, unique per prefab asset.</summary>
        public string TemplateId => _templateId;

        /// <summary>Unique per spawned instance and stable for that instance's lifetime.</summary>
        public string InstanceId
        {
            get
            {
                if (string.IsNullOrEmpty(_instanceId))
                {
                    _instanceId = Guid.NewGuid().ToString("N");
                }

                return _instanceId;
            }
        }

        public LaundryItemKind Kind => _kind;

        public bool IsCarryable => _carryable;

        /// <summary>Phase 3 allows only loose laundry into a washer.</summary>
        public bool IsWasherLoadable => _carryable && _kind == LaundryItemKind.LooseLaundry;

        /// <summary>Authoring and test setup path; never regenerates an existing instance ID.</summary>
        public void Initialize(string templateId, LaundryItemKind kind, bool carryable = true)
        {
            if (!string.IsNullOrWhiteSpace(templateId))
            {
                _templateId = templateId;
            }

            _kind = kind;
            _carryable = carryable;
        }

        /// <summary>Used by editor validation and setup where a prefab is authored in place.</summary>
        public void SetTemplateId(string templateId)
        {
            if (!string.IsNullOrWhiteSpace(templateId))
            {
                _templateId = templateId;
            }
        }
    }
}
