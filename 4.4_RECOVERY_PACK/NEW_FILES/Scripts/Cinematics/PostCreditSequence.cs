// 4.4 RECOVERY — PostCreditSequence.cs — **مُعاد** (البنية والثوابت ومحتوى الكروت موثّقة)
using System.Collections.Generic;

namespace TLC.Cinematics
{
    /// <summary>
    /// ما-بعد-النهاية: سلسلة كروت مستقلة لكل نهاية من نهايات الفصل الأول.
    /// ممنوع يعيد كتابة حوار القصة أو يكشف معلومات بدري — الكروت بتقرا اللي
    /// اللاعب شافه فعلًا وبتسيب سؤال مفتوح.
    /// </summary>
    public static class PostCreditSequence
    {
        public static readonly string[] SupportedEndings = { "CH1_A", "CH1_B", "CH1_C" };

        public static bool IsSupported(string endingId)
        {
            if (string.IsNullOrEmpty(endingId)) return false;
            foreach (var e in SupportedEndings) if (e == endingId) return true;
            return false;
        }

        public static string CinematicId(string endingId) => "postcredit_" + endingId;

        public static CardSequence For(string endingId)
        {
            switch (endingId)
            {
                case "CH1_A": return EndingA();
                case "CH1_B": return EndingB();
                case "CH1_C": return EndingC();
                default: return null;
            }
        }

        // A — الحقيقة: التسجيل سليم + توقيع شخص مُفترض ميت من ٧ سنين
        static CardSequence EndingA()
        {
            var s = new CardSequence(CinematicId("CH1_A"), "ما بعد النهاية أ — الحقيقة", 1);

            s.Add("ملف صوتي · الساعة ٧:٤٠", null,
                  "التسجيل خلص. الساعة اللي في الركن لسه بتبعد، والشقة ساكتة زيادة عن اللزوم.", 4.6f);
            s.Add("سجل · ٢٠١٨", null,
                  "اسم واحد في السجل اتسجّل «متوفى» سنة ٢٠١٨ — ومع ذلك فيه توقيع جديد بيحمل نفس الاسم.", 5.2f);
            s.Add("الورقة الأولى", "توقيع مألوف",
                  "آدم قرّب الورقة من النور. التوقيع مرتب، والحروف اللي فيه متكرّرة أكتر من مرة في الملف.", 5.0f);
            s.Add("سبع سنين", null,
                  "سبع سنين بين تاريخ الوفاة والتوقيع الأخير. فيه حد كان مستنّي السبع سنين دول يخلصوا.", 5.0f);
            s.Add("ما بعد النهاية · ١", "الملف لسه مفتوح",
                  "مفيش إجابة في الكروت دي. اللي وصلت ليه إن الاسم اللي على التوقيع مش حي.", 4.4f);

            return s;
        }

        // B — المشتبه به: ياسين يستلم تليفون اتفاق السبع سنين + آدم شاهد رقم ٠٤
        static CardSequence EndingB()
        {
            var s = new CardSequence(CinematicId("CH1_B"), "ما بعد النهاية ب — المشتبه به", 2);

            s.Add("مكتب التحقيقات · ١:١٢ص", null,
                  "التليفون على مكتب ياسين بيرن. المتصل مش بيتكلم.", 4.6f);
            s.Add("اتفاقية ٧١٤", null,
                  "اتفاقية السبع سنين، والورقة اللي فيها بند واحد بس مفعّل: «حماية الشاهد».", 5.2f);
            s.Add("سجل الشهود", "الشاهد رقم ٤",
                  "آدم مكتوب في السجل كشاهد رقم ٤. الاسم مش بالحروف — بالرقم.", 5.0f);
            s.Add("مكالمة تانية", null,
                  "ياسين ماسك التليفون بإيد، وبيقرأ الورقة بالإيد التانية. مفيش حد قال «متأكد».", 5.0f);
            s.Add("ما بعد النهاية · ٢", "بقيت في الورق",
                  "ده ما-بعد-نهاية، مش نهاية جديدة. اللي اتغيّر إن اسمك بقى مكتوب.", 4.4f);

            return s;
        }

        // C — الاختفاء: صورة الطفولة + «الهروب مش بيغيّر اللي حصل»
        static CardSequence EndingC()
        {
            var s = new CardSequence(CinematicId("CH1_C"), "ما بعد النهاية ج — الاختفاء", 3);

            s.Add("صورة · البيت القديم", null,
                  "صورة الطفولة لسه في إيده. الورق صفر، والوشين اللي في الصورة مش بيبصلوا لبعض.", 4.6f);
            s.Add("ظهر الصورة", null,
                  "في ظهر الصورة تاريخ مكتوب بالقلم الرصاص، ورا الحدود اللي اتقصت.", 5.0f);
            s.Add("البيت", null,
                  "البيت لسه واقف في نفس الشارع، والباب لسه بنفس اللون. حد فتح الباب مرة، وسابه مفتوح.", 5.2f);
            s.Add("جملة واحدة", "«الهروب مش بيغيّر اللي حصل»",
                  "مفيش حد قالها بصوت عالي. آدم سمعها في دماغه وهو بيقفل الباب.", 5.2f);
            s.Add("ما بعد النهاية · ٣", "الشارع زي ما هو",
                  "اللي فرّق إنه رجع، مش إنه مشي.", 4.4f);

            return s;
        }
    }
}
