// 4.4 RECOVERY — SettingsPersistence.cs — **مُعاد حرفيًا**
using UnityEngine;

namespace TLC.Settings
{
    /// <summary>
    /// بيربط إعدادات اللعبة بـPlayerPrefs عند بداية التشغيل، فالإعدادات
    /// (لغة/حساسية/جودة/صوت) بتفضل زي ما اللاعب سابها بعد قفل التطبيق.
    /// </summary>
    public static class SettingsPersistence
    {
        const string KeyLanguage = "tlc.settings.language";

        static bool _wired;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Wire();
            Localization.LoadPersisted();
            ApplySavedQuality();
            Debug.Log($"[TLC/Settings] إعدادات مستعادة: لغة={(Localization.IsRtl ? "ar" : "en")} · " +
                      $"حساسية={ControlSettings.LookLabel} · جودة={QualityManager.SavedTier(1)}");
        }

        public static void Wire()
        {
            if (_wired) return;
            _wired = true;

            Localization.LoadHook = () => PlayerPrefs.GetInt(KeyLanguage, (int)Localization.Lang.Arabic);
            Localization.SaveHook = v => { PlayerPrefs.SetInt(KeyLanguage, v); PlayerPrefs.Save(); };
        }

        static void ApplySavedQuality()
        {
            QualitySettings.SetQualityLevel((int)QualityManager.SavedTier(1), true);
        }

        /// <summary>للاختبار: اللغة المحفوظة على القرص فعلاً.</summary>
        public static int PersistedLanguage => PlayerPrefs.GetInt(KeyLanguage, (int)Localization.Lang.Arabic);
    }
}
