using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class SwordAnimationDriver : MonoBehaviour
{
    private const float LocomotionBlendTime = 0.14f;
    private const float ActionBlendIn = 0.055f;
    private const float ActionBlendOut = 0.12f;

    private Animator animator;
    private SwordCombatAnimationLibrary library;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable baseA;
    private AnimationClipPlayable baseB;
    private AnimationClipPlayable action;
    private AnimationClip clipA;
    private AnimationClip clipB;
    private int activeBaseSlot;
    private int targetBaseSlot;
    private float baseBlend;
    private bool baseBlending;
    private bool actionActive;
    private bool holdActionLastFrame;
    private bool actionHeld;
    private bool authoredAnimationEnabled;
    private float moveSpeed;
    private float actionWeight;

    public bool IsReady => graph.IsValid() && library != null && library.IsValid;
    public bool IsAuthoredAnimationEnabled => authoredAnimationEnabled && IsReady;

    public bool Initialize(Animator targetAnimator, SwordCombatAnimationLibrary animationLibrary)
    {
        if (targetAnimator == null || animationLibrary == null || !animationLibrary.IsValid)
            return false;

        animator = targetAnimator;
        library = animationLibrary;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.runtimeAnimatorController = null;

        graph = PlayableGraph.Create("Player Sword Combat Animations");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(graph, 3);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Sword Combat", animator);
        output.SetSourcePlayable(mixer);

        SetBasePlayable(0, library.idle);
        activeBaseSlot = 0;
        targetBaseSlot = 0;
        mixer.SetInputWeight(0, 1f);
        mixer.SetInputWeight(1, 0f);
        mixer.SetInputWeight(2, 0f);
        authoredAnimationEnabled = true;
        graph.Play();
        return true;
    }

    public void SetAuthoredAnimationEnabled(bool value)
    {
        if (!IsReady || authoredAnimationEnabled == value)
            return;

        authoredAnimationEnabled = value;
        if (value)
        {
            animator.Rebind();
            RequestBaseClip(library.idle);
            graph.Play();
        }
        else
        {
            ClearAction();
            graph.Stop();
            animator.Rebind();
        }
    }

    public void SetMoveSpeed(float value)
    {
        moveSpeed = Mathf.Max(0f, value);
    }

    public bool PlayAttack(int combo, float speed, out float hitDelay, out float totalDuration)
    {
        hitDelay = 0f;
        totalDuration = 0f;
        if (!IsAuthoredAnimationEnabled)
            return false;

        AnimationClip clip = library.GetAttack(combo);
        float normalizedHit = combo % 3 == 0 ? 0.43f : combo % 3 == 1 ? 0.46f : 0.52f;
        if (!PlayAction(clip, speed, false, out totalDuration))
            return false;

        hitDelay = totalDuration * normalizedHit;
        return true;
    }

    public bool PlayParry(float speed = 1f)
    {
        return PlayAction(library != null ? library.parry : null, speed, false, out _);
    }

    public bool PlayHit()
    {
        return PlayAction(library != null ? library.hit : null, 1f, false, out _);
    }

    public bool PlayDrink()
    {
        return PlayAction(library != null ? library.drink : null, 1f, false, out _);
    }

    public bool PlayDeath()
    {
        return PlayAction(library != null ? library.death : null, 1f, true, out _);
    }

    private void Update()
    {
        if (!IsAuthoredAnimationEnabled)
            return;

        AnimationClip requested = moveSpeed < 0.15f
            ? library.idle
            : moveSpeed > 5.2f ? library.run : library.walk;
        RequestBaseClip(requested);
        UpdateLocomotionPlayable(baseA, clipA);
        UpdateLocomotionPlayable(baseB, clipB);
        UpdateBaseBlend();
        UpdateActionBlend();
    }

    private void RequestBaseClip(AnimationClip clip)
    {
        AnimationClip activeClip = activeBaseSlot == 0 ? clipA : clipB;
        AnimationClip targetClip = targetBaseSlot == 0 ? clipA : clipB;
        if (clip == null || clip == activeClip || (baseBlending && clip == targetClip))
            return;

        targetBaseSlot = 1 - activeBaseSlot;
        SetBasePlayable(targetBaseSlot, clip);
        baseBlend = 0f;
        baseBlending = true;
    }

    private void SetBasePlayable(int slot, AnimationClip clip)
    {
        AnimationClipPlayable previous = slot == 0 ? baseA : baseB;
        if (previous.IsValid())
        {
            mixer.DisconnectInput(slot);
            graph.DestroyPlayable(previous);
        }

        AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetApplyFootIK(true);
        playable.SetApplyPlayableIK(false);
        playable.SetTime(0d);
        graph.Connect(playable, 0, mixer, slot);
        playable.Play();

        if (slot == 0)
        {
            baseA = playable;
            clipA = clip;
        }
        else
        {
            baseB = playable;
            clipB = clip;
        }
    }

    private void UpdateBaseBlend()
    {
        if (!baseBlending)
            return;

        baseBlend = Mathf.MoveTowards(baseBlend, 1f, Time.deltaTime / LocomotionBlendTime);
        mixer.SetInputWeight(activeBaseSlot, 1f - baseBlend);
        mixer.SetInputWeight(targetBaseSlot, baseBlend);
        if (baseBlend < 1f)
        {
            ApplyMixerWeights();
            return;
        }

        activeBaseSlot = targetBaseSlot;
        baseBlending = false;
        ApplyMixerWeights();
    }

    private bool PlayAction(AnimationClip clip, float speed, bool holdLastFrame, out float duration)
    {
        duration = 0f;
        if (!IsAuthoredAnimationEnabled || clip == null)
            return false;

        ClearAction();

        float playbackSpeed = Mathf.Max(0.1f, speed);
        action = AnimationClipPlayable.Create(graph, clip);
        action.SetApplyFootIK(true);
        action.SetApplyPlayableIK(false);
        action.SetSpeed(playbackSpeed);
        action.SetTime(0d);
        graph.Connect(action, 0, mixer, 2);
        action.Play();
        actionWeight = 1f;
        holdActionLastFrame = holdLastFrame;
        actionHeld = false;
        actionActive = true;
        ApplyMixerWeights();
        duration = clip.length / playbackSpeed;
        return true;
    }

    private void UpdateActionBlend()
    {
        if (!actionActive || !action.IsValid())
            return;

        if (actionHeld)
            return;

        double time = action.GetTime();
        double duration = action.GetAnimationClip().length;
        float weight;
        if (time < ActionBlendIn)
            weight = Mathf.Clamp01((float)time / ActionBlendIn);
        else if (time > duration - ActionBlendOut)
            weight = Mathf.Clamp01((float)(duration - time) / ActionBlendOut);
        else
            weight = 1f;

        actionWeight = weight;
        ApplyMixerWeights();

        if (time < duration)
            return;

        if (holdActionLastFrame)
        {
            action.SetTime(duration);
            action.SetSpeed(0d);
            actionWeight = 1f;
            actionHeld = true;
            ApplyMixerWeights();
            return;
        }

        actionWeight = 0f;
        mixer.DisconnectInput(2);
        graph.DestroyPlayable(action);
        action = default;
        actionActive = false;
        ApplyMixerWeights();
    }

    private void ClearAction()
    {
        actionWeight = 0f;
        actionActive = false;
        actionHeld = false;
        holdActionLastFrame = false;
        if (!action.IsValid())
            return;

        mixer.DisconnectInput(2);
        graph.DestroyPlayable(action);
        action = default;
        ApplyMixerWeights();
    }

    private void UpdateLocomotionPlayable(AnimationClipPlayable playable, AnimationClip clip)
    {
        if (!playable.IsValid() || clip == null || clip.length <= 0f)
            return;

        float playbackSpeed = clip == library.idle
            ? 1f
            : clip == library.walk
                ? Mathf.Clamp(moveSpeed / 3.5f, 0.65f, 1.35f)
                : Mathf.Clamp(moveSpeed / 6.5f, 0.75f, 1.25f);
        playable.SetSpeed(playbackSpeed);

        double time = playable.GetTime();
        if (time >= clip.length)
            playable.SetTime(time % clip.length);
    }

    private void ApplyMixerWeights()
    {
        float baseAWeight;
        float baseBWeight;
        if (baseBlending)
        {
            baseAWeight = activeBaseSlot == 0 ? 1f - baseBlend : baseBlend;
            baseBWeight = activeBaseSlot == 1 ? 1f - baseBlend : baseBlend;
        }
        else
        {
            baseAWeight = activeBaseSlot == 0 ? 1f : 0f;
            baseBWeight = activeBaseSlot == 1 ? 1f : 0f;
        }

        float baseWeight = 1f - actionWeight;
        mixer.SetInputWeight(0, baseAWeight * baseWeight);
        mixer.SetInputWeight(1, baseBWeight * baseWeight);
        mixer.SetInputWeight(2, actionWeight);
    }

    private void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}
