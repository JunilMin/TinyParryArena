# v3.2 Fear 게이지 적용

이 패치에는 숫자 없는 Fear 게이지가 포함되어 있다.

## 가장 빠른 생성 방법

Unity 상단 메뉴에서:

`GameObject > Tiny Parry Arena > Create Fear Gauge`

선택하면 다음이 자동 생성된다.

- HUD Canvas가 없으면 새 Canvas 생성
- 화면 아래 중앙에 Fear Gauge 생성
- 검은 배경
- 붉은 Fill 이미지
- 숫자 및 퍼센트 텍스트 없음
- 활성화된 보스의 FearController 자동 탐색

## 위치

기본 위치는 화면 아래 중앙이다.

- Width: 520
- Height: 24
- Bottom Offset: 72

RectTransform에서 자유롭게 수정할 수 있다.

## 동작 확인

플레이 모드에서 패링 성공 시:

- 일반 패링: 게이지 5% 증가
- 퍼펙트 패링: 게이지 10% 증가
- 피격: 게이지 15% 감소
- 패링 성공 없이 3초가 지나면 초당 1% 감소

## 게이지가 움직이지 않을 때

Fear 게이지 자체보다 먼저 실제 패링 코드가 아래 메서드를 호출하는지 확인한다.

```csharp
parryFearBridge.ReportNormalParry();
parryFearBridge.ReportPerfectParry();
```

보스의 `FearController`에 `FearConfig`가 연결되어 있어야 한다.

## 수동 연결

자동 탐색을 사용하지 않으려면 `FearGaugeUI` Inspector에서:

- Fear Controller: 현재 보스의 FearController
- Fill Image: 자식 Fill 오브젝트의 Image

를 직접 연결한다.
