using UnityEngine;

public class CarryTray : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;
    public Transform carryAnchor; // elde duracagi nokta (player icinde empty)
    public TrayCrateCollector collector;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.E;

    [Header("Pickup")]
    public float interactDistance = 2.5f;
    public LayerMask trayLayer; // tepsinin layer'i

    [Header("Drop")]
    public LayerMask groundLayer;
    public float dropRayHeight = 1.0f;
    public float maxDropDownDistance = 5f;

    [Header("Carry Pose")]
    public Vector3 carriedLocalPos = new Vector3(0f, -0.25f, 0.6f);
    public Vector3 carriedLocalEuler = new Vector3(0f, 0f, 0f);

    private bool isCarrying = false;

    // drop rotasyonunu korumak icin
    private Quaternion pickedWorldRotation;

    // tepsi rigidbody varsa kontrol
    private Rigidbody rb;
    private Collider[] allColliders;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        allColliders = GetComponentsInChildren<Collider>(true);
        if (collector == null) collector = GetComponentInChildren<TrayCrateCollector>();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (!isCarrying)
            {
                TryPickupTray();
            }
            else
            {
                DropTray();
            }
        }
    }

    void TryPickupTray()
    {
        if (playerCamera == null || carryAnchor == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactDistance, trayLayer, QueryTriggerInteraction.Ignore))
            return;

        // vurulan obje bu tepsi mi?
        CarryTray tray = hit.collider.GetComponentInParent<CarryTray>();
        if (tray == null) return;

        // baska bir tray scripti vurduysa onu kaldir
        tray.PickupInternal();
    }

    void PickupInternal()
    {
        if (isCarrying) return;

        isCarrying = true;
        pickedWorldRotation = transform.rotation;

        // fizik kapat
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // tepsi colliderlarini kapatmak genelde iyi (player ile carpismasin)
        SetCollidersEnabled(false);

        // elde anchor'a child
        transform.SetParent(carryAnchor, true);
        transform.localPosition = carriedLocalPos;
        transform.localRotation = Quaternion.Euler(carriedLocalEuler);
    }

    void DropTray()
    {
        if (!isCarrying) return;

        isCarrying = false;

        // parent'tan cikar
        transform.SetParent(null, true);

        // yere konum bul (player onune ray)
        Vector3 origin = playerCamera.transform.position + playerCamera.transform.forward * 1.2f;
        origin.y += dropRayHeight;

        Ray downRay = new Ray(origin, Vector3.down);
        RaycastHit hit;

        Vector3 dropPos = transform.position;
        if (Physics.Raycast(downRay, out hit, maxDropDownDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            dropPos = hit.point;
        }
        else
        {
            // zemin bulamazsa oldugu yerde birakir
            dropPos = origin + Vector3.down * 0.5f;
        }

        transform.position = dropPos;

        // aldigin rotasyonu koru
        transform.rotation = pickedWorldRotation;

        // fizik ac
        SetCollidersEnabled(true);
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    void SetCollidersEnabled(bool enabled)
    {
        if (allColliders == null) return;
        for (int i = 0; i < allColliders.Length; i++)
        {
            if (allColliders[i] == null) continue;
            // trigger collector collider'i acik kalsin istiyorsan burada filtreleyebilirsin
            allColliders[i].enabled = enabled;
        }
    }
}
