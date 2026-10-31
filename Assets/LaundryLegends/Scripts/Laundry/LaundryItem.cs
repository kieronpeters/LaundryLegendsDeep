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

                if (_wrappedVisual == null && _wrappedVisualPrefab != null)
                {
                    _wrappedVisual = Instantiate(_wrappedVisualPrefab);
                    _wrappedVisual.name = "WrappedVisual";
                    _wrappedVisual.transform.SetParent(transform, false);
                    _wrappedVisual.transform.localPosition = Vector3.zero;
                    _wrappedVisual.transform.localRotation = Quaternion.identity;
                    _wrappedVisual.transform.localScale = Vector3.one;
                }

                if (_wrappedVisual == null)
                {
                    // Items built at runtime (tests, spawned props) have no authored
                    // bundle reference, so keep a simple transparent wrap visible.
                    _wrappedVisual = CreateFallbackWrap();
                }

                if (_wrappedVisual != null)
                {
                    _wrappedVisual.SetActive(true);
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
        /// Minimal transparent wrap for items that have no authored bundle
        /// prefab (runtime-built items in tests and spawned props).
        /// </summary>
        private GameObject CreateFallbackWrap()
        {
            var plastic = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plastic.name = "PlasticWrap";
            Destroy(plastic.GetComponent<Collider>());
            plastic.transform.SetParent(transform, false);
            plastic.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            plastic.transform.localScale = new Vector3(0.38f, 0.24f, 0.32f);

            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.75f, 0.92f, 1f, 0.28f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
            plastic.GetComponent<Renderer>().sharedMaterial = material;
            return plastic;
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
