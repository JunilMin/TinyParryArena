using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class TinyParryStageManager : MonoBehaviour
{
    private enum GameState
    {
        Title, Prologue, Playing, Transitioning, AwaitingExit, ClassSelection, RestExploration, Resting,
        Paused, Dying, Defeat, Victory
    }

    [SerializeField] private GameObject[] stageRoots;
    [SerializeField] private CombatantHealth[] bosses;
    [SerializeField] private string[] stageNames;
    [SerializeField] private Transform player;
    [SerializeField] private TinyParryHUD hud;
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private float transitionDelay = 1.2f;
    [SerializeField] private float deathPresentationDelay = 1.15f;

    private static bool autoStartNextLoad;
    private int currentStage;
    private bool runtimeInitialized;
    private GameState state;
    private GameState stateBeforePause;
    private float runStartedAt;
    private bool debugInvincible;
    private CombatantHealth playerHealth;
    private PlayerPotion potion;
    private PlayerProgression progression;
    private PlayerInteractor interactor;
    private Vector3 restRoomPosition = new Vector3(42f, 0.15f, 0f);
    private StageExitDoor[] stageExitDoors;
    private StageExitDoor restExitDoor;
    private int restMenuCategory;
    private int restMenuSelection;
    private int prologuePage;

    public bool CanUseRestStatue => state == GameState.RestExploration;

    public void Configure(GameObject[] roots, CombatantHealth[] bossHealth, string[] names,
        Transform playerTransform, TinyParryHUD hudValue, PlayerController controllerValue, PlayerCombat combatValue)
    {
        stageRoots = roots;
        bosses = bossHealth;
        stageNames = names;
        player = playerTransform;
        hud = hudValue;
        controller = controllerValue;
        combat = combatValue;
        ActivateStageImmediate(0, false);
    }

    private void Start() => InitializeRuntime();

    private void InitializeRuntime()
    {
        if (runtimeInitialized) return;
        runtimeInitialized = true;
        Time.timeScale = 1f;
        currentStage = FindInitiallyActiveStage();

        if (player != null)
        {
            playerHealth = player.GetComponent<CombatantHealth>();
            potion = player.GetComponent<PlayerPotion>();
            progression = player.GetComponent<PlayerProgression>();
            if (progression == null) progression = player.gameObject.AddComponent<PlayerProgression>();
            interactor = player.GetComponent<PlayerInteractor>();
            if (interactor == null) interactor = player.gameObject.AddComponent<PlayerInteractor>();
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        }

        if (bosses != null)
            foreach (CombatantHealth boss in bosses)
                if (boss != null) boss.Died += OnAnyBossDied;

        CreateInteractionSpaces();
        ActivateStageImmediate(currentStage, true);
        if (autoStartNextLoad) { autoStartNextLoad = false; StartRun(); }
        else EnterTitle();
    }

    private int FindInitiallyActiveStage()
    {
        if (stageRoots == null || stageRoots.Length == 0) return 0;
        for (int i = 0; i < stageRoots.Length; i++)
            if (stageRoots[i] != null && stageRoots[i].activeSelf) return i;
        return 0;
    }

    private void Update()
    {
        if (!runtimeInitialized) return;
        switch (state)
        {
            case GameState.Title:
                if (ConfirmPressed()) BeginPrologue();
                break;
            case GameState.Prologue:
                if (PausePressed()) StartRun();
                else if (ConfirmPressed()) AdvancePrologue();
                break;
            case GameState.Playing:
                if (PausePressed()) { EnterPause(); return; }
                HandleDebugKeys();
                CheckCurrentBossDeath();
                break;
            case GameState.AwaitingExit:
            case GameState.RestExploration:
                if (PausePressed()) EnterPause();
                break;
            case GameState.ClassSelection:
                if (NumberPressed(1)) ChooseClass(PlayerWeaponClass.Greatsword);
                else if (NumberPressed(2)) ChooseClass(PlayerWeaponClass.Bow);
                break;
            case GameState.Resting:
                HandleRestMenu();
                break;
            case GameState.Paused:
                HandlePauseMenu();
                break;
            case GameState.Defeat:
                if (ConfirmPressed()) ReloadScene(true);
                else if (PausePressed() || TitlePressed()) ReloadScene(false);
                break;
            case GameState.Victory:
                if (ConfirmPressed() || PausePressed()) ReloadScene(false);
                break;
        }
    }


    private void HandlePauseMenu()
    {
        if (hud != null && hud.PauseSkillsVisible)
        {
            if (PausePressed()) { hud.ClosePauseSkills(); return; }
            if (ArrowPressed(-1)) hud.MovePauseSkillSelection(-1);
            else if (ArrowPressed(1)) hud.MovePauseSkillSelection(1);
            else if (ConfirmPressed()) hud.EquipSelectedPauseSkill();
            return;
        }

        if (LetterPressed('s')) { hud?.OpenPauseSkills(); return; }
        if (PausePressed() || ConfirmPressed()) ResumeFromPause();
        else if (RestartPressed()) ReloadScene(true);
        else if (TitlePressed()) ReloadScene(false);
    }

    private void CheckCurrentBossDeath()
    {
        if (bosses == null || currentStage < 0 || currentStage >= bosses.Length) return;
        CombatantHealth boss = bosses[currentStage];
        if (boss == null || boss.IsDead) BeginStageClear();
    }

    private void EnterTitle()
    {
        state = GameState.Title;
        Time.timeScale = 0f;
        SetGameplayControls(false);
        SetEnemySimulation(false);
        hud?.ShowTitle();
    }

    private void BeginPrologue()
    {
        state = GameState.Prologue;
        prologuePage = 0;
        Time.timeScale = 0f;
        SetGameplayControls(false);
        SetEnemySimulation(false);
        hud?.ShowProloguePage(prologuePage);
    }

    private void AdvancePrologue()
    {
        prologuePage++;
        if (prologuePage >= 6)
        {
            StartRun();
            return;
        }
        hud?.ShowProloguePage(prologuePage);
    }

    private void StartRun()
    {
        Time.timeScale = 1f;
        state = GameState.Playing;
        runStartedAt = Time.unscaledTime;
        hud?.ClearOverlay();
        SetEnemySimulation(true);
        SetGameplayControls(true);
        hud?.SetMessage($"ROYAL CASTLE  ·  {GetStageName(currentStage)}");
    }

    private void EnterPause()
    {
        stateBeforePause = state;
        state = GameState.Paused;
        SetGameplayControls(false);
        SetInteractionEnabled(false);
        Time.timeScale = 0f;
        hud?.ShowPause(progression);
    }

    private void ResumeFromPause()
    {
        Time.timeScale = 1f;
        state = stateBeforePause == GameState.AwaitingExit || stateBeforePause == GameState.RestExploration
            ? stateBeforePause
            : GameState.Playing;
        hud?.ClearOverlay();
        SetEnemySimulation(true);
        SetGameplayControls(true);
        SetInteractionEnabled(true);
    }

    private void OnAnyBossDied(CombatantHealth deadBoss)
    {
        if (state != GameState.Playing || bosses == null) return;
        if (currentStage >= 0 && currentStage < bosses.Length && bosses[currentStage] == deadBoss)
            BeginStageClear();
    }

    private void BeginStageClear()
    {
        if (state != GameState.Playing) return;
        SetGameplayControls(false);
        state = GameState.Transitioning;
        StartCoroutine(StageClearRoutine());
    }

    private IEnumerator StageClearRoutine()
    {
        GameAudioManager.Instance.PlayRoundClear();
        int clearedStage = currentStage + 1;
        hud?.SetMessage($"STAGE {clearedStage} CLEARED");

        if (progression != null)
            progression.AwardStageClearPoints(clearedStage);

        yield return new WaitForSecondsRealtime(transitionDelay);

        if (stageRoots == null || currentStage >= stageRoots.Length - 1)
        {
            yield return VictoryRoutine();
            yield break;
        }

        if (clearedStage == 3 && progression != null && !progression.HasChosenClass)
        {
            state = GameState.ClassSelection;
            hud?.ShowClassSelection();
        }
        else
        {
            UnlockCurrentExitDoor();
        }
    }

    private void ChooseClass(PlayerWeaponClass chosen)
    {
        progression?.ChooseClass(chosen);
        UnlockCurrentExitDoor();
    }

    private void UnlockCurrentExitDoor()
    {
        state = GameState.AwaitingExit;
        hud?.ClearOverlay();
        if (stageExitDoors != null && currentStage >= 0 && currentStage < stageExitDoors.Length)
            stageExitDoors[currentStage]?.SetUnlocked(true);
        SetGameplayControls(true);
        SetInteractionEnabled(true);
        hud?.SetMessage("THE SEALED DOOR HAS OPENED  -  APPROACH IT AND PRESS E");
    }

    private void ContinueAfterDecision()
    {
        hud?.ClearOverlay();
        restExitDoor?.ResetDoor();
        ActivateStageImmediate(currentStage + 1, true);
        state = GameState.Playing;
        SetGameplayControls(true);
        SetInteractionEnabled(true);
    }

    public void EnterRestChamberFromDoor(int stageIndex)
    {
        if (state != GameState.AwaitingExit || stageIndex != currentStage) return;
        state = GameState.RestExploration;
        TeleportPlayer(restRoomPosition + Vector3.back * 3.5f);
        if (playerHealth != null) playerHealth.Heal(playerHealth.MaxHealth);
        potion?.ResetPotions();
        hud?.ClearOverlay();
        SetGameplayControls(true);
        SetInteractionEnabled(true);
        hud?.SetMessage("REST CHAMBER  -  APPROACH THE STATUE AND PRESS E");
    }

    public void LeaveRestChamber()
    {
        if (state != GameState.RestExploration) return;
        ContinueAfterDecision();
    }

    public void OpenRestStatueMenu()
    {
        if (state != GameState.RestExploration) return;
        state = GameState.Resting;
        restMenuCategory = 0;
        restMenuSelection = 0;
        SetGameplayControls(false);
        SetInteractionEnabled(false);
        hud?.ShowRestRoom(progression, restMenuCategory, restMenuSelection);
    }

    private void HandleRestMenu()
    {
        if (PausePressed() || LetterPressed('e'))
        {
            state = GameState.RestExploration;
            hud?.ClearOverlay();
            SetGameplayControls(true);
            SetInteractionEnabled(true);
            return;
        }

        if (TabPressed())
        {
            restMenuCategory = 1 - restMenuCategory;
            restMenuSelection = 0;
            hud?.ShowRestRoom(progression, restMenuCategory, restMenuSelection);
            return;
        }

        int count = restMenuCategory == 0 ? 3 : PlayerProgression.TotalSkillCount;
        bool moved = false;
        if (ArrowPressed(-1)) { restMenuSelection = (restMenuSelection - 1 + count) % count; moved = true; }
        else if (ArrowPressed(1)) { restMenuSelection = (restMenuSelection + 1) % count; moved = true; }

        bool changed = false;
        if (ConfirmPressed() || HorizontalArrowPressed(1))
        {
            changed = progression != null && (restMenuCategory == 0
                ? progression.SpendStatPoint(restMenuSelection)
                : progression.SpendSkillPoint(restMenuSelection));
        }
        else if (HorizontalArrowPressed(-1) || BackspacePressed())
            changed = progression != null && (restMenuCategory == 0
                ? progression.RefundStatPoint(restMenuSelection)
                : progression.RefundSkillPoint(restMenuSelection));

        if (changed || moved) hud?.ShowRestRoom(progression, restMenuCategory, restMenuSelection);
    }

    private IEnumerator VictoryRoutine()
    {
        yield return new WaitForSecondsRealtime(deathPresentationDelay);
        state = GameState.Victory;
        int wins = PlayerPrefs.GetInt("TPA_Wins", 0) + 1;
        PlayerPrefs.SetInt("TPA_Wins", wins);
        PlayerPrefs.SetInt("TPA_HighestRound", Mathf.Max(stageRoots != null ? stageRoots.Length : 10, PlayerPrefs.GetInt("TPA_HighestRound", 0)));
        float elapsed = Mathf.Max(0f, Time.unscaledTime - runStartedAt);
        float best = PlayerPrefs.GetFloat("TPA_BestTime", 0f);
        if (best <= 0f || elapsed < best) PlayerPrefs.SetFloat("TPA_BestTime", elapsed);
        PlayerPrefs.Save();
        GameAudioManager.Instance.PlayVictory();
        hud?.ShowVictory(elapsed, wins);
    }

    private void ActivateStageImmediate(int index, bool updateHud)
    {
        if (stageRoots == null || stageRoots.Length == 0) return;
        currentStage = Mathf.Clamp(index, 0, stageRoots.Length - 1);
        for (int i = 0; i < stageRoots.Length; i++)
            if (stageRoots[i] != null) stageRoots[i].SetActive(i == currentStage);

        TeleportPlayer(GetPlayerSpawn(currentStage));
        controller?.ResetVerticalMotion();
        if (!updateHud) return;

        CombatantHealth boss = bosses != null && currentStage < bosses.Length ? bosses[currentStage] : null;
        string stageName = GetStageName(currentStage);
        hud?.SetBoss(boss, stageName);
        hud?.SetStage(currentStage + 1, stageRoots.Length, stageName);
        hud?.SetMessage($"ROYAL CASTLE  ·  {stageName}");
        GameAudioManager.Instance.PlayRoundStart();
        PlayerPrefs.SetInt("TPA_HighestRound", Mathf.Max(currentStage + 1, PlayerPrefs.GetInt("TPA_HighestRound", 0)));
        PlayerPrefs.Save();
    }

    private void TeleportPlayer(Vector3 position)
    {
        if (player == null) return;
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.position = position;
        player.rotation = Quaternion.identity;
        if (cc != null) cc.enabled = true;
    }

    private void CreateInteractionSpaces()
    {
        GameObject root = new GameObject("War Waiting Room");
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Waiting Room Floor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.position = new Vector3(restRoomPosition.x, -0.35f, restRoomPosition.z);
        floor.transform.localScale = new Vector3(14f, 0.6f, 14f);
        for (int i = 0; i < 4; i++)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Waiting Room Pillar";
            pillar.transform.SetParent(root.transform, false);
            float x = restRoomPosition.x + (i % 2 == 0 ? -5.5f : 5.5f);
            float z = restRoomPosition.z + (i < 2 ? -5.5f : 5.5f);
            pillar.transform.position = new Vector3(x, 1.5f, z);
            pillar.transform.localScale = new Vector3(0.6f, 2f, 0.6f);
        }

        GameObject statue = new GameObject("Royal Rest Statue");
        statue.transform.SetParent(root.transform, false);
        statue.transform.position = restRoomPosition + Vector3.forward * 2f;
        statue.AddComponent<RestStatueInteractable>().Configure(this);
        CreatePrimitive(statue.transform, "Stone Pedestal", PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(1.7f, 0.35f, 1.7f));
        CreatePrimitive(statue.transform, "Stone Body", PrimitiveType.Capsule, new Vector3(0f, 2.25f, 0f), new Vector3(1.35f, 1.8f, 1.35f));
        CreatePrimitive(statue.transform, "Stone Head", PrimitiveType.Sphere, new Vector3(0f, 4.15f, 0f), Vector3.one * 1.15f);
        CreatePrimitive(statue.transform, "Stone Sword", PrimitiveType.Cube, new Vector3(0f, 2.1f, -0.85f), new Vector3(0.28f, 3.2f, 0.28f));

        restExitDoor = CreateDoor(root.transform, "Rest Chamber Exit", restRoomPosition + Vector3.back * 6f, -1, true, false);

        stageExitDoors = new StageExitDoor[stageRoots != null ? stageRoots.Length : 0];
        for (int i = 0; i < stageExitDoors.Length; i++)
        {
            if (stageRoots[i] == null) continue;
            Vector3 localPosition = new Vector3(0f, 0f, 21f);
            if (bosses != null && i < bosses.Length && bosses[i] != null)
                localPosition = stageRoots[i].transform.InverseTransformPoint(bosses[i].transform.position) + Vector3.forward * 6f;
            stageExitDoors[i] = CreateDoor(stageRoots[i].transform, $"Stage {i + 1} Sealed Door", localPosition, i, false, true);
        }
    }

    private StageExitDoor CreateDoor(Transform parent, string name, Vector3 position, int stageIndex, bool restExit, bool localSpace)
    {
        GameObject door = new GameObject(name);
        door.transform.SetParent(parent, false);
        if (localSpace) door.transform.localPosition = position;
        else door.transform.position = position;

        CreatePrimitive(door.transform, "Left Frame", PrimitiveType.Cube, new Vector3(-2f, 2f, 0f), new Vector3(0.55f, 4f, 0.75f));
        CreatePrimitive(door.transform, "Right Frame", PrimitiveType.Cube, new Vector3(2f, 2f, 0f), new Vector3(0.55f, 4f, 0.75f));
        CreatePrimitive(door.transform, "Top Frame", PrimitiveType.Cube, new Vector3(0f, 4.15f, 0f), new Vector3(4.55f, 0.55f, 0.75f));
        GameObject slab = CreatePrimitive(door.transform, "Door Slab", PrimitiveType.Cube, new Vector3(0f, 2f, 0f), new Vector3(3.5f, 3.8f, 0.45f));
        StageExitDoor result = door.AddComponent<StageExitDoor>();
        result.Configure(this, stageIndex, restExit, slab.transform);
        return result;
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale)
    {
        GameObject value = GameObject.CreatePrimitive(type);
        value.name = name;
        value.transform.SetParent(parent, false);
        value.transform.localPosition = localPosition;
        value.transform.localScale = localScale;
        Renderer renderer = value.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = new Color(0.28f, 0.29f, 0.32f, 1f);
        return value;
    }

    private string GetStageName(int index) => stageNames != null && index >= 0 && index < stageNames.Length ? stageNames[index] : $"Round {index + 1}";
    private static Vector3 GetPlayerSpawn(int stageIndex) => new Vector3(0f, 0.15f, stageIndex == 2 || stageIndex >= 5 ? -16f : -14f);

    private void HandleDebugKeys()
    {
        if (FunctionKeyPressed(1) && playerHealth != null)
        {
            debugInvincible = !debugInvincible;
            playerHealth.SetInvincible(debugInvincible);
            hud?.SetMessage($"DEBUG F1 - INVINCIBLE {(debugInvincible ? "ON" : "OFF")}");
        }
        if (FunctionKeyPressed(2) && playerHealth != null) { playerHealth.Heal(playerHealth.MaxHealth); hud?.SetMessage("DEBUG F2 - FULL HEAL"); }
        if (FunctionKeyPressed(3) && potion != null) { potion.AddPotions(10); hud?.SetMessage("DEBUG F3 - POTION +10"); }
        if (FunctionKeyPressed(4) && bosses != null && currentStage < bosses.Length && bosses[currentStage] != null) bosses[currentStage].ForceKill();
        if (FunctionKeyPressed(5)) DebugAdvanceStage();
    }

    private void DebugAdvanceStage()
    {
        if (stageRoots == null || stageRoots.Length == 0) return;
        StopAllCoroutines();
        int next = Mathf.Min(currentStage + 1, stageRoots.Length - 1);
        if (next == currentStage) { BeginStageClear(); return; }
        ActivateStageImmediate(next, true);
        state = GameState.Playing;
        SetGameplayControls(true);
    }

    private void OnPlayerDied(CombatantHealth _)
    {
        if (state == GameState.Dying || state == GameState.Defeat || state == GameState.Victory) return;
        state = GameState.Dying;
        SetGameplayControls(false);
        player?.GetComponent<CharacterVisualController>()?.PlayDeath();
        int deaths = PlayerPrefs.GetInt("TPA_Deaths", 0) + 1;
        PlayerPrefs.SetInt("TPA_Deaths", deaths);
        PlayerPrefs.Save();
        StartCoroutine(DefeatRoutine(deaths));
    }

    private IEnumerator DefeatRoutine(int deaths)
    {
        yield return new WaitForSecondsRealtime(deathPresentationDelay);
        state = GameState.Defeat;
        GameAudioManager.Instance.PlayDefeat();
        hud?.ShowDefeat(currentStage + 1, deaths);
    }

    private void SetGameplayControls(bool enabled)
    {
        controller?.SetControlsEnabled(enabled);
        combat?.SetControlsEnabled(enabled);
        if (potion != null) potion.enabled = enabled;
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (interactor != null) interactor.enabled = enabled;
    }

    private void SetEnemySimulation(bool enabled)
    {
        if (bosses == null) return;
        foreach (CombatantHealth boss in bosses)
        {
            if (boss == null) continue;
            BossAI ai = boss.GetComponent<BossAI>();
            if (ai != null) ai.enabled = enabled;
            Animator animator = boss.GetComponentInChildren<Animator>();
            if (animator != null) animator.speed = enabled ? 1f : 0f;
        }
    }
    private static void ReloadScene(bool autoStart) { Time.timeScale = 1f; autoStartNextLoad = autoStart; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    private static bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }
    private static bool ContinuePressed() => LetterPressed('n') || ConfirmPressed();
    private static bool RestPressed() => LetterPressed('r');
    private static bool PausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; return k != null && k.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
    private static bool RestartPressed() => RestPressed();
    private static bool TitlePressed() => LetterPressed('t');
    private static bool LetterPressed(char c)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; if (k == null) return false;
        switch (char.ToLowerInvariant(c)) { case 'a': return k.aKey.wasPressedThisFrame; case 'c': return k.cKey.wasPressedThisFrame; case 'e': return k.eKey.wasPressedThisFrame; case 'h': return k.hKey.wasPressedThisFrame; case 'n': return k.nKey.wasPressedThisFrame; case 'r': return k.rKey.wasPressedThisFrame; case 's': return k.sKey.wasPressedThisFrame; case 't': return k.tKey.wasPressedThisFrame; case 'x': return k.xKey.wasPressedThisFrame; case 'z': return k.zKey.wasPressedThisFrame; default: return false; }
#else
        return Input.GetKeyDown(c.ToString());
#endif
    }
    private static bool TabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; return k != null && k.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
    }
    private static bool ArrowPressed(int direction)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && (direction < 0 ? k.upArrowKey.wasPressedThisFrame : k.downArrowKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(direction < 0 ? KeyCode.UpArrow : KeyCode.DownArrow);
#endif
    }
    private static bool HorizontalArrowPressed(int direction)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && (direction < 0 ? k.leftArrowKey.wasPressedThisFrame : k.rightArrowKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(direction < 0 ? KeyCode.LeftArrow : KeyCode.RightArrow);
#endif
    }
    private static bool BackspacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; return k != null && k.backspaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Backspace);
#endif
    }
    private static bool NumberPressed(int number)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; if (k == null) return false;
        switch (number)
        {
            case 0: return k.digit0Key.wasPressedThisFrame || k.numpad0Key.wasPressedThisFrame;
            case 1: return k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame;
            case 2: return k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame;
            case 3: return k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame;
            case 4: return k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame;
            case 5: return k.digit5Key.wasPressedThisFrame || k.numpad5Key.wasPressedThisFrame;
            case 6: return k.digit6Key.wasPressedThisFrame || k.numpad6Key.wasPressedThisFrame;
            case 7: return k.digit7Key.wasPressedThisFrame || k.numpad7Key.wasPressedThisFrame;
            case 8: return k.digit8Key.wasPressedThisFrame || k.numpad8Key.wasPressedThisFrame;
            case 9: return k.digit9Key.wasPressedThisFrame || k.numpad9Key.wasPressedThisFrame;
            default: return false;
        }
#else
        if (number < 0 || number > 9) return false;
        return Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + number)) ||
               Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + number));
#endif
    }
    private static bool ShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }
    private static bool FunctionKeyPressed(int number)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; if (k == null) return false;
        if (number == 1) return k.f1Key.wasPressedThisFrame; if (number == 2) return k.f2Key.wasPressedThisFrame; if (number == 3) return k.f3Key.wasPressedThisFrame; if (number == 4) return k.f4Key.wasPressedThisFrame; return number == 5 && k.f5Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(number == 1 ? KeyCode.F1 : number == 2 ? KeyCode.F2 : number == 3 ? KeyCode.F3 : number == 4 ? KeyCode.F4 : KeyCode.F5);
#endif
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        if (bosses != null) foreach (CombatantHealth boss in bosses) if (boss != null) boss.Died -= OnAnyBossDied;
    }
}
