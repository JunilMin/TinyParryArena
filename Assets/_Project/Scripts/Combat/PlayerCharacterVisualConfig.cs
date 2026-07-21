using UnityEngine;

[CreateAssetMenu(menuName = "Tiny Parry Arena/Character Visual Set")]
public sealed class PlayerCharacterVisualConfig : ScriptableObject
{
    public GameObject playerPrefab;
    public GameObject bossPrefab;
    [Tooltip("Optional stage-cycled boss appearances. The legacy boss prefab is used when empty.")]
    public GameObject[] bossPrefabs;
}
