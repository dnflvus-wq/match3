# Phase 7: 레벨 에디터 도구

## 목표
코드 없이 레벨을 대량 생산할 수 있는 에디터 도구

## 도구 구성

### 7-1. Custom Inspector 그리드 에디터
- LevelConfig SO를 Inspector에서 시각적 2D 그리드로 표시
- 마우스 클릭으로 타일 종류 배치 (일반/장애물/빈칸/얼음/사슬)
- 색상 팔레트 UI
- Play 버튼으로 즉시 테스트

### 7-2. CSV/Excel → ScriptableObject 파이프라인
- 기획자가 Excel/Google Sheets에서 레벨 데이터 작성
- 보드 크기, 이동수, 목표점수, 타일 가중치
- BakingSheet 또는 자체 CSV 파서로 임포트
- 임포트 시 LevelConfig SO 자동 생성

### 7-3. AI 레벨 밸런싱 (향후)
- 헤드리스 모드로 AI가 레벨 자동 플레이
- 클리어율/평균 이동수 통계 수집
- 난이도 자동 조정

## 참고 도구
- UnityGridLevelEditor (GitHub)
- BakingSheet (GitHub)
- Unity-QuickSheet

## 검증 항목
- [ ] Custom Inspector에서 그리드 시각적 편집 가능
- [ ] CSV에서 LevelConfig SO 자동 생성
- [ ] 생성된 레벨이 게임에서 정상 로드/플레이
- [ ] 에디터에서 즉시 테스트 가능

## 진행률: 0%
