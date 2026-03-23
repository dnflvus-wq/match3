# Phase 10: 성능 최적화

## 목표
모바일(저사양 Android)에서 60fps 안정 유지

## 최적화 항목

### 10-1. 오브젝트 풀링
- 타일, 파티클, 점수 팝업 전부 풀링
- Instantiate/Destroy 호출 제거
- GC 스파이크 제거

### 10-2. Sprite Atlas
- 타일 8색 → TileAtlas
- UI 아이콘 → UIAtlas
- 드로우 콜 감소

### 10-3. 텍스처 압축
- ASTC 형식 우선 적용 (모바일 범용)
- 텍스처 최대 크기 제한

### 10-4. Unity Profiler
- CPU/GPU/메모리 프로파일링
- 병목 구간 식별 및 개선
- 빌드 후 실기기 테스트

### 10-5. 배칭
- SRP Batcher 활용
- 동일 Material/Atlas 공유
- Draw Call 최소화

### 10-6. 물리 엔진 미사용
- Rigidbody/Physics 사용 금지
- 타일 이동은 직접 좌표 계산 + 트위닝

## 검증 항목
- [ ] Profiler에서 GC 스파이크 없음
- [ ] Draw Call 50 이하
- [ ] 저사양 기기에서 60fps
- [ ] 메모리 사용량 200MB 이하

## 진행률: 0%
