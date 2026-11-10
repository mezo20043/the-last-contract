// 4.4 RECOVERY — ExtrasCatalog.cs — **مُعاد حرفيًا**
using System.Collections.Generic;
using TLC.Cinematics;
using TLC.World;

namespace TLC.Core
{
    /// <summary>عنصر في مكتبة الإضافات (مشهد/فيلم/شخصية/سجل).</summary>
    public class ExtrasItem
    {
        public string id;
        public string title;
        public string detail;
        public bool unlocked;

        public ExtrasItem(string id, string title, string detail, bool unlocked)
        {
            this.id = id; this.title = title; this.detail = detail; this.unlocked = unlocked;
        }
    }

    /// <summary>
    /// كتالوج الإضافات — بيقرأ التقدّم الحقيقي (Progress) ومحتوى القصة،
    /// فالمقفول بيظهر مقفول مش «مفتوح بزرار».
    /// </summary>
    public static class ExtrasCatalog
    {
        public struct SceneInfo
        {
            public string act;
            public string title;
            public SceneInfo(string a, string t) { act = a; title = t; }
        }

        /// <summary>مشاهد الفصل الأول الستة بترتيب القصة.</summary>
        public static readonly SceneInfo[] ChapterOneScenes =
        {
            new SceneInfo("scene01", "شقة آدم — الدور التاسع"),
            new SceneInfo("scene03", "مقهى مدينة نصر"),
            new SceneInfo("scene04", "مكتب الصحافة — وسط البلد"),
            new SceneInfo("scene05", "مبنى الأرشيف القديم"),
            new SceneInfo("scene06", "مكتب التحقيقات"),
            new SceneInfo("scene08", "مكتب يوسف — رجل الأعمال"),
        };

        public static List<ExtrasItem> Scenes()
        {
            var list = new List<ExtrasItem>();
            foreach (var s in ChapterOneScenes)
            {
                bool seen = Progress.HasScene(s.act);
                list.Add(new ExtrasItem(s.act, seen ? s.title : "مشهد مقفول",
                                        seen ? "اتزار في اللعب" : "لعب المشهد عشان يفتح", seen));
            }
            return list;
        }

        public static List<ExtrasItem> Cinematics()
        {
            var list = new List<ExtrasItem>
            {
                new ExtrasItem("opening", "الافتتاحية — شقة آدم",
                               Progress.HasCinematic("opening") ? "اتفرّجت عليها" : "لسه ماتفرّجتش",
                               Progress.HasCinematic("opening")),
                new ExtrasItem(ChapterTwoTeaser.CinematicId, "تيزر الفصل الثاني",
                               Progress.HasCinematic(ChapterTwoTeaser.CinematicId) ? "اتفرّجت عليه" : "بيتفتح بعد أول نهاية",
                               Progress.HasPostCreditAny() || Progress.HasCinematic(ChapterTwoTeaser.CinematicId) || Progress.Current.teaserSeen),
            };

            foreach (var e in PostCreditSequence.SupportedEndings)
            {
                bool seen = Progress.HasSeenPostCredit(e);
                list.Add(new ExtrasItem(PostCreditSequence.CinematicId(e),
                                        $"ما بعد النهاية {e.Substring(e.Length - 1)}",
                                        seen ? "اتفرّجت عليه" : "بيتفتح لما توصل النهاية",
                                        seen));
            }
            return list;
        }

        public static List<ExtrasItem> Characters()
        {
            bool murad = Progress.HasScene("scene03");
            bool laila = Progress.HasScene("scene04");
            bool yassin = Progress.HasScene("scene06");
            bool yusuf = Progress.HasScene("scene08");

            return new List<ExtrasItem>
            {
                new ExtrasItem("adam", "آدم — بطل الحكاية",
                               "موظف أرشيف، بيصحى على ظرف مالوش مُرسل.", true),
                new ExtrasItem("murad", murad ? "مراد — صاحب المقهى" : "مراد — مقفول",
                               murad ? "صاحب آدم من زمان، وبيسأل أسئلة كتير." : "اتقابل في المقهى", murad),
                new ExtrasItem("laila", laila ? "ليلى — صحفية" : "ليلى — مقفولة",
                               laila ? "بتدور على نفس الملف من ناحية تانية." : "اتقابلت في مكتب الصحافة", laila),
                new ExtrasItem("yassin", yassin ? "ياسين — ضابط" : "ياسين — مقفول",
                               yassin ? "ملف القضية القديمة على مكتبه." : "اتقابل في مكتب التحقيقات", yassin),
                new ExtrasItem("yusuf", yusuf ? "يوسف — رجل أعمال" : "يوسف — مقفول",
                               yusuf ? "اسمه في الورق قبل ما يتقابل." : "اتقابل في مكتبه", yusuf),
            };
        }

        /// <summary>سجل القصة: القرارات اللي اللاعب خدها فعلاً (من الحالة الحالية).</summary>
        public static List<ExtrasItem> Records(GameState state)
        {
            var list = new List<ExtrasItem>();
            string[] keys =
            {
                GameState.TrustMurad, GameState.TrustLaila, GameState.TrustYassin,
                "Evidence_Count", "Suspicion"
            };

            foreach (var k in keys)
            {
                float v = state != null ? state.Get(k) : 0f;
                list.Add(new ExtrasItem(k, Label(k), $"{v:0}", true));
            }

            if (state != null && state.Decisions.Count > 0)
            {
                foreach (var d in state.Decisions)
                    list.Add(new ExtrasItem(d, "قرار: " + d, "اتسجّل في اللعب", true));
            }
            else
            {
                list.Add(new ExtrasItem("none", "مفيش قرارات مسجّلة", "العب مشهد فيه اختيار", false));
            }

            if (state != null && !string.IsNullOrEmpty(state.EndingId))
                list.Add(new ExtrasItem(state.EndingId, "النهاية: " + state.EndingId, "آخر نهاية وصلتها", true));

            return list;
        }

        static string Label(string key)
        {
            switch (key)
            {
                case GameState.TrustMurad: return "ثقة مراد";
                case GameState.TrustLaila: return "ثقة ليلى";
                case GameState.TrustYassin: return "ثقة ياسين";
                case "Evidence_Count": return "عدد الأدلة";
                case "Suspicion": return "الشك";
                default: return key;
            }
        }

        /// <summary>نسبة الاكتمال المعروضة في الإضافات (مبنية على بيانات حقيقية).</summary>
        public static string ProgressLine()
        {
            int scenes = 0;
            foreach (var s in ChapterOneScenes) if (Progress.HasScene(s.act)) scenes++;
            int post = 0;
            foreach (var e in PostCreditSequence.SupportedEndings) if (Progress.HasSeenPostCredit(e)) post++;
            return $"مشاهد {scenes}/{ChapterOneScenes.Length} · نهايات {Progress.SeenCount}/3 · ما بعد النهاية {post}/3";
        }
    }
}
