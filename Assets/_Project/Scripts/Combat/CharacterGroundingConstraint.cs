using UnityEngine;

[DefaultExecutionOrder(1100)]
public sealed class CharacterGroundingConstraint : MonoBehaviour
{
    [SerializeField] private Transform characterRoot;
    [SerializeField, Min(1f)] private float correctionSpeed = 30f;
    [SerializeField, Min(0.01f)] private float maximumStep = 0.35f;
    [SerializeField, Min(0f)] private float footPlantDepth = 0.10f;

    public void Configure(Transform owner) => characterRoot = owner;

    private void LateUpdate()
    {
        if (characterRoot == null)
            return;

        if (!TryGetGroundHeight(out float groundHeight))
            return;

        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers.Length == 0)
            return;

        float lowest = float.PositiveInfinity;
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                lowest = Mathf.Min(lowest, renderer.bounds.min.y);
        }
        if (float.IsPositiveInfinity(lowest))
            return;

        float difference = Mathf.Clamp(groundHeight - footPlantDepth - lowest, -maximumStep, maximumStep);
        float blend = 1f - Mathf.Exp(-correctionSpeed * Time.deltaTime);
        transform.position += Vector3.up * difference * blend;
    }

    private bool TryGetGroundHeight(out float height)
    {
        Vector3 origin = characterRoot.position + Vector3.up * 2.5f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
        height = float.NegativeInfinity;
        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;
            if (hitTransform == characterRoot || hitTransform.IsChildOf(characterRoot))
                continue;
            // Never snap to a ceiling, weapon or platform above the character.
            if (hit.point.y > characterRoot.position.y + 0.5f)
                continue;
            if (hit.point.y > height)
                height = hit.point.y;
        }
        return !float.IsNegativeInfinity(height);
    }
}
