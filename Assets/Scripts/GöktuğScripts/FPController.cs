using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class FPController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot; // Usually Main Camera

    public CharacterController controller;
    [HideInInspector]
    public bool freezeMovement = false;

    [Header("Look")]
    public float mouseSensitivity = 1.2f;
    public float lookXMultiplier = 1f;
    public float lookYMultiplier = 1f;
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
    public float airControl = 0.5f;
    public float gravity = -20f;
    public float jumpHeight = 1.2f;
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.12f;

    [Header("Grounding")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.28f;
    public float groundCheckOffset = 0.1f;
    public float slopeSlideGravity = -35f;

    [Header("Crouch")]
    public KeyCode crouchToggleFallback = KeyCode.LeftControl;
    public float standHeight = 1.8f;
    public float crouchHeight = 1.1f;
    public float crouchSmooth = 12f;

    [Header("Head Bob (optional)")]
    public bool enableHeadBob = true;
    public float bobFrequency = 1.8f;
    public float bobAmplitude = 0.04f;

    [Header("Controller Tuning")]
    public bool autoCenter = true;
    public bool bottomAnchored = true;

    [Header("Climb Settings")]
    public LayerMask climbableLayers;
    public float maxClimbRemaining = 2f;
    public float climbForwardOffset = 0.1f;
    public float climbTime = 0.25f;

    [Header("Climb Camera")]
    public bool enableClimbCameraAnimation = true;
    public float climbForwardLean = -10f;
    public float climbShakeAmount = 0.01f;

    [Header("Climb Angle Limits")]
    public float climbPitchMin = -45f;
    public float climbPitchMax = 45f;

    [Header("Ladder")]
    public LayerMask ladderMask;
    public float ladderSpeed = 3f;          // W / S speed
    public float ladderSideSpeed = 2.5f;    // A / D speed on ladder
    public float ladderAttachDistance = 0.6f;
    public float ladderSurfaceOffset = 0.2f;

    [Header("Ladder Rays")]
    public float ladderRayLength = 1.0f;        // length of ladder rays
    public float ladderExitRayOffset = 0.25f;   // from feet up (exit ray)
    public float ladderControlRayOffset = 0.0f; // from feet (control ray)

    [Header("Ladder Fall Settings")]
    public float ladderYawRange = 100f; // allowed yaw range in degrees (-range to +range)

    // Ladder state
    private bool isOnLadder = false;
    private RaycastHit ladderHit;
    private float ladderTopY;
    private float ladderBottomY;

    [Header("Ladder Look Limits (debug only)")]
    public float ladderYawAttach = 40f;
    public float ladderYawDetach = 55f;
    public float ladderPitchMin = -45f;
    public float ladderPitchMax = 60f;

    [Header("Ladder Camera")]
    public bool enableLadderCameraAnimation = false; // default OFF so camera does not auto move
    public float ladderStepInterval = 0.35f;
    public float ladderStepDuration = 0.25f;
    public float ladderStepPitchAmplitude = 4f;
    public float ladderStepVerticalAmplitude = 0.02f;
    public float ladderStepRollAmplitude = 1.2f;

    [Header("Ladder Debug")]
    public bool debugLadderAngles = true;
    public float currentLadderAngle = 0f;
    public float currentLadderPitch = 0f;

    // re-attach block timer
    private float _ladderReattachBlockTimer = 0f;

    [Header("Ladder Top Exit")]
    public float ladderTopExitDuration = 0.2f;  // smooth top exit duration
    public float ladderTopExtraForward = 0.6f;  // extra forward offset on top
    public float ladderTopExtraUp = 0.15f;      // small extra up on top
    public float ladderStepForwardAmplitude = 0.06f;
        [Header("Wall Block (camera clipping)")]
    public float wallBlockDistance = 0.25f;   // kafa ile duvar arasindaki min mesafe
    public LayerMask wallMask;               // duvar / zemin / map layerlari (Player HARIC)


    // YENI: ileri dogru cekme miktari

    private bool _isExitingLadderTop = false;
    private Vector3 _ladderExitStartPos;
    private Vector3 _ladderExitEndPos;
    private float _ladderExitTimer = 0f;
    private Vector3 _ladderExitCamStartLocalPos;
    private Vector3 _ladderExitCamStartLocalEuler;

    [HideInInspector]
    public bool isOnMovingPlatform = false;
    [HideInInspector]
    public Vector3 platformMoveDelta = Vector3.zero;

    // Internal
    private float _pitch;
    private Vector3 _velocity;
    private bool _isGrounded;
    private bool _isSprinting;
    private bool _isCrouching;
    private float _lastGroundedTime = -999f;
    private float _lastJumpPressedTime = -999f;
    private bool _jumpQueued = false;
    private float _bobTimer;
    private Vector3 _camLocalDefault;
    private float _baseBottomY = 0f;
    private float yVelocity = 0f;
    bool firstmove = true;

    public float climbPhase1TargetPitch = 40f;

    private bool isClimbing = false;

    // Ladder camera step state
    private Vector2 _ladderInput;
    private float _ladderStepProgress = -1f;
    private float _ladderStepCooldown = 0f;
    private int _ladderStepSign = 1;

#if ENABLE_INPUT_SYSTEM
    private Keyboard kb => Keyboard.current;
    private Mouse ms => Mouse.current;
#endif

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraPivot != null)
            _camLocalDefault = cameraPivot.localPosition;

        _baseBottomY = controller.center.y - controller.height * 0.5f;

        if (lockCursorOnStart) LockCursor(true);
    }

    void Update()
    {
        if (_ladderReattachBlockTimer > 0f)
            _ladderReattachBlockTimer -= Time.deltaTime;

        if (!IsInventoryOpen && !freezeMovement)
        {
            HandleLook();
        }

        HandleCrouch();

        if (!isClimbing && !isOnLadder && JumpPressedThisFrame())
        {
            _jumpQueued = true;
            _lastJumpPressedTime = Time.time;
        }

        if (!isClimbing)
        {
            HandleMovement();
        }

        HandleHeadBob();
        HandleLadderCamera();
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
        if (_isExitingLadderTop)
        {
            UpdateLadderTopExit();
            return;
        }

        if (isClimbing) return;

        Bounds b = controller.bounds;
        Vector3 feet = b.center + Vector3.down * (b.extents.y - 0.02f);
        float dynRadius = Mathf.Max(0.18f, controller.radius * 0.6f);
        _isGrounded = Physics.CheckSphere(feet, dynRadius, groundMask, QueryTriggerInteraction.Ignore) || controller.isGrounded;

        if (_isGrounded && !isOnLadder)
            _lastGroundedTime = Time.time;

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

        if (!isOnLadder)
        {
            if (hasInput && currentHorizontal.sqrMagnitude > 0.0001f)
            {
                Vector3 curNorm = currentHorizontal.normalized;
                Vector3 desiredNorm = desiredHorizontal.normalized;
                float dot = Vector3.Dot(curNorm, desiredNorm);

                if (dot < 0f)
                {
                    float reverseMult = _isGrounded ? 2.5f : 1.2f;
                    float brake = decel * reverseMult;
                    Vector3 brakeStep = Vector3.ClampMagnitude(-currentHorizontal, brake * Time.deltaTime);
                    currentHorizontal += brakeStep;
                }
            }

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

                if (currentHorizontal.magnitude < 0.05f)
                    currentHorizontal = Vector3.zero;
            }

            currentHorizontal = ProjectOnGround(currentHorizontal);
        }
        else
        {
            currentHorizontal = Vector3.zero;
        }

        HandleLadder(input, ref currentHorizontal);

        if (!isOnLadder && !_isExitingLadderTop)
        {
            if (_isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }
            else
            {
                _velocity.y += gravity * Time.deltaTime;
            }

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
        }

        _velocity.x = currentHorizontal.x;
        _velocity.z = currentHorizontal.z;

        Vector3 playerMovement = _velocity * Time.deltaTime;

        if (isOnMovingPlatform)
        {
            _velocity.y = -1f;
            _isGrounded = true;
            _lastGroundedTime = Time.time;
            playerMovement += platformMoveDelta;
        }

        controller.Move(playerMovement);
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

    #region Ladder
    void UpdateLadderTopExit()
    {
        if (!_isExitingLadderTop)
            return;

        _ladderExitTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_ladderExitTimer / Mathf.Max(0.0001f, ladderTopExitDuration));

        // move player
        transform.position = Vector3.Lerp(_ladderExitStartPos, _ladderExitEndPos, t);

        // simple camera anim
        if (cameraPivot != null)
        {
            Vector3 targetCamPos = _camLocalDefault;
            targetCamPos.y += 0.03f;

            cameraPivot.localPosition = Vector3.Lerp(
                _ladderExitCamStartLocalPos,
                targetCamPos,
                t
            );

            float targetPitch = _pitch - 6f;
            Vector3 euler = cameraPivot.localEulerAngles;
            float startPitch = NormalizeAngle(_ladderExitCamStartLocalEuler.x);
            float newPitch = Mathf.LerpAngle(startPitch, targetPitch, t);
            euler.x = newPitch;
            cameraPivot.localEulerAngles = euler;
        }

        if (t >= 1f)
        {
            _velocity = Vector3.zero;
            _isGrounded = true;
            _isExitingLadderTop = false;

            if (cameraPivot != null)
            {
                // 1) Kameranin final acisini al
                Vector3 camEuler = cameraPivot.localEulerAngles;
                float camPitch = NormalizeAngle(camEuler.x);

                // 2) Look sistemine de bu aciyi yaz
                _pitch = Mathf.Clamp(camPitch, pitchMin, pitchMax);

                // 3) Pozisyonu defaulta cek, sadece roll sifirla
                cameraPivot.localPosition = _camLocalDefault;
                camEuler.z = 0f;
                cameraPivot.localEulerAngles = camEuler;
            }
        }

    }

    void HandleLadder(Vector2 input, ref Vector3 currentHorizontal)
    {
        // while top-exit anim is playing, do not touch ladder logic
        if (_isExitingLadderTop)
        {
            _ladderInput = Vector2.zero;
            return;
        }

        if (isOnLadder)
        {
            float vertical = input.y;   // W / S
            float horizontal = input.x; // A / D

            _ladderInput = input;

            Bounds cb = controller.bounds;
            float feetY = cb.min.y;

            Vector3 ladderForward = -ladderHit.normal;
            ladderForward.y = 0f;
            if (ladderForward.sqrMagnitude < 0.0001f)
                ladderForward = transform.forward;
            ladderForward.Normalize();

            float rayLen = Mathf.Max(ladderRayLength, ladderAttachDistance + 0.2f);

            // control ray (feet)
            Vector3 controlOrigin = new Vector3(transform.position.x,
                                                feetY + ladderControlRayOffset,
                                                transform.position.z);

            // exit ray (above feet)
            Vector3 exitOrigin = new Vector3(transform.position.x,
                                             feetY + ladderExitRayOffset,
                                             transform.position.z);

            bool controlHit = Physics.Raycast(controlOrigin, ladderForward, out RaycastHit controlInfo, rayLen, ladderMask, QueryTriggerInteraction.Ignore);
            bool exitHit = Physics.Raycast(exitOrigin, ladderForward, out RaycastHit exitInfo, rayLen, ladderMask, QueryTriggerInteraction.Ignore);

            if (debugLadderAngles)
            {
                Debug.DrawRay(controlOrigin, ladderForward * rayLen, Color.green);
                Debug.DrawRay(exitOrigin, ladderForward * rayLen, Color.cyan);
            }

            // lost ladder entirely -> drop
            if (!controlHit)
            {
                ExitLadder();
                _velocity.y = gravity * 0.2f;
                return;
            }

            // still same ladder
            ladderHit = controlInfo;
            ladderTopY = controlInfo.collider.bounds.max.y;
            ladderBottomY = controlInfo.collider.bounds.min.y;

            // going down and below bottom -> drop
            if (cb.min.y <= ladderBottomY - 0.05f && vertical < 0f)
            {
                ExitLadder();
                return;
            }

            // top exit: lower ray hits, upper ray does not, and W pressed
            if (controlHit && !exitHit && vertical > 0.1f)
            {
                ExitLadderAtTopSmooth(controlInfo);
                return;
            }

            // normal ladder movement
            _velocity.y = vertical * ladderSpeed;

            Vector3 sideDir = transform.right;
            sideDir.y = 0f;
            if (sideDir.sqrMagnitude > 0.0001f)
                sideDir.Normalize();
            currentHorizontal = sideDir * (horizontal * ladderSideSpeed);

            AlignToLadderPlane();

            if (cameraPivot != null && ladderYawRange > 0f)
            {
                if (IsLadderYawOutsideRange(out float absAngle))
                {
                    currentLadderAngle = absAngle;
                    ExitLadder();
                    _velocity.y = gravity * 0.1f;
                    return;
                }
            }

            if (JumpPressedThisFrame())
            {
                ExitLadder();
                _velocity.y = 0f;
                return;
            }

            _isGrounded = false;
        }
        else
        {
            _ladderInput = Vector2.zero;

            if (_ladderReattachBlockTimer <= 0f &&
                input.y > 0.1f &&
                CheckForLadder(out ladderHit))
            {
                isOnLadder = true;

                Collider col = ladderHit.collider;
                Bounds bounds = col.bounds;
                ladderTopY = bounds.max.y;
                ladderBottomY = bounds.min.y;

                _velocity = Vector3.zero;
                AlignToLadderPlane();
                _isGrounded = false;
            }
        }
    }

    bool CheckForLadder(out RaycastHit hit)
    {
        float y = controller.bounds.min.y + controller.height * 0.25f;
        Vector3 origin = new Vector3(transform.position.x, y, transform.position.z);
        Vector3 dir = transform.forward;

        if (debugLadderAngles)
            Debug.DrawRay(origin, dir * ladderAttachDistance, Color.yellow);

        return Physics.Raycast(origin, dir, out hit, ladderAttachDistance, ladderMask, QueryTriggerInteraction.Ignore);
    }

    void AlignToLadderPlane()
    {
        Vector3 normal = ladderHit.normal;

        Vector3 pos = transform.position;
        float dist = Vector3.Dot(pos - ladderHit.point, normal);
        pos -= normal * (dist - ladderSurfaceOffset);
        transform.position = pos;
    }

    bool IsLadderYawOutsideRange(out float absAngle)
    {
        absAngle = 0f;

        if (cameraPivot == null)
            return false;

        Vector3 ladderForward = -ladderHit.normal;
        ladderForward.y = 0f;

        Vector3 camForward = cameraPivot.forward;
        camForward.y = 0f;

        if (ladderForward.sqrMagnitude < 0.0001f || camForward.sqrMagnitude < 0.0001f)
            return false;

        ladderForward.Normalize();
        camForward.Normalize();

        float angle = Vector3.SignedAngle(ladderForward, camForward, Vector3.up);
        absAngle = Mathf.Abs(angle);

        currentLadderAngle = absAngle;

        return absAngle > Mathf.Abs(ladderYawRange);
    }

    void ExitLadder()
    {
        isOnLadder = false;
        _ladderInput = Vector2.zero;
        _ladderReattachBlockTimer = 0.25f;
    }

    void ExitLadderSimple()
    {
        isOnLadder = false;
        _ladderInput = Vector2.zero;

        if (_velocity.y > 0f)
            _velocity.y = 0f;

        Vector3 pos = transform.position;
        pos.y += 0.05f;
        controller.Move(Vector3.zero); // just to update internal state

        _isGrounded = false;
        _lastGroundedTime = Time.time;
        _ladderReattachBlockTimer = 0.2f;
    }

    void ExitLadderAtTopSmooth(RaycastHit controlInfo)
    {
        isOnLadder = false;
        _ladderInput = Vector2.zero;

        Vector3 forward = -controlInfo.normal;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;
        else
            forward.Normalize();

        float halfH = controller.height * 0.5f;
        float topY = controlInfo.collider.bounds.max.y;

        _ladderExitStartPos = transform.position;

        Vector3 endPos = transform.position;
        endPos.y = topY + halfH + ladderTopExtraUp;
        endPos += forward * (controller.radius + ladderTopExtraForward);

        _ladderExitEndPos = endPos;
        _ladderExitTimer = 0f;
        _isExitingLadderTop = true;

        _velocity = Vector3.zero;
        _ladderReattachBlockTimer = 0.35f;

        if (cameraPivot != null)
        {
            _ladderExitCamStartLocalPos = cameraPivot.localPosition;
            _ladderExitCamStartLocalEuler = cameraPivot.localEulerAngles;
        }
    }
    #endregion

    #region Ladder Camera

    void HandleLadderCamera()
    {
        if (cameraPivot == null)
            return;

        // top-exit animasyonunda kameraya karismayalim
        if (_isExitingLadderTop)
            return;

        if (!enableLadderCameraAnimation)
        {
            ResetLadderCameraToDefault();
            return;
        }

        if (!isOnLadder)
        {
            ResetLadderCameraToDefault();
            return;
        }

        // adim tetikleme (W / S veya A / D hareket ediyorsa)
        if (Mathf.Abs(_ladderInput.y) > 0.1f || Mathf.Abs(_ladderInput.x) > 0.1f)
        {
            _ladderStepCooldown -= Time.deltaTime;
            if (_ladderStepCooldown <= 0f)
            {
                _ladderStepCooldown = ladderStepInterval;
                _ladderStepProgress = 0f;
                _ladderStepSign = _ladderInput.y >= 0f ? 1 : -1;
            }
        }
        else
        {
            _ladderStepCooldown = 0f;
        }

        float stepPitchOffset = 0f;
        float stepY = 0f;
        float stepRoll = 0f;
        float stepForward = 0f;

        // “tik” animasyonu
        if (_ladderStepProgress >= 0f)
        {
            _ladderStepProgress += Time.deltaTime / Mathf.Max(0.0001f, ladderStepDuration);
            float t = Mathf.Clamp01(_ladderStepProgress);

            // dalga 0 -> 1 -> 0 ama biz yukari cekerken once hizli yukari, sonra bekleme istiyoruz
            float pull = Mathf.Sin(t * Mathf.PI * 0.5f); // 0..1
                                                         // asagi donus yok, sadece yukari cekme hissi
            float upFactor = Mathf.Clamp01(pull);

            // kamera yukari
            float dir = (_ladderInput.y >= 0f) ? 1f : -1f;
            stepY = upFactor * ladderStepVerticalAmplitude * dir;

            // kamera biraz ileri
            stepForward = upFactor * ladderStepForwardAmplitude;

            // kol ile yukari cekerken hafif asagi bakma
            stepPitchOffset = upFactor * ladderStepPitchAmplitude * dir;

            // hafif sag/sol sallanma
            float waveSide = Mathf.Sin(t * Mathf.PI);
            stepRoll = waveSide * ladderStepRollAmplitude * _ladderStepSign;

            if (_ladderStepProgress >= 1f)
            {
                _ladderStepProgress = -1f;
            }
        }

        // pozisyon
        Vector3 targetPos = _camLocalDefault;
        targetPos.y += stepY;
        targetPos.z += stepForward;

        cameraPivot.localPosition = Vector3.Lerp(
            cameraPivot.localPosition,
            targetPos,
            Time.deltaTime * 18f
        );

        // rotasyon
        Vector3 camEuler = cameraPivot.localEulerAngles;
        float currentPitch = NormalizeAngle(camEuler.x);

        float targetPitch = Mathf.Clamp(
            _pitch + stepPitchOffset,
            pitchMin,
            pitchMax
        );

        float newPitch = Mathf.LerpAngle(
            currentPitch,
            targetPitch,
            Time.deltaTime * 18f
        );

        camEuler.x = newPitch;
        camEuler.z = Mathf.LerpAngle(
            camEuler.z,
            stepRoll,
            Time.deltaTime * 18f
        );

        cameraPivot.localEulerAngles = camEuler;
    }

    void ResetLadderCameraToDefault()
    {
        cameraPivot.localPosition = Vector3.Lerp(
            cameraPivot.localPosition,
            _camLocalDefault,
            Time.deltaTime * 10f
        );

        Vector3 euler = cameraPivot.localEulerAngles;
        float currentPitch = NormalizeAngle(euler.x);
        float blendedPitch = Mathf.LerpAngle(
            currentPitch,
            _pitch,
            Time.deltaTime * 10f
        );
        euler.x = blendedPitch;
        euler.z = Mathf.LerpAngle(euler.z, 0f, Time.deltaTime * 10f);
        cameraPivot.localEulerAngles = euler;

        _ladderStepProgress = -1f;
        _ladderStepCooldown = 0f;
        _ladderStepSign = 1;
    }

    #endregion

    #region Climb

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isClimbing || freezeMovement)
            return;

        if ((climbableLayers.value & (1 << hit.gameObject.layer)) == 0)
            return;

        if (_velocity.y >= 0f)
            return;

        if (_isGrounded)
            return;

        TryStartClimb(hit);
    }

    private void TryStartClimb(ControllerColliderHit hit)
    {
        float pitch = NormalizeAngle(_pitch);

        if (pitch < climbPitchMin || pitch > climbPitchMax)
            return;

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

        Vector3 camStartLocalPos = cameraPivot != null ? cameraPivot.localPosition : Vector3.zero;
        Vector3 camStartLocalEuler = cameraPivot != null ? cameraPivot.localEulerAngles : Vector3.zero;

        float startPitch = NormalizeAngle(camStartLocalEuler.x);

        float phase1Delta = climbPhase1TargetPitch - startPitch;

        controller.enabled = false;

        const float phase1End = 1f / 3f;
        const float phase2End = 2f / 3f;
        const float heightPhase1 = 0.2f;
        const float heightPhase2 = 0.7f;

        const float phase1DipY = -0.02f;
        const float phase1ForwardZ = 0.01f;
        const float phase2DipY = -0.05f;
        const float phase2ForwardZ = 0.07f;

        const float shakeFrequency = 6f;
        const float shakeRollMultiplier = 0.2f;

        float t = 0f;

        bool useClimbCamAnim = (cameraPivot != null && enableClimbCameraAnimation);

        float currentPitch = startPitch;

        while (t < climbTime)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / climbTime);

            float heightT;

            if (norm < phase1End)
            {
                float p = norm / phase1End;
                p *= p;
                heightT = Mathf.Lerp(0f, heightPhase1, p);
            }
            else if (norm < phase2End)
            {
                float p = (norm - phase1End) / (phase2End - phase1End);
                p = 1f - (1f - p) * (1f - p);
                heightT = Mathf.Lerp(heightPhase1, heightPhase2, p);
            }
            else
            {
                float p = (norm - phase2End) / (1f - phase2End);
                p = p * (2f - p);
                heightT = Mathf.Lerp(heightPhase2, 1f, p);
            }

            transform.position = Vector3.Lerp(startPos, targetPos, heightT);

            if (useClimbCamAnim)
            {
                float offsetY = 0f;
                float offsetZ = 0f;
                float pitchOffset = 0f;

                if (norm < phase1End)
                {
                    float p = norm / phase1End;

                    offsetY = Mathf.Lerp(0f, phase1DipY, p);
                    offsetZ = Mathf.Lerp(0f, phase1ForwardZ, p);

                    float lerpedPitch = Mathf.Lerp(startPitch, climbPhase1TargetPitch, p);
                    pitchOffset = lerpedPitch - startPitch;
                }
                else if (norm < phase2End)
                {
                    float p = (norm - phase1End) / (phase2End - phase1End);

                    offsetY = Mathf.Lerp(phase1DipY, phase2DipY, p);
                    offsetZ = Mathf.Lerp(phase1ForwardZ, phase2ForwardZ, p);

                    float startOffset = phase1Delta;
                    float endOffset = phase1Delta + climbForwardLean;
                    pitchOffset = Mathf.Lerp(startOffset, endOffset, p);
                }
                else
                {
                    float p = (norm - phase2End) / (1f - phase2End);

                    offsetY = Mathf.Lerp(phase2DipY, 0f, p);
                    offsetZ = Mathf.Lerp(phase2ForwardZ, 0f, p);

                    float startOffset = phase1Delta + climbForwardLean;
                    float endOffset = phase1Delta;
                    pitchOffset = Mathf.Lerp(startOffset, endOffset, p);
                }

                float shakeAmp = Mathf.Max(0f, climbShakeAmount);

                float noiseY = 0f;
                float noiseZ = 0f;

                if (shakeAmp > 0f)
                {
                    float t1 = Time.time * shakeFrequency;
                    float t2 = t1 + 37.13f;

                    noiseY = (Mathf.PerlinNoise(t1, 0f) - 0.5f) * 2f;
                    noiseZ = (Mathf.PerlinNoise(0f, t2) - 0.5f) * 2f;
                }

                float shakeY = noiseY * shakeAmp;
                float shakeForward = noiseZ * shakeAmp * 0.5f;
                float shakeRoll = noiseY * shakeRollMultiplier;

                Vector3 lp = camStartLocalPos;
                lp.y += offsetY + shakeY;
                lp.z += offsetZ + shakeForward;
                cameraPivot.localPosition = lp;

                Vector3 euler = camStartLocalEuler;
                euler.x = startPitch + pitchOffset;
                euler.z += shakeRoll;
                cameraPivot.localEulerAngles = euler;

                currentPitch = euler.x;
            }

            yield return null;
        }

        transform.position = targetPos;

        controller.enabled = true;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                            Vector3.down,
                            out RaycastHit hit2,
                            3f,
                            groundMask,
                            QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            float halfH = controller.height * 0.5f;
            p.y = hit2.point.y + halfH;
            transform.position = p;
        }

        if (cameraPivot != null)
        {
            cameraPivot.localPosition = camStartLocalPos;

            Vector3 finalEuler = cameraPivot.localEulerAngles;
            finalEuler.x = currentPitch;
            cameraPivot.localEulerAngles = finalEuler;
        }

        _pitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);

        _velocity = oldVelocity;
        _velocity.y = 0f;

        isClimbing = false;
        freezeMovement = false;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
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

        if (isClimbing || isOnLadder || _isExitingLadderTop) return;

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
