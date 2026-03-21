using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 모바일 노치/홈바 Safe Area를 RectTransform에 자동 적용한다.
    /// Canvas 하위의 최상단 Panel에 붙이면 된다.
    /// </summary>
    [ExecuteInEditMode]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea = Rect.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.orientation != lastOrientation)
                Apply();
        }

        private void Apply()
        {
            lastSafeArea = Screen.safeArea;
            lastOrientation = Screen.orientation;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var screenSize = new Vector2(Screen.width, Screen.height);
            var safeArea = Screen.safeArea;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
