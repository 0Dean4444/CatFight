#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CatFight.EditorTools
{
    [InitializeOnLoad]
    public static class CatFightAppIconAssigner
    {
        private const string IconAssetPath = "Assets/Resources/Sprites/UI/app_icon.png";

        static CatFightAppIconAssigner()
        {
            EditorApplication.delayCall += TryApplyAppIcon;
        }

        [MenuItem("CatFight/Cập Nhật Icon Đại Diện Game (App Icon)")]
        public static void TryApplyAppIcon()
        {
            Texture2D iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAssetPath);
            if (iconTex == null)
                return;

            TextureImporter importer = AssetImporter.GetAtPath(IconAssetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite && importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAssetPath);
            }

            if (iconTex != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new Texture2D[] { iconTex }, IconKind.Any);
            }
        }
    }
}
#endif
