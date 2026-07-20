using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class TinyParryHUD : MonoBehaviour
{
    [SerializeField] private CombatantHealth player;
    [SerializeField] private CombatantHealth boss;
    [SerializeField] private PlayerPotion potion;

    private BossAI bossAI;
    private string bossName = "BOSS";
    private string stageName = "";
    private int stageNumber = 1;
    private int totalStages = 5;
    private string messageText = "";
    private float messageHideAt;
    private string resultText = "";
    private string resultHint = "";
    private bool overlayVisible;
    private bool restRoomVisible;
    private PlayerProgression restRoomProgression;
    private Transform lockOnTarget;
    private bool pauseSkillsVisible;
    private bool pauseStatusVisible;
    private PlayerProgression pauseProgression;
    private int pauseSelectedSkill;
    private bool restShowingPassives;
    private bool prologueVisible;
    private bool titleVisible;
    private int prologuePage;

    private GUIStyle labelStyle;
    private GUIStyle centerStyle;
    private GUIStyle resultStyle;
    private Texture2D whiteTexture;

    public bool PauseSkillsVisible => pauseSkillsVisible;
    public bool PauseStatusVisible => pauseStatusVisible;
    public bool RestShowingPassives => restShowingPassives;

    public void Configure(CombatantHealth playerHealth)
    {
        player = playerHealth;
        potion = player != null ? player.GetComponent<PlayerPotion>() : null;
    }


    public void SetBoss(CombatantHealth bossHealth, string displayName)
    {
        boss = bossHealth;
        bossAI = boss != null ? boss.GetComponent<BossAI>() : null;
        bossName = string.IsNullOrWhiteSpace(displayName) ? "BOSS" : displayName;
    }

    public void SetStage(int number, int total, string displayName)
    {
        stageNumber = number;
        totalStages = total;
        stageName = displayName;
        resultText = "";
    }

    // Backward-compatible API for older stage-manager scripts that may still exist
    // in a project folder after a previous patch was applied.
    public void SetCurrentOpponent(CombatantHealth target, bool isBoss, int stage)
    {
        boss = target;
        bossAI = boss != null ? boss.GetComponent<BossAI>() : null;
        stageNumber = Mathf.Max(1, stage);
        bossName = target != null ? target.gameObject.name : (isBoss ? "BOSS" : "ENEMY");
    }

    public void SetStageInfo(int stage, string title)
    {
        stageNumber = Mathf.Max(1, stage);
        totalStages = Mathf.Max(totalStages, stageNumber);
        stageName = title ?? string.Empty;
        resultText = string.Empty;
    }

    public void SetMessage(string value)
    {
        messageText = value ?? string.Empty;
        messageHideAt = string.IsNullOrWhiteSpace(messageText)
            ? 0f
            : Time.unscaledTime + 2f;
    }
    public void ShowResult(string value)
    {
        restRoomVisible = false;
        restRoomProgression = null;
        resultText = value ?? string.Empty;
        resultHint = string.Empty;
        overlayVisible = !string.IsNullOrWhiteSpace(resultText);
    }

    public void ShowTitle()
    {
        restRoomVisible = false;
        restRoomProgression = null;
        prologueVisible = false;
        titleVisible = true;
        resultText = "THE KING\'S TOWER";
        resultHint = "ENTER  NEW GAME\nTAB  CONTROLS\nESC  QUIT";
        overlayVisible = true;
    }


    public void ShowProloguePage(int page)
    {
        prologuePage = Mathf.Clamp(page, 0, 5);
        prologueVisible = true;
        titleVisible = false;
        restRoomVisible = false;
        pauseSkillsVisible = false;
        pauseStatusVisible = false;
        resultText = "PROLOGUE";
        resultHint = string.Empty;
        overlayVisible = true;
    }

    public void ShowPause()
    {
        ShowPause(pauseProgression);
    }

    public void ShowPause(PlayerProgression progression)
    {
        titleVisible = false;
        restRoomVisible = false;
        restRoomProgression = null;
        pauseProgression = progression;
        pauseSkillsVisible = false;
        pauseStatusVisible = false;
        pauseSelectedSkill = progression != null
            ? Mathf.Clamp(progression.EquippedActiveSkill, 0, PlayerProgression.TotalSkillCount - 1)
            : 0;
        resultText = "PAUSED";
        resultHint = "ESC / ENTER  RESUME\nC  CHARACTER STATUS\nS  SKILLS\nR  RESTART\nT  RETURN TO TITLE";
        overlayVisible = true;
    }

    public void OpenPauseSkills()
    {
        if (pauseProgression == null)
        {
            SetMessage("Skill information becomes available after choosing a weapon.");
            return;
        }

        pauseStatusVisible = false;
        pauseSkillsVisible = true;
        pauseSelectedSkill = Mathf.Clamp(pauseSelectedSkill, 0, PlayerProgression.TotalSkillCount - 1);
        resultText = "SKILLS";
        overlayVisible = true;
    }

    public void ClosePauseSkills()
    {
        pauseSkillsVisible = false;
        resultText = "PAUSED";
        resultHint = "ESC / ENTER  RESUME\nC  CHARACTER STATUS\nS  SKILLS\nR  RESTART\nT  RETURN TO TITLE";
        overlayVisible = true;
    }

    public void OpenPauseStatus()
    {
        pauseSkillsVisible = false;
        pauseStatusVisible = true;
        resultText = "CHARACTER STATUS";
        overlayVisible = true;
    }

    public void ClosePauseStatus()
    {
        pauseStatusVisible = false;
        resultText = "PAUSED";
        resultHint = "ESC / ENTER  RESUME\nC  CHARACTER STATUS\nS  SKILLS\nR  RESTART\nT  RETURN TO TITLE";
        overlayVisible = true;
    }

    public void MovePauseSkillSelection(int direction)
    {
        int count = PlayerProgression.TotalSkillCount;
        if (count <= 0) return;
        int step = direction < 0 ? -1 : 1;
        pauseSelectedSkill = (pauseSelectedSkill + step + count) % count;
    }

    public void EquipSelectedPauseSkill()
    {
        if (pauseProgression == null) return;
        if (!pauseProgression.IsActiveSkill(pauseSelectedSkill))
        {
            SetMessage("Passive skills are always active and cannot be equipped.");
            return;
        }

        if (!pauseProgression.IsSkillLearned(pauseSelectedSkill))
        {
            SetMessage("This active skill has not been learned yet.");
            return;
        }

        if (pauseProgression.EquipActiveSkill(pauseSelectedSkill))
            SetMessage($"Equipped: {pauseProgression.GetSkillName(pauseSelectedSkill)}");
    }

    public void ToggleRestSkillCategory()
    {
        restShowingPassives = !restShowingPassives;
    }

    public void ShowDefeat(int round, int deaths)
    {
        titleVisible = false;
        restRoomVisible = false;
        restRoomProgression = null;
        resultText = "DEFEAT";
        resultHint = $"REACHED ROUND {round} / {totalStages}     DEATHS {deaths}\nENTER  RESTART\nESC  RETURN TO TITLE";
        overlayVisible = true;
    }

    public void ShowVictory(float clearTime, int wins)
    {
        restRoomVisible = false;
        restRoomProgression = null;
        resultText = "VICTORY";
        resultHint = $"CLEAR TIME  {clearTime:0.0}s     WINS {wins}\nENTER / ESC  RETURN TO TITLE";
        overlayVisible = true;
    }


    public void ShowStageDecision(int clearedStage, PlayerProgression progression)
    {
        restRoomVisible = false;
        restRoomProgression = null;
        resultText = $"STAGE {clearedStage} CLEARED";
        string rewards = clearedStage >= 4
            ? "STAT +1   SKILL +1\n"
            : "STAT +1\n";
        resultHint = rewards + "N / ENTER  CONTINUE\nR  REST IN CASTLE CHAMBER";
        overlayVisible = true;
    }

    public void ShowClassSelection()
    {
        titleVisible = false;
        restRoomVisible = false;
        restRoomProgression = null;
        resultText = "CHOOSE YOUR CLASS";
        resultHint = "1  GREATSWORD\n2  DUAL SWORDS\n3  SPEAR\n\nOnly skills for the selected weapon will be available.";
        overlayVisible = true;
    }

    public void ShowRestRoom(PlayerProgression progression)
    {
        titleVisible = false;
        restRoomProgression = progression;
        restRoomVisible = true;
        resultText = "CASTLE REST CHAMBER";
        resultHint = string.Empty;
        overlayVisible = true;
    }

    public void ClearOverlay()
    {
        restRoomVisible = false;
        restRoomProgression = null;
        pauseSkillsVisible = false;
        pauseProgression = null;
        resultText = string.Empty;
        resultHint = string.Empty;
        prologueVisible = false;
        titleVisible = false;
        overlayVisible = false;
    }
    public void ShowParryFeedback(bool success) { }
    public void SetLockOnTarget(Transform target) => lockOnTarget = target;



    private void DrawFearGauge(float width, float height)
    {
        const float gaugeWidth = 330f;
        const float gaugeHeight = 14f;
        const float yFromBottom = 72f;
        float left = width * 0.5f - gaugeWidth * 0.5f;

        if (boss != null && bossAI == null)
            bossAI = boss.GetComponent<BossAI>();

        Rect outer = new Rect(left, height - yFromBottom, gaugeWidth, gaugeHeight);
        DrawRect(outer, new Color(0.01f, 0.01f, 0.01f, 0.98f));

        float normalized = bossAI != null ? bossAI.FearNormalized : 0f;
        if (normalized <= 0f)
            return;

        Color fillColor;
        if (normalized >= 1f)
            fillColor = Color.white;
        else if (normalized >= 0.95f)
        {
            float blink = 0.55f + Mathf.PingPong(Time.unscaledTime * 7f, 0.45f);
            fillColor = new Color(1f, 0.02f, 0.02f, blink);
        }
        else if (normalized >= 0.75f)
            fillColor = new Color(0.90f, 0.08f, 0.08f, 1f);
        else if (normalized >= 0.50f)
            fillColor = new Color(1f, 0.48f, 0.10f, 1f);
        else if (normalized >= 0.25f)
            fillColor = new Color(0.95f, 0.78f, 0.18f, 1f);
        else
            fillColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        Rect fill = new Rect(outer.x + 2f, outer.y + 2f, (outer.width - 4f) * normalized, outer.height - 4f);
        DrawRect(fill, fillColor);
    }

    private void EnsureStyles()
    {
        if (whiteTexture == null)
        {
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }

        labelStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        labelStyle.normal.textColor = Color.white;

        centerStyle ??= new GUIStyle(labelStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            wordWrap = true
        };

        resultStyle ??= new GUIStyle(centerStyle)
        {
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
    }

    private void OnGUI()
    {
        EnsureStyles();
        float width = Screen.width;
        float height = Screen.height;

        // Bottom-left player HUD. Each line owns a separate vertical slot so labels never overlap.
        if (player != null)
        {
            const float hudLeft = 28f;
            const float hudWidth = 460f;
            const float potionYFromBottom = 42f;
            const float healthBarYFromBottom = 96f;
            const float activeSkillYFromBottom = 158f;

            Rect playerBar = new Rect(hudLeft, height - healthBarYFromBottom, 330f, 24f);
            DrawHealthBar(playerBar, "PLAYER", player.CurrentHealth, player.MaxHealth, new Color(0.18f, 0.82f, 0.32f));

            if (potion == null)
                potion = player.GetComponent<PlayerPotion>();

            if (potion != null)
                DrawPotionCount(new Rect(hudLeft, height - potionYFromBottom, 330f, 28f), potion.Remaining);

            PlayerProgression liveProgression = player.GetComponent<PlayerProgression>();
            if (liveProgression != null && liveProgression.HasChosenClass)
                DrawSkillIcons(liveProgression, width, height);
        }

        // Boss HUD stays centered and leaves the upper-right corner free.
        if (boss != null && boss.gameObject.activeInHierarchy && !boss.IsDead)
        {
            float bossBarWidth = Mathf.Min(520f, width * 0.46f);
            DrawBossHealthBar(new Rect(width * 0.5f - bossBarWidth * 0.5f, 30f, bossBarWidth, 27f),
                boss.CurrentHealth, boss.MaxHealth, new Color(0.78f, 0.18f, 0.12f));

        }

        // Working Fear gauge. Reads BossAI directly, so successful parries update it immediately.
        DrawFearGauge(width, height);

        // Dedicated stage panel in the upper-right, separated from the boss bar.
        Rect stagePanel = new Rect(width - 292f, 22f, 264f, 84f);
        DrawRect(stagePanel, new Color(0.03f, 0.03f, 0.03f, 0.78f));
        GUI.Label(new Rect(stagePanel.x + 14f, stagePanel.y + 10f, stagePanel.width - 28f, 28f),
            $"ROYAL CASTLE  ·  STAGE {stageNumber} / {totalStages}", labelStyle);
        GUI.Label(new Rect(stagePanel.x + 14f, stagePanel.y + 42f, stagePanel.width - 28f, 28f),
            stageName, labelStyle);

        if (!string.IsNullOrWhiteSpace(messageText))
        {
            if (messageHideAt > 0f && Time.unscaledTime >= messageHideAt)
            {
                messageText = string.Empty;
                messageHideAt = 0f;
            }
            else
            {
                GUI.Label(new Rect(width * 0.5f - 360f, 104f, 720f, 76f), messageText, centerStyle);
            }
        }

        if (overlayVisible && !string.IsNullOrWhiteSpace(resultText))
        {
            if (titleVisible || prologueVisible)
                DrawCastleBackdrop(width, height);

            DrawRect(new Rect(0f, 0f, width, height),
                titleVisible || prologueVisible
                    ? new Color(0f, 0f, 0f, 0.34f)
                    : new Color(0f, 0f, 0f, 0.62f));

            if (prologueVisible)
            {
                DrawPrologueOverlay(width, height);
            }
            else if (restRoomVisible)
            {
                DrawRestRoomOverlay(width, height);
            }
            else if (pauseSkillsVisible)
            {
                DrawPauseSkillsOverlay(width, height);
            }
            else if (pauseStatusVisible)
            {
                DrawPauseStatusOverlay(width, height);
            }
            else
            {
                GUI.Label(new Rect(width * 0.5f - 360f, height * 0.5f - 105f, 720f, 95f), resultText, resultStyle);

                GUIStyle hintStyle = new GUIStyle(centerStyle)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold
                };
                GUI.Label(new Rect(width * 0.5f - 330f, height * 0.5f + 5f, 660f, 170f), resultHint, hintStyle);
            }
        }

        if (lockOnTarget != null && lockOnTarget.gameObject.activeInHierarchy)
            DrawLockOnMarker(lockOnTarget);

        // Controls are visible only while TAB is held.
        if (IsControlsHeld())
        {
            Rect controlsPanel = new Rect(22f, 20f, 390f, 240f);
            DrawRect(controlsPanel, new Color(0.03f, 0.03f, 0.03f, 0.82f));
            GUI.Label(new Rect(controlsPanel.x + 14f, controlsPanel.y + 10f, controlsPanel.width - 28f, controlsPanel.height - 20f),
                "[TAB] 조작법\nWASD 이동 | Shift 달리기\n좌클릭 공격 | 우클릭 패링\nSpace 대시 | Q 락온\nR 액티브 스킬 | E 물약\n\n[TEST] F1 무적 | F2 회복 | F3 물약+10\nF4 라운드 클리어 | F5 다음 라운드", labelStyle);
        }
    }


    private void DrawCastleBackdrop(float width, float height)
    {
        // Procedural castle silhouette: no external texture dependency.
        DrawRect(new Rect(0f, 0f, width, height), new Color(0.018f, 0.026f, 0.050f, 1f));

        // Moon and distant haze.
        float moonSize = Mathf.Min(width, height) * 0.17f;
        DrawRect(new Rect(width * 0.72f, height * 0.10f, moonSize, moonSize), new Color(0.62f, 0.66f, 0.70f, 0.20f));
        DrawRect(new Rect(0f, height * 0.58f, width, height * 0.42f), new Color(0.025f, 0.028f, 0.035f, 1f));

        Color farStone = new Color(0.055f, 0.060f, 0.072f, 1f);
        Color stone = new Color(0.075f, 0.072f, 0.072f, 1f);
        Color darkStone = new Color(0.035f, 0.034f, 0.038f, 1f);
        Color window = new Color(0.82f, 0.48f, 0.12f, 0.76f);

        // Main keep and side towers.
        Rect keep = new Rect(width * 0.27f, height * 0.31f, width * 0.46f, height * 0.69f);
        Rect leftTower = new Rect(width * 0.12f, height * 0.43f, width * 0.18f, height * 0.57f);
        Rect rightTower = new Rect(width * 0.70f, height * 0.40f, width * 0.18f, height * 0.60f);
        DrawRect(keep, stone);
        DrawRect(leftTower, farStone);
        DrawRect(rightTower, farStone);

        // Battlements.
        DrawBattlements(keep, 9, stone);
        DrawBattlements(leftTower, 4, farStone);
        DrawBattlements(rightTower, 4, farStone);

        // Central tower and pointed roof.
        Rect central = new Rect(width * 0.42f, height * 0.17f, width * 0.16f, height * 0.32f);
        DrawRect(central, darkStone);
        DrawBattlements(central, 4, darkStone);

        // Gate opening and windows.
        DrawRect(new Rect(width * 0.455f, height * 0.72f, width * 0.09f, height * 0.28f), new Color(0.008f, 0.008f, 0.010f, 1f));
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                float x = width * (0.32f + col * 0.09f);
                float y = height * (0.47f + row * 0.14f);
                DrawRect(new Rect(x, y, width * 0.018f, height * 0.055f), window);
            }
        }

        // Ground fog layers.
        DrawRect(new Rect(0f, height * 0.84f, width, height * 0.16f), new Color(0.16f, 0.17f, 0.19f, 0.18f));
        DrawRect(new Rect(0f, height * 0.91f, width, height * 0.09f), new Color(0.24f, 0.25f, 0.27f, 0.12f));
    }

    private void DrawBattlements(Rect tower, int count, Color color)
    {
        float blockWidth = tower.width / Mathf.Max(1, count * 2 - 1);
        float blockHeight = Mathf.Max(10f, tower.height * 0.055f);
        for (int i = 0; i < count; i++)
            DrawRect(new Rect(tower.x + i * blockWidth * 2f, tower.y - blockHeight, blockWidth, blockHeight), color);
    }

    private void DrawPrologueOverlay(float width, float height)
    {
        string[] chapterTitles =
        {
            "A KINGDOM AT PEACE",
            "THE EMPTY THRONE",
            "THE SEALED NOTE",
            "A DOUBTFUL CLUE",
            "THE GENERAL'S OATH",
            "THE NORTHERN CASTLE"
        };

        string[] chapters =
        {
            "There was once a kingdom that had known peace for many years.\nYou served that kingdom as its most trusted general.",
            "Then, on an ordinary and quiet morning, the king vanished.\nThere was no blood, no struggle, and no witness.",
            "Only a sealed note remained where the king had stood.\nInside was a single location.",
            "No reason was written. No plea for rescue.\nThe location itself might have been a lie.",
            "You took up your sword and left alone.\nNot for glory, but to bring your king home.",
            "Beyond the northern road stood an abandoned royal castle.\nIts gates were open, as though someone had been waiting."
        };

        DrawRect(new Rect(0f, 0f, width, height), new Color(0.005f, 0.005f, 0.008f, 0.97f));
        float panelWidth = Mathf.Min(860f, width - 60f);
        Rect panel = new Rect((width - panelWidth) * 0.5f, height * 0.22f, panelWidth, height * 0.56f);
        DrawRect(panel, new Color(0.035f, 0.03f, 0.025f, 0.96f));
        DrawRect(new Rect(panel.x, panel.y, panel.width, 3f), new Color(0.62f, 0.49f, 0.22f, 1f));
        DrawRect(new Rect(panel.x, panel.yMax - 3f, panel.width, 3f), new Color(0.62f, 0.49f, 0.22f, 1f));

        GUIStyle title = new GUIStyle(resultStyle) { fontSize = 31, alignment = TextAnchor.MiddleCenter };
        GUIStyle body = new GUIStyle(centerStyle) { fontSize = 22, fontStyle = FontStyle.Normal, wordWrap = true };
        GUIStyle hint = new GUIStyle(centerStyle) { fontSize = 15, fontStyle = FontStyle.Normal };
        GUI.Label(new Rect(panel.x + 40f, panel.y + 38f, panel.width - 80f, 58f), chapterTitles[prologuePage], title);
        GUI.Label(new Rect(panel.x + 72f, panel.y + 120f, panel.width - 144f, panel.height - 210f), chapters[prologuePage], body);
        GUI.Label(new Rect(panel.x + 40f, panel.yMax - 72f, panel.width - 80f, 36f),
            $"ENTER  CONTINUE     ESC  SKIP     {prologuePage + 1} / 6", hint);
    }

    private void DrawRestRoomOverlay(float width, float height)
    {
        float scale = Mathf.Clamp(Mathf.Min(width / 1280f, height / 720f), 0.72f, 1.35f);
        float panelWidth = Mathf.Min(width - 40f, 920f * scale);
        float panelHeight = Mathf.Min(height - 36f, 590f * scale);
        Rect panel = new Rect((width - panelWidth) * 0.5f, (height - panelHeight) * 0.5f, panelWidth, panelHeight);

        DrawRect(panel, new Color(0.025f, 0.025f, 0.03f, 0.93f));

        float margin = 26f * scale;
        float titleHeight = 68f * scale;
        GUIStyle titleStyle = new GUIStyle(resultStyle)
        {
            fontSize = Mathf.RoundToInt(34f * scale),
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(panel.x + margin, panel.y + 8f * scale, panel.width - margin * 2f, titleHeight),
            "CASTLE REST CHAMBER", titleStyle);

        float contentTop = panel.y + titleHeight + 18f * scale;
        float gap = 18f * scale;
        float columnWidth = (panel.width - margin * 2f - gap) * 0.5f;
        float contentHeight = panel.height - titleHeight - 112f * scale;
        Rect statPanel = new Rect(panel.x + margin, contentTop, columnWidth, contentHeight);
        Rect skillPanel = new Rect(statPanel.xMax + gap, contentTop, columnWidth, contentHeight);

        DrawRect(statPanel, new Color(0.10f, 0.11f, 0.13f, 0.96f));
        DrawRect(skillPanel, new Color(0.10f, 0.11f, 0.13f, 0.96f));

        GUIStyle headerStyle = new GUIStyle(labelStyle)
        {
            fontSize = Mathf.RoundToInt(22f * scale),
            alignment = TextAnchor.UpperLeft
        };
        GUIStyle bodyStyle = new GUIStyle(labelStyle)
        {
            fontSize = Mathf.RoundToInt(17f * scale),
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            clipping = TextClipping.Overflow
        };
        GUIStyle pointStyle = new GUIStyle(bodyStyle)
        {
            fontStyle = FontStyle.Bold
        };

        float inner = 20f * scale;
        GUI.Label(new Rect(statPanel.x + inner, statPanel.y + inner, statPanel.width - inner * 2f, 34f * scale), "STATUS", headerStyle);
        GUI.Label(new Rect(skillPanel.x + inner, skillPanel.y + inner, skillPanel.width - inner * 2f, 34f * scale), "SKILLS", headerStyle);

        if (restRoomProgression == null)
        {
            GUI.Label(new Rect(statPanel.x + inner, statPanel.y + 64f * scale, statPanel.width - inner * 2f, 120f * scale),
                "HP and potions restored.", bodyStyle);
        }
        else
        {
            string statusText =
                $"CLASS  {restRoomProgression.GetClassName()}\n\n" +
                $"[H]  HP                  {restRoomProgression.Vitality}\n" +
                $"[A]  ATTACK              {restRoomProgression.Power}\n" +
                $"[S]  ATTACK SPEED        {restRoomProgression.Agility}";
            GUI.Label(new Rect(statPanel.x + inner, statPanel.y + 62f * scale, statPanel.width - inner * 2f, 190f * scale), statusText, bodyStyle);
            GUI.Label(new Rect(statPanel.x + inner, statPanel.yMax - 66f * scale, statPanel.width - inner * 2f, 40f * scale),
                $"STAT POINTS  {restRoomProgression.StatPoints}", pointStyle);

            float columnGap = 18f * scale;
            float skillColumnWidth = (skillPanel.width - inner * 2f - columnGap) * 0.5f;
            float activeX = skillPanel.x + inner;
            float passiveX = activeX + skillColumnWidth + columnGap;
            float skillsY = skillPanel.y + 62f * scale;

            string activeText = "ACTIVE  [1-5]\n\n";
            string passiveText = "PASSIVE  [6-0]\n\n";
            for (int i = 0; i < PlayerProgression.ActiveSkillCount; i++)
            {
                activeText += $"[{i + 1}] {restRoomProgression.GetSkillName(i)}  Lv.{restRoomProgression.GetSkillRank(i)}\n\n";
                int passiveSlot = PlayerProgression.ActiveSkillCount + i;
                string passiveKey = i == 4 ? "0" : (i + 6).ToString();
                passiveText += $"[{passiveKey}] {restRoomProgression.GetSkillName(passiveSlot)}  Lv.{restRoomProgression.GetSkillRank(passiveSlot)}\n\n";
            }

            GUI.Label(new Rect(activeX, skillsY, skillColumnWidth, skillPanel.height - 136f * scale), activeText, bodyStyle);
            GUI.Label(new Rect(passiveX, skillsY, skillColumnWidth, skillPanel.height - 136f * scale), passiveText, bodyStyle);
            GUI.Label(new Rect(skillPanel.x + inner, skillPanel.yMax - 60f * scale, skillPanel.width - inner * 2f, 30f * scale),
                $"SKILL POINTS  {restRoomProgression.SkillPoints}", pointStyle);
        }

        GUIStyle footerStyle = new GUIStyle(centerStyle)
        {
            fontSize = Mathf.RoundToInt(17f * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        string footer = restRoomProgression != null && restRoomProgression.HasChosenClass
            ? "H/A/S STAT   1-5 ACTIVE   6-0 PASSIVE   ENTER CONTINUE"
            : "H / A / S  SPEND STAT     ENTER  CONTINUE";
        GUI.Label(new Rect(panel.x + margin, panel.yMax - 58f * scale, panel.width - margin * 2f, 38f * scale),
            footer, footerStyle);
    }

    private static bool IsControlsHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.tabKey.isPressed;
#else
        return Input.GetKey(KeyCode.Tab);
#endif
    }

    private void DrawLockOnMarker(Transform target)
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        Vector3 screen = camera.WorldToScreenPoint(target.position + Vector3.up * 1.3f);
        if (screen.z <= 0f) return;
        GUI.Label(new Rect(screen.x - 30f, Screen.height - screen.y - 30f, 60f, 60f), "◎", centerStyle);
    }


    private void DrawSkillIcons(PlayerProgression progression, float width, float height)
    {
        PlayerCombat combat = player != null ? player.GetComponent<PlayerCombat>() : null;
        const float size = 82f;
        Rect icon = new Rect(width - size - 30f, height - size - 30f, size, size);
        DrawRect(icon, new Color(0.10f, 0.12f, 0.16f, 0.94f));
        DrawOutline(icon, new Color(0.95f, 0.82f, 0.25f, 1f), 3f);

        GUIStyle tagStyle = new GUIStyle(labelStyle) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
        GUIStyle iconStyle = new GUIStyle(centerStyle) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        GUIStyle cooldownStyle = new GUIStyle(centerStyle) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(icon.x + 6f, icon.y + 4f, 24f, 18f), "A", tagStyle);
        GUI.Label(new Rect(icon.xMax - 27f, icon.y + 4f, 20f, 18f), "R", tagStyle);
        GUI.Label(new Rect(icon.x + 7f, icon.y + 21f, icon.width - 14f, 45f), ShortSkillName(progression.GetEquippedActiveSkillName()), iconStyle);

        if (combat != null && !combat.IsActiveSkillReady)
        {
            DrawRect(icon, new Color(0f, 0f, 0f, 0.66f));
            GUI.Label(icon, combat.ActiveSkillCooldownRemaining.ToString("0.0"), cooldownStyle);
        }
    }

    private void DrawPauseStatusOverlay(float width, float height)
    {
        float panelWidth = Mathf.Min(width - 50f, 760f);
        float panelHeight = Mathf.Min(height - 44f, 560f);
        Rect panel = new Rect((width - panelWidth) * 0.5f, (height - panelHeight) * 0.5f, panelWidth, panelHeight);
        DrawRect(panel, new Color(0.025f, 0.025f, 0.03f, 0.96f));

        GUIStyle header = new GUIStyle(resultStyle) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
        GUIStyle section = new GUIStyle(labelStyle) { fontSize = 21, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
        GUIStyle body = new GUIStyle(labelStyle) { fontSize = 18, fontStyle = FontStyle.Normal, alignment = TextAnchor.UpperLeft, wordWrap = true };
        GUIStyle value = new GUIStyle(body) { alignment = TextAnchor.UpperRight, fontStyle = FontStyle.Bold };

        GUI.Label(new Rect(panel.x + 28f, panel.y + 18f, panel.width - 56f, 48f), "CHARACTER STATUS", header);

        if (pauseProgression == null)
        {
            GUI.Label(new Rect(panel.x + 40f, panel.y + 100f, panel.width - 80f, 100f), "Character data is unavailable.", body);
        }
        else
        {
            float left = panel.x + 54f;
            float right = panel.xMax - 54f;
            float y = panel.y + 92f;
            GUI.Label(new Rect(left, y, right - left, 30f), $"CLASS   {pauseProgression.GetClassName()}", section);
            y += 58f;

            string[] labels = { "HP", "VITALITY", "ATTACK", "POWER", "ATTACK SPEED", "AGILITY", "EQUIPPED ACTIVE", "STAT POINTS", "SKILL POINTS" };
            string[] values =
            {
                $"{pauseProgression.CurrentHealth} / {pauseProgression.MaxHealth}",
                pauseProgression.Vitality.ToString(),
                $"{pauseProgression.AttackPowerPercent}%",
                pauseProgression.Power.ToString(),
                $"{pauseProgression.AttackSpeedPercent}%",
                pauseProgression.Agility.ToString(),
                pauseProgression.HasChosenClass ? pauseProgression.GetEquippedActiveSkillName() : "NONE",
                pauseProgression.StatPoints.ToString(),
                pauseProgression.SkillPoints.ToString()
            };

            for (int i = 0; i < labels.Length; i++)
            {
                if (i == 1 || i == 3 || i == 5)
                {
                    GUIStyle subLabel = new GUIStyle(body) { fontSize = 15 };
                    subLabel.normal.textColor = new Color(0.72f, 0.72f, 0.76f, 1f);
                    GUIStyle subValue = new GUIStyle(subLabel) { alignment = TextAnchor.UpperRight };
                    GUI.Label(new Rect(left + 18f, y, 260f, 28f), labels[i], subLabel);
                    GUI.Label(new Rect(right - 280f, y, 280f, 28f), values[i], subValue);
                    y += 32f;
                }
                else
                {
                    GUI.Label(new Rect(left, y, 300f, 30f), labels[i], body);
                    GUI.Label(new Rect(right - 320f, y, 320f, 30f), values[i], value);
                    y += 40f;
                }
            }
        }

        GUIStyle footer = new GUIStyle(centerStyle) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(panel.x + 28f, panel.yMax - 48f, panel.width - 56f, 30f), "ESC  BACK", footer);
    }

    private void DrawPauseSkillsOverlay(float width, float height)
    {
        if (pauseProgression == null) return;
        float panelWidth = Mathf.Min(width - 50f, 1040f);
        float panelHeight = Mathf.Min(height - 44f, 650f);
        Rect panel = new Rect((width - panelWidth) * 0.5f, (height - panelHeight) * 0.5f, panelWidth, panelHeight);
        DrawRect(panel, new Color(0.025f, 0.025f, 0.03f, 0.96f));

        GUIStyle header = new GUIStyle(resultStyle) { fontSize = 32, alignment = TextAnchor.MiddleLeft };
        GUIStyle body = new GUIStyle(labelStyle) { fontSize = 16, fontStyle = FontStyle.Normal, wordWrap = true, alignment = TextAnchor.UpperLeft };
        GUIStyle small = new GUIStyle(body) { fontSize = 14 };
        GUI.Label(new Rect(panel.x + 28f, panel.y + 18f, panel.width - 56f, 48f), pauseProgression.GetClassName() + " SKILLS", header);

        float listX = panel.x + 28f;
        float listY = panel.y + 82f;
        float listW = panel.width * 0.45f;
        float rowH = 46f;
        for (int i = 0; i < PlayerProgression.TotalSkillCount; i++)
        {
            Rect row = new Rect(listX, listY + i * rowH, listW, rowH - 4f);
            bool selected = i == pauseSelectedSkill;
            if (selected) DrawRect(row, new Color(0.20f, 0.20f, 0.24f, 0.95f));
            string state = pauseProgression.IsSkillLearned(i) ? $"Lv.{pauseProgression.GetSkillRank(i)}" : "LOCKED";
            string equipped = pauseProgression.IsActiveSkill(i) && pauseProgression.EquippedActiveSkill == i ? "  [EQUIPPED]" : string.Empty;
            GUI.Label(new Rect(row.x + 10f, row.y + 4f, row.width - 20f, row.height - 8f),
                $"[{pauseProgression.GetSkillTypeLabel(i)}] {pauseProgression.GetSkillName(i)}   {state}{equipped}", body);
        }

        int slot = Mathf.Clamp(pauseSelectedSkill, 0, PlayerProgression.TotalSkillCount - 1);
        WeaponSkillInfo info = pauseProgression.GetSkillInfo(slot);
        Rect detail = new Rect(panel.x + panel.width * 0.50f, panel.y + 82f, panel.width * 0.46f, panel.height - 150f);
        DrawRect(detail, new Color(0.09f, 0.10f, 0.12f, 0.96f));
        GUI.Label(new Rect(detail.x + 22f, detail.y + 20f, detail.width - 44f, 38f), $"[{pauseProgression.GetSkillTypeLabel(slot)}] {info.Name}", new GUIStyle(header) { fontSize = 24 });
        string stats = info.Kind == SkillKind.Active
            ? $"Cooldown: {info.Cooldown:0.0}s\nBase damage: {info.DamagePercent:0}% of a standard attack benchmark\n"
            : "Persistent effect: remains active until the current run ends.\n";
        string availability = pauseProgression.IsSkillLearned(slot)
            ? $"Current level: {pauseProgression.GetSkillRank(slot)} / {PlayerProgression.MaxSkillRank}"
            : "Not learned yet. Its full description remains visible for planning.";
        GUI.Label(new Rect(detail.x + 22f, detail.y + 76f, detail.width - 44f, detail.height - 130f), info.Description + "\n\n" + stats + "\n" + availability, body);

        string footer = info.Kind == SkillKind.Active && pauseProgression.IsSkillLearned(slot)
            ? "UP / DOWN  SELECT     ENTER  EQUIP     ESC  BACK"
            : "UP / DOWN  SELECT     ESC  BACK";
        GUI.Label(new Rect(panel.x + 28f, panel.yMax - 48f, panel.width - 56f, 30f), footer, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
    }

    private static string ShortSkillName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "SKILL";
        string[] parts = fullName.Split(' ');
        if (parts.Length == 1) return parts[0].Length <= 8 ? parts[0].ToUpperInvariant() : parts[0].Substring(0, 8).ToUpperInvariant();
        string first = parts[0].Length <= 6 ? parts[0] : parts[0].Substring(0, 6);
        string second = parts[1].Length <= 6 ? parts[1] : parts[1].Substring(0, 6);
        return (first + "\n" + second).ToUpperInvariant();
    }

    private void DrawOutline(Rect rect, Color color, float thickness)
    {
        DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void DrawPotionCount(Rect rect, int remaining)
    {
        GUI.Label(rect, $"POTION [E]   x {remaining}", labelStyle);
    }

    private void DrawBossHealthBar(Rect rect, int current, int maximum, Color fill)
    {
        float ratio = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f;
        DrawRect(rect, new Color(0.04f, 0.04f, 0.04f, 0.92f));
        DrawRect(new Rect(rect.x + 3f, rect.y + 3f, (rect.width - 6f) * ratio, rect.height - 6f), fill);

        GUIStyle hpStyle = new GUIStyle(labelStyle)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 17
        };
        GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width - 8f, rect.height), $"{current}/{maximum}", hpStyle);
    }

    private void DrawHealthBar(Rect rect, string title, int current, int maximum, Color fill)
    {
        float ratio = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f;
        GUI.Label(new Rect(rect.x, rect.y - 24f, rect.width, 22f), $"{title}   {current}/{maximum}", labelStyle);
        DrawRect(rect, new Color(0.04f, 0.04f, 0.04f, 0.92f));
        DrawRect(new Rect(rect.x + 3f, rect.y + 3f, (rect.width - 6f) * ratio, rect.height - 6f), fill);
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = old;
    }
}
