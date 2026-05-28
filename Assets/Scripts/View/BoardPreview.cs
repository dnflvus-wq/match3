using UnityEngine;

namespace Match3
{
    /// <summary>
    /// Edit 모드에서 보드 그리드를 Scene View에 표시.
    /// GameGrid의 xDim, yDim을 읽어서 그리드 라인을 그린다.
    /// Play 모드에서는 비활성화.
    /// </summary>
    [ExecuteAlways]
    public class BoardPreview : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] float cellSize = 1.0f;
        [SerializeField] Color gridColor = new Color(1, 1, 1, 0.2f);

        [Header("Preview")]
        [SerializeField] bool showDummyTiles;
        [SerializeField] Sprite[] dummyTileSprites;

        private GameGrid _grid;

        private void OnValidate()
        {
            if (showDummyTiles && !Application.isPlaying)
                SpawnDummyTiles();
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;

            if (_grid == null) _grid = GetComponent<GameGrid>();
            if (_grid == null) return;

            int w = _grid.xDim;
            int h = _grid.yDim;
            if (w <= 0 || h <= 0) return;

            // 보드 원점: GameGrid.GetWorldPosition(0,0)의 좌하단
            Vector3 origin = transform.position + new Vector3(
                -w / 2.0f, h / 2.0f, 0);

            Gizmos.color = gridColor;

            // 세로선
            for (int x = 0; x <= w; x++)
                Gizmos.DrawLine(
                    origin + new Vector3(x * cellSize, 0, 0),
                    origin + new Vector3(x * cellSize, -h * cellSize, 0));

            // 가로선
            for (int y = 0; y <= h; y++)
                Gizmos.DrawLine(
                    origin + new Vector3(0, -y * cellSize, 0),
                    origin + new Vector3(w * cellSize, -y * cellSize, 0));

            // 셀 중심 점 표시
            Gizmos.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridColor.a * 2f);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    Gizmos.DrawWireSphere(
                        origin + new Vector3((x + 0.5f) * cellSize, -(y + 0.5f) * cellSize, 0),
                        0.05f);
        }

        private void SpawnDummyTiles()
        {
            // 더미 타일은 OnDrawGizmos로 대체 — 실제 오브젝트 생성 안 함
        }
    }
}
