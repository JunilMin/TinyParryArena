using TinyParryArena.Boss;
using TinyParryArena.Fear;
using UnityEngine;

namespace TinyParryArena.FinalStrike
{
    [DisallowMultipleComponent]
    public sealed class BossFinalStrikeTarget : MonoBehaviour
    {
        [SerializeField] private FearController fearController;
        [SerializeField] private BossFearBrain bossBrain;
        [SerializeField] private Animator bossAnimator;
        [SerializeField] private MonoBehaviour killableComponent;
        [SerializeField] private Transform executionPoint;

        private IBossKillable killable;

        public Transform ExecutionPoint => executionPoint != null ? executionPoint : transform;

        public bool CanExecute =>
            fearController != null &&
            fearController.IsBroken &&
            killable != null &&
            killable.IsAlive;

        private void Awake()
        {
            killable = killableComponent as IBossKillable;

            if (killableComponent != null && killable == null)
            {
                Debug.LogError($"{killableComponent.GetType().Name} must implement IBossKillable.", this);
            }
        }

        public bool TryExecute(Animator playerAnimator)
        {
            if (!CanExecute)
            {
                return false;
            }

            bossBrain?.SetCombatEnabled(false);
            playerAnimator?.SetTrigger("FinalStrike");
            bossAnimator?.SetTrigger("Executed");
            killable.KillByFinalStrike();
            return true;
        }
    }
}
