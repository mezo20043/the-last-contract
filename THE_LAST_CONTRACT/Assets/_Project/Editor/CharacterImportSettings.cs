using UnityEditor;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// إعدادات استيراد تلقائية للموديلات البشرية والتكستشرات.
    ///
    /// الفايدة: مفيش أي خطوة يدوية. أول ما المشروع يتفتح، Unity بيستورد
    /// الشخصيات كـ Humanoid (فيتولّد Avatar ويتعرّف على العظام)،
    /// وبيظبط التكستشرات (Normal maps / ضغط / مقاس) لوحدها.
    /// </summary>
    public class CharacterImportSettings : AssetPostprocessor
    {
        const string CharacterFolder = "/Resources/Characters/";

        void OnPreprocessModel()
        {
            if (assetPath.IndexOf(CharacterFolder, System.StringComparison.Ordinal) < 0) return;

            var mi = assetImporter as ModelImporter;
            if (mi == null) return;

            // الهيكل البشري — عشان GetBoneTransform يشتغل
            mi.animationType = ModelImporterAnimationType.Human;
            mi.importAnimation = true;
            mi.animationCompression = ModelImporterAnimationCompression.Optimal;

            // المواد والتكستشرات
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.materialSearch = ModelImporterMaterialSearch.Everywhere;

            // تحسين للموبايل
            mi.importBlendShapes = true;
            mi.importVisibility = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.optimizeMeshPolygons = true;
            mi.optimizeMeshVertices = true;
            mi.isReadable = false;

            // مفيش Colliders من الميش — الحركة بالـ CharacterController
            mi.addCollider = false;

            Debug.Log($"[TLC/Import] إعداد Humanoid: {System.IO.Path.GetFileName(assetPath)}");
        }

        void OnPreprocessTexture()
        {
            if (assetPath.IndexOf("Characters/", System.StringComparison.Ordinal) < 0) return;

            var ti = assetImporter as TextureImporter;
            if (ti == null) return;

            bool isNormal = assetPath.ToLowerInvariant().Contains("normal");
            bool isMask = assetPath.ToLowerInvariant().Contains("metallic")
                       || assetPath.ToLowerInvariant().Contains("roughness");

            ti.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !isNormal && !isMask;      // الخرايط الخطية مش sRGB
            ti.maxTextureSize = 1024;
            ti.mipmapEnabled = true;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.anisoLevel = 2;
        }
    }
}
