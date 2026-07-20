using UnityEngine;

namespace TinyParryArena.Fear
{
    [CreateAssetMenu(fileName = "FearConfig", menuName = "Tiny Parry Arena/Fear/Fear Config")]
    public sealed class FearConfig : ScriptableObject
    {
        [Header("Fear Range")]
        [Min(1f)] public float maxFear = 100f;

        [Header("Fear Gain")]
        [Min(0f)] public float normalParryGain = 5f;
        [Min(0f)] public float perfectParryGain = 10f;

        [Header("Fear Loss")]
        [Min(0f)] public float playerHitLoss = 15f;
        [Min(0f)] public float inactivityDelay = 3f;
        [Min(0f)] public float inactivityDecayPerSecond = 1f;

        [Header("Fear State Thresholds")]
        [Range(0f, 100f)] public float alertThreshold = 25f;
        [Range(0f, 100f)] public float afraidThreshold = 50f;
        [Range(0f, 100f)] public float desperateThreshold = 75f;
        [Range(0f, 100f)] public float collapseThreshold = 95f;

        private void OnValidate()
        {
            maxFear = Mathf.Max(1f, maxFear);
            alertThreshold = Mathf.Clamp(alertThreshold, 0f, maxFear);
            afraidThreshold = Mathf.Clamp(afraidThreshold, alertThreshold, maxFear);
            desperateThreshold = Mathf.Clamp(desperateThreshold, afraidThreshold, maxFear);
            collapseThreshold = Mathf.Clamp(collapseThreshold, desperateThreshold, maxFear);
        }
    }
}
