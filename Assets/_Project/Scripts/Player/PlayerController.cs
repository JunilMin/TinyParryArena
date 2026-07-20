using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CombatantHealth))]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 6.5f;
    [SerializeField] private float rotationSpeed = 14f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float fallResetY = -8f;
    [SerializeField] private Vector3 fallbackSpawn = new Vector3(0f, 0.15f, -14f);

    [Header("Dash / Dodge")]
    [SerializeField] private float dashDistance = 4.1f;
    [SerializeField] private float dashDuration = 0.22f;
    [SerializeField] private float dodgeInvulnerability = 0.16f;
    [SerializeField] private float dashCooldown = 0.55f;

    private CharacterController controller;
    private CombatantHealth health;
    private float verticalVelocity;
    private bool controlsEnabled = true;
    private bool dashing;
    private float nextDashTime;
    private Vector3 lastMoveDirection = Vector3.forward;

    public bool IsDashing => dashing;
    public void SetControlsEnabled(bool value) => controlsEnabled = value;
    public void ResetVerticalMotion() => verticalVelocity = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<CombatantHealth>();
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (transform.position.y < fallResetY)
        {
            controller.enabled = false;
            transform.position = fallbackSpawn;
            controller.enabled = true;
            verticalVelocity = 0f;
        }

        if (!controlsEnabled || dashing) return;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Vector2 input = ReadMoveInput();
        bool run = ReadRunInput();

        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * input.y + right * input.x;
        if (move.sqrMagnitude > 1f) move.Normalize();
        if (move.sqrMagnitude > 0.001f) lastMoveDirection = move.normalized;

        if (DashPressed() && Time.time >= nextDashTime)
        {
            Vector3 dashDirection = move.sqrMagnitude > 0.001f ? move.normalized : transform.forward;
            StartCoroutine(DashRoutine(dashDirection));
            return;
        }

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * (run ? runSpeed : walkSpeed);
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private IEnumerator DashRoutine(Vector3 direction)
    {
        dashing = true;
        nextDashTime = Time.time + dashCooldown;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = lastMoveDirection;
        direction.Normalize();
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        health.SetInvincible(true);
        float elapsed = 0f;
        float invulnerabilityElapsed = 0f;
        while (elapsed < dashDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            invulnerabilityElapsed += dt;
            if (invulnerabilityElapsed >= dodgeInvulnerability)
                health.SetInvincible(false);

            float normalized = Mathf.Clamp01(elapsed / dashDuration);
            float speedFactor = 1f - normalized * 0.35f;
            controller.Move(direction * (dashDistance / dashDuration) * speedFactor * dt);
            yield return null;
        }
        health.SetInvincible(false);
        dashing = false;
    }

    private static Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k == null) return Vector2.zero;
        Vector2 result = Vector2.zero;
        if (k.wKey.isPressed) result.y += 1f;
        if (k.sKey.isPressed) result.y -= 1f;
        if (k.dKey.isPressed) result.x += 1f;
        if (k.aKey.isPressed) result.x -= 1f;
        return result.normalized;
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
    }

    private static bool ReadRunInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }

    private static bool DashPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && k.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}
