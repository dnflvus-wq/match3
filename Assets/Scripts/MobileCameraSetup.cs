using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    [RequireComponent(typeof(Camera))]
    public class MobileCameraSetup : MonoBehaviour
    {
        private void Start()
        {
            Adjust();
        }

        public void Adjust()
        {
            var cam = GetComponent<Camera>();
            var grid = FindFirstObjectByType<GameGrid>();
            if (grid == null || cam == null) return;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.4f, 0.7f, 0.3f, 1f);

            int xDim = grid.xDim;
            int yDim = grid.yDim;
            float aspect = (float)Screen.width / Screen.height;

            // 그리드 중심
            float gridCenterX = grid.transform.position.x + (xDim - 1) / 2f - xDim / 2f;
            float gridCenterY = grid.transform.position.y + yDim / 2f - (yDim - 1) / 2f;

            // 가로 기준 orthoSize
            float padding = 0.5f;
            float orthoSize = (xDim / 2f + padding) / aspect;
            float minOrtho = yDim / 2f + 1.5f;
            orthoSize = Mathf.Max(orthoSize, minOrtho);
            cam.orthographicSize = orthoSize;

            // 카메라 Y: 그리드를 약간 위로 (HUD 공간)
            float camY = gridCenterY + orthoSize * 0.05f;
            transform.position = new Vector3(gridCenterX, camY, transform.position.z);

            // SpriteRenderer 배경을 화면 전체에 맞추기
            StretchSpriteBackground(cam);
        }

        private void StretchSpriteBackground(Camera cam)
        {
            var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            foreach (var sr in renderers)
            {
                string n = sr.gameObject.name.ToLower();
                if (!n.Contains("background") && !n.Contains("bg")) continue;
                // 타일 배경(piece_bg)은 제외
                if (n.Contains("piece")) continue;
                if (sr.sprite == null) continue;

                // 카메라 영역
                float camH = cam.orthographicSize * 2f;
                float camW = camH * cam.aspect;

                // 스프라이트 크기
                float sprW = sr.sprite.bounds.size.x;
                float sprH = sr.sprite.bounds.size.y;

                if (sprW <= 0 || sprH <= 0) continue;

                // Cover: 화면을 완전히 채우도록 (큰 쪽 기준)
                float scaleX = camW / sprW;
                float scaleY = camH / sprH;
                float scale = Mathf.Max(scaleX, scaleY);
                // 최소 1.0 (줄이지 않음) + 10% 여유
                scale = Mathf.Max(scale, 1f) * 1.1f;

                sr.transform.localScale = new Vector3(scale, scale, 1f);
                sr.transform.position = new Vector3(
                    cam.transform.position.x,
                    cam.transform.position.y,
                    1f // 타일보다 뒤, 카메라보다 앞
                );
                sr.sortingOrder = -100;
                sr.enabled = true;
            }
        }
    }
}
