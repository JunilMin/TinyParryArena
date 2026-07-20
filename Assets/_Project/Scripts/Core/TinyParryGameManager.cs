using System.Collections;
using UnityEngine;

public sealed class TinyParryGameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject[] opponents;
    [SerializeField] private GameObject[] stageEnvironments;
    [SerializeField] private TinyParryHUD hud;
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private float stageTransitionDelay = 1.35f;

    private int stageIndex;
    private bool transitioning;

    private static readonly string[] StageNames =
    {
        "STAGE 1  ·  ANCIENT PIT",
        "STAGE 2  ·  CASTLE COURTYARD",
        "STAGE 3  ·  KING'S FINAL HALL"
    };

    public void Configure(
        GameObject player,
        GameObject[] opponentObjects,
        GameObject[] environmentObjects,
        TinyParryHUD hudValue,
        PlayerController controllerValue,
        PlayerCombat combatValue)
    {
        playerObject = player;
        opponents = opponentObjects;
        stageEnvironments = environmentObjects;
        hud = hudValue;
        controller = controllerValue;
        combat = combatValue;
    }

    private void Start()
    {
        if (playerObject == null || opponents == null || opponents.Length < 3 ||
            stageEnvironments == null || stageEnvironments.Length < 3 || hud == null)
        {
            Debug.LogError("TinyParryGameManager is not fully configured.");
            enabled = false;
            return;
        }

        for (int i = 0; i < opponents.Length; i++)
        {
            int captured = i;
            CombatantHealth health = opponents[i].GetComponent<CombatantHealth>();
            if (health != null) health.Died += _ => OnOpponentDefeated(captured);
        }

        CombatantHealth playerHealth = playerObject.GetComponent<CombatantHealth>();
        if (playerHealth != null) playerHealth.Died += _ => OnPlayerDefeated();

        ActivateStage(0, true);
    }

    private void OnOpponentDefeated(int defeatedStage)
    {
        if (transitioning || defeatedStage != stageIndex) return;
        if (stageIndex >= opponents.Length - 1)
        {
            controller.SetControlsEnabled(false);
            combat.SetControlsEnabled(false);
            hud.ShowResult("VICTORY\nALL 3 STAGES CLEARED");
            hud.SetMessage("왕의 결전장에서 승리했습니다");
            return;
        }
        StartCoroutine(AdvanceStage());
    }

    private IEnumerator AdvanceStage()
    {
        transitioning = true;
        controller.SetControlsEnabled(false);
        combat.SetControlsEnabled(false);
        hud.SetMessage($"STAGE {stageIndex + 1} CLEAR");
        yield return new WaitForSeconds(stageTransitionDelay);
        stageIndex++;
        ActivateStage(stageIndex, false);
        transitioning = false;
    }

    private void ActivateStage(int index, bool firstStage)
    {
        for (int i = 0; i < stageEnvironments.Length; i++)
            if (stageEnvironments[i] != null) stageEnvironments[i].SetActive(i == index);
        for (int i = 0; i < opponents.Length; i++)
            if (opponents[i] != null) opponents[i].SetActive(i == index);

        playerObject.transform.position = new Vector3(0f, 0.05f, -4.2f);
        playerObject.transform.rotation = Quaternion.identity;
        opponents[index].transform.position = index == 2 ? new Vector3(0f, 0f, 4.8f) : new Vector3(0f, 0f, 2.4f);
        opponents[index].transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        controller.SetControlsEnabled(true);
        combat.SetControlsEnabled(true);
        hud.SetCurrentOpponent(opponents[index].GetComponent<CombatantHealth>(), index == 2, index + 1);
        hud.SetStageInfo(index + 1, StageNames[index]);
        hud.SetMessage(firstStage ? "경기장 수문장을 처치하세요" : $"STAGE {index + 1} START");
    }

    private void OnPlayerDefeated()
    {
        controller.SetControlsEnabled(false);
        combat.SetControlsEnabled(false);
        hud.ShowResult("DEFEAT");
    }
}
