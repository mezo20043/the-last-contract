using UnityEngine;
using UnityEngine.Rendering;

namespace TLC.World
{
    /// <summary>
    /// المشروع شغّال على URP، وشدة الإضاءة في URP مش زي Built-in:
    /// الـPoint Light بيتقاس بالـlumen مش بوحدات قديمة. القيم القديمة (0.4 – 3)
    /// كانت بتطلّع مشاهد مظلمة جدًا. هنا بنحوّل القيمة "القديمة" لقيمة URP صحيحة
    /// في مكان واحد، عشان كل الأنظمة (شقة/مواقع) تستخدم نفس المعيار.
    /// </summary>
    public static class LightUtil
    {
        public static bool IsUrp => GraphicsSettings.currentRenderPipeline != null;

        /// <summary>لمبة نقطية: القيمة القديمة × 380 lumen تقريبًا.</summary>
        public static float Point(float legacy) => IsUrp ? legacy * 380f : legacy;

        /// <summary>ضوء اتجاهي (قمر/شمس): القيمة القديمة × 2.2 lux.</summary>
        public static float Directional(float legacy) => IsUrp ? legacy * 2.2f : legacy;

        /// <summary>كشاف/بقعة.</summary>
        public static float Spot(float legacy) => IsUrp ? legacy * 380f : legacy;
    }
}
