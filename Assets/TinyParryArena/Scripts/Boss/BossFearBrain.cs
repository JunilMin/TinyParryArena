using System.Collections;
using System.Collections.Generic;
using TinyParryArena.Fear;
using UnityEngine;

namespace TinyParryArena.Boss
{
    [DisallowMultipleComponent]
    public sealed class BossFearBrain : MonoBehaviour
    {
        [SerializeField] private FearController fearController;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform target;
        [Min(0f)] [SerializeField] private float firstDecisionDelay = 0.5f;
        [Min(0f)] [SerializeField] private float decisionInterval = 0.25f;
        [SerializeField] private List<FearPatternPool> patternPools = new();

        private bool combatEnabled = true;
        private bool isExecutingPattern;
        private Coroutine brainRoutine;

        private void OnEnable()
        {
            if (fearController != null)
            {
                fearController.Broken += HandleBroken;
            }

            brainRoutine = StartCoroutine(BrainLoop());
        }

        private void OnDisable()
        {
            if (fearController != null)
            {
                fearController.Broken -= HandleBroken;
            }

            if (brainRoutine != null)
            {
                StopCoroutine(brainRoutine);
            }
        }

        public void SetTarget(Transform newTarget) => target = newTarget;
        public void SetCombatEnabled(bool value) => combatEnabled = value;

        private IEnumerator BrainLoop()
        {
            if (firstDecisionDelay > 0f)
            {
                yield return new WaitForSeconds(firstDecisionDelay);
            }

            while (true)
            {
                if (combatEnabled && !isExecutingPattern &&
                    fearController != null && !fearController.IsBroken &&
                    target != null)
                {
                    BossAttackPattern pattern = SelectPattern(
                        fearController.CurrentState,
                        Vector3.Distance(transform.position, target.position));

                    if (pattern != null)
                    {
                        yield return ExecutePattern(pattern);
                    }
                }

                yield return new WaitForSeconds(decisionInterval);
            }
        }

        private IEnumerator ExecutePattern(BossAttackPattern pattern)
        {
            isExecutingPattern = true;
            yield return pattern.Execute(animator);
            isExecutingPattern = false;
        }

        private BossAttackPattern SelectPattern(FearState state, float distance)
        {
            FearPatternPool pool = patternPools.Find(p => p.FearState == state);

            if (pool == null || pool.Patterns == null || pool.Patterns.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;

            foreach (BossAttackPattern pattern in pool.Patterns)
            {
                if (pattern != null && pattern.IsInRange(distance))
                {
                    totalWeight += pattern.Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);

            foreach (BossAttackPattern pattern in pool.Patterns)
            {
                if (pattern == null || !pattern.IsInRange(distance))
                {
                    continue;
                }

                roll -= pattern.Weight;
                if (roll <= 0f) return pattern;
            }

            return null;
        }

        private void HandleBroken()
        {
            combatEnabled = false;
            isExecutingPattern = false;

            if (animator != null)
            {
                animator.SetTrigger("Broken");
            }
        }
    }
}
