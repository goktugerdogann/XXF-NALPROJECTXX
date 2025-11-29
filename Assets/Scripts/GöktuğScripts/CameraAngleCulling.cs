using UnityEngine;

public class CameraAngleCulling : MonoBehaviour
{
    [Header("Kamera Ayarlarý")]
    public Camera targetCamera;
    [Range(0f, 180f)]
    public float angleThreshold = 60f;   // kamera konisi
    public float maxDistance = 80f;      // bu mesafenin ötesi her türlü kapalý

    [Header("Kontrol Sýklýðý")]
    public float checkInterval = 0.1f;   // 0.1 = saniyede 10 kez
    private float _nextCheckTime = 0f;

    [Header("Ne Aç/Kapa Yapýlacak?")]
    public bool useSetActive = false;
    public Renderer[] renderersToToggle;
    public Behaviour[] componentsToToggle;

    // Kamera cache
    static Vector3 _camPos;
    static Vector3 _camForward;
    static bool _camCachedThisFrame = false;

    float _cosThreshold;
    float _maxDistanceSqr;
    bool _isActive = true;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (renderersToToggle == null || renderersToToggle.Length == 0)
        {
            var r = GetComponent<Renderer>();
            if (r != null)
                renderersToToggle = new Renderer[] { r };
        }

        _cosThreshold = Mathf.Cos(angleThreshold * Mathf.Deg2Rad);
        _maxDistanceSqr = maxDistance * maxDistance;

        ForceUpdateState();
    }

    void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        UpdateStateByAngleAndDistance();
    }

    void LateUpdate()
    {
        // her frame sonunda kamera cache flag’ini sýfýrla
        _camCachedThisFrame = false;
    }

    void CacheCameraDataIfNeeded()
    {
        if (_camCachedThisFrame) return;
        if (targetCamera == null) return;

        _camPos = targetCamera.transform.position;
        _camForward = targetCamera.transform.forward.normalized;
        _camCachedThisFrame = true;
    }

    public void ForceUpdateState()
    {
        UpdateStateByAngleAndDistance(true);
    }

    void UpdateStateByAngleAndDistance(bool force = false)
    {
        if (targetCamera == null) return;

        CacheCameraDataIfNeeded();

        Vector3 toObj = transform.position - _camPos;
        float sqrDist = toObj.sqrMagnitude;

        // Mesafe dýþýndaysa direkt kapat
        if (sqrDist > _maxDistanceSqr)
        {
            SetState(false, force);
            return;
        }

        Vector3 dirToObj = toObj.normalized;

        float dot = Vector3.Dot(_camForward, dirToObj);

        // Kamera arkasýndaysa kapat
        if (dot <= 0f)
        {
            SetState(false, force);
            return;
        }

        // Açý kontrolü: dot = cos(angle)
        bool visible = dot >= _cosThreshold;

        SetState(visible, force);
    }

    void SetState(bool state, bool force = false)
    {
        if (!force && state == _isActive) return;
        _isActive = state;

        if (useSetActive)
        {
            gameObject.SetActive(state);
            return;
        }

        if (renderersToToggle != null)
        {
            foreach (var r in renderersToToggle)
                if (r != null) r.enabled = state;
        }

        if (componentsToToggle != null)
        {
            foreach (var c in componentsToToggle)
                if (c != null) c.enabled = state;
        }
    }
}
