using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class CombatAnimationSetup
{
    private const string SourceRoot = "Assets/_Project/Art/Animations/KayKit";
    private const string MeleeFile = "Rig_Medium_CombatMelee";
    private const string GeneralFile = "Rig_Medium_General";
    private const string MovementBasicFile = "Rig_Medium_MovementBasic";
    private const string MovementAdvancedFile = "Rig_Medium_MovementAdvanced";
    private const string OutputRoot = "Assets/_Project/Resources/Animations";
    private const string PlayerControllerPath = OutputRoot + "/PlayerCombat.controller";
    private const string BossControllerPath = OutputRoot + "/BossCombat.controller";
    private const string PlayerModelPath = "Assets/ThirdParty/KayKit/Characters/fbx/Knight.fbx";
    private const string BossModelPath = "Assets/ThirdParty/KayKit/Characters/fbx/Barbarian.fbx";
    private const string ControllerSchema = "kaykit-combat-animation-v1";
    private static bool running;

    static CombatAnimationSetup() => EditorApplication.delayCall += SetupIfNeeded;

    [MenuItem("Tiny Parry Arena/Animations/Rebuild Combat Animators")]
    public static void Rebuild()
    {
        AssetDatabase.DeleteAsset(PlayerControllerPath);
        AssetDatabase.DeleteAsset(BossControllerPath);
        SetupIfNeeded();
    }

    private static void SetupIfNeeded()
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode) return;
        ConfigureCharacterRig(PlayerModelPath);
        ConfigureCharacterRig(BossModelPath);
        if (HasCurrentController(PlayerControllerPath) && HasCurrentController(BossControllerPath)) return;

        running = true;
        try
        {
            Directory.CreateDirectory(OutputRoot);
            string[] paths = Directory.GetFiles(SourceRoot, "*.fbx", SearchOption.TopDirectoryOnly);
            foreach (string rawPath in paths)
            {
                string path = rawPath.Replace('\\', '/');
                ConfigureImporter(path);
            }

            AssetDatabase.Refresh();
            AssetDatabase.DeleteAsset(PlayerControllerPath);
            AssetDatabase.DeleteAsset(BossControllerPath);
            CreateController(PlayerControllerPath);
            CreateController(BossControllerPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Combat animation controllers generated successfully.");
        }
        finally
        {
            running = false;
        }
    }

    private static bool HasCurrentController(string path)
    {
        AssetImporter importer = AssetImporter.GetAtPath(path);
        return importer != null && importer.userData == ControllerSchema;
    }

    private static void ConfigureCharacterRig(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null ||
            (importer.animationType == ModelImporterAnimationType.Human &&
             importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)) return;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();
    }

    private static void ConfigureImporter(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            bool loop = IsLoopingClip(clip.name);
            clip.loopTime = loop;
            clip.loopPose = loop;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.lockRootRotation = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalPositionXZ = true;
            clip.keepOriginalOrientation = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static bool IsLoopingClip(string clipName) =>
        clipName.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
        clipName.IndexOf("Walking", StringComparison.OrdinalIgnoreCase) >= 0 ||
        clipName.IndexOf("Running", StringComparison.OrdinalIgnoreCase) >= 0 ||
        string.Equals(clipName, "Melee_Blocking", StringComparison.OrdinalIgnoreCase);

    private static void CreateController(string path)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        string[] triggers = { "Attack1", "Attack2", "Attack3", "Block", "Hit", "Death",
            "DodgeForward", "DodgeBackward", "DodgeLeft", "DodgeRight" };
        foreach (string trigger in triggers) controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState locomotion = machine.AddState("Locomotion");
        BlendTree tree = new BlendTree { name = "Locomotion", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(LoadClip(MeleeFile, "Melee_2H_Idle"), 0f);
        tree.AddChild(LoadClip(MovementBasicFile, "Walking_A"), 2.4f);
        tree.AddChild(LoadClip(MovementBasicFile, "Running_A"), 5.5f);
        locomotion.motion = tree;
        machine.defaultState = locomotion;

        AddAction(machine, locomotion, "Attack 1", "Attack1", MeleeFile, "Melee_2H_Attack_Slice", 0.05f, true, 0.82f);
        AddAction(machine, locomotion, "Attack 2", "Attack2", MeleeFile, "Melee_2H_Attack_Chop", 0.05f, true, 0.92f);
        AddAction(machine, locomotion, "Attack 3", "Attack3", MeleeFile, "Melee_2H_Attack_Spin", 0.05f, true, 1.05f);
        AddAction(machine, locomotion, "Block", "Block", MeleeFile, "Melee_Block", 0.04f, true);
        AddAction(machine, locomotion, "Hit", "Hit", GeneralFile, "Hit_A", 0.03f, true);
        AddAction(machine, locomotion, "Dodge Forward", "DodgeForward", MovementAdvancedFile, "Dodge_Forward", 0.03f, true);
        AddAction(machine, locomotion, "Dodge Backward", "DodgeBackward", MovementAdvancedFile, "Dodge_Backward", 0.03f, true);
        AddAction(machine, locomotion, "Dodge Left", "DodgeLeft", MovementAdvancedFile, "Dodge_Left", 0.03f, true);
        AddAction(machine, locomotion, "Dodge Right", "DodgeRight", MovementAdvancedFile, "Dodge_Right", 0.03f, true);
        AddAction(machine, locomotion, "Death", "Death", GeneralFile, "Death_A", 0.06f, false);
        AssetImporter importer = AssetImporter.GetAtPath(path);
        importer.userData = ControllerSchema;
        importer.SaveAndReimport();
    }

    private static void AddAction(AnimatorStateMachine machine, AnimatorState locomotion, string stateName,
        string trigger, string fileName, string clipName, float duration, bool returns, float targetDuration = 0f)
    {
        AnimatorState state = machine.AddState(stateName);
        state.motion = LoadClip(fileName, clipName);
        if (targetDuration > 0f && state.motion is AnimationClip clip)
            state.speed = Mathf.Max(0.01f, clip.length / targetDuration);
        AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.duration = duration;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        if (!returns) return;
        AnimatorStateTransition exit = state.AddTransition(locomotion);
        exit.hasExitTime = true;
        exit.exitTime = 0.9f;
        exit.duration = 0.1f;
    }

    private static AnimationClip LoadClip(string fileName, string clipName)
    {
        string path = SourceRoot + "/" + fileName + ".fbx";
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is AnimationClip clip && string.Equals(clip.name, clipName, StringComparison.OrdinalIgnoreCase))
                return clip;
        throw new InvalidOperationException("Animation clip not found: " + path + " / " + clipName);
    }
}
