using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Imports the CC0 Quaternius Universal Animation Library FBX as a humanoid
    /// rig and reports the animation takes Unity generated from it.
    /// </summary>
    public static class AnimationSetupTool
    {
        public const string AnimationRoot = "Assets/LaundryLegends/Art/Animations";
        public const string LibraryPath = AnimationRoot + "/UAL1_Standard.fbx";

        [MenuItem("Laundry Legends/Phase 4/Import Animation Library")]
        public static void ImportAnimationLibrary()
        {
            if (!File.Exists(LibraryPath))
            {
                throw new InvalidOperationException($"Missing animation FBX: {LibraryPath}");
            }

            var importer = AssetImporter.GetAtPath(LibraryPath) as ModelImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(LibraryPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(LibraryPath) as ModelImporter;
            }

            if (importer == null)
            {
                throw new InvalidOperationException($"No ModelImporter for {LibraryPath}");
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importNormals = ModelImporterNormals.None;
            importer.importTangents = ModelImporterTangents.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.SaveAndReimport();

            AssetDatabase.Refresh();
            ReportClips();
        }

        [MenuItem("Laundry Legends/Phase 4/Report Animation Clips")]
        public static void ReportClips()
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(LibraryPath);
            foreach (UnityEngine.Object asset in avatar)
            {
                if (asset is Avatar humanoid)
                {
                    Debug.Log($"[ANIM] Avatar '{humanoid.name}' isHuman={humanoid.isHuman} isValid={humanoid.isValid}");
                }
            }

            List<AnimationClip> clips = LoadClips();
            Debug.Log($"[ANIM] Clip count={clips.Count}");
            foreach (AnimationClip clip in clips)
            {
                Debug.Log($"[ANIM] CLIP|{clip.name}|{clip.length:0.000}");
            }
        }

        public static List<AnimationClip> LoadClips()
        {
            var clips = new List<AnimationClip>();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(LibraryPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    clips.Add(clip);
                }
            }

            clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return clips;
        }
    }
}
