using System.Collections;
using UnityEngine;

namespace TinyParryArena.Boss
{
    [CreateAssetMenu(fileName = "BossAttackPattern", menuName = "Tiny Parry Arena/Boss/Attack Pattern")]
    public sealed class BossAttackPattern : ScriptableObject
    {
        [SerializeField] private string animationTrigger = "Attack";
        [Min(0f)] [SerializeField] private float minimumRange = 0f;
        [Min(0f)] [SerializeField] private float maximumRange = 3f;
        [Min(0f)] [SerializeField] private float recoveryTime = 1f;
        [Min(0f)] [SerializeField] private float weight = 1f;

        public float Weight => Mathf.Max(0f, weight);

        public bool IsInRange(float distance)
        {
            return distance >= minimumRange && distance <= maximumRange;
        }

        public IEnumerator Execute(Animator animator)
        {
            if (animator != null && !string.IsNullOrWhiteSpace(animationTrigger))
            {
                animator.SetTrigger(animationTrigger);
            }

            if (recoveryTime > 0f)
            {
                yield return new WaitForSeconds(recoveryTime);
            }
        }
    }
}
