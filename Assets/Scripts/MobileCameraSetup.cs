using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 모바일 Portrait 모드에서 그리드가 화면에 꽉 차도록 카메라를 자동 조정.
    /// HUD(상단) + 그리드(중앙) + 부스터UI(하단)를 모두 고려.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MobileCameraSetup : MonoBehaviour
    {
        [Tooltip("상단 HUD가 차지하는 화면 비율 (0~1)")]
        public float hudHeightRatio = 0.12f;

        [Tooltip("하단 부스터UI가 차지하는 화면 비율 (0~1)")]
        public float bottomUIRatio = 0.05f;

        [Tooltip("좌우 여백 (Unity unit)")]
        public float horizontalPadding = 0.3f;

        [Tooltip("상하 여백 (Unity unit)")]
        public float verticalPadding = 0.3f;

        private void Start()
        {
            Adjust();
        }

        public void Adjust()
        {
            var cam = GetComponent<Camera>();
            var grid = FindFirstObjectByType<GameGrid>();
            if (grid == null || cam == null) return;

            // 카메라 배경색
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.68f, 0.85f, 0.95f, 1f);

            int xDim = grid.xDim;
            int yDim = grid.yDim;
            float aspect = (float)Screen.width / Screen.height;

            // 그리드 중심 (GameGrid.GetWorldPosition 기준)
            // GetWorldPosition(0,0) = 좌상단, GetWorldPosition(xDim-1, yDim-1) = 우하단
            Vector2 topLeft = grid.GetWorldPosition(0, 0);
            Vector2 bottomRight = grid.GetWorldPosition(xDim - 1, yDim - 1);
            float gridCenterX = (topLeft.x + bottomRight.x) / 2f;
            float gridCenterY = (topLeft.y + bottomRight.y) / 2f;

            float gridWidth = xDim + horizontalPadding * 2f;
            float gridHeight = yDim + verticalPadding * 2f;

            // 그리드가 차지할 수 있는 화면 비율 (HUD + 하단 UI 제외)
            float usableRatio = 1f - hudHeightRatio - bottomUIRatio;

            // 가로 기준 ortho size: 그리드 폭이 화면 폭에 맞도록
            float orthoByWidth = (gridWidth / 2f) / aspect;

            // 세로 기준 ortho size: 그리드 높이가 사용 가능 영역에 맞도록
            float orthoByHeight = (gridHeight / 2f) / usableRatio;

            // 더 큰 값을 사용 (잘림 방지)
            float orthoSize = Mathf.Max(orthoByWidth, orthoByHeight);
            cam.orthographicSize = orthoSize;

            // 카메라 Y 위치: 그리드가 HUD 아래, 부스터 위 중앙에 오도록
            // 화면 상단 = camY + orthoSize
            // 화면 하단 = camY - orthoSize
            // HUD 하단 = camY + orthoSize - (orthoSize * 2 * hudHeightRatio)
            //           = camY + orthoSize * (1 - 2 * hudHeightRatio)
            // 부스터 상단 = camY - orthoSize + (orthoSize * 2 * bottomUIRatio)
            //             = camY - orthoSize * (1 - 2 * bottomUIRatio)
            // 사용 가능 영역 중심 Y = camY + orthoSize * (bottomUIRatio - hudHeightRatio)
            // 그리드 중심을 사용 가능 영역 중심에 맞춤
            float camY = gridCenterY - orthoSize * (bottomUIRatio - hudHeightRatio);

            transform.position = new Vector3(gridCenterX, camY, transform.position.z);
        }
    }
}
