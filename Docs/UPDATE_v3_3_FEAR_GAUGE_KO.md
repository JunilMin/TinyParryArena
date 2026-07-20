# v3.3 Fear Gauge 수정

이번 버전에서는 메뉴를 눌러 게이지를 만들 필요가 없다.

게임을 실행하면 자동으로:

- 화면 중앙 하단에 Fear 게이지 생성
- 보스가 없어도 빈 게이지 표시
- 보스의 FearController 자동 탐색
- 패링 성공 시 즉시 게이지 증가
- 패링 성공 시 흰색 플래시
- 패링 성공 시 짧은 확대 펄스
- Fear 단계별 색상 변경
- 95~99 구간 깜빡임
- Fear 100에서 흰색 표시

## 기본 위치

- 중앙 하단
- 화면 아래에서 64px 위
- 크기 560 x 26
- 숫자와 퍼센트 없음

## 중요

기존 v3.2 폴더를 남겨둔 채 중복으로 넣지 말고,
이번 압축의 `Assets/TinyParryArena`로 교체한다.

## 패링 코드 연결

게이지는 FearController의 이벤트를 읽는다.
따라서 기존 패링 성공 코드가 반드시 다음을 호출해야 한다.

```csharp
parryFearBridge.ReportNormalParry();
```

또는

```csharp
parryFearBridge.ReportPerfectParry();
```
