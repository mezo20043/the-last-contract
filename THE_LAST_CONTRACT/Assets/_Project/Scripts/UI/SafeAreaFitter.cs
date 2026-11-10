using UnityEngine;

namespace TLC.UI
{
    /// <summary>
    /// بيظبّط الواجهة على منطقة الأمان في الموبايل (النوتش، الشرائط، الحواف المنحنية).
    /// من غير الكومبوننت ده، أزرار اللعب بتغرق تحت شريط الأندرويد أو تحت النوتش.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] bool applyTop = true;
        [SerializeField] bool applyBottom = true;
        [SerializeField] bool applyLeft = true;
        [SerializeField] bool applyRight = true;

        RectTransform _rt;
        Rect _lastSafeArea;
        Vector2Int _lastScreen;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreen.x || Screen.height != _lastScreen.y)
                Apply();
        }

        void Apply()
        {
            if (_rt == null) return;

            Rect safe = Screen.safeArea;
            _lastSafeArea = safe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width == 0 || Screen.height == 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;  min.y /= Screen.height;
            max.x /= Screen.width;  max.y /= Screen.height;

            _rt.anchorMin = new Vector2(
                applyLeft ? min.x : 0f,
                applyBottom ? min.y : 0f);

            _rt.anchorMax = new Vector2(
                applyRight ? max.x : 1f,
                applyTop ? max.y : 1f);

            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
