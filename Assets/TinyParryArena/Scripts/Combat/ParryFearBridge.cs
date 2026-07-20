using TinyParryArena.Fear;
using UnityEngine;

namespace TinyParryArena.Combat
{
    [DisallowMultipleComponent]
    public sealed class ParryFearBridge : MonoBehaviour
    {
        [SerializeField] private FearController targetFear;

        public void ReportNormalParry()
        {
            targetFear?.RegisterSuccessfulParry(ParryQuality.Normal);
        }

        public void ReportPerfectParry()
        {
            targetFear?.RegisterSuccessfulParry(ParryQuality.Perfect);
        }

        public void ReportPlayerHit()
        {
            targetFear?.RegisterPlayerHit();
        }

        public void SetTarget(FearController fearController)
        {
            targetFear = fearController;
        }
    }
}
