using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TLC.EditorTools
{
    /// <summary>
    /// إنشاء وربط خطّ معالجة URP بالمشروع.
    ///
    /// ليه؟ المشروع بيستخدم مواد `Universal Render Pipeline/Lit`، لكن
    /// `ProjectSettings/GraphicsSettings.asset` كان `m_CustomRenderPipeline: {fileID: 0}`
    /// ⇒ المحرك بيشتغل على Built-in RP ⇒ **كل المواد بتطلع ماجنتا**.
    /// وده بالظبط اللي ظهر في أول ٨ لقطات حقيقية من Play Mode.
    /// </summary>
    public static class URPSetup
    {
        const string Dir = "Assets/_Project/Settings";
        const string RendererPath = Dir + "/TLC_UniversalRenderer.asset";
        const string PipelinePath = Dir + "/TLC_URP.asset";

        [MenuItem("TLC/إعداد URP")]
        public static void Setup() => Run(true);

        public static void Run(bool verbose)
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/_Project", "Settings");

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                if (verbose) Debug.Log("[TLC] اتعمل Renderer: " + RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                if (verbose) Debug.Log("[TLC] اتعمل Pipeline: " + PipelinePath);
            }

            // ضبط جودة مناسب للموبايل + HDR للسينمائية
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 30f;
            pipeline.shadowCascadeCount = 2;

            pipeline.shadowDepthBias = 1.0f;
            pipeline.maxAdditionalLightsCount = 4;
            EditorUtility.SetDirty(pipeline);

            // الربط على مستوى المشروع + على مستوى الجودة
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (verbose)
                Debug.Log("[TLC] ✅ URP مربوط: GraphicsSettings.defaultRenderPipeline = " +
                          (GraphicsSettings.defaultRenderPipeline != null
                              ? GraphicsSettings.defaultRenderPipeline.name
                              : "NULL"));
        }
    }
}
