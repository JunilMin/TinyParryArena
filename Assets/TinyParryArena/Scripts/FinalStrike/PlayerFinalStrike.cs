using UnityEngine;

namespace TinyParryArena.FinalStrike
{
    [DisallowMultipleComponent]
    public sealed class PlayerFinalStrike : MonoBehaviour
    {
        [SerializeField] private Animator playerAnimator;
        [Min(0.1f)] [SerializeField] private float executionRange = 2f;
        [SerializeField] private LayerMask bossLayerMask;

        public bool TryFinalStrike()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                executionRange,
                bossLayerMask,
                QueryTriggerInteraction.Collide);

            BossFinalStrikeTarget closest = null;
            float closestDistance = float.PositiveInfinity;

            foreach (Collider hit in hits)
            {
                BossFinalStrikeTarget target = hit.GetComponentInParent<BossFinalStrikeTarget>();

                if (target == null || !target.CanExecute)
                {
                    continue;
                }

                float distance = (target.ExecutionPoint.position - transform.position).sqrMagnitude;

                if (distance < closestDistance)
                {
                    closest = target;
                    closestDistance = distance;
                }
            }

            return closest != null && closest.TryExecute(playerAnimator);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, executionRange);
        }
#endif
    }
}
