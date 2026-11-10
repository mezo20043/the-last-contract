using UnityEngine;

namespace TLC.Settings
{
    /// <summary>
    /// ثلاثة مستويات جودة (LOW / MEDIUM / HIGH) بتظبّط الظلال، المسافة، الجزيئات،
    /// ومعدل الإطارات. الكشف أوتوماتيكي على الموبايل وقابل للتبديل يدوياً.
    /// </summary>
    public class QualityManager : MonoBehaviour
    {
        public enum Tier { Low = 0, Medium = 1, High = 2 }

        [System.Serializable]
        public class Preset
        {
            public string name;
            public int targetFrameRate = 30;
            public float shadowDistance = 18f;
            public ShadowResolution shadowResolution = ShadowResolution.Medium;
            public int pixelLightCount = 1;
            public float cameraFarClip = 60f;
            public float rainEmissionRate = 300f;
            public int maxParticles = 2000;
            public bool usePostProcessing = false;
            public float lodBias = 1f;
        }

        [SerializeField] Preset low = new Preset
        {
            name = "LOW", targetFrameRate = 30, shadowDistance = 12f,
            shadowResolution = ShadowResolution.Low, pixelLightCount = 1,
            cameraFarClip = 45f, rainEmissionRate = 150f, maxParticles = 800,
            usePostProcessing = false, lodBias = 0.7f
        };

        [SerializeField] Preset medium = new Preset
        {
            name = "MEDIUM", targetFrameRate = 30, shadowDistance = 22f,
            shadowResolution = ShadowResolution.Medium, pixelLightCount = 2,
            cameraFarClip = 60f, rainEmissionRate = 400f, maxParticles = 2000,
            usePostProcessing = true, lodBias = 1f
        };

        [SerializeField] Preset high = new Preset
        {
            name = "HIGH", targetFrameRate = 60, shadowDistance = 40f,
            shadowResolution = ShadowResolution.High, pixelLightCount = 4,
            cameraFarClip = 90f, rainEmissionRate = 900f, maxParticles = 5000,
            usePostProcessing = true, lodBias = 1.6f
        };

        [SerializeField] Camera targetCamera;
        [SerializeField] ParticleSystem rain;

        public Tier CurrentTier { get; private set; }
        public Preset CurrentPreset { get; private set; }

        /// <summary>مرجع عالمي — الواجهة بتقرأ منه المستوى الحالي.</summary>
        public static QualityManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            Apply(Detect());
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>يحفظ اختيار اللاعب ويرجع له بعد إعادة التشغيل.</summary>
        public void SetTierFromUser(Tier tier)
        {
            PlayerPrefs.SetInt("tlc.quality", (int)tier);
            PlayerPrefs.Save();
            Apply(tier);
        }

        public static QualityManager.Tier SavedTier(int fallback) =>
            (Tier)PlayerPrefs.GetInt("tlc.quality", fallback);

        Tier Detect()
        {
            // تقدير: على الموبايل منخفض افتراضياً، والرام هي المؤشر المتاح عالمياً
            if (Application.isMobilePlatform)
                return SystemInfo.systemMemorySize >= 6000 ? Tier.Medium : Tier.Low;

            return Tier.High;
        }

        public void Apply(Tier tier)
        {
            CurrentTier = tier;
            Preset p = tier == Tier.Low ? low : tier == Tier.Medium ? medium : high;
            CurrentPreset = p;

            Application.targetFrameRate = p.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowDistance = p.shadowDistance;
            QualitySettings.shadowResolution = p.shadowResolution;
            QualitySettings.pixelLightCount = p.pixelLightCount;
            QualitySettings.lodBias = p.lodBias;
            QualitySettings.antiAliasing = tier == Tier.High ? 4 : tier == Tier.Medium ? 2 : 0;

            if (targetCamera != null)
            {
                targetCamera.farClipPlane = p.cameraFarClip;
                targetCamera.allowHDR = tier != Tier.Low;
            }

            if (rain != null)
            {
                var main = rain.main;
                main.maxParticles = p.maxParticles;

                var emission = rain.emission;
                emission.rateOverTime = p.rainEmissionRate;
            }

            Debug.Log($"[TLC] الجودة: {p.name} · {p.targetFrameRate} إطار");
        }

        public void Next()
        {
            Apply((Tier)(((int)CurrentTier + 1) % 3));
        }
    }
}
