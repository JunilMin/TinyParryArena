using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TinyParryPhysicalFeedbackSetup
{
    static TinyParryPhysicalFeedbackSetup()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path != "Assets/_Project/Scenes/Arena.unity")
            return;

        EditorApplication.delayCall += ApplyAutomatically;
    }

    private static void ApplyAutomatically()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject player = GameObject.Find("Player");
        GameObject enemy = GameObject.Find("Normal Enemy");
        GameObject boss = GameObject.Find("Boss");

        if (player == null || enemy == null || boss == null)
            return;

        bool changed = false;
        changed |= EnsureKnockback(player);
        changed |= EnsureKnockback(enemy);
        changed |= EnsureKnockback(boss);

        PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
        Transform equipment = player.transform.Find("Equipment");
        Transform shield = equipment != null ? equipment.Find("Shield") : null;

        if (playerCombat != null && shield != null)
        {
            SerializedObject serialized = new SerializedObject(playerCombat);
            SerializedProperty property = serialized.FindProperty("shieldPivot");

            if (property != null && property.objectReferenceValue != shield)
            {
                property.objectReferenceValue = shield;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[Tiny Parry Arena] Physical parry feedback configured automatically.");
        }
    }

    private static bool EnsureKnockback(GameObject target)
    {
        if (target.GetComponent<KnockbackReceiver>() != null)
            return false;

        target.AddComponent<KnockbackReceiver>();
        return true;
    }
}
