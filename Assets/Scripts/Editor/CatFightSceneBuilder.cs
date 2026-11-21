#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatFight.Managers;

namespace CatFight.EditorTools
{
    [InitializeOnLoad]
    public static class CatFightSceneBuilder
    {
        static CatFightSceneBuilder()
        {
            EditorApplication.delayCall += EnsureProjectConfigured;
        }

        [MenuItem("CatFight/Configure Sprites & Setup SampleScene")]
        public static void ManualSetupMenu()
        {
            ConfigureAllSpritesInResources();
            EnsureSceneHasBootstrap(true);
            Debug.Log("[Argon - CatFight] All 2D Cartoon Sprites and SampleScene configured successfully!");
        }

        private static void EnsureProjectConfigured()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            ConfigureAllSpritesInResources();
            EnsureSceneHasBootstrap(false);
        }

        private static void ConfigureAllSpritesInResources()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Sprites" });
            bool changedAny = false;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool needsReimport = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    needsReimport = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    needsReimport = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    needsReimport = true;
                }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    needsReimport = true;
                }

                if (needsReimport)
                {
                    importer.SaveAndReimport();
                    changedAny = true;
                }
            }

            if (changedAny)
                AssetDatabase.Refresh();
        }

        private static void EnsureSceneHasBootstrap(bool forceSave)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            Camera mainCam = Camera.main;
            if (mainCam != null && !mainCam.orthographic)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5.0f;
                mainCam.transform.position = new Vector3(0f, 0f, -10f);
                mainCam.transform.rotation = Quaternion.identity;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.18f, 0.14f, 0.13f, 1f);
                EditorUtility.SetDirty(mainCam.gameObject);
            }

            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                GameObject go = new GameObject("CatFight_GameManager");
                go.AddComponent<GameManager>();
                EditorSceneManager.MarkSceneDirty(activeScene);
                if (forceSave)
                    EditorSceneManager.SaveScene(activeScene);
            }
        }
    }
}
#endif
