using UnityEngine;
using TLC.Settings;

namespace TLC.Mobile
{
    /// <summary>
    /// تحسينات أندرويد وقت التشغيل:
    /// معدل إطارات متكيّف، تعامل مع سخونة الجهاز، إيقاف الظلال عند التبريد،
    /// وتخفيف الحمل لما اللعبة تروح للخلفية.
    /// </summary>
    public class MobileOptimizations : MonoBehaviour
    {
        [Header("معدل الإطارات")]
        [SerializeField] int idleFrameRate = 30;
        [SerializeField] int activeFrameRate = 60;
        [SerializeField] bool allowAdaptiveFrameRate = true;

        [Header("الحرارة")]
        [SerializeField] float throttleCheckInterval = 5f;
        [SerializeField] int sustainedFramesBeforeThrottle = 3;

        QualityManager _quality;
        float _timer;
        int _overBudgetCount;
        bool _throttled;
        float _smoothedDelta = 0.016f;

        public void Setup(QualityManager quality)
        {
            _quality = quality;
            ApplyBaseline();
        }

        void ApplyBaseline()
        {
            Application.targetFrameRate = idleFrameRate;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void Update()
        {
            if (!allowAdaptiveFrameRate) return;

            _smoothedDelta = Mathf.Lerp(_smoothedDelta, Time.unscaledDeltaTime, 0.05f);

            _timer += Time.unscaledDeltaTime;
            if (_timer < throttleCheckInterval) return;
            _timer = 0f;

            float fps = 1f / Mathf.Max(0.0001f, _smoothedDelta);

            if (fps < idleFrameRate * 0.8f)
            {
                _overBudgetCount++;

                if (_overBudgetCount >= sustainedFramesBeforeThrottle && !_throttled)
                {
                    Throttle();
                }
            }
            else
            {
                _overBudgetCount = 0;

                if (_throttled && fps > idleFrameRate * 1.1f)
                    Recover();
            }
        }

        void Throttle()
        {
            _throttled = true;
            Application.targetFrameRate = idleFrameRate;

            QualitySettings.shadowDistance *= 0.7f;
            QualitySettings.antiAliasing = 0;

            Debug.Log($"[TLC/Mobile] الجهاز تحت الحمل — تخفيف الجودة (FPS≈{1f / _smoothedDelta:0})");
        }

        void Recover()
        {
            _throttled = false;
            Application.targetFrameRate = activeFrameRate;
            _quality?.Apply(_quality.CurrentTier);
            Debug.Log("[TLC/Mobile] الأداء رجع طبيعي.");
        }

        void OnApplicationPause(bool paused)
        {
            // توفير بطارية: وقف الرندرة لما اللعبة تروح للخلفية
            Application.targetFrameRate = paused ? 15 : idleFrameRate;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) Application.targetFrameRate = 15;
            else if (!_throttled) Application.targetFrameRate = idleFrameRate;
        }
    }
}
