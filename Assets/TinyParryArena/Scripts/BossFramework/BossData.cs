using UnityEngine;

namespace TinyParryArena.BossFramework
{
    [CreateAssetMenu(fileName = "BossData", menuName = "Tiny Parry Arena/Boss Data")]
    public sealed class BossData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Boss";
        [SerializeField] private BossPersonality personality = BossPersonality.Knight;
        [SerializeField] private WeaponType weaponType = WeaponType.Sword;

        [Header("Defeat Presentation")]
        [SerializeField] private BrokenPose brokenPose = BrokenPose.Kneel;
        [SerializeField] private ExecutionType executionType = ExecutionType.HeartStab;

        [Header("Optional Overrides")]
        [Tooltip("A value of 0 keeps the value already configured on the scene boss.")]
        [Min(0)] [SerializeField] private int maxHealthOverride;
        [Tooltip("A value of 0 keeps the value already configured on the scene boss.")]
        [Min(0f)] [SerializeField] private float moveSpeedOverride;

        public string DisplayName => displayName;
        public BossPersonality Personality => personality;
        public WeaponType WeaponType => weaponType;
        public BrokenPose BrokenPose => brokenPose;
        public ExecutionType ExecutionType => executionType;
        public int MaxHealthOverride => maxHealthOverride;
        public float MoveSpeedOverride => moveSpeedOverride;
    }
}
