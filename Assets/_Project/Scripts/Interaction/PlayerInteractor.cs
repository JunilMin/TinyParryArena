using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(PlayerController))]
public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField, Min(1f)] private float interactionRange = 3.2f;

    private TinyParryHUD hud;
    private IPlayerInteractable current;
    private readonly Collider[] nearbyColliders = new Collider[32];

    private void Start() => hud = FindFirstObjectByType<TinyParryHUD>();

    private void Update()
    {
        current = FindNearest();
        hud?.SetInteractionPrompt(current != null ? $"[E]  {current.InteractionPrompt}" : string.Empty);
        if (current != null && InteractPressed())
            current.Interact();
    }

    private IPlayerInteractable FindNearest()
    {
        IPlayerInteractable nearest = null;
        float nearestSqr = interactionRange * interactionRange;
        int count = Physics.OverlapSphereNonAlloc(transform.position, interactionRange, nearbyColliders, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Collider hit = nearbyColliders[i];
            if (hit == null) continue;
            IPlayerInteractable candidate = hit.GetComponentInParent<IPlayerInteractable>();
            if (candidate == null || !candidate.CanInteract || candidate.InteractionTransform == null)
                continue;

            Vector3 delta = candidate.InteractionTransform.position - transform.position;
            delta.y = 0f;
            float sqr = delta.sqrMagnitude;
            if (sqr > nearestSqr)
                continue;
            nearest = candidate;
            nearestSqr = sqr;
        }
        return nearest;
    }

    private static bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private void OnDisable()
    {
        current = null;
        hud?.SetInteractionPrompt(string.Empty);
    }
}
