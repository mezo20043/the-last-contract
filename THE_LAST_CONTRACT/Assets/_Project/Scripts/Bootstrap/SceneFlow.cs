using UnityEngine;
using UnityEngine.SceneManagement;

namespace TLC.Bootstrap
{
    /// <summary>
    /// الانتقال بين المشاهد. كل حاجة في مكان واحد عشان الأسماء تتحقق في مكان واحد.
    ///
    /// التحقق المسبق مهم: لو المشهد مش مضاف في Build Settings،
    /// <c>SceneManager.LoadScene</c> بيرمي استثناء ويوقف اللعبة.
    /// هنا بنفحص الأول وبنسجّل خطأ مفهوم بدل ما اللعبة تقع.
    /// </summary>
    public static class SceneFlow
    {
        public const string MainScene = "Main";
        public const string PlayScene = "Play";

        /// <summary>هل المشهد مضاف في Build Settings ومتاح للتحميل؟</summary>
        public static bool CanLoad(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            try
            {
                return Application.CanStreamedLevelBeLoaded(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TLC/Scenes] تعذّر فحص المشهد '{sceneName}': {e.Message}");
                return false;
            }
        }

        /// <summary>بيحمّل مشهد اللعب. بيرجّع false لو المشهد مش في Build Settings.</summary>
        public static bool LoadPlay()
        {
            if (!CanLoad(PlayScene))
            {
                Debug.LogError(
                    "[TLC/Scenes] مشهد 'Play' مش مضاف في Build Settings — " +
                    "افتح File → Build Profiles وشيل علامة المشهد، أو منيو TLC → 5.");
                return false;
            }

            Debug.Log("[TLC/Scenes] تحميل مشهد اللعب.");
            SceneManager.LoadScene(PlayScene, LoadSceneMode.Single);
            return true;
        }

        public static bool LoadMain()
        {
            if (!CanLoad(MainScene))
            {
                Debug.LogError("[TLC/Scenes] مشهد 'Main' مش في Build Settings.");
                return false;
            }

            SceneManager.LoadScene(MainScene, LoadSceneMode.Single);
            return true;
        }

        /// <summary>اسم المشهد الحالي — بيفيد في السجلات والتشخيص.</summary>
        public static string ActiveName()
        {
            var s = SceneManager.GetActiveScene();
            return s.IsValid() ? s.name : "(مشهد غير معروف)";
        }
    }
}
