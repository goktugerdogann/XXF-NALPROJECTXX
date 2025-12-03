using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    [Header("References")]
    public Transform pivot;          // genelde HeadPivot veya Playerin kafasi

    [Header("Distances")]
    public float collisionRadius = 0.25f;  // sphere yaricapi
    public float smooth = 10f;             // kameranin gecisi ne kadar yumusak olsun

    [Header("Layers")]
    public LayerMask collisionMask;  // duvarlar, objeler vs. (Player layer HARIC)

    private Vector3 _defaultLocalPos;

    void Awake()
    {
        // kameranin sahnedeki default local pozisyonunu kaydet
        _defaultLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (pivot == null)
            return;

        // Kamera normalde nerede durmak istiyor?
        Vector3 desiredWorldPos = pivot.TransformPoint(_defaultLocalPos);
        Vector3 origin = pivot.position;

        Vector3 dir = desiredWorldPos - origin;
        float dist = dir.magnitude;
        if (dist <= 0.0001f)
            return;

        dir /= dist;

        float targetDist = dist;

        // SphereCast ile yolumuzda collider var mi bak
        if (Physics.SphereCast(
                origin,
                collisionRadius,
                dir,
                out RaycastHit hit,
                dist,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            // Carpilan noktanin biraz onunde dur
            targetDist = hit.distance - collisionRadius;
        }

        // eger kisa mesafe kaldiysa 0in altina dusmesin
        targetDist = Mathf.Max(0.0f, targetDist);

        Vector3 targetWorldPos = origin + dir * targetDist;

        // Kamerayi yumusakca oraya cek
        transform.position = Vector3.Lerp(
            transform.position,
            targetWorldPos,
            Time.deltaTime * smooth
        );
    }
}
