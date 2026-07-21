using System;
using System.Collections;
using UnityEngine;

public sealed class CharacterVisualController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float speedDamp = 0.12f;

    private Vector3 lastPosition;
    private Coroutine actionRoutine;

    private Transform leftUpperArm;
    private Transform rightUpperArm;
    private Transform leftLowerArm;
    private Transform rightLowerArm;
    private Transform leftUpperLeg;
    private Transform rightUpperLeg;

    private Quaternion leftUpperArmBase;
    private Quaternion rightUpperArmBase;
    private Quaternion leftLowerArmBase;
    private Quaternion rightLowerArmBase;
    private Quaternion leftUpperLegBase;
    private Quaternion rightUpperLegBase;

    private float moveSpeed;
    private float actionWeight;
    private Vector3 actionEuler;
    private Vector3 visualBasePosition;
    private Quaternion visualBaseRotation;
    private bool dead;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int BlockHash = Animator.StringToHash("Block");
    private static readonly int DodgeForwardHash = Animator.StringToHash("DodgeForward");
    private static readonly int DodgeBackwardHash = Animator.StringToHash("DodgeBackward");
    private static readonly int DodgeLeftHash = Animator.StringToHash("DodgeLeft");
    private static readonly int DodgeRightHash = Animator.StringToHash("DodgeRight");

    public bool UsesAuthoredAnimations => animator != null && animator.runtimeAnimatorController != null;

    private void Awake()
    {
        if (PlayerCharacterVisualBootstrap.TryReplace(gameObject, out Animator replacementAnimator, out Transform replacementRoot))
        {
            animator = replacementAnimator;
            visualRoot = replacementRoot;
        }

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (visualRoot == null && animator != null) visualRoot = animator.transform;
        if (visualRoot == null) visualRoot = transform;

        visualBasePosition = visualRoot.localPosition;
        visualBaseRotation = visualRoot.localRotation;
        CacheBones();
        lastPosition = transform.position;
    }

    private void CacheBones()
    {
        if (animator != null && animator.isHuman)
        {
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        }

        // KayKit free character FBXs are commonly imported as Generic rigs. In that
        // case Animator.GetBoneTransform returns null, so locate the actual rig bones.
        leftUpperArm ??= FindBone("upperarm.l");
        rightUpperArm ??= FindBone("upperarm.r");
        leftLowerArm ??= FindBone("lowerarm.l");
        rightLowerArm ??= FindBone("lowerarm.r");
        leftUpperLeg ??= FindBone("upperleg.l");
        rightUpperLeg ??= FindBone("upperleg.r");
        leftUpperArm ??= FindBoneEndingWith("leftarm");
        rightUpperArm ??= FindBoneEndingWith("rightarm");
        leftLowerArm ??= FindBoneEndingWith("leftforearm");
        rightLowerArm ??= FindBoneEndingWith("rightforearm");
        leftUpperLeg ??= FindBoneEndingWith("leftupleg");
        rightUpperLeg ??= FindBoneEndingWith("rightupleg");

        if (leftUpperArm != null) leftUpperArmBase = leftUpperArm.localRotation;
        if (rightUpperArm != null) rightUpperArmBase = rightUpperArm.localRotation;
        if (leftLowerArm != null) leftLowerArmBase = leftLowerArm.localRotation;
        if (rightLowerArm != null) rightLowerArmBase = rightLowerArm.localRotation;
        if (leftUpperLeg != null) leftUpperLegBase = leftUpperLeg.localRotation;
        if (rightUpperLeg != null) rightUpperLegBase = rightUpperLeg.localRotation;
    }

    private Transform FindBone(string wanted)
    {
        Transform[] all = visualRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (string.Equals(t.name, wanted, StringComparison.OrdinalIgnoreCase)) return t;
        }
        return null;
    }

    private Transform FindBoneEndingWith(string suffix)
    {
        Transform[] all = visualRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return t;
        }
        return null;
    }

    private void Update()
    {
        moveSpeed = Vector3.Distance(transform.position, lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat(SpeedHash, moveSpeed, speedDamp, Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (!dead && !UsesAuthoredAnimations) ApplyProceduralStance();
    }

    private void ApplyProceduralStance()
    {
        if (leftUpperArm == null || rightUpperArm == null) return;

        float moving = Mathf.Clamp01(moveSpeed / 4f);
        float phase = Time.time * Mathf.Lerp(2.2f, 7.5f, moving);
        float armSwing = Mathf.Sin(phase) * 0.16f * moving;
        float legSwing = Mathf.Sin(phase) * 18f * moving;

        // Always reset to bind pose first so rotations do not accumulate.
        leftUpperArm.localRotation = leftUpperArmBase;
        rightUpperArm.localRotation = rightUpperArmBase;
        if (leftLowerArm != null) leftLowerArm.localRotation = leftLowerArmBase;
        if (rightLowerArm != null) rightLowerArm.localRotation = rightLowerArmBase;

        // Aim each upper arm toward a relaxed down/forward direction in character space.
        // This is axis-independent and therefore works on Generic KayKit rigs.
        AimLimb(leftUpperArm, leftLowerArm, new Vector3(-0.38f - armSwing, -0.82f, 0.34f));
        AimLimb(rightUpperArm, rightLowerArm, new Vector3(0.38f + armSwing, -0.82f, 0.34f));

        // Bend elbows into a sword-and-shield guard instead of leaving them straight.
        if (leftLowerArm != null)
            leftLowerArm.localRotation = leftLowerArm.localRotation * Quaternion.Euler(0f, 0f, -28f);
        if (rightLowerArm != null)
            rightLowerArm.localRotation = rightLowerArm.localRotation * Quaternion.Euler(0f, 0f, 32f);

        if (actionWeight > 0f)
        {
            rightUpperArm.localRotation *= Quaternion.Euler(actionEuler * actionWeight);
            leftUpperArm.localRotation *= Quaternion.Euler(actionEuler * actionWeight * 0.25f);
        }

        if (leftUpperLeg != null)
            leftUpperLeg.localRotation = leftUpperLegBase * Quaternion.Euler(legSwing, 0f, 0f);
        if (rightUpperLeg != null)
            rightUpperLeg.localRotation = rightUpperLegBase * Quaternion.Euler(-legSwing, 0f, 0f);
    }

    private void AimLimb(Transform upper, Transform lower, Vector3 localDesiredDirection)
    {
        if (upper == null || lower == null) return;
        Vector3 current = lower.position - upper.position;
        if (current.sqrMagnitude < 0.000001f) return;
        Vector3 desired = transform.TransformDirection(localDesiredDirection.normalized);
        upper.rotation = Quaternion.FromToRotation(current.normalized, desired) * upper.rotation;
    }

    public void PlayAttack(int combo) => PlayAttack(combo, 1f);
    public void PlayAttack(int combo, float intensity)
    {
        if (UsesAuthoredAnimations)
        {
            animator.SetTrigger(Animator.StringToHash("Attack" + (Math.Abs(combo) % 3 + 1)));
            return;
        }
        StartAction(AttackPose(combo, intensity));
    }
    public void PlayTelegraph(int combo, float duration, float intensity = 1.45f)
    {
        if (UsesAuthoredAnimations) return;
        Vector3 pose = combo == 0 ? new Vector3(-72f, -18f, 58f)
            : combo == 1 ? new Vector3(-22f, -88f, 70f)
            : new Vector3(-34f, 12f, 86f);
        StartAction(ActionPose(pose * intensity, Mathf.Min(0.16f, duration * 0.35f), Mathf.Max(0.08f, duration * 0.45f), Mathf.Min(0.16f, duration * 0.20f)));
    }
    public void PlayParry()
    {
        if (UsesAuthoredAnimations) animator.SetTrigger(BlockHash);
        else StartAction(ActionPose(new Vector3(-18f, -10f, 42f), 0.08f, 0.16f, 0.11f));
    }
    public void PlayDodge(Vector3 localDirection)
    {
        if (!UsesAuthoredAnimations) return;
        if (Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.z))
            animator.SetTrigger(localDirection.x >= 0f ? DodgeRightHash : DodgeLeftHash);
        else
            animator.SetTrigger(localDirection.z >= 0f ? DodgeForwardHash : DodgeBackwardHash);
    }
    public void PlayDrink() => StartAction(ActionPose(new Vector3(-40f, 0f, 20f), 0.12f, 0.35f, 0.18f));

    public void PlayHit()
    {
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(HitHash);
        if (UsesAuthoredAnimations) return;
        StartAction(ActionPose(new Vector3(-12f, 0f, 25f), 0.06f, 0.16f, 0.12f));
    }

    public void PlayStagger(float duration = 1.5f)
    {
        if (UsesAuthoredAnimations) animator.SetTrigger(HitHash);
        else StartAction(ActionPose(new Vector3(28f, 0f, -20f), 0.09f, Mathf.Max(0.1f, duration - 0.22f), 0.13f));
    }

    public void PlayDeath()
    {
        if (dead) return;
        dead = true;
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        actionRoutine = null;
        actionWeight = 0f;
        actionEuler = Vector3.zero;
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(DeathHash);
        if (UsesAuthoredAnimations) return;
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        Quaternion fromRotation = visualRoot.localRotation;
        Vector3 fromPosition = visualRoot.localPosition;
        Quaternion toRotation = visualBaseRotation * Quaternion.Euler(0f, 0f, -88f);
        Vector3 toPosition = visualBasePosition + new Vector3(0.12f, 0f, 0f);

        // Preserve the lowest visible point so the death fall never pushes the model underground.
        float groundY = GetLowestRendererY();
        float duration = 0.72f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / duration));
            visualRoot.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
            visualRoot.localPosition = Vector3.Lerp(fromPosition, toPosition, t);
            KeepVisualAboveGround(groundY);
            yield return null;
        }

        visualRoot.localRotation = toRotation;
        visualRoot.localPosition = toPosition;
        KeepVisualAboveGround(groundY);
    }

    private float GetLowestRendererY()
    {
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return transform.position.y;

        float lowest = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
            lowest = Mathf.Min(lowest, renderer.bounds.min.y);
        return lowest;
    }

    private void KeepVisualAboveGround(float groundY)
    {
        float currentLowest = GetLowestRendererY();
        float correction = groundY - currentLowest;
        if (correction > 0.0001f)
            visualRoot.position += Vector3.up * correction;
    }

    private IEnumerator AttackPose(int combo, float intensity)
    {
        Vector3 windup;
        Vector3 strike;
        switch (combo % 3)
        {
            case 0: windup = new Vector3(-25f, -15f, -35f); strike = new Vector3(30f, 20f, 55f); break;
            case 1: windup = new Vector3(-15f, 20f, 45f); strike = new Vector3(25f, -25f, -55f); break;
            default: windup = new Vector3(-55f, 0f, -10f); strike = new Vector3(65f, 0f, 15f); break;
        }
        intensity = Mathf.Max(0.5f, intensity);
        windup *= intensity;
        strike *= intensity;
        yield return BlendAction(Vector3.zero, windup, 0f, 1f, 0.13f);
        yield return BlendAction(windup, strike, 1f, 1f, 0.11f);
        yield return new WaitForSeconds(0.04f);
        yield return BlendAction(strike, Vector3.zero, 1f, 0f, 0.17f);
    }

    private IEnumerator ActionPose(Vector3 euler, float inTime, float holdTime, float outTime)
    {
        yield return BlendAction(Vector3.zero, euler, 0f, 1f, inTime);
        yield return new WaitForSeconds(holdTime);
        if (outTime > 0f) yield return BlendAction(euler, Vector3.zero, 1f, 0f, outTime);
    }

    private IEnumerator BlendAction(Vector3 fromEuler, Vector3 toEuler, float fromWeight, float toWeight, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Smooth(Mathf.Clamp01(t / Mathf.Max(duration, 0.0001f)));
            actionEuler = Vector3.LerpUnclamped(fromEuler, toEuler, a);
            actionWeight = Mathf.LerpUnclamped(fromWeight, toWeight, a);
            yield return null;
        }
        actionEuler = toEuler;
        actionWeight = toWeight;
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    private void StartAction(IEnumerator routine)
    {
        if (actionRoutine != null) StopCoroutine(actionRoutine);
        actionEuler = Vector3.zero;
        actionWeight = 0f;
        actionRoutine = StartCoroutine(routine);
    }
}
