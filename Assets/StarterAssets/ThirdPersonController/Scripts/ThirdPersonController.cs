 using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    //[RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        public float sensitivityMouseY = 1.5f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        public float checkEdgeDist = 0.3f;

        [Tooltip("How much to smooth the camera rotation. Lower values are more responsive.")]
        public float CameraSmoothTime = 0.03f;

        // cinemachine

        [ReadOnlyField] public float _cinemachineTargetYaw;
        [ReadOnlyField] public float _cinemachineTargetPitch;

        private float _yawVelocity;
        private float _pitchVelocity;
        private float _smoothedYaw;
        private float _smoothedPitch;
        private float _rawTargetYaw;
        private float _rawTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        [SerializeField]
        private Animator _animator;
        [SerializeField] AutoJumpComponent autoJump;
        [SerializeField] public float autoJumpSpeed = 1.1f;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;
        private const float MobileTouchLookOutlierThreshold = 5000f;
        private const float MobileTouchLookMultiplier = 0.199f;

        private bool _hasAnimator;
        float ebaniyTimer = 0;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        public float CurrentSpeed => _speed;
        public bool IsOwner { get; set; } = true;
        [field:SerializeField]
        public bool AllowCameraRotation { get; set; } = true;
        public bool AllowGravityLogic { get; set; } = true;
        public bool NoFall { get; set; } = false;
        public bool AutoJump { get; set; } = true;

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _smoothedYaw = _cinemachineTargetYaw;
            _smoothedPitch = _cinemachineTargetPitch;
            _rawTargetYaw = _cinemachineTargetYaw;
            _rawTargetPitch = _cinemachineTargetPitch;

            _hasAnimator = true;
            _animator = GetComponentInChildren<Animator>();
            _controller = GetComponent<CharacterController>();
            _input ??= GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput ??= GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        public void SetInput(StarterAssetsInputs inputs, PlayerInput playerInput)
        {
            _input = inputs;
            _playerInput = playerInput;
        }

        private void Update()
        {
#if !UNITY_SERVER
            if (!IsOwner)
                return;

            JumpAndGravity();
            GroundedCheck();
            Move();
#endif
        }

        private void LateUpdate()
        {
#if !UNITY_SERVER
            if (!IsOwner || !_input || !AllowCameraRotation || !_input.cursorInputForLook)
                return;

            CameraRotation();
#endif
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            float lookX = _input.look.x;
            float lookY = _input.look.y;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
            {
                // Mobile WebGL uses the same normalized touch-look path as native Android/iOS builds.
                if (!TryApplyMobileTouchLook(ref lookX, ref lookY)) return;
            }
            else
            {
                // FALLBACK: New Input System in desktop WebGL often has bugs with Pointer Lock and DPI.
                // Using legacy Input.GetAxis is much more stable in many browsers with mouse input.
                lookX = Input.GetAxis("Mouse X") * 1.6f; // Reduced sensitivity (down by 20%)
                lookY = -Input.GetAxis("Mouse Y") * 1.6f; // Fixed inversion (down by 20%)

                // WebGL Outlier Rejection: Ignore huge spikes caused by browser events or DPI scaling glitches.
                // A delta larger than 25 in one frame is physically impossible for normal mouse movement.
                if (Mathf.Abs(lookX) > 25f || Mathf.Abs(lookY) > 25f) return;
            }
#elif UNITY_ANDROID || UNITY_IOS
            // Native Android/iOS builds receive a resolution-normalized touch-look rate from
            // TouchLookNormalizer.ToLookRate(), so this branch must not use raw screen pixels.
            if (!TryApplyMobileTouchLook(ref lookX, ref lookY)) return;
#else
            // Outlier rejection for New Input System (if still used)
            if (Mathf.Abs(lookX) > 100f || Mathf.Abs(lookY) > 100f) return;

            // Increase sensitivity for non-WebGL platforms (Editor/Standalone)
            lookX *= 2.0f;
            lookY *= 2.0f;
#endif

            // if there is an input and camera position is not fixed
            if ((Mathf.Abs(lookX) > _threshold || Mathf.Abs(lookY) > _threshold) && !LockCameraPosition)
            {
                _cinemachineTargetYaw += lookX;
                _cinemachineTargetPitch += lookY * sensitivityMouseY;
            }

            // Properly wrap Yaw around 360 degrees using Mathf.Repeat to avoid precision issues and jumps
            _cinemachineTargetYaw = Mathf.Repeat(_cinemachineTargetYaw, 360f);

            // Strict clamp for Pitch
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Apply smoothing using INTERNAL variables (_smoothedYaw, _smoothedPitch).
            // This is CRITICAL because reading from transform.rotation.eulerAngles creates jitter
            // when the parent object (Player) is also moving/rotating in the same frame.
            if (Mathf.Abs(lookX) > _threshold || Mathf.Abs(lookY) > _threshold)
            {
                _smoothedYaw = Mathf.SmoothDampAngle(_smoothedYaw, _cinemachineTargetYaw, ref _yawVelocity, CameraSmoothTime);
                _smoothedPitch = Mathf.SmoothDampAngle(_smoothedPitch, _cinemachineTargetPitch, ref _pitchVelocity, CameraSmoothTime);

                CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_smoothedPitch + CameraAngleOverride, _smoothedYaw, 0.0f);
            }
            else
            {
                // No input: Snap to the target and reset velocities to avoid any drifting.
                _smoothedYaw = _cinemachineTargetYaw;
                _smoothedPitch = _cinemachineTargetPitch;
                CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_smoothedPitch + CameraAngleOverride, _smoothedYaw, 0.0f);
                _yawVelocity = 0;
                _pitchVelocity = 0;
            }

            // Sync raw targets for internal consistency
            _rawTargetYaw = _cinemachineTargetYaw;
            _rawTargetPitch = _cinemachineTargetPitch;
        }

        private static bool TryApplyMobileTouchLook(ref float lookX, ref float lookY)
        {
            // TouchLookNormalizer converts Android touch pixels to a screen-size independent rate.
            // Values above this threshold are input spikes, not real finger movement.
            if (Mathf.Abs(lookX) > MobileTouchLookOutlierThreshold ||
                Mathf.Abs(lookY) > MobileTouchLookOutlierThreshold)
            {
                return false;
            }

            lookX *= MobileTouchLookMultiplier;
            lookY *= MobileTouchLookMultiplier;
            return true;
        }

        public bool useSprint;

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            var move = targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;

            var pendingPos = transform.position + move;
            //Vector3 spherePosition = new Vector3
            //(
            //    pendingPos.x,
            //    pendingPos.y - GroundedOffset,
            //    pendingPos.z
            //);

            if (AutoJump && autoJump.HandleAutoJump(move))
            {
                if (useSprint)
                {
                    move.y = Mathf.Sqrt(autoJumpSpeed * targetSpeed * -2f * Gravity) * Time.deltaTime;
                }
                else
                {
                    move.y = Mathf.Sqrt(autoJumpSpeed * MoveSpeed * -2f * Gravity) * Time.deltaTime;

                }
                //_animator.SetBool(_animIDJump, true);
            }

            if (NoFall)
            {
                if (Grounded)
                {
                    move = AdjustMovementForEdge(move);
                }
                else
                {
                    // Костыль, иногда только, что поставленный блок не упевает
                    // дать коллизию с рейкастом, поэтому ждем время, чтобы
                    // точно убедиться, что под нами нет поверхности.
                    ebaniyTimer += Time.deltaTime;
                    if (ebaniyTimer < 0.3f)
                    {
                        move = AdjustMovementForEdge(move);
                    }
                }
            }
            else
            {
                ebaniyTimer = 0;
            }

            _controller.Move(move);
            // update animator if using character

            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);

        }

        Vector3 AdjustMovementForEdge(Vector3 moveDirection)
        {
            // Проверяем движение вперед по оси Z
            if (!IsEdgeSafe(Vector3.forward) && moveDirection.z > 0)
            {
                moveDirection.z = 0; // Останавливаем движение вперед
            }
            // Проверяем движение назад по оси Z
            if (!IsEdgeSafe(Vector3.back) && moveDirection.z < 0)
            {
                moveDirection.z = 0; // Останавливаем движение назад
            }
            // Проверяем движение вправо по оси X
            if (!IsEdgeSafe(Vector3.right) && moveDirection.x > 0)
            {
                moveDirection.x = 0; // Останавливаем движение вправо
            }
            // Проверяем движение влево по оси X
            if (!IsEdgeSafe(Vector3.left) && moveDirection.x < 0)
            {
                moveDirection.x = 0; // Останавливаем движение влево
            }

            return moveDirection;
        }

        bool IsEdgeSafe(Vector3 direction)
        {
            RaycastHit hit;
            Vector3 checkPosition = transform.position + (direction * checkEdgeDist) + (Vector3.up * 0.5f);

            checkPosition -= Vector3.up * 0.58f;

            return Physics.CheckSphere
            (
                checkPosition,
                0.18f,
                GroundLayers,
                QueryTriggerInteraction.Ignore
            );
            
            //if (Physics.Raycast(checkPosition, Vector3.down, out hit, 0.5f, GroundLayers))
            //{
            //    return hit.collider != null;
            //}

            //return false;
        }


        private void JumpAndGravity()
        {
            if (!AllowGravityLogic)
                return;

            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        public void SetPitchAndYaw(float pitch, float yaw)
        {
            _cinemachineTargetPitch = pitch;
            _cinemachineTargetYaw = yaw;

            _rawTargetPitch = pitch;
            _rawTargetYaw = yaw;

            _cinemachineTargetYaw = Mathf.Repeat(_cinemachineTargetYaw, 360f);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        public void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        public void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}