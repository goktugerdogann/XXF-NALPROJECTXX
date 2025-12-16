using System.Collections;
using UnityEngine;

public class ShoppingCartFollowController : MonoBehaviour
{
    [Header("Refs")]
    public FPController fp;
    public Transform cameraPivot;

    [Tooltip("Child root that must NOT change (local transform stays constant).")]
    public Transform cartRoot;

    [Header("Keys")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Snap Animation (PLAYER ONLY)")]
    public float snapDuration = 0.55f;
    public AnimationCurve snapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Look Target Tuning (CAMERA ONLY)")]
    public float lookDownOffset = 0.15f;
    public float pitchClampMin = -70f;
    public float pitchClampMax = 70f;

    [Header("Cart Follow (MOVES THIS OBJECT - MOVER)")]
    public Vector3 followOffset = new Vector3(0f, 0f, 1.5f);
    public float followPosLerp = 10f;
    public float followYawLerp = 12f;
    public bool lockY = true;

    [Header("Pitch Clamp While Controlling (FPController)")]
    public bool clampPitchWhileControlling = true;
    public float pitchMinClamp = -25f;
    public float pitchMaxClamp = 35f;

    private CartTargetZone _zone;
    private bool _inControl = false;
    private bool _isSnapping = false;
    private Coroutine _snapRoutine;

    private float _lockedY;
    [Header("Physics Proxy")]
    public Transform physicsProxy;   // CartPhysicsProxy transform
    public float visualSmoothTime = 0.05f;
    private Vector3 _visVel;

    public void SetCurrentZone(CartTargetZone zone) => _zone = zone;
    public void ClearZone(CartTargetZone zone) { if (_zone == zone) _zone = null; }

    void Awake()
    {
        if (cartRoot == null)
        {
            Debug.LogError("cartRoot not assigned! Assign HandCartRoot (child).");
            enabled = false;
            return;
        }

        _lockedY = transform.position.y;
    }

    void Update()
    {
        if (fp == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (_isSnapping) return;

            if (!_inControl)
            {
                if (_zone != null && _zone.snapPoint != null)
                {
                    if (_snapRoutine != null) StopCoroutine(_snapRoutine);
                    _snapRoutine = StartCoroutine(SnapPlayerAndEnterControl());
                }
            }
            else
            {
                ExitControl();
            }
        }

        if (_inControl && !_isSnapping)
            UpdateMoverFollow();
    }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }

    IEnumerator SnapPlayerAndEnterControl()
    {
        if (_zone == null || _zone.snapPoint == null) yield break;
        if (fp.controller == null) yield break;

        _isSnapping = true;

        fp.freezeMovement = true;
        fp.ForceStopMotor();

        Vector3 startPos = fp.transform.position;
        Vector3 endPos = _zone.snapPoint.position;
        endPos.y = startPos.y; // player Y degisme

        float startYaw = fp.GetYaw();
        float endYaw = _zone.snapPoint.eulerAngles.y;

        float startPitch = NormalizeAngle(cameraPivot != null ? cameraPivot.localEulerAngles.x : fp.GetPitch());
        float targetPitch = startPitch;

        // Look target varsa pitch’i ona dogru ayarla (sadece snap animasyonu icin)
        if (_zone.lookTarget != null && cameraPivot != null)
        {
            Vector3 camPos = cameraPivot.position;
            Vector3 tgt = _zone.lookTarget.position + Vector3.down * Mathf.Max(0f, lookDownOffset);
            Vector3 dir = (tgt - camPos).normalized;

            float horizontal = Mathf.Sqrt(dir.x * dir.x + dir.z * dir.z);
            float pitchDeg = -Mathf.Atan2(dir.y, horizontal) * Mathf.Rad2Deg;

            pitchDeg = Mathf.Clamp(pitchDeg, pitchClampMin, pitchClampMax);
            targetPitch = Mathf.Clamp(pitchDeg, fp.pitchMin, fp.pitchMax);
        }

        float t = 0f;
        Vector3 lastPos = startPos;

        while (t < snapDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, snapDuration));
            float k = snapCurve.Evaluate(u);

            // Position: controller.Move ile snap
            Vector3 desired = Vector3.Lerp(startPos, endPos, k);
            Vector3 delta = desired - lastPos;
            fp.controller.Move(delta);
            lastPos = desired;

            // Yaw: snap point’e cevir
            float yaw = Mathf.LerpAngle(startYaw, endYaw, k);
            fp.SetYawExternal(yaw);

            // Pitch: snap sirasinda yazariz ama sonunda FP’ye sync edecegiz
            float pitch = Mathf.Lerp(startPitch, targetPitch, k);
            if (cameraPivot != null)
            {
                Vector3 e = cameraPivot.localEulerAngles;
                e.x = pitch;
                cameraPivot.localEulerAngles = e;
            }

            yield return null;
        }

        // Snap bitti: FP’nin internal pitch/yaw degerlerini kamera ile senkronla
        float finalYaw = fp.GetYaw();
        float finalPitch = (cameraPivot != null)
            ? NormalizeAngle(cameraPivot.localEulerAngles.x)
            : targetPitch;

        // Kontrol modunda pitch clamp acacaksak, simdiden clamp’le
        if (clampPitchWhileControlling)
            finalPitch = Mathf.Clamp(finalPitch, pitchMinClamp, pitchMaxClamp);

        fp.ForceSetYawPitch(finalYaw, finalPitch);

        EnterControl();

        if (lockY) _lockedY = transform.position.y;

        fp.freezeMovement = false;
        _isSnapping = false;
    }

    void UpdateMoverFollow()
    {
        // hedef pozisyonu hesapla (same as before)
        Vector3 flatForward = fp.transform.forward; flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
        flatForward.Normalize();

        Vector3 flatRight = fp.transform.right; flatRight.y = 0f;
        if (flatRight.sqrMagnitude < 0.0001f) flatRight = Vector3.right;
        flatRight.Normalize();

        Vector3 desiredPos =
            fp.transform.position +
            flatRight * followOffset.x +
            flatForward * followOffset.z;

        if (lockY) desiredPos.y = _lockedY;

        // 1) Visual mover (this object) target = desiredPos (güzel his)
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _visVel, visualSmoothTime);

        float targetYaw = fp.transform.eulerAngles.y;
        float yaw = Mathf.LerpAngle(transform.eulerAngles.y, targetYaw, Time.deltaTime * followYawLerp);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // 2) Physics proxy varsa: onu bizim vizyona yaklaþtýran script zaten var (target = this.transform)
        // Burada ekstra bir þey yapmana gerek yok.
    }


void EnterControl()
    {
        _inControl = true;

        if (fp != null)
        {
            fp.blockJumpExternal = true;

            // YAW SERBEST — sadece pitch clamp
            if (clampPitchWhileControlling)
                fp.EnterExternalPitchClamp(pitchMinClamp, pitchMaxClamp);
        }
    }

    void ExitControl()
    {
        _inControl = false;

        if (fp != null)
        {
            fp.blockJumpExternal = false;

            if (clampPitchWhileControlling)
                fp.ExitExternalPitchClamp();
        }
    }
}
