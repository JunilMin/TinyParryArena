using UnityEngine;

public interface IPlayerInteractable
{
    Transform InteractionTransform { get; }
    bool CanInteract { get; }
    string InteractionPrompt { get; }
    void Interact();
}
