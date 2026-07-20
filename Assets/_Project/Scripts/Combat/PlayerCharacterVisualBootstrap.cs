using System;
using UnityEngine;

public static class PlayerCharacterVisualBootstrap
{
    private const string ResourcePath = "Characters/PlayerCuteKnight";
    private const string ReplacementName = "Cute Player Visual";

    public static bool TryReplace(
        GameObject player,
        out Animator replacementAnimator,
        out Transform replacementRoot)
    {
        replacementAnimator = null;
        replacementRoot = null;
        if (player == null)
            return false;

        Transform existingReplacement = FindDescendant(player.transform, ReplacementName);
        if (existingReplacement != null)
        {
            replacementAnimator = existingReplacement.GetComponentInChildren<Animator>(true);
            replacementRoot = existingReplacement;
            return replacementAnimator != null;
        }

        PlayerCharacterVisualConfig config =
            Resources.Load<PlayerCharacterVisualConfig>(ResourcePath);
        if (config == null || config.characterPrefab == null)
            return false;

        Animator oldAnimator = player.GetComponentInChildren<Animator>(true);
        Transform oldVisualRoot = oldAnimator != null ? oldAnimator.transform : null;
        Transform rightEquipment = FindDescendant(player.transform, "Right Equipment Pivot");
        Transform leftEquipment = FindDescendant(player.transform, "Left Equipment Pivot");

        GameObject replacement = UnityEngine.Object.Instantiate(
            config.characterPrefab,
            player.transform);
        replacement.name = ReplacementName;
        replacement.transform.localPosition = Vector3.zero;
        replacement.transform.localRotation = Quaternion.identity;
        replacement.transform.localScale = Vector3.one;

        FitToHeightAndGround(replacement, player.transform, config.targetHeight);
        replacementAnimator = replacement.GetComponentInChildren<Animator>(true);
        if (replacementAnimator == null || !replacementAnimator.isHuman)
        {
            Debug.LogWarning("Cute player character does not have a valid Humanoid Animator.");
            UnityEngine.Object.Destroy(replacement);
            replacementAnimator = null;
            return false;
        }

        ReparentEquipment(rightEquipment, replacementAnimator, HumanBodyBones.RightHand);
        ReparentEquipment(leftEquipment, replacementAnimator, HumanBodyBones.LeftHand);
        FitEquipmentToLength(FindDescendant(player.transform, "Player Greatsword"), 1.65f);

        foreach (Collider childCollider in replacement.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.Destroy(childCollider);

        if (oldVisualRoot != null && oldVisualRoot != replacement.transform)
            oldVisualRoot.gameObject.SetActive(false);

        replacementAnimator.applyRootMotion = false;
        replacementRoot = replacement.transform;
        return true;
    }

    private static void ReparentEquipment(
        Transform equipment,
        Animator animator,
        HumanBodyBones handBone)
    {
        if (equipment == null || animator == null)
            return;

        Transform hand = animator.GetBoneTransform(handBone);
        if (hand == null)
            return;

        equipment.SetParent(hand, false);
        equipment.localPosition = Vector3.zero;
        equipment.localRotation = Quaternion.identity;
        equipment.localScale = Vector3.one;
    }

    private static void FitToHeightAndGround(
        GameObject visual,
        Transform player,
        float targetHeight)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = CalculateBounds(renderers);
        float scale = Mathf.Max(0.01f, targetHeight) / Mathf.Max(0.001f, bounds.size.y);
        visual.transform.localScale = Vector3.one * scale;

        bounds = CalculateBounds(renderers);
        visual.transform.position += Vector3.up * (player.position.y - bounds.min.y);
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void FitEquipmentToLength(Transform equipment, float targetLength)
    {
        if (equipment == null || !equipment.gameObject.activeInHierarchy)
            return;

        Renderer[] renderers = equipment.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = CalculateBounds(renderers);
        float currentLength = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (currentLength <= 0.001f)
            return;

        equipment.localScale *= targetLength / currentLength;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform descendant in descendants)
        {
            if (string.Equals(descendant.name, name, StringComparison.OrdinalIgnoreCase))
                return descendant;
        }
        return null;
    }
}
