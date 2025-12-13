using System.Collections.Generic;
using UnityEngine;

public class CrateCarrier : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;
    public Transform carryAnchor;

    [Header("Scan")]
    public float interactDistance = 3f;
    public LayerMask zoneMask = ~0;

    [Header("Keys")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("Carry Layout")]
    public float spacingX = 0.35f;
    public float spacingZ = 0.35f;
    public int perRow = 3;
    public float liftY = 0.15f;

    [Header("Drop")]
    public LayerMask groundLayer;
    public float dropForward = 1.5f;
    public float dropYOffset = 0.02f;

    [Header("Debug")]
    public bool debugLog = false;

    private bool carrying = false;
    private readonly List<FishCrate> carried = new List<FishCrate>();
    private readonly List<Rigidbody> carriedRBs = new List<Rigidbody>();
    private readonly List<Collider[]> carriedCols = new List<Collider[]>();

    void Start()
    {
        if (playerCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null) playerCamera = cam;
            else playerCamera = FindFirstObjectByType<Camera>();
        }

        if (carryAnchor == null && playerCamera != null)
        {
            Transform t = playerCamera.transform.Find("HoldPoint");
            if (t != null) carryAnchor = t;
            else carryAnchor = playerCamera.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (!carrying) TryPickupFromZone();
            else DropAll();
        }
    }

    void TryPickupFromZone()
    {
        if (playerCamera == null) return;

        // Senin placement/pickup sistemi ile cakismasin
        InteractionRaycaster ray = FindObjectOfType<InteractionRaycaster>();
        if (ray != null && (ray.HasHeldWorldPreview() || ray.HasActiveInventoryPreview))
        {
            if (debugLog) Debug.Log("[CrateCarrier] blocked by InteractionRaycaster preview");
            return;
        }

        Ray r = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // IMPORTANT: RaycastAll -> kutuya carpsa bile zone'u bul
        RaycastHit[] hits = Physics.RaycastAll(
            r,
            interactDistance,
            zoneMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
        {
            if (debugLog) Debug.Log("[CrateCarrier] no hits");
            return;
        }

        // En yakinlardan baslayarak zone ara
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        CrateLoadingZone zone = null;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null) continue;

            zone = col.GetComponentInParent<CrateLoadingZone>();
            if (zone != null) break;
        }

        if (zone == null)
        {
            if (debugLog)
            {
                Debug.Log("[CrateCarrier] hits var ama zone yok. Ilk hit: " +
                          (hits[0].collider != null ? hits[0].collider.name : "null"));
            }
            return;
        }

        List<FishCrate> list = zone.GetCrates();
        if (list == null || list.Count == 0)
        {
            if (debugLog) Debug.Log("[CrateCarrier] zone bulundu ama crate yok");
            return;
        }

        carrying = true;

        carried.Clear();
        carriedRBs.Clear();
        carriedCols.Clear();

        for (int i = 0; i < list.Count; i++)
        {
            FishCrate c = list[i];
            if (c == null) continue;

            carried.Add(c);

            Rigidbody rb = c.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
                carriedRBs.Add(rb);
            }
            else carriedRBs.Add(null);

            Collider[] cols = c.GetComponentsInChildren<Collider>(true);
            carriedCols.Add(cols);
            for (int k = 0; k < cols.Length; k++)
                if (cols[k] != null) cols[k].enabled = false;

            c.transform.SetParent(carryAnchor, true);
        }

        LayoutCarried();

        if (debugLog) Debug.Log("[CrateCarrier] PICKUP ok. Count: " + carried.Count);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    void LayoutCarried()
    {
        for (int i = 0; i < carried.Count; i++)
        {
            FishCrate c = carried[i];
            if (c == null) continue;

            int row = i / perRow;
            int col = i % perRow;

            float x = (col - (perRow - 1) * 0.5f) * spacingX;
            float z = row * spacingZ;

            c.transform.localPosition = new Vector3(x, liftY, z);
            c.transform.localRotation = Quaternion.identity;
        }
    }

    void DropAll()
    {
        if (carried.Count == 0)
        {
            carrying = false;
            return;
        }

        if (playerCamera == null)
        {
            carrying = false;
            carried.Clear();
            carriedRBs.Clear();
            carriedCols.Clear();
            return;
        }

        Vector3 basePos = playerCamera.transform.position + playerCamera.transform.forward * dropForward;
        Vector3 dropPos = basePos;

        RaycastHit hit;
        if (Physics.Raycast(basePos + Vector3.up * 2f, Vector3.down, out hit, 6f, groundLayer, QueryTriggerInteraction.Ignore))
            dropPos = hit.point + Vector3.up * dropYOffset;

        for (int i = 0; i < carried.Count; i++)
        {
            FishCrate c = carried[i];
            if (c == null) continue;

            int row = i / perRow;
            int col = i % perRow;

            float x = (col - (perRow - 1) * 0.5f) * spacingX;
            float z = row * spacingZ;

            c.transform.SetParent(null, true);
            c.transform.position = dropPos + new Vector3(x, 0f, z);

            Collider[] cols = carriedCols[i];
            if (cols != null)
            {
                for (int k = 0; k < cols.Length; k++)
                    if (cols[k] != null) cols[k].enabled = true;
            }

            Rigidbody rb = carriedRBs[i];
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        carried.Clear();
        carriedRBs.Clear();
        carriedCols.Clear();
        carrying = false;

        if (debugLog) Debug.Log("[CrateCarrier] DROP ok");

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    public bool IsCarryingCrates()
    {
        return carrying;
    }
}
