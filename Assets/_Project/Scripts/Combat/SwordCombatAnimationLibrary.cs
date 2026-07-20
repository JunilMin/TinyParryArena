using UnityEngine;

[CreateAssetMenu(menuName = "Tiny Parry Arena/Sword Combat Animation Library")]
public sealed class SwordCombatAnimationLibrary : ScriptableObject
{
    [Header("Locomotion")]
    public AnimationClip idle;
    public AnimationClip walk;
    public AnimationClip run;

    [Header("Combat")]
    public AnimationClip attackOne;
    public AnimationClip attackTwo;
    public AnimationClip attackThree;
    public AnimationClip parry;
    public AnimationClip drink;
    public AnimationClip hit;
    public AnimationClip death;

    public bool IsValid =>
        idle != null && walk != null && run != null &&
        attackOne != null && attackTwo != null && attackThree != null &&
        parry != null;

    public AnimationClip GetAttack(int combo)
    {
        switch (combo % 3)
        {
            case 0: return attackOne;
            case 1: return attackTwo;
            default: return attackThree;
        }
    }
}
