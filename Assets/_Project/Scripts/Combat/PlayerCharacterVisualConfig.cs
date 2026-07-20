using UnityEngine;

[CreateAssetMenu(menuName = "Tiny Parry Arena/Player Character Visual Config")]
public sealed class PlayerCharacterVisualConfig : ScriptableObject
{
    public GameObject characterPrefab;
    [Min(0.5f)] public float targetHeight = 1.84f;
}
