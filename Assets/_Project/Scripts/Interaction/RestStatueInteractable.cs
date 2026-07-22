using UnityEngine;

public sealed class RestStatueInteractable : MonoBehaviour, IPlayerInteractable
{
    private TinyParryStageManager manager;

    public Transform InteractionTransform => transform;
    public bool CanInteract => manager != null && manager.CanUseRestStatue;
    public string InteractionPrompt => "COMMUNE WITH THE ROYAL STATUE";

    public void Configure(TinyParryStageManager owner) => manager = owner;
    public void Interact() => manager?.OpenRestStatueMenu();
}
