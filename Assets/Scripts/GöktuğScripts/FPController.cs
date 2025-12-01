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
    [Tooltip("Forward lean during climb (degrees). Negative means leaning forward.")]
    public float climbForwardLean = -10f;
    [Tooltip("Camera shake amount during climb. Set 0 to disable.")]
    public float climbShakeAmount = 0.01f;

    [Header("Climb Angle Limit")]
    [Tooltip("Max absolute pitch angle allowed to start a climb (degrees).")]
    [Header("Climb Angle Limits")]
    public float climbPitchMin = -45f;   // En aşşağı bakış limiti
    public float climbPitchMax = 45f;    // En yukarı bakış limiti

    [HideInInspector] 
    public bool isOnMovingPlatform = false; 
    [HideInInspector] 
    public Vector3 platformMoveDelta = Vector3.zero; // Trenin anlık hareket vektörü
    
    // Internal
    private float _pitch;
    private Vector3 _velocity; // world-space
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
    private bool _headBobJustStarted = true;
    bool firstmove = true;
    public float climbPhase1TargetPitch = 40f; // desired pitch at end of phase 1


    private bool isClimbing = false;

#if ENABLE_INPUT_SYSTEM
    private Keyboard kb => Keyboard.current;
    private Mouse ms => Mouse.current;
#endif

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // cameraPivot'u sadece Inspector'dan veriyoruz
        // if (cameraPivot == null && Camera.main != null)
        //     cameraPivot = Camera.main.transform;

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

        // Direction change and braking tweak (less sliding)
        if (hasInput && currentHorizontal.sqrMagnitude > 0.0001f)
        {
            Vector3 curNorm = currentHorizontal.normalized;
            Vector3 desiredNorm = desiredHorizontal.normalized;
            float dot = Vector3.Dot(curNorm, desiredNorm);

            // If we are trying to move in almost opposite direction, apply strong brake first
            if (dot < 0f)
            {
                float reverseMult = _isGrounded ? 2.5f : 1.2f;
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

      //controller.Move(_velocity * Time.deltaTime); //Tren hareketi için kaldırılan kod.
      // YENİ: Oyuncunun tüm hareketini hesapla
      Vector3 playerMovement = _velocity * Time.deltaTime; 

      // YENİ KOD BLOĞU: Platform Üzerinde Durma ve Hareket Etme Kontrolü
      if (isOnMovingPlatform)
      {
          // BİREBİR platform delta zaten frame-bazlı world-space delta (trainPos - lastTrainPos).
          // 1) Yerçekimini yok sayma / hafif snap
          _velocity.y = -1f;

          // 2) Grounded kaybını önle (platform üzerindeyken kendini grounded say)
          _isGrounded = true;
          _lastGroundedTime = Time.time;

          // 3) Platform delta (world-space) doğrudan playerMovement'e ekle.
          // NOT: platformMoveDelta bir 'delta position' (metre/frame) olmalı. Eğer train velocity veriyorsa çarp Time.deltaTime.
          playerMovement += platformMoveDelta;
      }

      // SON TAŞIMA KOMUTU:
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

    #region Climb

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isClimbing || freezeMovement)
            return;

        // Only climbable layers
        if ((climbableLayers.value & (1 << hit.gameObject.layer)) == 0)
            return;

        // Only climb when falling
        if (_velocity.y >= 0f)
            return;

        // Do not start climb if still grounded
        if (_isGrounded)
            return;

        TryStartClimb(hit);
    }

    private void TryStartClimb(ControllerColliderHit hit)
    {
        // Normalize pitch to -180..180
        float pitch = NormalizeAngle(_pitch);

        // Cancel climb if pitch is outside allowed range
        if (pitch < climbPitchMin || pitch > climbPitchMax)
            return;

        Collider col = hit.collider;

        float topY = col.bounds.max.y;
        float contactY = hit.point.y;
        float remaining = topY - contactY;

        if (remaining <= 0f)
            return;

        // Do not allow climbing if the ledge is too high
        if (remaining > maxClimbRemaining)
            return;

        float playerHalfHeight = transform.localScale.y * 0.5f;
        float climbUp = remaining + playerHalfHeight;

        Vector3 targetPos = transform.position;
        targetPos.y += climbUp;

        // Small forward offset on top
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

        // Camera local state at climb start
        Vector3 camStartLocalPos = cameraPivot != null ? cameraPivot.localPosition : Vector3.zero;
        Vector3 camStartLocalEuler = cameraPivot != null ? cameraPivot.localEulerAngles : Vector3.zero;

        // Start pitch normalized
        float startPitch = NormalizeAngle(camStartLocalEuler.x);

        // How far from start to target at end of phase 1
        float phase1Delta = climbPhase1TargetPitch - startPitch;

        // Controller off during climb to avoid jitter
        controller.enabled = false;

        // Phase ratios (0..1)
        const float phase1End = 1f / 3f;
        const float phase2End = 2f / 3f;
        const float heightPhase1 = 0.2f;
        const float heightPhase2 = 0.7f;

        // Camera offsets
        const float phase1DipY = -0.02f;
        const float phase1ForwardZ = 0.01f;
        const float phase2DipY = -0.05f;
        const float phase2ForwardZ = 0.07f;

        // Shake parameters
        const float shakeFrequency = 6f;
        const float shakeRollMultiplier = 0.2f;

        float t = 0f;

        bool useClimbCamAnim = (cameraPivot != null && enableClimbCameraAnimation);

        // Track current animated pitch so we can sync _pitch at the end
        float currentPitch = startPitch;

        while (t < climbTime)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / climbTime); // 0..1

            // -------- Root movement (3-phase height curve) --------
            float heightT;

            if (norm < phase1End)
            {
                float p = norm / phase1End;
                p *= p; // ease-in
                heightT = Mathf.Lerp(0f, heightPhase1, p);
            }
            else if (norm < phase2End)
            {
                float p = (norm - phase1End) / (phase2End - phase1End);
                p = 1f - (1f - p) * (1f - p); // ease-out
                heightT = Mathf.Lerp(heightPhase1, heightPhase2, p);
            }
            else
            {
                float p = (norm - phase2End) / (1f - phase2End);
                p = p * (2f - p); // ease-in-out
                heightT = Mathf.Lerp(heightPhase2, 1f, p);
            }

            transform.position = Vector3.Lerp(startPos, targetPos, heightT);

            // -------- Camera animation --------
            if (useClimbCamAnim)
            {
                float offsetY = 0f;
                float offsetZ = 0f;
                float pitchOffset = 0f;

                if (norm < phase1End)
                {
                    // Phase 1: move camera a bit down/forward AND
                    // move pitch from startPitch to climbPhase1TargetPitch
                    float p = norm / phase1End;

                    offsetY = Mathf.Lerp(0f, phase1DipY, p);
                    offsetZ = Mathf.Lerp(0f, phase1ForwardZ, p);

                    float lerpedPitch = Mathf.Lerp(startPitch, climbPhase1TargetPitch, p);
                    pitchOffset = lerpedPitch - startPitch;
                }
                else if (norm < phase2End)
                {
                    // Phase 2: from target pitch to target + lean
                    float p = (norm - phase1End) / (phase2End - phase1End);

                    offsetY = Mathf.Lerp(phase1DipY, phase2DipY, p);
                    offsetZ = Mathf.Lerp(phase1ForwardZ, phase2ForwardZ, p);

                    float startOffset = phase1Delta;
                    float endOffset = phase1Delta + climbForwardLean;
                    pitchOffset = Mathf.Lerp(startOffset, endOffset, p);
                }
                else
                {
                    // Phase 3: from target + lean back to target
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

                    noiseY = (Mathf.PerlinNoise(t1, 0f) - 0.5f) * 2f; // -1..1
                    noiseZ = (Mathf.PerlinNoise(0f, t2) - 0.5f) * 2f; // -1..1
                }

                float shakeY = noiseY * shakeAmp;
                float shakeForward = noiseZ * shakeAmp * 0.5f;
                float shakeRoll = noiseY * shakeRollMultiplier;

                // Position: base offset + subtle shake
                Vector3 lp = camStartLocalPos;
                lp.y += offsetY + shakeY;
                lp.z += offsetZ + shakeForward;
                cameraPivot.localPosition = lp;

                // Rotation: always relative to the original startPitch
                Vector3 euler = camStartLocalEuler;
                euler.x = startPitch + pitchOffset;
                euler.z += shakeRoll;
                cameraPivot.localEulerAngles = euler;

                currentPitch = euler.x; // track animated pitch
            }

            yield return null;
        }

        // Snap to target position
        transform.position = targetPos;

        controller.enabled = true;

        // Small ground snap
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                            Vector3.down,
                            out RaycastHit hit,
                            3f,
                            groundMask,
                            QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            float halfH = controller.height * 0.5f;
            p.y = hit.point.y + halfH;
            transform.position = p;
        }

        // Restore only camera local position, keep final pitch
        if (cameraPivot != null)
        {
            cameraPivot.localPosition = camStartLocalPos;

            // Ensure camera rotation uses the final animated pitch
            Vector3 finalEuler = cameraPivot.localEulerAngles;
            finalEuler.x = currentPitch;
            cameraPivot.localEulerAngles = finalEuler;
        }

        // Sync look pitch so HandleLook continues smoothly from final angle
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

        // During climb we use our own camera animation
        if (isClimbing) return;

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
