using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Static mapping from a speaker archetype and dialogue trigger to the
    /// optional voice clips for that key. Text stays in <see cref="DialogueBank"/>:
    /// a key with no clips still renders as a subtitle. Clip order matches the
    /// authored line order so the spoken sentence and its subtitle agree.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SO_DialogueVoiceLibrary",
        menuName = "Laundry Legends/Dialogue Voice Library")]
    public sealed class DialogueVoiceLibrary : ScriptableObject
    {
        /// <summary>One archetype/trigger key and its ordered clips.</summary>
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private NpcArchetype _archetype;
            [SerializeField] private DialogueTrigger _trigger;
            [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();

            /// <summary>Unity serialization constructor.</summary>
            public Entry()
            {
            }

            /// <summary>Creates a populated entry for editor tooling.</summary>
            /// <param name="archetype">Speaker archetype.</param>
            /// <param name="trigger">Conversation moment.</param>
            /// <param name="clips">Ordered clips; may be empty but never null.</param>
            public Entry(NpcArchetype archetype, DialogueTrigger trigger, AudioClip[] clips)
            {
                _archetype = archetype;
                _trigger = trigger;
                _clips = clips ?? Array.Empty<AudioClip>();
            }

            /// <summary>Speaker archetype this entry answers.</summary>
            public NpcArchetype Archetype => _archetype;

            /// <summary>Conversation moment this entry answers.</summary>
            public DialogueTrigger Trigger => _trigger;

            /// <summary>Ordered clips for the key; empty when no voice exists.</summary>
            public IReadOnlyList<AudioClip> Clips => _clips ?? Array.Empty<AudioClip>();
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        /// <summary>Number of archetype/trigger keys stored.</summary>
        public int EntryCount => _entries != null ? _entries.Length : 0;

        /// <summary>Total number of clips across every key.</summary>
        public int TotalClipCount
        {
            get
            {
                int total = 0;
                if (_entries == null)
                {
                    return total;
                }

                for (int i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i] != null)
                    {
                        total += _entries[i].Clips.Count;
                    }
                }

                return total;
            }
        }

        /// <summary>
        /// Returns the ordered clip pool for a key. Unknown keys return an empty
        /// list rather than null, so callers can always treat text-only entries
        /// the same way as voiced ones.
        /// </summary>
        /// <param name="archetype">Speaker archetype to look up.</param>
        /// <param name="trigger">Conversation moment to look up.</param>
        /// <returns>Ordered clips; empty for an unknown key.</returns>
        public IReadOnlyList<AudioClip> GetClips(NpcArchetype archetype, DialogueTrigger trigger)
        {
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    Entry entry = _entries[i];
                    if (entry != null && entry.Archetype == archetype && entry.Trigger == trigger)
                    {
                        return entry.Clips;
                    }
                }
            }

            return Array.Empty<AudioClip>();
        }

        /// <summary>Number of clips available for a key; zero when unvoiced.</summary>
        /// <param name="archetype">Speaker archetype to look up.</param>
        /// <param name="trigger">Conversation moment to look up.</param>
        /// <returns>The clip count for the key.</returns>
        public int GetClipCount(NpcArchetype archetype, DialogueTrigger trigger)
        {
            return GetClips(archetype, trigger).Count;
        }

        /// <summary>Total clips owned by one archetype across all triggers.</summary>
        /// <param name="archetype">Speaker archetype to total.</param>
        /// <returns>The number of clips for the archetype.</returns>
        public int GetArchetypeClipCount(NpcArchetype archetype)
        {
            int total = 0;
            foreach (DialogueTrigger trigger in Enum.GetValues(typeof(DialogueTrigger)))
            {
                total += GetClipCount(archetype, trigger);
            }

            return total;
        }

        /// <summary>Removes every stored key. Used by the editor tool before a rebuild.</summary>
        public void Clear()
        {
            _entries = Array.Empty<Entry>();
        }

        /// <summary>
        /// Adds or replaces one archetype/trigger key. Kept public so the editor
        /// tool can rebuild the asset without touching serialized internals.
        /// </summary>
        /// <param name="archetype">Speaker archetype.</param>
        /// <param name="trigger">Conversation moment.</param>
        /// <param name="clips">Ordered clips; may be empty but never null.</param>
        public void SetClips(NpcArchetype archetype, DialogueTrigger trigger, AudioClip[] clips)
        {
            AudioClip[] safeClips = clips ?? Array.Empty<AudioClip>();
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    Entry entry = _entries[i];
                    if (entry != null && entry.Archetype == archetype && entry.Trigger == trigger)
                    {
                        _entries[i] = new Entry(archetype, trigger, safeClips);
                        return;
                    }
                }
            }

            int count = _entries != null ? _entries.Length : 0;
            var grown = new Entry[count + 1];
            if (count > 0)
            {
                Array.Copy(_entries, grown, count);
            }

            grown[count] = new Entry(archetype, trigger, safeClips);
            _entries = grown;
        }
    }
}
