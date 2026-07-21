using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class CombatAnimationSetup
{
    private const string SourceRoot = "Assets/_Project/Art/Animations/Combat";
    private const string OutputRoot = "Assets/_Project/Resources/Animations";
    private const string PlayerControllerPath = OutputRoot + "/PlayerCombat.controller";
    private const string BossControllerPath = OutputRoot + "/BossCombat.controller";
    private const string PlayerModelPath = "Assets/ThirdParty/KayKit/Characters/fbx/Knight.fbx";
    private const string BossModelPath = "Assets/ThirdParty/KayKit/Characters/fbx/Barbarian.fbx";
    private const string ControllerSchema = "combat-animation-v2";
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
                ConfigureImporter(path, IsLooping(path));
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

    private static void ConfigureImporter(string path, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
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

    private static bool IsLooping(string path) =>
        path.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
        path.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0 ||
        path.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0;

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
        tree.AddChild(LoadClip("GreatSword_Idle"), 0f);
        tree.AddChild(LoadClip("GreatSword_Walk"), 2.4f);
        tree.AddChild(LoadClip("GreatSword_Run"), 5.5f);
        locomotion.motion = tree;
        machine.defaultState = locomotion;

        AddAction(machine, locomotion, "Attack 1", "Attack1", "GreatSword_Attack01", 0.06f, true, 0.82f);
        AddAction(machine, locomotion, "Attack 2", "Attack2", "GreatSword_Attack02", 0.06f, true, 0.98f);
        AddAction(machine, locomotion, "Attack 3", "Attack3", "GreatSword_Attack03", 0.06f, true, 1.12f);
        AddAction(machine, locomotion, "Block", "Block", "GreatSword_Block", 0.05f, true);
        AddAction(machine, locomotion, "Hit", "Hit", "GreatSword_Hit", 0.04f, true);
        AddAction(machine, locomotion, "Dodge Forward", "DodgeForward", "Dodge_Forward", 0.04f, true);
        AddAction(machine, locomotion, "Dodge Backward", "DodgeBackward", "Dodge_Backward", 0.04f, true);
        AddAction(machine, locomotion, "Dodge Left", "DodgeLeft", "Dodge_Left", 0.04f, true);
        AddAction(machine, locomotion, "Dodge Right", "DodgeRight", "Dodge_Right", 0.04f, true);
        AddAction(machine, locomotion, "Death", "Death", "GreatSword_Death", 0.08f, false);
        AssetImporter importer = AssetImporter.GetAtPath(path);
        importer.userData = ControllerSchema;
        importer.SaveAndReimport();
    }

    private static void AddAction(AnimatorStateMachine machine, AnimatorState locomotion, string stateName,
        string trigger, string clipName, float duration, bool returns, float targetDuration = 0f)
    {
        AnimatorState state = machine.AddState(stateName);
        state.motion = LoadClip(clipName);
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

    private static AnimationClip LoadClip(string fileName)
    {
        string path = SourceRoot + "/" + fileName + ".fbx";
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                return clip;
        throw new InvalidOperationException("Animation clip not found: " + path);
    }
}
