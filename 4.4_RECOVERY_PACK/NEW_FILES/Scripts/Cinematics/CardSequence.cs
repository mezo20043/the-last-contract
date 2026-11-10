// 4.4 RECOVERY — CardSequence.cs — **مُعاد من البنية الموثّقة** (النص الأصلي مش كامل)
using System.Collections.Generic;

namespace TLC.Cinematics
{
    /// <summary>كارت واحدة في سلسلة نص سينمائي (ما بعد النهاية / التيزر).</summary>
    public class Card
    {
        public string kicker;   // سطر علوي صغير (تأريخ/مصدر)
        public string title;    // عنوان كبير (اختياري)
        public string body;     // النص الأساسي
        public float seconds = 4.2f;

        public Card(string kicker, string title, string body, float seconds = 4.2f)
        {
            this.kicker = kicker; this.title = title; this.body = body; this.seconds = seconds;
        }
    }

    /// <summary>سلسلة كروت بمُعرّف — تُحفظ في التقدّم لما تتشاف.</summary>
    public class CardSequence
    {
        public string id;
        public string label;
        public int endingVariant;       // 0 = عام · 1..3 = A/B/C
        public List<Card> cards = new List<Card>();

        public float TotalSeconds
        {
            get
            {
                float t = 0f;
                foreach (var c in cards) t += c.seconds;
                return t;
            }
        }

        public CardSequence(string id, string label, int variant = 0)
        {
            this.id = id; this.label = label; endingVariant = variant;
        }

        public CardSequence Add(string kicker, string title, string body, float seconds = 4.2f)
        {
            cards.Add(new Card(kicker, title, body, seconds));
            return this;
        }
    }
}
