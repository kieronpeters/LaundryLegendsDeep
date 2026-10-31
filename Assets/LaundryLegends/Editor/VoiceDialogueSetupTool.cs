using System;
using System.Collections.Generic;
using System.IO;
using LaundryLegends.NPC;
using LaundryLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds the dialogue voice library from the rendered WAVs and wires the
    /// three scene NPCs to speak bank lines with audio and subtitles.
    /// </summary>
    public static class VoiceDialogueSetupTool
    {
        private const string VoiceRoot = "Assets/LaundryLegends/Audio/Voice/Dialogue";
        private const string LibraryPath = "Assets/LaundryLegends/ScriptableObjects/SO_DialogueVoiceLibrary.asset";
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        private static readonly (string Npc, NpcArchetype Archetype, DialogueTrigger[] Triggers)[] Wiring =
        {
            ("Attendant", NpcArchetype.Attendant, new[] { DialogueTrigger.Counter, DialogueTrigger.Idle }),
            ("PhoneRegular", NpcArchetype.PhoneRegular, new[] { DialogueTrigger.Counter, DialogueTrigger.Waiting }),
            ("StreetCustomer", NpcArchetype.DropOff, new[] { DialogueTrigger.Enter, DialogueTrigger.Pickup })
        };

        [MenuItem("Laundry Legends/Phase 4/Wire Dialogue Voices")]
        public static void SetupAll()
        {
            DialogueVoiceLibrary library = BuildLibrary();
            WireScene(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[VOICE] Wired. entries={library.EntryCount} clips={library.TotalClipCount}");
        }

        [MenuItem("Laundry Legends/Phase 4/Validate Dialogue Voices")]
        public static void Validate()
        {
            var library = AssetDatabase.LoadAssetAtPath<DialogueVoiceLibrary>(LibraryPath);
            if (library == null)
            {
                throw new InvalidOperationException("No dialogue voice library asset.");
            }

            if (library.EntryCount != 15)
            {
                throw new InvalidOperationException($"Expected 15 archetype/trigger entries, found {library.EntryCount}.");
            }

            if (library.TotalClipCount != 54)
            {
                throw new InvalidOperationException($"Expected 54 clips, found {library.TotalClipCount}.");
            }

            Debug.Log("[VOICE] Validation passed: 15 entries, 54 clips.");
        }

        private static DialogueVoiceLibrary BuildLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<DialogueVoiceLibrary>(LibraryPath);
            if (library == null)
            {
                EnsureFolder("Assets/LaundryLegends/ScriptableObjects");
                library = ScriptableObject.CreateInstance<DialogueVoiceLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.Clear();

            foreach (NpcArchetype archetype in Enum.GetValues(typeof(NpcArchetype)))
            {
                foreach (DialogueTrigger trigger in Enum.GetValues(typeof(DialogueTrigger)))
                {
                    List<AudioClip> clips = LoadClips(archetype, trigger);
                    if (clips.Count > 0)
                    {
                        library.SetClips(archetype, trigger, clips.ToArray());
                    }
                }
            }

            EditorUtility.SetDirty(library);
            Debug.Log($"[VOICE] Library rebuilt: {library.EntryCount} entries, {library.TotalClipCount} clips.");
            return library;
        }

        private static List<AudioClip> LoadClips(NpcArchetype archetype, DialogueTrigger trigger)
        {
            string folder = $"{VoiceRoot}/{FolderFor(archetype)}";
            var clips = new List<AudioClip>();
            if (!Directory.Exists(folder))
            {
                return clips;
            }

            string prefix = $"{FolderFor(archetype)}_{trigger.ToString().ToLowerInvariant()}_";
            foreach (string file in Directory.GetFiles(folder, "*.wav"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{folder}/{Path.GetFileName(file)}");
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return clips;
        }

        private static string FolderFor(NpcArchetype archetype)
        {
            switch (archetype)
            {
                case NpcArchetype.Attendant:
                    return "attendant";
                case NpcArchetype.PhoneRegular:
                    return "phone_regular";
                default:
                    return "drop_off";
            }
        }

        private static void WireScene(DialogueVoiceLibrary library)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("AmbientActors");
            if (root == null)
            {
                throw new InvalidOperationException("No AmbientActors root.");
            }

            foreach ((string npcName, NpcArchetype archetype, DialogueTrigger[] triggers) in Wiring)
            {
                Transform npc = root.transform.Find(npcName);
                if (npc == null)
                {
                    throw new InvalidOperationException($"Missing NPC '{npcName}'.");
                }

                NpcDialogue dialogue = npc.GetComponent<NpcDialogue>();
                if (dialogue == null)
                {
                    dialogue = npc.gameObject.AddComponent<NpcDialogue>();
                }

                dialogue.ConfigureForBank(archetype, triggers[0]);
                dialogue.SetAmbientEnabled(false);

                AudioSource source = npc.GetComponent<AudioSource>();
                if (source == null)
                {
                    source = npc.gameObject.AddComponent<AudioSource>();
                }

                source.spatialBlend = 0f;
                source.playOnAwake = false;
                source.volume = 0.85f;

                SubtitleView subtitle = npc.GetComponent<SubtitleView>();
                if (subtitle == null)
                {
                    subtitle = npc.gameObject.AddComponent<SubtitleView>();
                }

                subtitle.Configure(dialogue, source);

                DialogueVoicePlayer player = npc.GetComponent<DialogueVoicePlayer>();
                if (player == null)
                {
                    player = npc.gameObject.AddComponent<DialogueVoicePlayer>();
                }

                player.Configure(archetype, library, source, dialogue, triggers);

                Debug.Log($"[VOICE] {npcName}: {archetype}, triggers={string.Join(",", triggers)}");
            }

            FixAmbience(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Removes the legacy stock voice clips and adds the rare street ambience.
        /// The old thank-you clip contained an unwanted recording, so the dialogue
        /// bank is now the only speech source.
        /// </summary>
        private static void FixAmbience(GameObject root)
        {
            var ambient = root.GetComponentInChildren<LaundryLegends.NPC.AmbientNpc>(true);
            if (ambient != null)
            {
                ambient.ClearVoiceClips();
            }

            GameObject ambience = GameObject.Find("StreetAmbience");
            if (ambience == null)
            {
                ambience = new GameObject("StreetAmbience");
            }

            if (ambience.GetComponent<AudioSource>() == null)
            {
                ambience.AddComponent<AudioSource>();
            }

            if (ambience.GetComponent<LaundryLegends.Audio.AmbientScooterHorn>() == null)
            {
                ambience.AddComponent<LaundryLegends.Audio.AmbientScooterHorn>();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                string name = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolder(parent);
                }

                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
