using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class FPController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot; // Genelde Main Camera
    private CharacterController controller;
    [HideInInspector]
    public bool freezeMovement = false;


    [Header("Look")]
    public float mouseSensitivity = 1.2f;     // Genel hassasiyet carpani
    public float lookXMultiplier = 1f;        // Yaw
    public float lookYMultiplier = 1f;        // Pitch
    public float pitchMin = -85f;
    public float pitchMax = 85f;
    public bool lockCursorOnStart = true;
    public bool invertY = false;

    [Header("Move Speeds")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.5f;
    public float crouchSpeed = 2.5f;

    [Header("Move Feel")]
    public float acceleration = 18f;
    public float deceleration = 24f;
    public float airControl = 0.5f;           // Havada yatay kontrol
    public float gravity = -20f;
    public float jumpHeight = 1.2f;
    public float coyoteTime = 0.12f;          // Yerden kesildikten sonra kisa sure ziplamaya izin
    public float jumpBuffer = 0.12f;          // Ziplamayi erken basmayi tamponlar

    [Header("Grounding")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.28f;   // Cizimde kullaniliyor ama asagida bounds tan dinamik hesaplaniyor
    public float groundCheckOffset = 0.1f;    // (Gizmos icin kaldi)
    public float slopeSlideGravity = -35f;    // cok dik yamacta hafif kayma

    [Header("Crouch")]
    public KeyCode crouchToggleFallback = KeyCode.LeftControl;
    public float standHeight = 1.8f;
    public float crouchHeight = 1.1f;
    public float crouchSmooth = 12f;

    [Header("Head Bob (opsiyonel)")]
    public bool enableHeadBob = true;
    public float bobFrequency = 1.8f;
    public float bobAmplitude = 0.04f;

    [Header("Controller Tuning")]
    public bool autoCenter = true;      // CharacterController center otomatik ayarlansin mi
    public bool bottomAnchored = true;  // true: tabani sabit tut (height degisse de ayak yerde kalsin)

    [Header("Climb Settings")]
    public LayerMask climbableLayers;       // Tirmanilabilir objelerin layer lari
    public float maxClimbRemaining = 2f;    // Kalan yukseklik bunun ustundeyse tirmanma
    public float climbForwardOffset = 0.1f; // Tirmanirken ileri dogru hafif itme
    public float climbTime = 0.25f;         // Tirmanma animasyon suresi

    // Internal
    private float _pitch;
    private Vector3 _velocity;                // world-space
    private bool _isGrounded;
    private bool _isSprinting;
    private bool _isCrouching;
    private float _lastGroundedTime = -999f;
    private float _lastJumpPressedTime = -999f;
    private bool _jumpQueued = false;
    private float _bobTimer;
    private Vector3 _camLocalDefault;
    private float _baseBottomY = 0f;          // center.y - height/2 (taban referansi)
    private float yVelocity = 0f;
    private bool _headBobJustStarted = true;
    bool firstmove = true;

    private bool isClimbing = false;

#if ENABLE_INPUT_SYSTEM
    private Keyboard kb => Keyboard.current;
    private Mouse ms => Mouse.current;
#endif

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;
        if (cameraPivot != null)
            _camLocalDefault = cameraPivot.localPosition;

        _baseBottomY = controller.center.y - controller.height * 0.5f;

        if (lockCursorOnStart) LockCursor(true);
    }


    void Update()
    {
        if (!IsInventoryOpen && !freezeMovement)
        {
            HandleLook();
        }

        HandleCrouch();

        if (!isClimbing && JumpPressedThisFrame())
        {
            _jumpQueued = true;
            _lastJumpPressedTime = Time.time;
        }

        if (!isClimbing)
        {
            HandleMovement();
        }

        HandleHeadBob();
        HandleCursorToggle();
    }

    bool IsInventoryOpen
    {
        get
        {
            return InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;
        }
    }

    #region Look
    void HandleLook()
    {
        if (freezeMovement) return;

        Vector2 lookDelta = GetLookDelta();
        float invert = invertY ? 1f : -1f;

        transform.Rotate(Vector3.up, lookDelta.x * mouseSensitivity * lookXMultiplier);

        _pitch += lookDelta.y * mouseSensitivity * lookYMultiplier * invert;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        if (cameraPivot != null)
        {
            Vector3 e = cameraPivot.localEulerAngles;
            e.x = _pitch;
            cameraPivot.localEulerAngles = e;
        }
    }

    Vector2 GetLookDelta()
    {
        if (freezeMovement)
            return Vector2.zero;
        if (IsInventoryOpen)
            return Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (ms != null)
        {
            Vector2 md = ms.delta.ReadValue() * (ms.rightButton.isPressed ? 0f : 1f);
            return md * 0.1f;
        }
#endif
        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");
        return new Vector2(x, y);
    }
    #endregion

    #region Input
    Vector2 GetMoveInput()
    {
        if (freezeMovement)
            return Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (kb != null)
        {
            float x = 0f, y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            Vector2 v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }
#endif
        float h = Input.GetAxisRaw("Horizontal");
        float v2 = Input.GetAxisRaw("Vertical");
        Vector2 vv = new Vector2(h, v2);
        return vv.sqrMagnitude > 1f ? vv.normalized : vv;
    }

    bool JumpPressedThisFrame()
    {
        if (freezeMovement)
            return false;
#if ENABLE_INPUT_SYSTEM
        if (kb != null) return kb.spaceKey.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(KeyCode.Space);
    }

    bool SprintHeld()
    {
        if (freezeMovement)
            return false;
#if ENABLE_INPUT_SYSTEM
        if (kb != null) return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
#endif
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    bool CrouchHeld()
    {
        if (freezeMovement)
            return false;
#if ENABLE_INPUT_SYSTEM
        if (kb != null)
            return kb.leftCtrlKey.isPressed;
#endif
        return Input.GetKey(KeyCode.LeftControl);
    }

    #endregion

    #region Movement
    void HandleMovement()
    {
        if (isClimbing) return;

        Bounds b = controller.bounds;
        Vector3 feet = b.center + Vector3.down * (b.extents.y - 0.02f);
        float dynRadius = Mathf.Max(0.18f, controller.radius * 0.6f);
        _isGrounded = Physics.CheckSphere(feet, dynRadius, groundMask, QueryTriggerInteraction.Ignore) || controller.isGrounded;

        if (_isGrounded) _lastGroundedTime = Time.time;

        _isSprinting = SprintHeld() && !_isCrouching;
        float desiredSpeed = _isCrouching ? crouchSpeed : (_isSprinting ? sprintSpeed : walkSpeed);

        Vector2 input = GetMoveInput();
        Vector3 inputWorld = transform.TransformDirection(new Vector3(input.x, 0f, input.y));
        Vector3 desiredHorizontal = inputWorld.normalized * desiredSpeed;
        float desiredMag = desiredHorizontal.magnitude;
        bool hasInput = desiredMag > 0.01f;

        float accel = _isGrounded ? acceleration : acceleration * airControl;
        float decel = _isGrounded ? deceleration : deceleration * 0.5f;

        Vector3 currentHorizontal = new Vector3(_velocity.x, 0f, _velocity.z);

        // --- Direction change and braking tweak (less sliding) ---
        if (hasInput && currentHorizontal.sqrMagnitude > 0.0001f)
        {
            Vector3 curNorm = currentHorizontal.normalized;
            Vector3 desiredNorm = desiredHorizontal.normalized;
            float dot = Vector3.Dot(curNorm, desiredNorm);

            // If we are trying to move in almost opposite direction, apply strong brake first
            if (dot < 0f)
            {
                float reverseMult = _isGrounded ? 2.5f : 1.2f; // stronger on ground, softer in air
                float brake = decel * reverseMult;
                Vector3 brakeStep = Vector3.ClampMagnitude(-currentHorizontal, brake * Time.deltaTime);
                currentHorizontal += brakeStep;
            }
        }

        // Normal accel / decel
        if (hasInput)
        {
            Vector3 diff = desiredHorizontal - currentHorizontal;
            Vector3 change = Vector3.ClampMagnitude(diff, accel * Time.deltaTime);
            currentHorizontal += change;
        }
        else
        {
            Vector3 change = Vector3.ClampMagnitude(-currentHorizontal, decel * Time.deltaTime);
            currentHorizontal += change;

            // Small deadzone so we do not drift forever with tiny velocity
            if (currentHorizontal.magnitude < 0.05f)
                currentHorizontal = Vector3.zero;
        }

        currentHorizontal = ProjectOnGround(currentHorizontal);

        if (_isGrounded && _velocity.y < 0f) _velocity.y = -2f;
        else _velocity.y += gravity * Time.deltaTime;

        bool canJump =
            _jumpQueued &&
            Time.timeSinceLevelLoad >= 0.1f &&
            (Time.time - _lastGroundedTime) <= coyoteTime &&
            (Time.time - _lastJumpPressedTime) <= jumpBuffer;

        if (canJump)
        {
            _jumpQueued = false;
            _lastJumpPressedTime = -999f;
            _lastGroundedTime = -999f;
            _velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }

        if (!_isGrounded && IsOnSteepSlope(out Vector3 steepNormal))
        {
            Vector3 steepDown = Vector3.ProjectOnPlane(Vector3.down, steepNormal).normalized;
            currentHorizontal += steepDown * (Mathf.Abs(slopeSlideGravity) * 0.2f * Time.deltaTime);
        }

        _velocity.x = currentHorizontal.x;
        _velocity.z = currentHorizontal.z;

        controller.Move(_velocity * Time.deltaTime);
    }

    Vector3 ProjectOnGround(Vector3 vec)
    {
        if (GetGround(out RaycastHit hit))
            return Vector3.ProjectOnPlane(vec, hit.normal);
        return vec;
    }

    bool GetGround(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float rayLen = (controller.height * 0.5f) + 0.5f;
        return Physics.SphereCast(origin, controller.radius * 0.95f, Vector3.down, out hit, rayLen, groundMask, QueryTriggerInteraction.Ignore);
    }

    bool IsOnSteepSlope(out Vector3 normal)
    {
        normal = Vector3.up;
        if (GetGround(out RaycastHit hit))
        {
            normal = hit.normal;
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            return angle > controller.slopeLimit + 1f;
        }
        return false;
    }
    #endregion

    #region Climb

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isClimbing) return;
        if (freezeMovement) return;

        // Sadece tirmanilabilir layer
        if ((climbableLayers.value & (1 << hit.gameObject.layer)) == 0)
            return;

        // Tirmanma sadece dususte calissin (tepeyi gectikten sonra)
        if (_velocity.y >= 0f)
            return;

        // Hala grounded ise (duvara surtunurken) tirmanma baslatma
        if (_isGrounded)
            return;

        TryStartClimb(hit);
    }

    private void TryStartClimb(ControllerColliderHit hit)
    {
        Collider col = hit.collider;

        float topY = col.bounds.max.y;
        float contactY = hit.point.y;
        float remaining = topY - contactY;

        if (remaining <= 0f)
            return;

        if (remaining > maxClimbRemaining)
            return;

        float playerHalfHeight = transform.localScale.y * 0.5f;
        float climbUp = remaining + playerHalfHeight;

        Vector3 targetPos = transform.position;
        targetPos.y += climbUp;

        Vector3 forward = cameraPivot != null ? cameraPivot.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
            forward.Normalize();

        targetPos += forward * climbForwardOffset;

        StartCoroutine(ClimbRoutine(targetPos));
    }

    private System.Collections.IEnumerator ClimbRoutine(Vector3 targetPos)
    {
        isClimbing = true;
        freezeMovement = true;

        Vector3 startPos = transform.position;
        Vector3 oldVelocity = _velocity;
        _velocity = Vector3.zero;

        float t = 0f;

        while (t < climbTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / climbTime);

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, lerp);
            Vector3 delta = newPos - transform.position;
            controller.Move(delta);

            yield return null;
        }

        Vector3 finalDelta = targetPos - transform.position;
        controller.Move(finalDelta);

        _velocity = oldVelocity;
        _velocity.y = 0f;

        isClimbing = false;
        freezeMovement = false;
    }

    #endregion

    #region Crouch
    void HandleCrouch()
    {
        if (CrouchHeld())
        {
            _isCrouching = true;
        }
        else
        {
            if (CanStandUp())
                _isCrouching = false;
        }

        float targetH = _isCrouching ? crouchHeight : standHeight;
        float newH = Mathf.Lerp(controller.height, targetH, Time.deltaTime * crouchSmooth);
        controller.height = newH;

        if (autoCenter)
        {
            float newCenterY = bottomAnchored
                ? _baseBottomY + controller.height * 0.5f
                : controller.height * 0.5f;

            controller.center = new Vector3(0f, newCenterY, 0f);
        }

        if (cameraPivot != null)
        {
            float camTargetY = (_isCrouching ? 0.85f : 1.2f);
            Vector3 lp = cameraPivot.localPosition;
            lp.y = Mathf.Lerp(lp.y, camTargetY, Time.deltaTime * crouchSmooth);
            cameraPivot.localPosition = lp;
        }
    }


    bool CanStandUp()
    {
        float extra = (standHeight - controller.height) + 0.05f;
        Vector3 start = transform.position + Vector3.up * controller.height;
        return !Physics.SphereCast(start, controller.radius * 0.95f, Vector3.up, out _, extra, ~0, QueryTriggerInteraction.Ignore);
    }
    #endregion

    #region HeadBob
    void HandleHeadBob()
    {
        if (!enableHeadBob || cameraPivot == null) return;

        Vector3 horiz = new Vector3(_velocity.x, 0f, _velocity.z);
        bool moving = _isGrounded && horiz.magnitude > 0.2f;

        if (moving)
        {
            _bobTimer += Time.deltaTime * bobFrequency * (_isSprinting ? 1.35f : 1f);
            float offset = Mathf.Sin(_bobTimer * Mathf.PI * 2f) * bobAmplitude;
            float targetY = _camLocalDefault.y + offset;

            if (firstmove)
            {
                float smoothY = Mathf.SmoothDamp(
                    cameraPivot.localPosition.y,
                    targetY,
                    ref yVelocity,
                    0.08f
                );

                cameraPivot.localPosition = new Vector3(
                    _camLocalDefault.x,
                    smoothY,
                    _camLocalDefault.z
                );

                if (Mathf.Abs(smoothY - targetY) < 0.001f)
                {
                    firstmove = false;
                    cameraPivot.localPosition = new Vector3(
                        _camLocalDefault.x,
                        targetY,
                        _camLocalDefault.z
                    );
                }
            }
            else
            {
                cameraPivot.localPosition = new Vector3(
                    _camLocalDefault.x,
                    targetY,
                    _camLocalDefault.z
                );
            }
        }
        else
        {
            firstmove = true;
            yVelocity = 0f;
            _bobTimer = 0f;

            cameraPivot.localPosition = Vector3.Lerp(
                cameraPivot.localPosition,
                _camLocalDefault,
                Time.deltaTime * 10f
            );
        }
    }
    #endregion

    #region Cursor
    void HandleCursorToggle()
    {
        if (freezeMovement)
            return;
        if (IsInventoryOpen)
            return;
#if ENABLE_INPUT_SYSTEM
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            LockCursor(false);
#else
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(false);
#endif
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
    #endregion

    void OnDrawGizmosSelected()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        Bounds b = controller.bounds;
        Vector3 feet = b.center + Vector3.down * (b.extents.y - 0.02f);
        float dynRadius = Mathf.Max(0.18f, controller.radius * 0.6f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(feet, dynRadius);
    }

}
