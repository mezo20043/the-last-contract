using UnityEngine;

namespace TLC.Bootstrap
{
    /// <summary>إزاي دخلنا مشهد اللعب.</summary>
    public enum PlayEntryMode
    {
        NewGame = 0,
        Continue = 1,
        Chapter = 2
    }

    /// <summary>
    /// تسليم الطلب بين مشهد القائمة (Main) ومشهد اللعب (Play).
    ///
    /// Unity بيمسح كل كائنات المشهد القديم لما بنحمل مشهد جديد، فمينفعش نمرّر
    /// الطلب عن طريق مرجع في المشهد. الحل: حالة ثابتة (static) بتعيش أطول من المشهد،
    /// ومشهد اللعب بياخدها ويصفّرها مرة واحدة.
    ///
    /// ليه مش PlayerPrefs؟ عشان الطلب مؤقت ومش عايزين "ابدأ لعبة جديدة"
    /// يفضل متعلّق لو التطبيق قفل في النص.
    /// </summary>
    public static class PlaySession
    {
        public static bool HasRequest { get; private set; }
        public static PlayEntryMode Mode { get; private set; } = PlayEntryMode.NewGame;

        /// <summary>رقم الفصل 1..3 — بيتستخدم مع <see cref="PlayEntryMode.Chapter"/>.</summary>
        public static int Chapter { get; private set; } = 1;

        public static void RequestNewGame()
        {
            Set(PlayEntryMode.NewGame, 1);
        }

        public static void RequestContinue()
        {
            Set(PlayEntryMode.Continue, 0);
        }

        public static void RequestChapter(int chapter)
        {
            Set(PlayEntryMode.Chapter, Mathf.Clamp(chapter, 1, 3));
        }

        static void Set(PlayEntryMode mode, int chapter)
        {
            Mode = mode;
            Chapter = chapter;
            HasRequest = true;
        }

        /// <summary>بياخد الطلب مرة واحدة بس — أي نداء تاني بيرجّع false.</summary>
        public static bool Consume(out PlayEntryMode mode, out int chapter)
        {
            mode = Mode;
            chapter = Chapter;
            bool had = HasRequest;
            HasRequest = false;
            return had;
        }

        public static void Clear()
        {
            HasRequest = false;
            Mode = PlayEntryMode.NewGame;
            Chapter = 1;
        }

        /// <summary>وصف مقروء للسجل — بيتطبع مرة واحدة عند بداية اللعب.</summary>
        public static string Describe(PlayEntryMode mode, int chapter)
        {
            switch (mode)
            {
                case PlayEntryMode.Continue: return "متابعة آخر حفظ";
                case PlayEntryMode.Chapter:  return $"بداية من الفصل {chapter}";
                default:                     return "لعبة جديدة من الفصل 1";
            }
        }
    }
}
