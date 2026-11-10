using System;
using UnityEngine;

namespace TLC.Settings
{
    public enum JoystickSide
    {
        Left = 0,
        Right = 1
    }

    /// <summary>
    /// إعدادات تحكم الموبايل — محفوظة في PlayerPrefs وباقية بين الجلسات.
    ///
    /// ليه كلاس مستقل عن الواجهة: عشان القيم والمدى والقيم الافتراضية
    /// تكون قابلة للمراجعة والاختبار من غير ما نفتح Unity،
    /// والواجهة تبقى مجرد عرض.
    /// </summary>
    public static class ControlSettings
    {
        // ── مفاتيح الحفظ ──
        const string KeyLook     = "tlc.ctrl.look";
        const string KeyStick    = "tlc.ctrl.stick";
        const string KeySide     = "tlc.ctrl.side";
        const string KeyInvertY  = "tlc.ctrl.invertY";
        const string KeyAutoRun  = "tlc.ctrl.autoRun";

        // ── المدى المسموح ──
        public const float LookMin = 0.35f;
        public const float LookMax = 2.00f;
        public const float StickMin = 0.75f;
        public const float StickMax = 1.35f;
        public const float LookStep = 0.15f;

        // ── الافتراضيات ──
        public const float DefaultLook     = 1.00f;
        public const float DefaultStick    = 1.00f;
        public const JoystickSide DefaultSide = JoystickSide.Left;
        public const bool DefaultInvertY   = false;
        public const bool DefaultAutoRun   = false;

        /// <summary>بيتنادى كل ما إعداد يتغير عشان الواجهة تحدّث نفسها فوراً.</summary>
        public static event Action Changed;

        // ── القراءة (بترجّع قيم مقصوصة جوه المدى دايماً) ──
        public static float LookSensitivity =>
            Clamp(PlayerPrefs.GetFloat(KeyLook, DefaultLook), LookMin, LookMax);

        public static float StickScale =>
            Clamp(PlayerPrefs.GetFloat(KeyStick, DefaultStick), StickMin, StickMax);

        public static JoystickSide StickSide
        {
            get
            {
                int v = PlayerPrefs.GetInt(KeySide, (int)DefaultSide);
                return v == (int)JoystickSide.Right ? JoystickSide.Right : JoystickSide.Left;
            }
        }

        public static bool InvertY => PlayerPrefs.GetInt(KeyInvertY, DefaultInvertY ? 1 : 0) == 1;
        public static bool AutoRun => PlayerPrefs.GetInt(KeyAutoRun, DefaultAutoRun ? 1 : 0) == 1;

        // ── الكتابة ──
        public static void SetLookSensitivity(float v)
        {
            PlayerPrefs.SetFloat(KeyLook, Clamp(v, LookMin, LookMax));
            Commit();
        }

        public static void SetStickScale(float v)
        {
            PlayerPrefs.SetFloat(KeyStick, Clamp(v, StickMin, StickMax));
            Commit();
        }

        public static void SetStickSide(JoystickSide side)
        {
            PlayerPrefs.SetInt(KeySide, (int)side);
            Commit();
        }

        public static void ToggleStickSide() =>
            SetStickSide(StickSide == JoystickSide.Left ? JoystickSide.Right : JoystickSide.Left);

        public static void SetInvertY(bool v)
        {
            PlayerPrefs.SetInt(KeyInvertY, v ? 1 : 0);
            Commit();
        }

        public static void ToggleInvertY() => SetInvertY(!InvertY);

        public static void SetAutoRun(bool v)
        {
            PlayerPrefs.SetInt(KeyAutoRun, v ? 1 : 0);
            Commit();
        }

        public static void ToggleAutoRun() => SetAutoRun(!AutoRun);

        public static void ResetToDefaults()
        {
            PlayerPrefs.SetFloat(KeyLook, DefaultLook);
            PlayerPrefs.SetFloat(KeyStick, DefaultStick);
            PlayerPrefs.SetInt(KeySide, (int)DefaultSide);
            PlayerPrefs.SetInt(KeyInvertY, DefaultInvertY ? 1 : 0);
            PlayerPrefs.SetInt(KeyAutoRun, DefaultAutoRun ? 1 : 0);
            Commit();
        }

        static void Commit()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // ── مساعدات الْتَقْليب في القائمة ──

        /// <summary>القيمة اللي بعدها في اتجاه الحساسية (مع لفّ للبداية).</summary>
        public static float NextLook(float current)
        {
            float next = current + LookStep;
            if (next > LookMax + 0.001f) next = LookMin;
            return Clamp(next, LookMin, LookMax);
        }

        public static float NextStick(float current)
        {
            float next = current + 0.10f;
            if (next > StickMax + 0.001f) next = StickMin;
            return Clamp(next, StickMin, StickMax);
        }

        /// <summary>نسبة 0..1 — مفيدة لشريط في الواجهة.</summary>
        public static float Look01 => Mathf.InverseLerp(LookMin, LookMax, LookSensitivity);
        public static float Stick01 => Mathf.InverseLerp(StickMin, StickMax, StickScale);

        public static float Clamp(float v, float min, float max) =>
            float.IsNaN(v) ? min : Mathf.Clamp(v, min, max);

        /// <summary>نص وصفي للقائمة — رقمين عشريين كفاية.</summary>
        public static string LookLabel => LookSensitivity.ToString("0.00");
        public static string StickLabel => StickScale.ToString("0.00");
    }
}
