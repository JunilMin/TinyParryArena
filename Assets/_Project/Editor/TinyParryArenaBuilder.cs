using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TinyParryArenaBuilder
{
    private enum MonsterKind { Bear, Golem, Dragon }
    private const string ScenePath = "Assets/_Project/Scenes/Arena.unity";
    private const string KayRoot = "Assets/ThirdParty/KayKit";
    private const string KenneyArenaRoot = "Assets/ThirdParty/KenneyMiniArena/Models/FBX format";
    private const string MaterialRoot = "Assets/_Project/Art/Materials";
    private const string FreeGolemPath = "Assets/ThirdParty/Golem_Free/FBX/Characters/Rock_Golem.fbx";
    private const float CourtyardFloorSize = 96f;
    private const float CourtyardWallOffset = 44f;
    private const float CourtyardWallLength = 88f;
    private const float HallWidth = 48f;
    private const float HallLength = 64f;

    [MenuItem("Tiny Parry Arena/Build Game")]
    public static void BuildGame()
    {
        EnsureFolder("Assets/_Project/Scenes");
        EnsureFolder(MaterialRoot);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject systems = new GameObject("--- SYSTEMS ---");
        GameObject stagesRoot = new GameObject("--- STAGES ---");
        GameObject charactersRoot = new GameObject("--- CHARACTERS ---");
        GameObject uiRoot = new GameObject("--- UI ---");

        GameObject player = CreatePlayer(charactersRoot.transform);
        TinyParryHUD hud = CreateHUD(uiRoot.transform, player.GetComponent<CombatantHealth>());
        player.GetComponent<PlayerCombat>().ConfigureHUD(hud);
        player.GetComponent<PlayerLockOn>().Configure(hud);

        string[] stageNames =
        {
            "Castle Gate",
            "Training Yard",
            "Barracks",
            "Courtyard",
            "Chapel",
            "Royal Library",
            "Dungeon",
            "Moonlit Garden",
            "Royal Hall",
            "Tower Entrance"
        };

        GameObject[] stageRoots = new GameObject[10];
        CombatantHealth[] bosses = new CombatantHealth[10];

        stageRoots[0] = CreateCastleCourtyard(stagesRoot.transform);
        stageRoots[0].name = "Stage 1 - Castle Gate";
        bosses[0] = CreateBoss(
            stageRoots[0].transform, player.transform, "Arena Champion",
            KayRoot + "/Characters/fbx/Rogue.fbx",
            KayRoot + "/Weapons/sword_1handed.fbx",
            KayRoot + "/Weapons/shield_badge.fbx",
            120, 1.22f, 2.85f, 1, new Vector3(0f, 0.05f, 14.0f));

        stageRoots[1] = CreateCastleCourtyard(stagesRoot.transform);
        bosses[1] = CreateBoss(
            stageRoots[1].transform, player.transform, "Castle Warden",
            KayRoot + "/Characters/fbx/Knight.fbx",
            KayRoot + "/Weapons/sword_1handed.fbx",
            KayRoot + "/Weapons/shield_badge.fbx",
            175, 1.36f, 3.05f, 2, new Vector3(0f, 0.05f, 14.0f));

        stageRoots[2] = CreateThroneHall(stagesRoot.transform);
        bosses[2] = CreateBoss(
            stageRoots[2].transform, player.transform, "The Fallen King",
            KayRoot + "/Characters/fbx/Barbarian.fbx",
            KayRoot + "/Weapons/sword_2handed.fbx",
            KayRoot + "/Weapons/shield_round_barbarian.fbx",
            260, 1.52f, 3.25f, 3, new Vector3(0f, 0.05f, 16.0f));

        stageRoots[3] = CreateCastleCourtyard(stagesRoot.transform);
        stageRoots[3].name = "Stage 4 - Twinblade Pit";
        bosses[3] = CreateBoss(
            stageRoots[3].transform, player.transform, "Twinblade Duelist",
            KayRoot + "/Characters/fbx/Rogue_Hooded.fbx",
            KayRoot + "/Weapons/sword_1handed.fbx",
            KayRoot + "/Weapons/sword_1handed.fbx",
            230, 1.30f, 2.95f, 4, new Vector3(0f, 0.05f, 14.5f));

        stageRoots[4] = CreateCastleCourtyard(stagesRoot.transform);
        stageRoots[4].name = "Stage 5 - Sentinel Courtyard";
        bosses[4] = CreateBoss(
            stageRoots[4].transform, player.transform, "Spear Sentinel",
            KayRoot + "/Characters/fbx/Ranger.fbx",
            KayRoot + "/Weapons/sword_2handed.fbx",
            "",
            270, 1.42f, 3.85f, 5, new Vector3(0f, 0.05f, 15.0f));

        stageRoots[5] = CreateThroneHall(stagesRoot.transform);
        stageRoots[5].name = "Stage 6 - Execution Ground";
        bosses[5] = CreateBoss(
            stageRoots[5].transform, player.transform, "Greatsword Executioner",
            KayRoot + "/Characters/fbx/Barbarian.fbx",
            KayRoot + "/Weapons/sword_2handed.fbx",
            "",
            325, 1.62f, 3.55f, 6, new Vector3(0f, 0.05f, 16.0f));

        stageRoots[6] = CreateThroneHall(stagesRoot.transform);
        stageRoots[6].name = "Stage 7 - Royal Command Hall";
        bosses[6] = CreateBoss(
            stageRoots[6].transform, player.transform, "Royal Commander",
            KayRoot + "/Characters/fbx/Knight.fbx",
            KayRoot + "/Weapons/sword_1handed.fbx",
            KayRoot + "/Weapons/shield_badge.fbx",
            390, 1.55f, 3.45f, 7, new Vector3(0f, 0.05f, 16.0f));

        stageRoots[7] = CreateCastleCourtyard(stagesRoot.transform);
        stageRoots[7].name = "Stage 8 - Moonlit Garden";
        bosses[7] = CreateBoss(
            stageRoots[7].transform, player.transform, "Royal Assassin",
            KayRoot + "/Characters/fbx/Rogue_Hooded.fbx",
            KayRoot + "/Weapons/sword_1handed.fbx",
            KayRoot + "/Weapons/sword_1handed.fbx",
            470, 1.48f, 3.25f, 8, new Vector3(0f, 0.05f, 15.0f));

        stageRoots[8] = CreateThroneHall(stagesRoot.transform);
        stageRoots[8].name = "Stage 9 - Royal Hall";
        bosses[8] = CreateBoss(
            stageRoots[8].transform, player.transform, "King's Champion",
            KayRoot + "/Characters/fbx/Knight.fbx",
            KayRoot + "/Weapons/sword_2handed.fbx",
            "",
            560, 1.64f, 3.70f, 9, new Vector3(0f, 0.05f, 16.0f));

        stageRoots[9] = CreateThroneHall(stagesRoot.transform);
        stageRoots[9].name = "Stage 10 - Tower Entrance";
        bosses[9] = CreateBoss(
            stageRoots[9].transform, player.transform, "Tower Gatekeeper",
            KayRoot + "/Characters/fbx/Barbarian.fbx",
            KayRoot + "/Weapons/sword_2handed.fbx",
            KayRoot + "/Weapons/shield_round_barbarian.fbx",
            680, 1.78f, 4.05f, 10, new Vector3(0f, 0.05f, 16.5f));

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(systems.transform, false);
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 58f;
        camera.nearClipPlane = 0.1f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<ThirdPersonCameraController>().SetTarget(player.transform);

        GameObject managerObject = new GameObject("Stage Manager");
        managerObject.transform.SetParent(systems.transform, false);
        TinyParryStageManager manager = managerObject.AddComponent<TinyParryStageManager>();
        manager.Configure(
            stageRoots,
            bosses,
            stageNames,
            player.transform,
            hud,
            player.GetComponent<PlayerController>(),
            player.GetComponent<PlayerCombat>());

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog(
            "Tiny Parry Arena",
            "THE KING'S TOWER v0.6 created.\n\nAll ten bosses are human.\nStage 8: Royal Assassin\nStage 9: King's Champion\nStage 10: Tower Gatekeeper\n\nTitle and prologue pause combat. Castle-themed presentation and human final stages are enabled.",
            "OK");
    }

    private static GameObject CreatePlayer(Transform parent)
    {
        GameObject root = new GameObject("Player");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0.15f, -14f);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.height = 2f;
        cc.radius = 0.45f;

        root.AddComponent<PlayerController>();
        root.AddComponent<PlayerLockOn>();
        PlayerCombat combat = root.AddComponent<PlayerCombat>();
        root.AddComponent<PlayerPotion>();
        root.AddComponent<PlayerProgression>();
        CombatantHealth hp = root.GetComponent<CombatantHealth>();
        if (hp == null) hp = root.AddComponent<CombatantHealth>();
        hp.Configure(80);
        root.AddComponent<KnockbackReceiver>();
        root.AddComponent<CharacterVisualController>();

        GameObject visual = InstantiateCharacter(KayRoot + "/Characters/fbx/Knight.fbx", root.transform, "Knight Visual", 1f);
        Animator animator = visual.GetComponent<Animator>();
        Transform rightHand = GetBoneOrFallback(animator, HumanBodyBones.RightHand, visual.transform);
        Transform leftHand = GetBoneOrFallback(animator, HumanBodyBones.LeftHand, visual.transform);

        Transform rightRoot = new GameObject("Right Equipment Pivot").transform;
        rightRoot.SetParent(rightHand, false);
        Transform leftRoot = new GameObject("Left Equipment Pivot").transform;
        leftRoot.SetParent(leftHand, false);

        Transform sword = AttachEquipment(KayRoot + "/Weapons/sword_2handed.fbx", rightRoot, "Player Greatsword");
        Transform shield = AttachEquipment(KayRoot + "/Weapons/shield_badge.fbx", leftRoot, "Player Shield");
        Transform rightDagger = AttachEquipment(KayRoot + "/Weapons/sword_1handed.fbx", rightRoot, "Right Sword");
        Transform leftDagger = AttachEquipment(KayRoot + "/Weapons/sword_1handed.fbx", leftRoot, "Left Sword");
        Transform spear = AttachEquipment("Assets/ThirdParty/KenneyMiniArena/Models/FBX format/weapon-spear.fbx", rightRoot, "Player Spear");

        // Build three clearly different weapon silhouettes.
        sword.localScale = Vector3.one * 1.35f;
        sword.localPosition = new Vector3(0f, 0.02f, 0.08f);
        sword.localRotation = Quaternion.Euler(0f, 0f, 0f);
        rightDagger.localScale = Vector3.one * 1.02f;
        leftDagger.localScale = Vector3.one * 1.02f;
        rightDagger.localPosition = new Vector3(0f, 0.02f, 0.05f);
        leftDagger.localPosition = new Vector3(0f, 0.02f, 0.05f);
        rightDagger.localRotation = Quaternion.Euler(0f, 0f, 0f);
        leftDagger.localRotation = Quaternion.Euler(0f, 180f, 0f);

        // Kenney's spear is intentionally small. Enlarge it to a readable polearm length.
        spear.localScale = Vector3.one * 2.35f;
        spear.localPosition = new Vector3(0f, 0f, 0.22f);
        spear.localRotation = Quaternion.Euler(90f, 0f, 0f);
        sword.gameObject.SetActive(true);
        shield.gameObject.SetActive(false);
        rightDagger.gameObject.SetActive(false);
        leftDagger.gameObject.SetActive(false);
        spear.gameObject.SetActive(false);

        SerializedObject combatObject = new SerializedObject(combat);
        combatObject.FindProperty("weaponPivot").objectReferenceValue = rightRoot;
        combatObject.FindProperty("shieldPivot").objectReferenceValue = leftRoot;
        combatObject.FindProperty("shieldRenderer").objectReferenceValue = null;
        combatObject.FindProperty("swordModel").objectReferenceValue = sword.gameObject;
        combatObject.FindProperty("shieldModel").objectReferenceValue = shield.gameObject;
        combatObject.FindProperty("rightDaggerModel").objectReferenceValue = rightDagger.gameObject;
        combatObject.FindProperty("leftDaggerModel").objectReferenceValue = leftDagger.gameObject;
        combatObject.FindProperty("spearModel").objectReferenceValue = spear.gameObject;
        combatObject.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static CombatantHealth CreateBoss(
        Transform stageParent,
        Transform player,
        string bossName,
        string characterPath,
        string weaponPath,
        string shieldPath,
        int health,
        float scale,
        float attackRange,
        int stageLevel,
        Vector3 position)
    {
        GameObject root = new GameObject(bossName);
        root.transform.SetParent(stageParent, false);
        root.transform.localPosition = position;
        root.transform.localScale = Vector3.one * scale;

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.height = 2f;
        collider.radius = 0.52f;

        CombatantHealth hp = root.AddComponent<CombatantHealth>();
        hp.Configure(health);
        root.AddComponent<KnockbackReceiver>();
        root.AddComponent<CharacterVisualController>();

        GameObject visual = InstantiateCharacter(characterPath, root.transform, bossName + " Visual", 1f);
        Animator animator = visual.GetComponent<Animator>();
        Transform rightHand = GetBoneOrFallback(animator, HumanBodyBones.RightHand, visual.transform);
        Transform leftHand = GetBoneOrFallback(animator, HumanBodyBones.LeftHand, visual.transform);
        AttachEquipment(weaponPath, rightHand, bossName + " Weapon");
        AttachEquipment(shieldPath, leftHand, bossName + " Shield");

        BossAI ai = root.AddComponent<BossAI>();
        ai.SetPlayer(player);
        if (stageLevel == 1)
            ai.ConfigureDifficulty(1, 3, 1.65f, attackRange, 0.30f, 100, 40);
        else if (stageLevel == 2)
            ai.ConfigureDifficulty(2, 5, 1.82f, attackRange, 0.28f, 120, 35);
        else if (stageLevel == 3)
            ai.ConfigureDifficulty(3, 7, 2.0f, attackRange, 0.25f, 140, 30);
        else if (stageLevel == 4)
            ai.ConfigureDifficulty(4, 8, 2.16f, attackRange, 0.22f, 165, 27);
        else
            ai.ConfigureDifficulty(5, 8, 2.32f, attackRange, 0.20f, 190, 24);
        SerializedObject aiObject = new SerializedObject(ai);
        aiObject.FindProperty("bodyRenderer").objectReferenceValue = visual.GetComponentInChildren<Renderer>();
        aiObject.FindProperty("attackRange").floatValue = attackRange;
        aiObject.ApplyModifiedPropertiesWithoutUndo();

        return hp;
    }

    private static CombatantHealth CreateMonsterBoss(
        Transform stageParent,
        Transform player,
        string bossName,
        MonsterKind kind,
        int health,
        float scale,
        float attackRange,
        int stageLevel,
        Vector3 position)
    {
        GameObject root = new GameObject(bossName);
        root.transform.SetParent(stageParent, false);
        root.transform.localPosition = position;
        root.transform.localScale = Vector3.one * scale;

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = kind == MonsterKind.Dragon ? new Vector3(0f, 1.3f, 0f) : new Vector3(0f, 1.4f, 0f);
        collider.height = kind == MonsterKind.Golem ? 3.3f : 2.8f;
        collider.radius = kind == MonsterKind.Dragon ? 1.15f : 0.9f;

        CombatantHealth hp = root.AddComponent<CombatantHealth>();
        hp.Configure(health);
        root.AddComponent<KnockbackReceiver>();
        root.AddComponent<CharacterVisualController>();

        GameObject visual = CreateMonsterVisual(root.transform, kind, bossName + " Visual");
        BossAI ai = root.AddComponent<BossAI>();
        ai.SetPlayer(player);

        if (stageLevel == 3)
            ai.ConfigureDifficulty(3, 7, 2.05f, attackRange, 0.25f, 140, 30);
        else if (stageLevel == 4)
            ai.ConfigureDifficulty(4, 8, 1.72f, attackRange, 0.22f, 175, 27);
        else
            ai.ConfigureDifficulty(5, 8, 2.42f, attackRange, 0.20f, 210, 24);

        SerializedObject aiObject = new SerializedObject(ai);
        aiObject.FindProperty("bodyRenderer").objectReferenceValue = visual.GetComponentInChildren<Renderer>();
        aiObject.FindProperty("attackRange").floatValue = attackRange;
        aiObject.ApplyModifiedPropertiesWithoutUndo();
        return hp;
    }

    private static GameObject CreateMonsterVisual(Transform parent, MonsterKind kind, string name)
    {
        GameObject visual = new GameObject(name);
        visual.transform.SetParent(parent, false);

        Material primary;
        Material secondary;
        switch (kind)
        {
            case MonsterKind.Bear:
                primary = GetOrCreateMaterial("BearFur", new Color(0.20f, 0.10f, 0.045f));
                secondary = GetOrCreateMaterial("BearMuzzle", new Color(0.40f, 0.24f, 0.11f));
                BuildBear(visual.transform, primary, secondary);
                break;
            case MonsterKind.Golem:
                if (!TryInstantiateAndFitModel(FreeGolemPath, visual.transform, 3.7f))
                {
                    primary = GetOrCreateMaterial("GolemStone", new Color(0.24f, 0.27f, 0.29f));
                    secondary = GetOrCreateMaterial("GolemCore", new Color(0.12f, 0.75f, 1.0f));
                    BuildGolem(visual.transform, primary, secondary);
                }
                break;
            default:
                primary = GetOrCreateMaterial("DragonScale", new Color(0.025f, 0.03f, 0.04f));
                secondary = GetOrCreateMaterial("DragonEmber", new Color(0.80f, 0.08f, 0.025f));
                BuildDragon(visual.transform, primary, secondary);
                break;
        }

        foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(childCollider);
        return visual;
    }

    private static bool TryInstantiateAndFitModel(string assetPath, Transform parent, float targetHeight)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return false;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = Path.GetFileNameWithoutExtension(assetPath);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return true;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float height = Mathf.Max(0.001f, bounds.size.y);
        float scale = targetHeight / height;
        instance.transform.localScale = Vector3.one * scale;

        renderers = instance.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        instance.transform.position += Vector3.up * (parent.position.y - bounds.min.y);
        return true;
    }

    private static void BuildBear(Transform root, Material fur, Material muzzle)
    {
        CreatePrimitive(root, PrimitiveType.Sphere, "Bear Body", new Vector3(0f, 1.35f, 0f), new Vector3(1.55f, 1.35f, 1.25f), fur);
        CreatePrimitive(root, PrimitiveType.Sphere, "Bear Chest", new Vector3(0f, 1.75f, 0.28f), new Vector3(1.25f, 1.15f, 1.0f), fur);
        CreatePrimitive(root, PrimitiveType.Sphere, "Bear Head", new Vector3(0f, 2.55f, 0.35f), new Vector3(0.92f, 0.82f, 0.85f), fur);
        CreatePrimitive(root, PrimitiveType.Sphere, "Bear Muzzle", new Vector3(0f, 2.38f, 1.02f), new Vector3(0.52f, 0.35f, 0.45f), muzzle);
        CreatePrimitive(root, PrimitiveType.Sphere, "Left Ear", new Vector3(-0.55f, 3.05f, 0.35f), new Vector3(0.28f, 0.28f, 0.22f), fur);
        CreatePrimitive(root, PrimitiveType.Sphere, "Right Ear", new Vector3(0.55f, 3.05f, 0.35f), new Vector3(0.28f, 0.28f, 0.22f), fur);
        for (int side = -1; side <= 1; side += 2)
        {
            CreatePrimitive(root, PrimitiveType.Capsule, "Bear Arm", new Vector3(side * 1.0f, 1.45f, 0.25f), new Vector3(0.46f, 0.92f, 0.46f), fur).transform.localRotation = Quaternion.Euler(12f, 0f, side * -18f);
            CreatePrimitive(root, PrimitiveType.Capsule, "Bear Leg", new Vector3(side * 0.58f, 0.48f, 0f), new Vector3(0.48f, 0.65f, 0.52f), fur);
        }
    }

    private static void BuildGolem(Transform root, Material stone, Material core)
    {
        CreatePrimitive(root, PrimitiveType.Cube, "Golem Torso", new Vector3(0f, 1.75f, 0f), new Vector3(1.75f, 1.65f, 1.15f), stone);
        CreatePrimitive(root, PrimitiveType.Cube, "Golem Head", new Vector3(0f, 3.05f, 0.15f), new Vector3(1.15f, 0.82f, 0.92f), stone);
        CreatePrimitive(root, PrimitiveType.Sphere, "Golem Core", new Vector3(0f, 1.85f, 0.95f), new Vector3(0.38f, 0.38f, 0.22f), core);
        for (int side = -1; side <= 1; side += 2)
        {
            CreatePrimitive(root, PrimitiveType.Cube, "Golem Shoulder", new Vector3(side * 1.25f, 2.25f, 0f), new Vector3(0.85f, 0.85f, 0.9f), stone);
            CreatePrimitive(root, PrimitiveType.Cube, "Golem Arm", new Vector3(side * 1.45f, 1.15f, 0.10f), new Vector3(0.72f, 1.35f, 0.72f), stone);
            CreatePrimitive(root, PrimitiveType.Cube, "Golem Fist", new Vector3(side * 1.48f, 0.28f, 0.30f), new Vector3(0.9f, 0.72f, 0.92f), stone);
            CreatePrimitive(root, PrimitiveType.Cube, "Golem Leg", new Vector3(side * 0.58f, 0.45f, 0f), new Vector3(0.72f, 0.9f, 0.78f), stone);
        }
    }

    private static void BuildDragon(Transform root, Material scale, Material ember)
    {
        CreatePrimitive(root, PrimitiveType.Capsule, "Dragon Body", new Vector3(0f, 1.35f, 0f), new Vector3(1.25f, 1.35f, 1.75f), scale).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        CreatePrimitive(root, PrimitiveType.Sphere, "Dragon Chest", new Vector3(0f, 1.65f, 0.45f), new Vector3(1.45f, 1.15f, 1.2f), scale);
        CreatePrimitive(root, PrimitiveType.Sphere, "Dragon Head", new Vector3(0f, 2.45f, 1.20f), new Vector3(0.82f, 0.66f, 1.05f), scale);
        CreatePrimitive(root, PrimitiveType.Cube, "Dragon Snout", new Vector3(0f, 2.30f, 2.05f), new Vector3(0.75f, 0.42f, 0.85f), scale);
        CreatePrimitive(root, PrimitiveType.Sphere, "Dragon Throat", new Vector3(0f, 2.15f, 1.67f), new Vector3(0.36f, 0.28f, 0.42f), ember);
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject wing = CreatePrimitive(root, PrimitiveType.Cube, "Dragon Wing", new Vector3(side * 1.75f, 2.05f, -0.15f), new Vector3(2.3f, 0.10f, 1.35f), scale);
            wing.transform.localRotation = Quaternion.Euler(0f, side * 12f, side * 20f);
            CreatePrimitive(root, PrimitiveType.Capsule, "Dragon Leg", new Vector3(side * 0.72f, 0.55f, 0.20f), new Vector3(0.44f, 0.70f, 0.50f), scale);
            GameObject horn = CreatePrimitive(root, PrimitiveType.Cylinder, "Dragon Horn", new Vector3(side * 0.43f, 2.95f, 0.92f), new Vector3(0.12f, 0.55f, 0.12f), ember);
            horn.transform.localRotation = Quaternion.Euler(-55f, 0f, side * 12f);
        }
        GameObject tail = CreatePrimitive(root, PrimitiveType.Capsule, "Dragon Tail", new Vector3(0f, 1.0f, -1.75f), new Vector3(0.42f, 1.5f, 0.42f), scale);
        tail.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
    }

    private static GameObject CreateAncientColosseum(Transform parent)
    {
        GameObject root = NewStageRoot(parent, "Stage 1 - Ancient Colosseum");
        Material stone = GetOrCreateMaterial("StoneWarm", new Color(0.28f, 0.22f, 0.16f));
        Material darkStone = GetOrCreateMaterial("StoneDark", new Color(0.12f, 0.10f, 0.09f));
        Material red = GetOrCreateMaterial("BannerRed", new Color(0.45f, 0.05f, 0.04f));

        CreatePrimitive(root.transform, PrimitiveType.Cube, "Arena Floor", new Vector3(0f, -0.5f, 0f), new Vector3(48f, 1f, 48f), stone);
        for (int i = 0; i < 20; i++)
        {
            float angle = i * Mathf.PI * 2f / 20f;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * 23.0f, 2.2f, Mathf.Cos(angle) * 23.0f);
            GameObject wall = CreatePrimitive(root.transform, PrimitiveType.Cube, "Colosseum Wall", pos, new Vector3(7.4f, 4.8f, 0.9f), darkStone);
            wall.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
        }
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * 19.0f, 2.2f, Mathf.Cos(angle) * 19.0f);
            CreatePrimitive(root.transform, PrimitiveType.Cylinder, "Stone Pillar", pos, new Vector3(0.65f, 2.8f, 0.65f), stone);
            CreateTorch(root.transform, pos + Vector3.up * 2.5f);
        }
        for (int i = -2; i <= 2; i += 2)
            CreatePrimitive(root.transform, PrimitiveType.Cube, "Red Banner", new Vector3(i * 4.2f, 4.0f, 22.3f), new Vector3(1.1f, 2.0f, 0.12f), red);

        CreateDirectionalLight(root.transform, new Color(1f, 0.76f, 0.55f), 1.1f, new Vector3(50f, -25f, 0f));
        return root;
    }

    private static GameObject CreateCastleCourtyard(Transform parent)
    {
        GameObject root = NewStageRoot(parent, "Stage 2 - Moonlit Castle Courtyard");
        Material stone = GetOrCreateMaterial("CastleStone", new Color(0.16f, 0.19f, 0.23f));
        Material blue = GetOrCreateMaterial("BannerBlue", new Color(0.04f, 0.14f, 0.36f));
        Material floor = GetOrCreateMaterial("CourtyardFloor", new Color(0.10f, 0.12f, 0.15f));

        CreatePrimitive(root.transform, PrimitiveType.Cube, "Courtyard Floor", new Vector3(0f, -0.5f, 0f), new Vector3(CourtyardFloorSize, 1f, CourtyardFloorSize), floor);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "North Wall", new Vector3(0f, 2.4f, CourtyardWallOffset), new Vector3(CourtyardWallLength, 5.5f, 0.8f), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "South Wall", new Vector3(0f, 2.4f, -CourtyardWallOffset), new Vector3(CourtyardWallLength, 5.5f, 0.8f), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "East Wall", new Vector3(CourtyardWallOffset, 2.4f, 0f), new Vector3(0.8f, 5.5f, CourtyardWallLength), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "West Wall", new Vector3(-CourtyardWallOffset, 2.4f, 0f), new Vector3(0.8f, 5.5f, CourtyardWallLength), stone);

        Vector3[] towers =
        {
            new Vector3(46f, 3.5f, 46f), new Vector3(-46f, 3.5f, 46f),
            new Vector3(46f, 3.5f, -46f), new Vector3(-46f, 3.5f, -46f)
        };
        foreach (Vector3 pos in towers)
            CreatePrimitive(root.transform, PrimitiveType.Cylinder, "Watch Tower", pos, new Vector3(1.3f, 3.2f, 1.3f), stone);

        CreatePrimitive(root.transform, PrimitiveType.Cube, "Castle Gate", new Vector3(0f, 2.5f, CourtyardWallOffset), new Vector3(6.0f, 5.0f, 0.6f), GetOrCreateMaterial("GateWood", new Color(0.12f, 0.07f, 0.04f)));
        for (int i = -2; i <= 2; i += 2)
            CreatePrimitive(root.transform, PrimitiveType.Cube, "Blue Banner", new Vector3(i * 4.0f, 4.2f, CourtyardWallOffset - 0.5f), new Vector3(1.0f, 1.8f, 0.10f), blue);

        CreateTorch(root.transform, new Vector3(-36f, 1.4f, 42.5f));
        CreateTorch(root.transform, new Vector3(36f, 1.4f, 42.5f));
        CreateTorch(root.transform, new Vector3(-36f, 1.4f, -42.5f));
        CreateTorch(root.transform, new Vector3(36f, 1.4f, -42.5f));
        CreateCourtyardArchitecture(root.transform);
        CreateDirectionalLight(root.transform, new Color(0.42f, 0.55f, 0.85f), 0.75f, new Vector3(55f, 30f, 0f));
        return root;
    }

    private static GameObject CreateThroneHall(Transform parent)
    {
        GameObject root = NewStageRoot(parent, "Stage 3 - King's Throne Hall");
        Material floor = GetOrCreateMaterial("HallFloor", new Color(0.08f, 0.07f, 0.065f));
        Material stone = GetOrCreateMaterial("HallStone", new Color(0.19f, 0.17f, 0.15f));
        Material carpet = GetOrCreateMaterial("RoyalCarpet", new Color(0.38f, 0.025f, 0.025f));
        Material gold = GetOrCreateMaterial("RoyalGold", new Color(0.58f, 0.38f, 0.08f));

        CreatePrimitive(root.transform, PrimitiveType.Cube, "Hall Floor", new Vector3(0f, -0.5f, 0f), new Vector3(HallWidth, 1f, HallLength), floor);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Back Wall", new Vector3(0f, 4.0f, 32f), new Vector3(HallWidth, 9f, 1f), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Left Wall", new Vector3(-24f, 4.0f, 0f), new Vector3(1f, 9f, HallLength), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Right Wall", new Vector3(24f, 4.0f, 0f), new Vector3(1f, 9f, HallLength), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Front Left Wall", new Vector3(-14f, 4.0f, -32f), new Vector3(20f, 9f, 1f), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Front Right Wall", new Vector3(14f, 4.0f, -32f), new Vector3(20f, 9f, 1f), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Front Gate Lintel", new Vector3(0f, 7.0f, -32f), new Vector3(8f, 3f, 1f), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Red Carpet", new Vector3(0f, 0.03f, 1f), new Vector3(4.0f, 0.08f, 59f), carpet);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Throne Dais", new Vector3(0f, 0.4f, 29.0f), new Vector3(5.0f, 0.8f, 3.2f), stone);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Throne Seat", new Vector3(0f, 1.35f, 29.6f), new Vector3(1.5f, 1.8f, 1.2f), gold);
        CreatePrimitive(root.transform, PrimitiveType.Cube, "Throne Back", new Vector3(0f, 2.6f, 30.1f), new Vector3(2.2f, 3.2f, 0.5f), gold);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int z = -5; z <= 5; z += 5)
            {
                Vector3 pos = new Vector3(side * 18.0f, 2.6f, z * 3.6f);
                CreatePrimitive(root.transform, PrimitiveType.Cylinder, "Royal Pillar", pos, new Vector3(0.55f, 2.6f, 0.55f), stone);
                CreateTorch(root.transform, pos + Vector3.up * 2.6f);
            }
        }

        CreateThroneHallArchitecture(root.transform);
        CreateDirectionalLight(root.transform, new Color(1f, 0.42f, 0.22f), 0.9f, new Vector3(45f, -20f, 0f));
        return root;
    }

    private static void CreateCourtyardArchitecture(Transform parent)
    {
        const float facadeOffset = 43.45f;
        const float wallHeight = 5.6f;
        Material roof = GetOrCreateMaterial("RampartRoof", new Color(0.075f, 0.085f, 0.10f));

        CreatePrimitive(parent, PrimitiveType.Cube, "North Covered Rampart", new Vector3(0f, 5.9f, 40.5f), new Vector3(88f, 0.8f, 7f), roof);
        CreatePrimitive(parent, PrimitiveType.Cube, "South Covered Rampart", new Vector3(0f, 5.9f, -40.5f), new Vector3(88f, 0.8f, 7f), roof);
        CreatePrimitive(parent, PrimitiveType.Cube, "East Covered Rampart", new Vector3(40.5f, 5.9f, 0f), new Vector3(7f, 0.8f, 74f), roof);
        CreatePrimitive(parent, PrimitiveType.Cube, "West Covered Rampart", new Vector3(-40.5f, 5.9f, 0f), new Vector3(7f, 0.8f, 74f), roof);
        CreatePrimitive(parent, PrimitiveType.Cube, "Gatehouse Roof", new Vector3(0f, 8.2f, 40.5f), new Vector3(20f, 1.0f, 8.5f), roof);

        for (int offset = -39; offset <= 39; offset += 6)
        {
            if (Mathf.Abs(offset) >= 6)
                CreateEnvironmentModel("wall.fbx", parent, "North Modular Wall", new Vector3(offset, 0f, facadeOffset), Vector3.zero, wallHeight);

            CreateEnvironmentModel("wall.fbx", parent, "South Modular Wall", new Vector3(offset, 0f, -facadeOffset), new Vector3(0f, 180f, 0f), wallHeight);
            CreateEnvironmentModel("wall.fbx", parent, "East Modular Wall", new Vector3(facadeOffset, 0f, offset), new Vector3(0f, 90f, 0f), wallHeight);
            CreateEnvironmentModel("wall.fbx", parent, "West Modular Wall", new Vector3(-facadeOffset, 0f, offset), new Vector3(0f, -90f, 0f), wallHeight);
        }

        CreateEnvironmentModel("wall-gate.fbx", parent, "Castle Gate Facade", new Vector3(0f, 0f, facadeOffset - 0.15f), Vector3.zero, 7.2f);

        Vector3[] corners =
        {
            new Vector3(facadeOffset, 0f, facadeOffset),
            new Vector3(-facadeOffset, 0f, facadeOffset),
            new Vector3(facadeOffset, 0f, -facadeOffset),
            new Vector3(-facadeOffset, 0f, -facadeOffset)
        };
        foreach (Vector3 corner in corners)
            CreateEnvironmentModel("wall-corner.fbx", parent, "Castle Wall Corner", corner, Vector3.zero, wallHeight + 0.5f);

        for (int side = -1; side <= 1; side += 2)
        {
            CreateEnvironmentModel("statue.fbx", parent, "Gate Guardian Statue", new Vector3(side * 8.5f, 0f, 38.5f), new Vector3(0f, 180f, 0f), 5.2f);
            CreateEnvironmentModel("weapon-rack.fbx", parent, "Courtyard Weapon Rack", new Vector3(side * 9f, 0f, -39.5f), Vector3.zero, 2.4f);

            for (int z = -24; z <= 24; z += 24)
            {
                CreateEnvironmentModel("column.fbx", parent, "Courtyard Column", new Vector3(side * 37f, 0f, z), Vector3.zero, 6.2f);
                CreateTorch(parent, new Vector3(side * 35.8f, 3.7f, z));
            }
        }

        for (int x = -30; x <= 30; x += 15)
        {
            if (x != 0)
                CreateEnvironmentModel("banner.fbx", parent, "Castle Banner", new Vector3(x, 2.0f, facadeOffset - 0.65f), Vector3.zero, 3.2f);
        }

        for (int x = -8; x <= 8; x += 4)
            CreateEnvironmentModel("border-straight.fbx", parent, "Gatehouse Battlement", new Vector3(x, 8.7f, 43.4f), Vector3.zero, 1.15f);
    }

    private static void CreateThroneHallArchitecture(Transform parent)
    {
        const float sideWallOffset = 23.45f;
        const float backWallOffset = 31.45f;
        const float wallHeight = 8.8f;
        Material ceiling = GetOrCreateMaterial("HallCeilingStone", new Color(0.09f, 0.085f, 0.08f));
        Material beam = GetOrCreateMaterial("HallRoofBeam", new Color(0.095f, 0.055f, 0.03f));

        CreatePrimitive(parent, PrimitiveType.Cube, "Stone Hall Ceiling", new Vector3(0f, 8.7f, 0f), new Vector3(HallWidth, 1.2f, HallLength), ceiling);
        for (int z = -28; z <= 28; z += 8)
            CreatePrimitive(parent, PrimitiveType.Cube, "Ceiling Cross Beam", new Vector3(0f, 7.95f, z), new Vector3(46f, 0.45f, 0.65f), beam);
        for (int x = -15; x <= 15; x += 15)
            CreatePrimitive(parent, PrimitiveType.Cube, "Ceiling Long Beam", new Vector3(x, 7.85f, 0f), new Vector3(0.55f, 0.55f, 62f), beam);

        for (int z = -27; z <= 27; z += 6)
        {
            CreateEnvironmentModel("wall.fbx", parent, "Hall Left Modular Wall", new Vector3(-sideWallOffset, 0f, z), new Vector3(0f, -90f, 0f), wallHeight);
            CreateEnvironmentModel("wall.fbx", parent, "Hall Right Modular Wall", new Vector3(sideWallOffset, 0f, z), new Vector3(0f, 90f, 0f), wallHeight);
        }

        for (int x = -21; x <= 21; x += 6)
            CreateEnvironmentModel("wall.fbx", parent, "Hall Back Modular Wall", new Vector3(x, 0f, backWallOffset), Vector3.zero, wallHeight);

        for (int x = -21; x <= 21; x += 6)
        {
            if (Mathf.Abs(x) >= 6)
                CreateEnvironmentModel("wall.fbx", parent, "Hall Front Modular Wall", new Vector3(x, 0f, -backWallOffset), new Vector3(0f, 180f, 0f), wallHeight);
        }
        CreateEnvironmentModel("wall-gate.fbx", parent, "Hall Entrance Gate", new Vector3(0f, 0f, -backWallOffset + 0.15f), new Vector3(0f, 180f, 0f), 8.6f);

        CreateEnvironmentModel("wall-corner.fbx", parent, "Hall Back Left Corner", new Vector3(-sideWallOffset, 0f, backWallOffset), Vector3.zero, wallHeight);
        CreateEnvironmentModel("wall-corner.fbx", parent, "Hall Back Right Corner", new Vector3(sideWallOffset, 0f, backWallOffset), new Vector3(0f, 90f, 0f), wallHeight);

        for (int side = -1; side <= 1; side += 2)
        {
            CreateEnvironmentModel("statue.fbx", parent, "Royal Statue", new Vector3(side * 8f, 0f, 25.5f), new Vector3(0f, 180f, 0f), 5.8f);
            CreateEnvironmentModel("banner.fbx", parent, "Royal Banner", new Vector3(side * 7f, 3.2f, 30.75f), Vector3.zero, 3.8f);

            for (int z = -20; z <= 20; z += 10)
            {
                CreateEnvironmentModel(z == 0 ? "column-damaged.fbx" : "column.fbx", parent, "Royal Stone Column", new Vector3(side * 18f, 0f, z), Vector3.zero, 7.2f);
            }
        }

        CreateEnvironmentModel("trophy.fbx", parent, "Royal Trophy", new Vector3(0f, 0.8f, 29.1f), new Vector3(0f, 180f, 0f), 2.1f);

        for (int z = -20; z <= 20; z += 10)
            CreateHangingBrazier(parent, new Vector3(0f, 5.8f, z));
    }

    private static GameObject NewStageRoot(Transform parent, string name)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        return root;
    }

    private static void CreateTorch(Transform parent, Vector3 position)
    {
        Material metal = GetOrCreateMaterial("TorchMetal", new Color(0.08f, 0.07f, 0.06f));
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Torch Stand", position - Vector3.up * 0.55f, new Vector3(0.10f, 0.65f, 0.10f), metal);
        GameObject lightObject = new GameObject("Torch Light");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.37f, 0.08f);
        light.intensity = 5.0f;
        light.range = 6.5f;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateHangingBrazier(Transform parent, Vector3 position)
    {
        Material metal = GetOrCreateMaterial("BrazierMetal", new Color(0.055f, 0.05f, 0.045f));
        CreatePrimitive(parent, PrimitiveType.Cylinder, "Brazier Chain", position + Vector3.up * 1.2f, new Vector3(0.035f, 1.2f, 0.035f), metal);
        CreatePrimitive(parent, PrimitiveType.Sphere, "Hanging Brazier", position, new Vector3(0.55f, 0.18f, 0.55f), metal);

        GameObject lightObject = new GameObject("Brazier Light");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = position + Vector3.up * 0.25f;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.32f, 0.07f);
        light.intensity = 5.5f;
        light.range = 10f;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateDirectionalLight(Transform parent, Color color, float intensity, Vector3 rotation)
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localRotation = Quaternion.Euler(rotation);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.shadows = LightShadows.Soft;
    }

    private static GameObject CreatePrimitive(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return go;
    }

    private static GameObject CreateEnvironmentModel(
        string fileName,
        Transform parent,
        string name,
        Vector3 basePosition,
        Vector3 rotation,
        float targetHeight)
    {
        string path = KenneyArenaRoot + "/" + fileName;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new FileNotFoundException("Kenney Mini Arena model missing", path);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = basePosition;
        instance.transform.localRotation = Quaternion.Euler(rotation);
        instance.transform.localScale = Vector3.one;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return instance;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float scale = targetHeight / Mathf.Max(0.001f, bounds.size.y);
        instance.transform.localScale = Vector3.one * scale;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        instance.transform.position += Vector3.up * (basePosition.y - bounds.min.y);

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(collider);

        return instance;
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material;
        if (shader != null)
        {
            material = new Material(shader);
        }
        else
        {
            Material builtIn = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            if (builtIn == null)
                throw new System.InvalidOperationException("Unity default material and supported shaders could not be found.");
            material = new Material(builtIn);
        }

        material.name = name;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject InstantiateCharacter(string path, Transform parent, string name, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new FileNotFoundException("KayKit model missing", path);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * scale;
        Animator animator = go.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
        }
        return go;
    }

    private static Transform GetBoneOrFallback(Animator animator, HumanBodyBones bone, Transform fallback)
    {
        if (animator != null && animator.isHuman)
        {
            Transform boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform != null) return boneTransform;
        }

        string[] names = bone == HumanBodyBones.RightHand
            ? new[] { "handslot.r", "hand.r" }
            : new[] { "handslot.l", "hand.l" };
        Transform[] all = fallback.GetComponentsInChildren<Transform>(true);
        foreach (string wanted in names)
            foreach (Transform t in all)
                if (string.Equals(t.name, wanted, System.StringComparison.OrdinalIgnoreCase))
                    return t;
        return fallback;
    }

    private static Transform AttachEquipment(string path, Transform parent, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject go = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        foreach (Collider collider in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(collider);
        return go.transform;
    }

    private static TinyParryHUD CreateHUD(Transform parent, CombatantHealth player)
    {
        GameObject hudObject = new GameObject("HUD");
        hudObject.transform.SetParent(parent, false);
        TinyParryHUD hud = hudObject.AddComponent<TinyParryHUD>();
        hud.Configure(player);
        return hud;
    }

    private static void EnsureFolder(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath)) return;
        string parent = Path.GetDirectoryName(fullPath)?.Replace("\\", "/");
        string folder = Path.GetFileName(fullPath);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
