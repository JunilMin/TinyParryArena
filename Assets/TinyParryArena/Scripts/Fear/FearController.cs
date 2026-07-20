using System;
using UnityEngine;

namespace TinyParryArena.Fear
{
    [DisallowMultipleComponent]
    public sealed class FearController : MonoBehaviour
    {
        [SerializeField] private FearConfig config;
        [SerializeField] private bool resetOnEnable = true;

        public float CurrentFear { get; private set; }
        public float MaxFear => config != null ? config.maxFear : 100f;
        public float NormalizedFear => MaxFear <= 0f ? 0f : CurrentFear / MaxFear;
        public FearState CurrentState { get; private set; } = FearState.Calm;
        public bool IsBroken => CurrentState == FearState.Broken;

        public event Action<float, float> FearChanged;
        public event Action<FearState, FearState> FearStateChanged;
        public event Action Broken;

        private float timeSinceLastSuccessfulParry;
        private bool decayEnabled = true;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(FearController)} on '{name}' has no FearConfig.", this);
                enabled = false;
                return;
            }

            ResetFear();
        }

        private void OnEnable()
        {
            if (resetOnEnable && config != null)
            {
                ResetFear();
            }
        }

        private void Update()
        {
            if (!decayEnabled || IsBroken || CurrentFear <= 0f)
            {
                return;
            }

            timeSinceLastSuccessfulParry += Time.deltaTime;

            if (timeSinceLastSuccessfulParry >= config.inactivityDelay)
            {
                RemoveFear(config.inactivityDecayPerSecond * Time.deltaTime);
            }
        }

        public void RegisterSuccessfulParry(ParryQuality quality)
        {
            if (IsBroken)
            {
                return;
            }

            timeSinceLastSuccessfulParry = 0f;

            float amount = quality == ParryQuality.Perfect
                ? config.perfectParryGain
                : config.normalParryGain;

            SetFear(CurrentFear + amount);
        }

        public void RegisterPlayerHit()
        {
            if (!IsBroken)
            {
                RemoveFear(config.playerHitLoss);
            }
        }

        public void SetDecayEnabled(bool enabledValue)
        {
            decayEnabled = enabledValue;
        }

        public void ResetFear()
        {
            FearState previousState = CurrentState;
            CurrentFear = 0f;
            CurrentState = FearState.Calm;
            timeSinceLastSuccessfulParry = 0f;
            decayEnabled = true;

            FearChanged?.Invoke(CurrentFear, MaxFear);

            if (previousState != CurrentState)
            {
                FearStateChanged?.Invoke(previousState, CurrentState);
            }
        }

        private void RemoveFear(float amount)
        {
            if (amount > 0f)
            {
                SetFear(CurrentFear - amount);
            }
        }

        private void SetFear(float value)
        {
            float previousFear = CurrentFear;
            FearState previousState = CurrentState;

            CurrentFear = Mathf.Clamp(value, 0f, MaxFear);
            CurrentState = EvaluateState(CurrentFear);

            if (!Mathf.Approximately(previousFear, CurrentFear))
            {
                FearChanged?.Invoke(CurrentFear, MaxFear);
            }

            if (previousState != CurrentState)
            {
                FearStateChanged?.Invoke(previousState, CurrentState);
            }

            if (previousState != FearState.Broken && CurrentState == FearState.Broken)
            {
                decayEnabled = false;
                Broken?.Invoke();
            }
        }

        private FearState EvaluateState(float fear)
        {
            if (fear >= MaxFear) return FearState.Broken;
            if (fear >= config.collapseThreshold) return FearState.Collapse;
            if (fear >= config.desperateThreshold) return FearState.Desperate;
            if (fear >= config.afraidThreshold) return FearState.Afraid;
            if (fear >= config.alertThreshold) return FearState.Alert;
            return FearState.Calm;
        }
    }
}
