using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class PlayerSwordAnimationAssetSetup
{
    private const string LibraryPath =
        "Assets/_Project/Resources/Animations/PlayerSwordCombat.asset";
    private const string AnimationRoot =
        "Assets/ThirdParty/ProSwordShieldAnimations/";
    private const string CharacterConfigPath =
        "Assets/_Project/Resources/Characters/PlayerCuteKnight.asset";
    private const string CuteKnightPath =
        "Assets/ThirdParty/KayKit/Characters/fbx/Knight.fbx";

    static PlayerSwordAnimationAssetSetup()
    {
        EditorApplication.delayCall += EnsureLibraryReferences;
    }

    [MenuItem("Tiny Parry Arena/Repair Player Sword Animation References")]
    private static void EnsureLibraryReferences()
    {
        EnsureCharacterVisualConfig();
        ConfigureClip("sword and shield idle.fbx", true);
        ConfigureClip("sword and shield walk.fbx", true);
        ConfigureClip("sword and shield run.fbx", true);
        ConfigureClip("sword and shield slash.fbx", false);
        ConfigureClip("sword and shield slash (2).fbx", false);
        ConfigureClip("sword and shield attack (4).fbx", false);
        ConfigureClip("sword and shield block (2).fbx", false);
        ConfigureClip("sword and shield casting.fbx", false);
        ConfigureClip("sword and shield impact.fbx", false);
        ConfigureClip("sword and shield death.fbx", false);

        SwordCombatAnimationLibrary library =
            AssetDatabase.LoadAssetAtPath<SwordCombatAnimationLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<SwordCombatAnimationLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        bool changed = false;
        changed |= Assign(ref library.idle, LoadClip("sword and shield idle.fbx"));
        changed |= Assign(ref library.walk, LoadClip("sword and shield walk.fbx"));
        changed |= Assign(ref library.run, LoadClip("sword and shield run.fbx"));
        changed |= Assign(ref library.attackOne, LoadClip("sword and shield slash.fbx"));
        changed |= Assign(ref library.attackTwo, LoadClip("sword and shield slash (2).fbx"));
        changed |= Assign(ref library.attackThree, LoadClip("sword and shield attack (4).fbx"));
        changed |= Assign(ref library.parry, LoadClip("sword and shield block (2).fbx"));
        changed |= Assign(ref library.drink, LoadClip("sword and shield casting.fbx"));
        changed |= Assign(ref library.hit, LoadClip("sword and shield impact.fbx"));
        changed |= Assign(ref library.death, LoadClip("sword and shield death.fbx"));

        if (!changed)
            return;

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
    }

    private static AnimationClip LoadClip(string fileName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(AnimationRoot + fileName)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
    }

    private static void EnsureCharacterVisualConfig()
    {
        PlayerCharacterVisualConfig config =
            AssetDatabase.LoadAssetAtPath<PlayerCharacterVisualConfig>(CharacterConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<PlayerCharacterVisualConfig>();
            AssetDatabase.CreateAsset(config, CharacterConfigPath);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CuteKnightPath);
        if (config.characterPrefab == prefab && Mathf.Approximately(config.targetHeight, 1.84f))
            return;

        config.characterPrefab = prefab;
        config.targetHeight = 1.84f;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureClip(string fileName, bool loop)
    {
        string path = AnimationRoot + fileName;
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        bool changed = false;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            if (clip.loopTime != loop) { clip.loopTime = loop; changed = true; }
            if (clip.loopPose != loop) { clip.loopPose = loop; changed = true; }
            if (!clip.lockRootRotation) { clip.lockRootRotation = true; changed = true; }
            if (clip.keepOriginalOrientation) { clip.keepOriginalOrientation = false; changed = true; }
            if (!clip.lockRootHeightY) { clip.lockRootHeightY = true; changed = true; }
            if (clip.keepOriginalPositionY) { clip.keepOriginalPositionY = false; changed = true; }
            if (!clip.heightFromFeet) { clip.heightFromFeet = true; changed = true; }
            if (!clip.lockRootPositionXZ) { clip.lockRootPositionXZ = true; changed = true; }
            if (clip.keepOriginalPositionXZ) { clip.keepOriginalPositionXZ = false; changed = true; }
        }

        if (!changed)
            return;

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static bool Assign(ref AnimationClip destination, AnimationClip source)
    {
        if (destination == source)
            return false;

        destination = source;
        return true;
    }
}
