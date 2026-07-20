using System.Collections;
using TinyParryArena.Fear;
using UnityEngine;
using UnityEngine.UI;

namespace TinyParryArena.UI
{
    [DisallowMultipleComponent]
    public sealed class FearGaugeUI : MonoBehaviour
    {
        [SerializeField] private FearController fearController;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image flashImage;
        [SerializeField] private RectTransform gaugeRoot;

        [SerializeField] private bool autoFindFearController = true;
        [Min(0.1f)] [SerializeField] private float retryInterval = 0.25f;
        [SerializeField] private bool smoothFill = true;
        [Min(0f)] [SerializeField] private float fillSpeed = 2.75f;
        [Min(0f)] [SerializeField] private float pulseScale = 1.07f;
        [Min(0.01f)] [SerializeField] private float pulseDuration = 0.12f;
        [Min(0.01f)] [SerializeField] private float flashDuration = 0.10f;
        [Min(0.1f)] [SerializeField] private float collapseBlinkSpeed = 7f;

        private readonly Color calmColor = new(0.45f, 0.45f, 0.45f, 1f);
        private readonly Color alertColor = new(0.95f, 0.78f, 0.18f, 1f);
        private readonly Color afraidColor = new(1.00f, 0.48f, 0.10f, 1f);
        private readonly Color desperateColor = new(0.90f, 0.08f, 0.08f, 1f);
        private readonly Color collapseColor = new(1.00f, 0.02f, 0.02f, 1f);
        private readonly Color brokenColor = Color.white;

        private float targetFill;
        private float retryTimer;
        private FearState currentState = FearState.Calm;
        private Coroutine pulseRoutine;
        private Coroutine flashRoutine;
        private Vector3 baseScale = Vector3.one;
        private bool initialized;

        public void Initialize(
            Image background,
            Image fill,
            Image flash,
            RectTransform root)
        {
            backgroundImage = background;
            fillImage = fill;
            flashImage = flash;
            gaugeRoot = root;

            ConfigureVisuals();
            initialized = true;
        }

        private void Awake()
        {
            // Runtime bootstrap assigns references immediately after AddComponent.
            // Do not disable here because Awake runs before those assignments.
            if (gaugeRoot == null)
            {
                gaugeRoot = transform as RectTransform;
            }
        }

        private void Start()
        {
            if (!initialized)
            {
                ConfigureVisuals();
            }

            if (fillImage == null)
            {
                Debug.LogError("FearGaugeUI has no Fill Image.", this);
                enabled = false;
                return;
            }

            Bind(fearController);

            if (fearController == null && autoFindFearController)
            {
                TryFindFearController();
            }
        }

        private void ConfigureVisuals()
        {
            if (gaugeRoot == null)
            {
                gaugeRoot = transform as RectTransform;
            }

            if (gaugeRoot != null)
            {
                baseScale = gaugeRoot.localScale;
            }

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.fillAmount = 0f;
                fillImage.color = calmColor;
            }

            if (flashImage != null)
            {
                flashImage.color = new Color(1f, 1f, 1f, 0f);
            }

            targetFill = 0f;
        }

        private void OnDisable()
        {
            Unbind();

            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            if (flashRoutine != null) StopCoroutine(flashRoutine);

            pulseRoutine = null;
            flashRoutine = null;
        }

        private void Update()
        {
            if (fillImage == null)
            {
                return;
            }

            if (fearController == null && autoFindFearController)
            {
                retryTimer -= Time.unscaledDeltaTime;

                if (retryTimer <= 0f)
                {
                    retryTimer = retryInterval;
                    TryFindFearController();
                }
            }

            fillImage.fillAmount = smoothFill
                ? Mathf.MoveTowards(fillImage.fillAmount, targetFill, fillSpeed * Time.unscaledDeltaTime)
                : targetFill;

            if (currentState == FearState.Collapse)
            {
                float blink = 0.55f + Mathf.PingPong(Time.unscaledTime * collapseBlinkSpeed, 0.45f);
                Color color = collapseColor;
                color.a = blink;
                fillImage.color = color;
            }
        }

        public void Bind(FearController controller)
        {
            if (fearController == controller)
            {
                if (fearController != null)
                {
                    SetFill(fearController.CurrentFear, fearController.MaxFear);
                    SetVisualState(fearController.CurrentState);
                }
                return;
            }

            Unbind();
            fearController = controller;

            if (fearController == null)
            {
                targetFill = 0f;
                SetVisualState(FearState.Calm);
                return;
            }

            fearController.FearChanged += HandleFearChanged;
            fearController.FearStateChanged += HandleFearStateChanged;
            fearController.Broken += HandleBroken;

            SetFill(fearController.CurrentFear, fearController.MaxFear);
            SetVisualState(fearController.CurrentState);
        }

        private void Unbind()
        {
            if (fearController != null)
            {
                fearController.FearChanged -= HandleFearChanged;
                fearController.FearStateChanged -= HandleFearStateChanged;
                fearController.Broken -= HandleBroken;
            }

            fearController = null;
        }

        private void TryFindFearController()
        {
#if UNITY_2023_1_OR_NEWER
            FearController found = FindFirstObjectByType<FearController>();
#else
            FearController found = FindObjectOfType<FearController>();
#endif
            if (found != null)
            {
                Bind(found);
            }
        }

        private void HandleFearChanged(float currentFear, float maxFear)
        {
            float next = maxFear <= 0f ? 0f : Mathf.Clamp01(currentFear / maxFear);
            bool increased = next > targetFill;
            targetFill = next;

            if (increased)
            {
                PlayPulse(pulseScale, pulseDuration);
                PlayFlash(flashDuration);
            }
        }

        private void HandleFearStateChanged(FearState previous, FearState current)
        {
            SetVisualState(current);
        }

        private void HandleBroken()
        {
            SetVisualState(FearState.Broken);
            PlayPulse(1.12f, 0.18f);
            PlayFlash(0.18f);
        }

        private void SetFill(float currentFear, float maxFear)
        {
            targetFill = maxFear <= 0f ? 0f : Mathf.Clamp01(currentFear / maxFear);
        }

        private void SetVisualState(FearState state)
        {
            currentState = state;

            if (fillImage == null)
            {
                return;
            }

            fillImage.color = state switch
            {
                FearState.Alert => alertColor,
                FearState.Afraid => afraidColor,
                FearState.Desperate => desperateColor,
                FearState.Collapse => collapseColor,
                FearState.Broken => brokenColor,
                _ => calmColor
            };
        }

        private void PlayPulse(float scale, float duration)
        {
            if (gaugeRoot == null) return;
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine(scale, duration));
        }

        private IEnumerator PulseRoutine(float scale, float duration)
        {
            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                gaugeRoot.localScale = Vector3.Lerp(baseScale, baseScale * scale, elapsed / half);
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                gaugeRoot.localScale = Vector3.Lerp(baseScale * scale, baseScale, elapsed / half);
                yield return null;
            }

            gaugeRoot.localScale = baseScale;
            pulseRoutine = null;
        }

        private void PlayFlash(float duration)
        {
            if (flashImage == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(duration));
        }

        private IEnumerator FlashRoutine(float duration)
        {
            Color color = flashImage.color;
            color.a = 0.9f;
            flashImage.color = color;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(0.9f, 0f, elapsed / duration);
                flashImage.color = color;
                yield return null;
            }

            color.a = 0f;
            flashImage.color = color;
            flashRoutine = null;
        }
    }
}
