using UnityEngine;

public sealed class StageExitDoor : MonoBehaviour, IPlayerInteractable
{
    private TinyParryStageManager manager;
    private int stageIndex;
    private bool restExit;
    private bool unlocked;
    private bool opened;
    private Transform doorSlab;

    public Transform InteractionTransform => transform;
    public bool CanInteract => unlocked && !opened;
    public string InteractionPrompt => restExit ? "ENTER THE NEXT FLOOR" : "OPEN THE STAIR GATE";

    public void Configure(TinyParryStageManager owner, int index, bool isRestExit, Transform slab)
    {
        manager = owner;
        stageIndex = index;
        restExit = isRestExit;
        doorSlab = slab;
        unlocked = isRestExit;
    }

    public void SetUnlocked(bool value) => unlocked = value;

    public void ResetDoor()
    {
        opened = false;
        unlocked = restExit;
        if (doorSlab != null) doorSlab.gameObject.SetActive(true);
    }

    public void Interact()
    {
        if (!CanInteract || manager == null)
            return;
        opened = true;
        if (doorSlab != null) doorSlab.gameObject.SetActive(false);
        if (restExit) manager.LeaveRestChamber();
        else manager.EnterRestChamberFromDoor(stageIndex);
    }
}
