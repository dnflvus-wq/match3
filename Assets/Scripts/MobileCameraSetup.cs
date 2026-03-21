using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 런타임에 화면 비율에 맞게 카메라 orthographic size와 위치를 자동 조정한다.
    /// Portrait 모드 기준으로 그리드가 화면에 꽉 차도록 설정.
    /// Main Camera 오브젝트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MobileCameraSetup : MonoBehaviour
    {
        [Tooltip("상단 HUD가 차지하는 화면 비율 (0~1). 기본 0.15 = 15%")]
        public float hudHeightRatio = 0.18f;

        [Tooltip("하단 여백 화면 비율 (0~1). 기본 0.08 = 8%")]
        public float bottomMarginRatio = 0.08f;

        [Tooltip("좌우 여백 (Unity unit)")]
        public float horizontalPadding = 0.1f;

        private void Start()
        {
            Adjust();
        }

        public void Adjust()
        {
            var cam = GetComponent<Camera>();
            var grid = FindFirstObjectByType<GameGrid>();
            if (grid == null || cam == null) return;

            // 카메라 배경을 밝은 파스텔 하늘색으로 설정
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.68f, 0.85f, 0.95f, 1f);

            int xDim = grid.xDim;
            int yDim = grid.yDim;

            float aspect = (float)Screen.width / Screen.height;

            // 가로 기준: 그리드 xDim 칸 + 좌우 여백이 화면에 맞도록
            float requiredHalfWidth = xDim / 2f + horizontalPadding;
            float orthoByWidth = requiredHalfWidth / aspect;

            // 세로 기준: 그리드 yDim 칸 + HUD + 하단 여백이 화면에 맞도록
            float usableHeightRatio = 1f - hudHeightRatio - bottomMarginRatio;
            float orthoByHeight = (yDim / 2f) / usableHeightRatio;

            // 더 큰 값을 사용해야 그리드가 잘리지 않음
            float orthoSize = Mathf.Max(orthoByWidth, orthoByHeight);
            cam.orthographicSize = orthoSize;

            // 카메라 Y 위치: 그리드 상단이 HUD 바로 아래에 오도록 정확히 계산
            float screenHeightInUnits = orthoSize * 2f;
            float hudHeightInUnits = screenHeightInUnits * hudHeightRatio;

            // 그리드 상단 Y (GameGrid가 origin에 있을 때)
            float gridTopY = yDim / 2f;
            float topPadding = 1.5f; // 그리드 상단 위 여백 (배경 보드 영역과 맞추기)

            // 그리드 상단이 화면 상단에서 hudHeight만큼 아래에 오도록 camY 결정
            // 화면 상단 Y = camY + orthoSize
            // 그리드 상단 = (camY + orthoSize) - hudHeightInUnits - topPadding
            // → camY = gridTopY + topPadding + hudHeightInUnits - orthoSize
            float camY = gridTopY + topPadding + hudHeightInUnits - orthoSize;
            transform.position = new Vector3(transform.position.x, camY, transform.position.z);
        }
    }
}
