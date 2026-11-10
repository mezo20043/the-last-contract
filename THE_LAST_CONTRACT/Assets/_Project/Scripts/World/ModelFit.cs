using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// موديلات الأصول (Poly Haven/الشخصيات) بتستورد بمقاسات ومحاور مختلفة:
    /// بعضها وحداتها مش مترّية، وبعضها المحور عند الحوض مش عند القدم.
    /// الدوال هنا بتطبّع الحجم وتنزّل الموديل على الأرض بحدود **محسوبة فورًا**
    /// من localBounds (مش Renderer.bounds اللي بيتحدّث بعد فريم).
    /// </summary>
    public static class ModelFit
    {
        public static Bounds PreciseBounds(Transform root)
        {
            var rs = root.GetComponentsInChildren<Renderer>(true);
            bool first = true;
            var b = new Bounds();

            foreach (var r in rs)
            {
                // الشخصيات SkinnedMeshRenderer: بناخد localBounds كمان،
                // لأن .bounds بتاعها بيتحدّث بعد فريم ومش مناسب للتعديل الفوري.
                var lb = r.localBounds;
                var m = r.transform.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                {
                    var corner = new Vector3(
                        (c & 1) == 0 ? lb.min.x : lb.max.x,
                        (c & 2) == 0 ? lb.min.y : lb.max.y,
                        (c & 4) == 0 ? lb.min.z : lb.max.z);
                    var w = m.MultiplyPoint3x4(corner);
                    if (first) { b = new Bounds(w, Vector3.zero); first = false; }
                    else b.Encapsulate(w);
                }
            }
            if (first) b = new Bounds(root.position, Vector3.zero);
            return b;
        }

        /// <summary>يظبط ارتفاع الموديل للمتر المطلوب (مقياس موحّد).</summary>
        public static void FitHeight(Transform t, float targetHeight)
        {
            var b = PreciseBounds(t);
            if (b.size.y < 0.0001f) return;

            float f = targetHeight / b.size.y;
            if (float.IsNaN(f) || float.IsInfinity(f)) return;
            f = Mathf.Clamp(f, 0.0002f, 3000f);
            t.localScale *= f;
        }

        /// <summary>
        /// بيلفّ الموديل في نود وسيطة (MODEL_PIVOT) عشان تعديل الارتفاع ما يتمسحش
        /// لما الـAnimator يكتب على الـTransform بتاع الموديل (Root Motion).
        /// </summary>
        public static GameObject WrapInPivot(GameObject model, string pivotName)
        {
            if (model == null) return null;
            var oldParent = model.transform.parent;

            var pivot = new GameObject(pivotName);
            pivot.transform.SetParent(oldParent, false);
            pivot.transform.localPosition = Vector3.zero;
            pivot.transform.localRotation = Quaternion.identity;
            pivot.transform.localScale = Vector3.one;

            model.transform.SetParent(pivot.transform, true);
            return pivot;
        }

        /// <summary>بيوقف Root Motion عند Animator الموديل (كان بيرجّع الموديل لمكانه).</summary>
        public static void DisableRootMotion(GameObject model)
        {
            if (model == null) return;
            foreach (var a in model.GetComponentsInChildren<Animator>(true))
                if (a != null) a.applyRootMotion = false;
        }

        /// <summary>ينزل الموديل لحد ما أدنى نقطة فيه تلمس مستوى الأرض المطلوب.</summary>
        public static void DropToGround(Transform t, float groundY)
        {
            var b = PreciseBounds(t);
            float dy = groundY - b.min.y;
            if (Mathf.Abs(dy) > 0.001f) t.position += Vector3.up * dy;
        }
    }
}
