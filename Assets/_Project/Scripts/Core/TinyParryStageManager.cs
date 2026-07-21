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
        Title, Prologue, Playing, Transitioning, PostStageDecision, ClassSelection, Resting,
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
    private float runStartedAt;
    private bool debugInvincible;
    private CombatantHealth playerHealth;
    private PlayerPotion potion;
    private PlayerProgression progression;
    private Vector3 restRoomPosition = new Vector3(42f, 0.15f, 0f);
    private int prologuePage;

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
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        }

        if (bosses != null)
            foreach (CombatantHealth boss in bosses)
                if (boss != null) boss.Died += OnAnyBossDied;

        CreateWaitingRoom();
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
            case GameState.PostStageDecision:
                if (ContinuePressed()) ContinueAfterDecision();
                else if (RestPressed()) EnterRestRoom();
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
        state = GameState.Paused;
        SetGameplayControls(false);
        Time.timeScale = 0f;
        hud?.ShowPause(progression);
    }

    private void ResumeFromPause()
    {
        Time.timeScale = 1f;
        state = GameState.Playing;
        hud?.ClearOverlay();
        SetEnemySimulation(true);
        SetGameplayControls(true);
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
            state = GameState.PostStageDecision;
            hud?.ShowStageDecision(clearedStage, progression);
        }
    }

    private void ChooseClass(PlayerWeaponClass chosen)
    {
        progression?.ChooseClass(chosen);
        state = GameState.PostStageDecision;
        hud?.ShowStageDecision(currentStage + 1, progression);
    }

    private void ContinueAfterDecision()
    {
        hud?.ClearOverlay();
        ActivateStageImmediate(currentStage + 1, true);
        state = GameState.Playing;
        SetGameplayControls(true);
    }

    private void EnterRestRoom()
    {
        state = GameState.Resting;
        SetGameplayControls(false);
        TeleportPlayer(restRoomPosition);
        if (playerHealth != null) playerHealth.Heal(playerHealth.MaxHealth);
        potion?.ResetPotions();
        hud?.ShowRestRoom(progression);
    }

    private void HandleRestMenu()
    {
        if (ConfirmPressed())
        {
            ContinueAfterDecision();
            return;
        }

        bool changed = false;
        if (LetterPressed('h')) changed = progression != null && progression.SpendStatPoint(0);
        else if (LetterPressed('a')) changed = progression != null && progression.SpendStatPoint(1);
        else if (LetterPressed('s')) changed = progression != null && progression.SpendStatPoint(2);
        else
        {
            bool refund = ShiftHeld();
            for (int slot = 0; slot < PlayerProgression.TotalSkillCount && !changed; slot++)
            {
                int number = slot == 9 ? 0 : slot + 1;
                if (!NumberPressed(number)) continue;
                changed = progression != null && (refund
                    ? progression.RefundSkillPoint(slot)
                    : progression.SpendSkillPoint(slot));
            }
        }

        if (changed) hud?.ShowRestRoom(progression);
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

    private void CreateWaitingRoom()
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

    private void SetGameplayControls(bool enabled) { controller?.SetControlsEnabled(enabled); combat?.SetControlsEnabled(enabled); }

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
        switch (char.ToLowerInvariant(c)) { case 'a': return k.aKey.wasPressedThisFrame; case 'c': return k.cKey.wasPressedThisFrame; case 'h': return k.hKey.wasPressedThisFrame; case 'n': return k.nKey.wasPressedThisFrame; case 'r': return k.rKey.wasPressedThisFrame; case 's': return k.sKey.wasPressedThisFrame; case 't': return k.tKey.wasPressedThisFrame; case 'x': return k.xKey.wasPressedThisFrame; case 'z': return k.zKey.wasPressedThisFrame; default: return false; }
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
