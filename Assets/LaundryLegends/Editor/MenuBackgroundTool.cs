using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Re-renders the main-menu background from the current scene, using the same
    /// camera as the original storefront shot. The old image still showed the grey
    /// NPCs from before the material fix.
    /// </summary>
    public static class MenuBackgroundTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        private const string OutputPath = "Assets/LaundryLegends/UI/MenuBackground.png";

        [MenuItem("Laundry Legends/Phase 8/Recapture Menu Background")]
        public static void Recapture()
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var cameraObject = new GameObject("MenuBackgroundCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            cameraObject.transform.position = new Vector3(7.5f, 2.8f, -14.0f);
            cameraObject.transform.LookAt(new Vector3(0f, 3.2f, -6.0f));

            var renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            camera.targetTexture = renderTexture;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            camera.Render();
            RenderTexture.active = previous;

            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(OutputPath, png);

            camera.targetTexture = null;
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);

            // Unity needs the texture imported as a sprite/tex for the RawImage; ensure
            // it is readable as a plain texture with mipmaps off for a full-screen UI blit.
            var importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MENU] background recaptured: {png.Length / 1024} KB -> {OutputPath}");
        }
    }
}
