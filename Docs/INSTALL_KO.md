# Tiny Parry Arena v3.1 Fear Core

## 실제 구현된 기능

- Fear는 일반 패링(+5) 또는 퍼펙트 패링(+10)으로만 증가
- 플레이어 피격 시 Fear -15
- 성공 패링이 3초간 없으면 초당 1 감소
- Fear 단계 자동 전환
- Fear 100에서 Broken
- Broken에서 보스 AI 중지
- Fear 단계별 보스 패턴 풀
- 거리와 가중치 기반 패턴 선택
- 근거리 Final Strike
- Final Strike 즉사 인터페이스

UI, 사운드, 이펙트는 포함하지 않았다.

## 적용 순서

1. `Assets/TinyParryArena`를 기존 Unity 프로젝트의 `Assets` 아래에 복사한다.
2. `Create > Tiny Parry Arena > Fear > Fear Config`로 설정 파일을 만든다.
3. 보스에 `FearController`, `BossFearBrain`, `BossFinalStrikeTarget`을 추가한다.
4. 플레이어에 `ParryFearBridge`, `PlayerFinalStrike`를 추가한다.
5. 기존 패링 코드에서:
   - 일반 패링: `ReportNormalParry()`
   - 퍼펙트 패링: `ReportPerfectParry()`
6. 기존 플레이어 피격 코드에서 `ReportPlayerHit()`을 호출한다.
7. 기존 공격 입력 코드에서 `TryFinalStrike()`를 먼저 호출하고, false일 때만 일반 공격을 실행한다.
8. 기존 BossHealth가 `IBossKillable`을 구현하게 한다.

## BossHealth 연결 예시

```csharp
using TinyParryArena.FinalStrike;
using UnityEngine;

public sealed class BossHealth : MonoBehaviour, IBossKillable
{
    [SerializeField] private float currentHealth = 100f;

    public bool IsAlive => currentHealth > 0f;

    public void KillByFinalStrike()
    {
        if (!IsAlive) return;
        currentHealth = 0f;
        Die();
    }

    private void Die()
    {
        // 기존 사망 처리
    }
}
```

## Animator Trigger

- 보스: `Broken`
- 보스: `Executed`
- 플레이어: `FinalStrike`

FearController에는 공개 AddFear 메서드가 없다.
Fear 증가 경로는 `RegisterSuccessfulParry()`로 제한했다.
