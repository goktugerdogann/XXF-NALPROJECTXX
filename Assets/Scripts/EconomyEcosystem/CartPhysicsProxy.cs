using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CartPhysicsProxyFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;     // CartMover (ShoppingCartFollowController'ýn oldugu obje)
    public bool lockY = true;
    public float lockedY;

    [Header("Tight Follow")]
    public float maxSpeed = 25f;          // m/s (buyuk -> gecikme azalir)
    public float accel = 120f;            // hizlanma (buyuk -> daha “yapisik”)
    public float stopDistance = 0.001f;   // hedefe gelince titremesin

    [Header("Rotation")]
    public bool followYaw = true;
    public float yawSpeed = 1440f;        // deg/sec

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // IMPORTANT: dynamic olsun ki collision response olsun
        rb.isKinematic = false;

        // devrilmesin
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (lockY) lockedY = transform.position.y;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position;
        if (lockY) desired.y = lockedY;

        Vector3 pos = rb.position;
        Vector3 delta = desired - pos;

        // hedefe cok yakinsa titremeyi kes
        if (delta.sqrMagnitude <= stopDistance * stopDistance)
        {
            rb.velocity = Vector3.zero;
            rb.MovePosition(desired);
        }
        else
        {
            // velocity tabanli "tight follow" (lag yok)
            Vector3 desiredVel = delta.normalized * Mathf.Min(maxSpeed, delta.magnitude / Time.fixedDeltaTime);
            rb.velocity = Vector3.MoveTowards(rb.velocity, desiredVel, accel * Time.fixedDeltaTime);
        }

        if (followYaw)
        {
            float currentYaw = rb.rotation.eulerAngles.y;
            float targetYaw = target.eulerAngles.y;
            float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, yawSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
        }
    }
}
