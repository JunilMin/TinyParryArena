using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class WeaponBodyClearance : MonoBehaviour
{
    [SerializeField] private Transform characterRoot;
    [SerializeField, Min(0.05f)] private float torsoRadius = 0.28f;
    [SerializeField, Min(0f)] private float maximumCorrection = 0.75f;
    [SerializeField, Min(1f)] private float correctionSpeed = 24f;

    private Vector3 baseLocalPosition;
    private Vector3 currentCorrection;

    public void Configure(Transform owner, bool isPlayer)
    {
        characterRoot = owner;
        torsoRadius = isPlayer ? 0.30f : 0.27f;
        baseLocalPosition = transform.localPosition;
    }

    public void SetClearance(float radius) => torsoRadius = Mathf.Max(0.05f, radius);

    private void Awake()
    {
        if (characterRoot == null)
            characterRoot = transform.root;
        baseLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (characterRoot == null)
            return;

        // Animator movement is evaluated before this component. Restore the
        // authored hand offset first so clearance correction never accumulates.
        transform.localPosition = baseLocalPosition;

        Vector3 desiredCorrection = CalculateCorrection();
        float blend = 1f - Mathf.Exp(-correctionSpeed * Time.deltaTime);
        currentCorrection = Vector3.Lerp(currentCorrection, desiredCorrection, blend);
        transform.position += currentCorrection;
    }

    private Vector3 CalculateCorrection()
    {
        if (!TryGetActiveWeaponAxis(out Vector3 weaponStart, out Vector3 weaponEnd))
            return Vector3.zero;

        Vector3 torsoBottom = characterRoot.position + Vector3.up * 0.48f;
        Vector3 torsoTop = characterRoot.position + Vector3.up * 1.48f;
        ClosestPointsOnSegments(weaponStart, weaponEnd, torsoBottom, torsoTop,
            out Vector3 weaponPoint, out Vector3 torsoPoint);

        Vector3 separation = weaponPoint - torsoPoint;
        separation.y = 0f;
        float distance = separation.magnitude;
        if (distance >= torsoRadius)
            return Vector3.zero;

        Vector3 outward = distance > 0.001f
            ? separation / distance
            : (Vector3.Dot(transform.position - characterRoot.position, characterRoot.right) >= 0f
                ? characterRoot.right
                : -characterRoot.right);
        outward.y = 0f;
        outward.Normalize();
        return outward * Mathf.Min(torsoRadius - distance, maximumCorrection);
    }

    private bool TryGetActiveWeaponAxis(out Vector3 start, out Vector3 end)
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(false);
        MeshFilter selected = null;
        float longest = 0f;
        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null || !filter.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = filter.sharedMesh.bounds;
            float length = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (length > longest)
            {
                longest = length;
                selected = filter;
            }
        }

        if (selected == null)
        {
            start = end = Vector3.zero;
            return false;
        }

        Bounds meshBounds = selected.sharedMesh.bounds;
        Vector3 axis = meshBounds.size.x >= meshBounds.size.y && meshBounds.size.x >= meshBounds.size.z
            ? Vector3.right * meshBounds.extents.x
            : meshBounds.size.y >= meshBounds.size.z
                ? Vector3.up * meshBounds.extents.y
                : Vector3.forward * meshBounds.extents.z;
        start = selected.transform.TransformPoint(meshBounds.center - axis);
        end = selected.transform.TransformPoint(meshBounds.center + axis);
        return true;
    }

    private static void ClosestPointsOnSegments(
        Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2,
        out Vector3 first, out Vector3 second)
    {
        Vector3 d1 = q1 - p1;
        Vector3 d2 = q2 - p2;
        Vector3 r = p1 - p2;
        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);
        float s;
        float t;

        if (a <= Mathf.Epsilon && e <= Mathf.Epsilon)
        {
            first = p1;
            second = p2;
            return;
        }

        if (a <= Mathf.Epsilon)
        {
            s = 0f;
            t = Mathf.Clamp01(f / e);
        }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= Mathf.Epsilon)
            {
                t = 0f;
                s = Mathf.Clamp01(-c / a);
            }
            else
            {
                float b = Vector3.Dot(d1, d2);
                float denominator = a * e - b * b;
                s = denominator > Mathf.Epsilon ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
                t = (b * s + f) / e;
                if (t < 0f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = Mathf.Clamp01((b - c) / a);
                }
            }
        }

        first = p1 + d1 * s;
        second = p2 + d2 * t;
    }
}
