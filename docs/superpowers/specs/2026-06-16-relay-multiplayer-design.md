# Relay 원격 접속 + 식별/색상 + 로깅 설계

작성일: 2026-06-16
대상: Unity 6.3 (6000.3.8f1), 2D(URP), NGO 2.12 + com.unity.services.multiplayer 2.2.3

## 목표 (이번 주)

로컬 졸업 → **Unity Relay**로 각자 다른 컴퓨터에서 접속. 유저별 색상/닉네임 식별, 호스트 측 로깅.

## 확정 요구사항

| 항목 | 결정 |
|---|---|
| 접속 | Unity Relay + **join 코드만** (Lobby/방 목록 X) |
| 인증 | Unity 익명 인증 → 기기별 고정 PlayerId |
| 식별 | 닉네임(접속 시 입력) + PlayerId |
| 색상 | 서버가 고유 색상 부여, NetworkVariable로 전원 동기화 |
| 로깅 | 호스트가 접속/퇴장/식별 이벤트를 콘솔 + 파일(persistentDataPath, 타임스탬프) |
| 권한 모델 | Client-Server (호스트 권한) 유지 |

## 아키텍처 / 데이터 흐름

1. 앱 시작 → `UnityServices.InitializeAsync()` → 익명 로그인 → PlayerId 확보
2. 닉네임 입력 → **Host**: Relay 할당 + join 코드 생성 → UnityTransport에 Relay 데이터 설정 → `StartHost`
3. **Join**: 코드로 Relay 접속 → transport 설정 → `StartClient`
4. 접속 시 플레이어 프리팹 스폰 → 소유자가 {PlayerId, 닉네임}을 ServerRpc로 제출
5. 서버가 고유 색상 배정 → NetworkVariable(닉네임/색상) 세팅 → 전 클라가 SpriteRenderer 색 + 머리 위 이름표 적용
6. 호스트(GameLogger)가 연결/식별/해제를 콘솔 + 파일에 기록

## 컴포넌트

1. **GameNetworkBootstrap.cs** — UGS 초기화 + 익명 로그인. PlayerId 보관. MPPM 가상 플레이어는 태그별 인증 프로필 분리(고유 PlayerId).
2. **RelayConnector.cs** — Relay 할당/조인 코드, UnityTransport 설정, StartHost/StartClient. 코드 문자열 노출.
3. **PlayerIdentity.cs** (프리팹 NetworkBehaviour) — `NetworkVariable<Color32>`, `NetworkVariable<FixedString32Bytes>`. 소유자 ServerRpc 제출 → 서버 배정 → 전 클라 적용.
4. **ColorAssigner.cs** — 서버 측 고유 색상 팔레트(8~12색) 배정/회수.
5. **GameLogger.cs** — 호스트 전용. 연결/해제/식별 콜백 → 콘솔 + persistentDataPath 로그 파일.
6. **ConnectionUI.cs** — 닉네임 입력 + Host/Join 버튼 + 코드 표시/복사.
7. **PlayerMovement.cs** — 기존 유지 (서버 권한 이동).

## 에러 처리

- UGS 초기화/로그인 실패 → UI 에러 표시, Host/Join 비활성
- Relay 생성/조인 실패(잘못된 코드·네트워크) → 예외 캐치 후 메시지, 미접속 유지
- 퇴장 시 → 색상 회수 + 로그

## 테스트

- **로컬 1차**: MPPM 가상 플레이어 2~4명, 인증 프로필 분리로 각자 다른 PlayerId.
- **실제 검증(핵심)**: 빌드 후 두 대의 컴퓨터에서 Host(코드 공유) ↔ Join → 서로 다른 색/이름 + 이동 동기화 + 호스트 로그 파일 확인.

## 범위 밖 (YAGNI)

Lobby/방 목록, 매치메이킹, 영구 통계 DB, 클라우드 로깅 — 다음 주 이후.

## 구현 순서 (슬라이스)

1. UGS 초기화 + 익명 인증 (PlayerId 로그 확인)
2. Relay 접속 + 닉네임/코드 UI (MPPM 또는 두 빌드로 접속 검증)
3. 식별 + 색상 동기화
4. 호스트 로깅
