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
