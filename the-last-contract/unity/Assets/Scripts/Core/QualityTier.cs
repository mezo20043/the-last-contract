using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TLC.Core
{
    /// <summary>
    /// يكتشف مستوى الجهاز ويظبّط الجودة + معدّل الإطارات.
    /// الحاجات اللي بيتحكم فيها على الموبايل: ظلال، بعد معالجة، كثافة بكسل، نطاق رؤية.
    /// </summary>
    public class QualityTier : MonoBehaviour
    {
        public enum Tier { Low, Mid, High }

        [System.Serializable]
        public class TierSettings
        {
            public string name;
            public float renderScale = 1f;
            public int shadowDistance = 20;
            public bool bloom = true;
            public bool vignette = true;
            public bool filmGrain = false;
            public bool ssao = false;
            public int targetFrameRate = 30;
            public int textureMipLimit = 1;   // 0=كامل 1=نص 2=ربع
        }

        [SerializeField] TierSettings low  = new() { name="Low",  renderScale=0.75f, shadowDistance=12, bloom=false, vignette=true,  filmGrain=false, ssao=false, targetFrameRate=30, textureMipLimit=2 };
        [SerializeField] TierSettings mid  = new() { name="Mid",  renderScale=0.9f,  shadowDistance=18, bloom=true,  vignette=true,  filmGrain=false, ssao=false, targetFrameRate=30, textureMipLimit=1 };
        [SerializeField] TierSettings high = new() { name="High", renderScale=1f,    shadowDistance=25, bloom=true,  vignette=true,  filmGrain=true,  ssao=false, targetFrameRate=60, textureMipLimit=0 };

        [SerializeField] UniversalRenderPipelineAsset urpAsset;
        [SerializeField] Volume postProcessVolume;

        public Tier CurrentTier { get; private set; }

        void Awake() => Apply(Detect());

        Tier Detect()
        {
            // تقدير بسيط — بدّله بمقارنة SystemInfo.processorType / graphicsMemorySize في البناء النهائي
            int mem = SystemInfo.graphicsMemorySize;
            int cores = SystemInfo.processorCount;

#if UNITY_EDITOR
            return Tier.High;
#endif
            if (mem >= 3000 && cores >= 8) return Tier.High;
            if (mem >= 1500) return Tier.Mid;
            return Tier.Low;
        }

        public void Apply(Tier tier)
        {
            CurrentTier = tier;
            var s = tier switch { Tier.Low => low, Tier.Mid => mid, _ => high };

            Application.targetFrameRate = s.targetFrameRate;
            QualitySettings.vSyncCount = 0;

            if (urpAsset != null)
            {
                urpAsset.renderScale = s.renderScale;
                urpAsset.shadowDistance = s.shadowDistance;
                urpAsset.supportsHDR = tier != Tier.Low;
            }

            if (postProcessVolume != null &&
                postProcessVolume.profile.TryGet(out Bloom bloom)) bloom.active = s.bloom;

            if (postProcessVolume != null &&
                postProcessVolume.profile.TryGet(out Vignette vig)) vig.active = s.vignette;

            if (postProcessVolume != null &&
                postProcessVolume.profile.TryGet(out FilmGrain grain)) grain.active = s.filmGrain;

            if (postProcessVolume != null &&
                postProcessVolume.profile.TryGet(out ScreenSpaceAmbientOcclusion ssao)) ssao.active = s.ssao;

            QualitySettings.globalTextureMipmapLimit = s.textureMipLimit;

            Debug.Log($"[TLC] جودة الجهاز: {s.name} — {s.targetFrameRate} إطار");
        }
    }
}
