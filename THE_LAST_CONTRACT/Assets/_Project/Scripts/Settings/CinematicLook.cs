using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TLC.Settings
{
    /// <summary>
    /// المظهر السينمائي: بيركّب Volume عام على الكاميرا فيه Tonemapping ACES، Bloom،
    /// Vignette، تصحيح ألوان، WhiteBalance، وFilmGrain خفيف — كلها متفصّلة للموبايل.
    ///
    /// بيراعي مستوى الجودة: LOW بيشغّل Tonemapping + Vignette بس (رخيص)،
    /// MEDIUM بيضيف Bloom وColorAdjustments، HIGH بيضيف DepthOfField وFilmGrain.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CinematicLook : MonoBehaviour
    {
        public Volume Volume { get; private set; }
        public VolumeProfile Profile { get; private set; }

        Bloom _bloom;
        Vignette _vignette;
        ColorAdjustments _color;
        Tonemapping _tone;
        DepthOfField _dof;
        FilmGrain _grain;
        WhiteBalance _wb;
        MotionBlur _motion;
        ChromaticAberration _chroma;

        QualityManager.Tier _tier = QualityManager.Tier.Medium;

        public void Build(QualityManager.Tier tier)
        {
            _tier = tier;

            var camData = GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = tier == QualityManager.Tier.Low
                ? AntialiasingMode.None
                : AntialiasingMode.FastApproximateAntialiasing;
            camData.renderShadows = true;

            var go = new GameObject("CinematicLook_Volume");
            go.transform.SetParent(transform, false);
            Volume = go.AddComponent<Volume>();
            Volume.isGlobal = true;
            Volume.priority = 10f;

            Profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Profile.name = "TLC_CinematicLook_" + tier;
            Volume.sharedProfile = Profile;

            // ── Tonemapping: ACES عشان الألوان تبان "فيلم" مش "لعبة" ──
            _tone = Profile.Add<Tonemapping>(true);
            _tone.mode.Override(TonemappingMode.ACES);

            // ── Vignette: بيحبس العين على آدم ──
            _vignette = Profile.Add<Vignette>(true);
            _vignette.intensity.Override(tier == QualityManager.Tier.Low ? 0.22f : 0.30f);
            _vignette.smoothness.Override(0.55f);
            _vignette.color.Override(new Color(0.02f, 0.02f, 0.04f));

            // ── تصحيح الألوان: ليل أزرق دافي مع رفع الظلال ──
            _color = Profile.Add<ColorAdjustments>(true);
            _color.postExposure.Override(0.15f);
            _color.contrast.Override(12f);
            _color.saturation.Override(tier == QualityManager.Tier.Low ? 2f : 8f);
            _color.colorFilter.Override(new Color(0.98f, 0.99f, 1.0f));

            _wb = Profile.Add<WhiteBalance>(true);
            _wb.temperature.Override(-6f);
            _wb.tint.Override(4f);

            if (tier != QualityManager.Tier.Low)
            {
                _bloom = Profile.Add<Bloom>(true);
                _bloom.threshold.Override(1.05f);
                _bloom.intensity.Override(tier == QualityManager.Tier.High ? 0.55f : 0.35f);
                _bloom.scatter.Override(0.62f);
                _bloom.tint.Override(new Color(1f, 0.94f, 0.88f));
            }

            if (tier == QualityManager.Tier.High)
            {
                _dof = Profile.Add<DepthOfField>(true);
                _dof.mode.Override(DepthOfFieldMode.Gaussian);
                _dof.gaussianStart.Override(6f);
                _dof.gaussianEnd.Override(22f);
                _dof.gaussianMaxRadius.Override(0.55f);

                _grain = Profile.Add<FilmGrain>(true);
                _grain.type.Override(FilmGrainLookup.Thin1);
                _grain.intensity.Override(0.12f);

                _motion = Profile.Add<MotionBlur>(true);
                _motion.intensity.Override(0.12f);
            }
        }

        /// <summary>حالة "توتر" سينمائية: تشبع أعلى وفينييت أقوى وبلوم أوضح.</summary>
        public void SetMood(float tension01)
        {
            tension01 = Mathf.Clamp01(tension01);
            if (_vignette != null) _vignette.intensity.Override(Mathf.Lerp(0.28f, 0.48f, tension01));
            if (_color != null)
            {
                _color.saturation.Override(Mathf.Lerp(4f, -18f, tension01));
                _color.contrast.Override(Mathf.Lerp(12f, 22f, tension01));
            }
            if (_bloom != null) _bloom.intensity.Override(Mathf.Lerp(0.35f, 0.7f, tension01));
        }

        /// <summary>لقطة حوار: عمق ميدان ضحل يبرز الوش.</summary>
        public void SetDialogueFocus(bool on)
        {
            if (_dof == null) return;
            _dof.gaussianStart.Override(on ? 1.6f : 6f);
            _dof.gaussianEnd.Override(on ? 7f : 22f);
            _dof.gaussianMaxRadius.Override(on ? 1.1f : 0.55f);
        }
    }
}
