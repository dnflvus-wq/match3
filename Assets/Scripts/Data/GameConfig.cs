using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 게임 전체 설정값을 Inspector에서 편집할 수 있는 ScriptableObject.
    /// 하드코딩/매직넘버를 여기에 모아서 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Match3/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Board")]
        [Tooltip("타일 낙하 한 스텝 시간")]
        public float fillTime = 0.1f;

        [Header("Animation")]
        [Tooltip("스왑 애니메이션 시간")]
        public float swapTime = 0.25f;
        [Tooltip("스왑 실패(핑퐁) 애니메이션 시간")]
        public float swapBackTime = 0.2f;

        [Header("Hint")]
        [Tooltip("무입력 시 힌트 표시까지 대기 시간(초)")]
        public float hintDelay = 5f;

        [Header("Match")]
        [Tooltip("최소 매치 길이")]
        public int matchLength = 3;
        [Tooltip("4매치 시 카메라 쉐이크 강도")]
        public float shakeIntensity = 0.05f;
        [Tooltip("4매치 시 카메라 쉐이크 시간")]
        public float shakeDuration = 0.2f;

        [Header("Input")]
        [Tooltip("스와이프 감지 임계값 (월드 좌표 단위)")]
        public float swipeThreshold = 0.3f;

        [Header("Score")]
        [Tooltip("콤보 표시 시작 횟수")]
        public int comboDisplayThreshold = 2;
    }
}
