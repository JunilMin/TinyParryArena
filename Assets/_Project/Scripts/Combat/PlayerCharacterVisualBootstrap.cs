using System;
using UnityEngine;

public static class PlayerCharacterVisualBootstrap
{
    private const string ResourcePath = "Characters/OriginalCharacterSet";

    public static bool TryReplace(GameObject character, out Animator animator, out Transform visualRoot)
    {
        animator = null;
        visualRoot = null;
        if (character == null) return false;

        bool isPlayer = character.GetComponent<PlayerCombat>() != null;
        string replacementName = isPlayer ? "Original Knight Visual" : "KayKit Boss Visual";
        Transform existing = FindDescendant(character.transform, replacementName);
        if (existing != null)
        {
            CharacterGroundingConstraint existingGrounding = existing.GetComponent<CharacterGroundingConstraint>();
            if (existingGrounding == null)
                existingGrounding = existing.gameObject.AddComponent<CharacterGroundingConstraint>();
            existingGrounding.Configure(character.transform);
            animator = existing.GetComponentInChildren<Animator>(true);
            ApplyCombatController(animator, isPlayer, replacementName);
            visualRoot = existing;
            return true;
        }

        PlayerCharacterVisualConfig config = Resources.Load<PlayerCharacterVisualConfig>(ResourcePath);
        GameObject prefab = isPlayer ? config?.playerPrefab : SelectBossPrefab(config, character);
        if (prefab == null) return false;

        Animator oldAnimator = character.GetComponentInChildren<Animator>(true);
        Transform oldVisual = oldAnimator != null ? oldAnimator.transform : null;
        Transform rightEquipment = FindDescendant(character.transform, "Right Equipment Pivot");
        Transform leftEquipment = FindDescendant(character.transform, "Left Equipment Pivot");
        if (rightEquipment == null) rightEquipment = FindDescendantEndingWith(character.transform, " Weapon");
        if (leftEquipment == null) leftEquipment = FindDescendantEndingWith(character.transform, " Shield");
        float targetHeight = GetVisualHeight(oldVisual);

        GameObject replacement = UnityEngine.Object.Instantiate(prefab, character.transform);
        replacement.name = replacementName;
        replacement.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        replacement.transform.localScale = Vector3.one;
        FitToHeightAndGround(replacement, character.transform, targetHeight);
        CharacterGroundingConstraint grounding = replacement.GetComponent<CharacterGroundingConstraint>();
        if (grounding == null)
            grounding = replacement.AddComponent<CharacterGroundingConstraint>();
        grounding.Configure(character.transform);

        animator = replacement.GetComponentInChildren<Animator>(true);
        ReparentEquipment(rightEquipment, FindHand(animator, replacement.transform, true));
        ReparentEquipment(leftEquipment, FindHand(animator, replacement.transform, false));
        if (rightEquipment != null)
        {
            float lateralOffset = isPlayer ? 0.28f : 0.14f;
            float forwardOffset = isPlayer ? 0.12f : 0.06f;
            rightEquipment.position += character.transform.right * lateralOffset + character.transform.forward * forwardOffset;
            if (isPlayer)
                rightEquipment.Rotate(character.transform.forward, -22f, Space.World);

            WeaponBodyClearance clearance = rightEquipment.GetComponent<WeaponBodyClearance>();
            if (clearance == null)
                clearance = rightEquipment.gameObject.AddComponent<WeaponBodyClearance>();
            clearance.Configure(character.transform, isPlayer);
        }

        foreach (Collider childCollider in replacement.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.Destroy(childCollider);

        if (oldVisual != null && !oldVisual.IsChildOf(replacement.transform))
            oldVisual.gameObject.SetActive(false);

        ApplyCombatController(animator, isPlayer, replacementName);
        visualRoot = replacement.transform;
        return true;
    }

    private static void ApplyCombatController(Animator targetAnimator, bool isPlayer, string visualName)
    {
        if (targetAnimator == null)
            return;

        targetAnimator.applyRootMotion = false;
        RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>(
            isPlayer ? "Animations/PlayerCombat" : "Animations/BossCombat");
        if (controller != null && targetAnimator.isHuman)
            targetAnimator.runtimeAnimatorController = controller;
        else if (!targetAnimator.isHuman)
            Debug.LogError(visualName + " must use a Humanoid avatar for combat animation retargeting.");
    }

    private static GameObject SelectBossPrefab(PlayerCharacterVisualConfig config, GameObject character)
    {
        if (config == null)
            return null;

        BossAI boss = character.GetComponent<BossAI>();
        if (boss == null || config.bossPrefabs == null || config.bossPrefabs.Length == 0)
            return config.bossPrefab;

        int startIndex = Mathf.Max(0, boss.StageLevel - 1) % config.bossPrefabs.Length;
        for (int offset = 0; offset < config.bossPrefabs.Length; offset++)
        {
            GameObject candidate = config.bossPrefabs[(startIndex + offset) % config.bossPrefabs.Length];
            if (candidate != null)
                return candidate;
        }
        return config.bossPrefab;
    }

    private static Transform FindHand(Animator animator, Transform root, bool right)
    {
        // KayKit rigs provide dedicated equipment sockets. They include the grip
        // orientation and offset that a raw humanoid hand bone does not.
        string socketName = right ? "handslot.r" : "handslot.l";
        Transform socket = FindDescendant(root, socketName);
        if (socket != null) return socket;

        if (animator != null && animator.isHuman)
        {
            Transform hand = animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (hand != null) return hand;
        }

        string wanted = right ? "hand.r" : "hand.l";
        return FindDescendant(root, wanted);
    }

    private static void ReparentEquipment(Transform equipment, Transform hand)
    {
        if (equipment == null || hand == null) return;
        equipment.SetParent(hand, false);
        equipment.localPosition = Vector3.zero;
        equipment.localRotation = Quaternion.identity;
        equipment.localScale = Vector3.one;
    }

    private static float GetVisualHeight(Transform visual)
    {
        if (visual == null) return 1.84f;
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        return renderers.Length == 0 ? 1.84f : Mathf.Clamp(CalculateBounds(renderers).size.y, 1.4f, 2.2f);
    }

    private static void FitToHeightAndGround(GameObject visual, Transform character, float targetHeight)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = CalculateBounds(renderers);
        visual.transform.localScale = Vector3.one * (targetHeight / Mathf.Max(0.001f, bounds.size.y));
        bounds = CalculateBounds(renderers);
        visual.transform.position += Vector3.up * (character.position.y - bounds.min.y);
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }

    private static Transform FindDescendantEndingWith(Transform root, string suffix)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }
}
