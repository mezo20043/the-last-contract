// 4.4 RECOVERY — ChapterTwoTeaser.cs — **مُعاد حرفيًا** (مع إصلاح استدعاء CinematicId)
using TLC.Core;

namespace TLC.Cinematics
{
    /// <summary>
    /// تيزر الفصل الثاني — «الاسم الذي لم يوجد».
    /// تيزر مش لعبة: ٦ كروت، بينتهي ويرجّع للقائمة. كل نهاية ليها كارت افتتاحي
    /// مختلف يحترم قرار اللاعب، والباقي مشترك عشان ما يحصلش تناقض مع أي مسار.
    /// </summary>
    public static class ChapterTwoTeaser
    {
        public const string CinematicId = "teaser_ch2";
        public const string TeaserTitle = "THE NAME THAT NEVER EXISTED";
        public const string TeaserTitleArabic = "الاسم الذي لم يوجد";

        public static CardSequence For(string endingId)
        {
            var s = new CardSequence(CinematicId, "تيزر الفصل الثاني", 0);

            s.Add("تيزر · الفصل الثاني", TeaserTitleArabic,
                  "مش جزء من اللعب — إعلان قصير للفصل الجاي.", 3.6f);

            switch (endingId)
            {
                case "CH1_A":
                    s.Add("من نهاية «الحقيقة»", null,
                          "الاسم اللي على التوقيع متسجّل «متوفى» من سبع سنين. الملف الجاي بيسأل سؤال واحد: مين اللي دفنه؟", 5.2f);
                    break;
                case "CH1_B":
                    s.Add("من نهاية «المشتبه به»", null,
                          "اتفاقية ٧١٤ لسه شغّالة، واسمك بقى على ورقة الشهود. الفصل الجاي بيبدأ من مكتب محامي.", 5.2f);
                    break;
                case "CH1_C":
                    s.Add("من نهاية «الاختفاء»", null,
                          "صورة الطفولة في إيدك، والبيت القديم لسه واقف. الفصل الجاي بيرجعك لنفس الشارع.", 5.2f);
                    break;
                default:
                    s.Add("بداية جديدة", null,
                          "كل نهاية بتفتح باب مختلف — والباب اللي هتختاره بيفضل مفتوح.", 4.6f);
                    break;
            }

            s.Add("العباسية · البيت القديم", "بيت واحد… ملفين",
                  "في الفصل الثاني، البيت اللي في الصورة بيتحوّل لمكان حقيقي بيتزار.", 4.8f);
            s.Add("شخصية جديدة", "الاسم اللي ملوش وجود",
                  "في كل السجلات اسم واحد مكرّر — ومفيش حد يعرف صاحبه.", 4.8f);
            s.Add("قرارك لسه في إيدك", null,
                  "علاقاتك مع مراد وليلى وياسين هتتغيّر حسب اختياراتك في الفصل الأول.", 4.8f);
            s.Add("تيزر · نهاية", "الفصل الثاني — قريبًا",
                  "ده تيزر. الفصل الثاني نفسه مش متاح للعب في النسخة دي.", 4.4f);

            return s;
        }

        public static string VariantLabel(string endingId)
        {
            switch (endingId)
            {
                case "CH1_A": return "variante A";
                case "CH1_B": return "variant B";
                case "CH1_C": return "variant C";
                default: return "generic";
            }
        }
    }
}
