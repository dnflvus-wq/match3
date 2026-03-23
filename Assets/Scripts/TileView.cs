using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 타일 Prefab에 부착하는 View 스크립트.
    /// 스프라이트 교체, 이동 애니메이션, 파괴 이펙트를 담당합니다.
    /// (Phase 1에서는 생성만, Phase 4에서 실제 연결)
    /// </summary>
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = sprite;
        }

        public void SetColor(Color color)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = color;
        }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }
}
