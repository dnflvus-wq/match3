# Phase 16: 운영 인프라

## 목표
출시 후 안정적 운영을 위한 모니터링/분석/알림 인프라

## 16-1. Analytics (사용자 행동 분석)
- Firebase Analytics 또는 GameAnalytics
- 추적 이벤트:
  - 레벨 시작/클리어/실패 (레벨별 클리어율)
  - 부스터 사용 (종류, 빈도)
  - IAP 결제 (아이템, 금액, 시점)
  - 세션 길이, DAU/MAU
  - 이탈 구간 (퍼널 분석)
  - 광고 시청률

## 16-2. 크래시 리포팅
- Firebase Crashlytics
- 자동 크래시 로그 수집
- 사용자 환경(기기, OS) 자동 태깅
- 심각도별 알림

## 16-3. 푸시 알림
- Firebase Cloud Messaging (FCM) 또는 OneSignal
- 알림 종류:
  - 생명 회복 완료
  - 일일 보상 미수령
  - 이벤트 시작/종료 임박
  - 친구가 하트 보냄
- 사용자 세그먼트별 타겟 푸시

## 16-4. A/B 테스트 (향후)
- Firebase Remote Config
- 테스트 대상: 난이도, IAP 가격, 광고 빈도, UI 레이아웃
- 유저 그룹별 다른 설정 적용 → 지표 비교

## 16-5. 서버 모니터링
- Spring Boot Actuator + Prometheus + Grafana (기존 인프라 활용)
- API 응답 시간, 에러율 모니터링

## 검증 항목
- [ ] Analytics 이벤트 정상 수신
- [ ] 크래시 리포트 대시보드 확인
- [ ] 푸시 알림 발송/수신
- [ ] Remote Config 값 변경 반영

## 진행률: 0%
