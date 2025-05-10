using UnityEngine;
using System.Collections;
using SmallHedge.SoundManager;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Start Menu Settings")]
        [Tooltip("Color de fondo del menú de inicio")]
        public Color startMenuBackgroundColor = Color.black;

        [Tooltip("¿La cámara gira automáticamente al comenzar?")]
        public bool startAutoCam = true;

        [Tooltip("¿Mutear audio al comenzar?")]
        public bool startMuted = false;

        [Header("Player")]
        public float MoveSpeed = 5.0f;
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        [Header("Inicio del Juego")]
        public bool AutoStartMovement = false;
        private bool _gameStarted = false;

        [Header("Footstep Settings")]
        public float FootstepInterval = 0.245f;
        public float FootstepAudioVolume = 1f;

        private double _nextFootstepTime;

        [Space(10)]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;

        [Space(10)]
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;
        public float CameraAutoFollowSpeed = 3.0f;

        // Nuevos parámetros para ajustar la cámara desde el inspector
        [Header("Camera Position Adjustment")]
        [Tooltip("Ajusta la altura vertical de la cámara")]
        public float CameraHeightOffset = 0.0f;
        [Tooltip("Ajusta la distancia de la cámara al jugador")]
        public float CameraDistanceOffset = 0.0f;
        [Tooltip("Ajusta la rotación horizontal de la cámara")]
        public float CameraYawOffset = 0.0f;
        [Tooltip("Ajusta la rotación vertical de la cámara (inclinación)")]
        public float CameraPitchOffset = 0.0f;
        [Tooltip("Desplazamiento lateral de la cámara")]
        public float CameraLateralOffset = 0.0f;

        [Header("Slide Settings")]
        public float SlideDuration = 0.75f;
        public float SlideHeight = 0.5f;
        public Vector3 SlideCenter = new Vector3(0, 0.25f, 0);
        [SerializeField] private float slideRestoreTime = 0.1f;

        [Header("Wall Run Settings")]
        public float WallRunDuration = 1.5f;
        [SerializeField] private float wallRunDelay = 0.7f;
        public float WallRunRotationAngle = 90f;
        public float WallRunSmoothTime = 0.2f;

        [Header("Lateral Movement Settings")]
        public float HorizontalSmoothTime = 0.1f;
        public float LateralSpeedMultiplier = 2.0f;
        [SerializeField] private float hitStopDelay = 0.5f;

        // Variables internas
        private float _hitStopTimer = 1f;
        private int _animIDHitLegs;
        private bool _isHit = false;
        private int _animIDHitShelf;
        private bool _isShelfHit = false;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDSlide;
        private int _animIDWallRun;
        private int _animIDHorizontal;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;


        private const float _threshold = 0.01f;
        private bool _hasAnimator;

        public bool IsDead => _isHit || _isShelfHit;

        private bool _isSliding;
        private float _slideTimer;
        private float _originalHeight;
        private Vector3 _originalCenter;

        private bool _isWallRunning;
        private float _wallRunTimer;
        private float _targetYRotation;

        private float _currentHorizontal = 0f;
        private bool _wallRunPending = false;

        private bool IsCurrentDeviceMouse =>
#if ENABLE_INPUT_SYSTEM
            _playerInput.currentControlScheme == "KeyboardMouse";
#else
            false;
#endif
        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            // ── INICIALIZACIÓN EXISTENTE ──
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
            AssignAnimationIDs();
            _originalHeight = _controller.height;
            _originalCenter = _controller.center;
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _targetYRotation = transform.eulerAngles.y;
            // ── FIN INICIALIZACIÓN EXISTENTE ──

            // Aplicar ajustes iniciales a la cámara
            SetupCameraPosition();

            // Configurar menú de inicio
            _gameStarted = AutoStartMovement;
            if (!_gameStarted)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
                if (_animator != null) _animator.speed = 0f;
                StopMovement();

                // mostrar cursor para el menú
                _input.cursorLocked = false;
                _input.cursorInputForLook = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            _nextFootstepTime = AudioSettings.dspTime + FootstepInterval;
        }

        // Nueva función para configurar la posición de la cámara
        private void SetupCameraPosition()
        {
            if (CinemachineCameraTarget != null)
            {
                // Guardar la posición local original
                Vector3 originalLocalPosition = CinemachineCameraTarget.transform.localPosition;

                // Aplicar los offsets de posición configurados en el inspector
                Vector3 newPosition = originalLocalPosition;
                newPosition.y += CameraHeightOffset;
                newPosition.x += CameraLateralOffset;
                newPosition.z -= CameraDistanceOffset; // Negativo para alejar la cámara

                // Aplicar la nueva posición
                CinemachineCameraTarget.transform.localPosition = newPosition;
            }
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDSlide = Animator.StringToHash("Slide");
            _animIDWallRun = Animator.StringToHash("Wall Run");
            _animIDHorizontal = Animator.StringToHash("Horizontal");
            _animIDHitLegs = Animator.StringToHash("HitLegs");
            _animIDHitShelf = Animator.StringToHash("HitShelf");
        }


        private void Update()
        {
            if (GameMenu.Instance != null && GameMenu.Instance.isPaused) return;
            if (!_gameStarted && Input.GetKeyDown(KeyCode.W)) StartMovement();
            if (_gameStarted)
            {
                CameraRotation();
                UpdateCameraPosition(); // Actualizar posición de la cámara
            }
            _hasAnimator = TryGetComponent(out _animator);
            GroundedCheck();
            if (!_isWallRunning) JumpAndGravity();
            HandleSlide();
            HandleWallRunInput();

            _animator.applyRootMotion = (_isWallRunning || _isHit || _isShelfHit);
          
            if (_isHit || _isShelfHit)
            {
                if (Cursor.visible == false)
                {
                    _input.cursorLocked = false;
                    _input.cursorInputForLook = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                if (_hitStopTimer > 0f)
                {
                    _hitStopTimer -= Time.deltaTime;
                    Move();
                }
                else
                {
                    StopMovement();
                }
            }
            else
            {
                Move();
            }

            UpdateFootstepTimer();
        }

        private void UpdateFootstepTimer()
        {
            if (Grounded && !_isSliding && !_isWallRunning && !_isHit && !_isShelfHit)
            {
                if (AudioSettings.dspTime >= _nextFootstepTime)
                {
                    SoundManager.PlaySound(SoundType.Footstep, null, FootstepAudioVolume);
                    _nextFootstepTime = AudioSettings.dspTime + FootstepInterval;
                }
            }
        }

        // Nueva función para actualizar la posición de la cámara durante el juego
        private void UpdateCameraPosition()
        {
            if (CinemachineCameraTarget != null)
            {
                // Obtener la posición actual
                Vector3 currentPosition = CinemachineCameraTarget.transform.localPosition;

                // Calcular la nueva posición basada en los offsets del inspector
                Vector3 basePosition = new Vector3(
                    currentPosition.x - CameraLateralOffset,
                    currentPosition.y - CameraHeightOffset,
                    currentPosition.z + CameraDistanceOffset
                );

                // Aplicar los offsets actualizados
                Vector3 newPosition = basePosition;
                newPosition.y += CameraHeightOffset;
                newPosition.x += CameraLateralOffset;
                newPosition.z -= CameraDistanceOffset;

                // Aplicar la nueva posición solo si ha cambiado
                if (Vector3.Distance(currentPosition, newPosition) > 0.001f)
                {
                    CinemachineCameraTarget.transform.localPosition = newPosition;
                }
            }
        }

        private void CameraRotation()
        {
            // Si el jugador está muerto, activar el cursor
            if (_isHit || _isShelfHit)
            {
                _input.cursorLocked = false;
                _input.cursorInputForLook = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            // Modo automático: la cámara siempre sigue al jugador
            if (LockCameraPosition)
            {
                // Normalizar el ángulo del jugador para evitar saltos de 360 grados
                float playerAngle = transform.eulerAngles.y;
                float currentAngle = _cinemachineTargetYaw % 360;

                // Calcular la diferencia de ángulo más corta
                float diff = Mathf.DeltaAngle(currentAngle, playerAngle);

                // Aplicar la rotación suavemente
                _cinemachineTargetYaw += diff * Time.deltaTime * 5.0f;
            }
            // Modo manual: solo el mouse controla la cámara
            else if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // Aplicar límites y rotación final
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Aplicar rotaciones adicionales desde el inspector
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride + CameraPitchOffset,
                _cinemachineTargetYaw + CameraYawOffset,
                0.0f);
        }

        private void Move()
        {
            if (_isWallRunning) return;

            Vector2 rawInput = _input.move;
            rawInput.y = 1f;
            _input.move = rawInput;
            _speed = MoveSpeed;
            _animationBlend = MoveSpeed;

            Vector3 movement = transform.forward * _speed + transform.right * (_input.move.x * LateralSpeedMultiplier);
            _controller.Move(movement * Time.deltaTime + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, 1f);
                _animator.SetFloat(_animIDHorizontal, _currentHorizontal = Mathf.Lerp(
                    _currentHorizontal, _input.move.x * LateralSpeedMultiplier, Time.deltaTime / HorizontalSmoothTime));
            }
        }

        public void StartMovement()
        {
            _gameStarted = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (_animator != null) _animator.speed = 1f;

            // volver a bloquear cursor para jugar
            _input.cursorLocked = true;
            _input.cursorInputForLook = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }


        public void StopMovement()
        {
            _speed = 0f;
            _animationBlend = 0f;
            _currentHorizontal = 0f;
        }

        public void OnGamePaused()
        {
            Debug.Log("ThirdPersonController: Juego pausado");
        }

        public void OnGameResumed()
        {
            Debug.Log("ThirdPersonController: Juego reanudado");
        }

        public void RestartLvl()
        {
            _isHit = false;
            _isShelfHit = false;
            Debug.Log("RestartLvl: Estados de impacto reiniciados.");
        }

        public void LastCheckPoint()
        {
            Debug.Log("LastCheckPoint: Estados de impacto reiniciados.");
        }
        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                    _verticalVelocity = -2f;

                if (!_isSliding && !_wallRunPending && _input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    if (_hasAnimator) _animator.SetBool(_animIDJump, true);
                }

                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                    _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator)
                    _animator.SetBool(_animIDFreeFall, true);

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        private void HandleSlide()
        {
            if (_isSliding)
            {
                _slideTimer -= Time.deltaTime;
                if (_slideTimer <= 0f)
                    StartCoroutine(RestoreControllerValues());
                return;
            }

            if (Grounded && !_isSliding && !_isWallRunning && !_wallRunPending &&
                (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.LeftControl)))
            {
                StartSlide();
            }
        }

        private void StartSlide()
        {
            _isSliding = true;
            _slideTimer = SlideDuration;
            _controller.height = SlideHeight;
            _controller.center = SlideCenter;
            if (_hasAnimator)
                _animator.SetBool(_animIDSlide, true);
        }

        private IEnumerator RestoreControllerValues()
        {
            float elapsed = 0f;
            float startHeight = _controller.height;
            Vector3 startCenter = _controller.center;

            while (elapsed < slideRestoreTime)
            {
                _controller.height = Mathf.Lerp(startHeight, _originalHeight, elapsed / slideRestoreTime);
                _controller.center = Vector3.Lerp(startCenter, _originalCenter, elapsed / slideRestoreTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _controller.height = _originalHeight;
            _controller.center = _originalCenter;
            _isSliding = false;

            if (_hasAnimator)
                _animator.SetBool(_animIDSlide, false);
        }

        private void HandleWallRunInput()
        {
            if (_isWallRunning)
            {
                _wallRunTimer -= Time.deltaTime;
                if (_wallRunTimer <= 0f) EndWallRun();
                return;
            }

            if (!_isSliding && !_isWallRunning && Input.GetKeyDown(KeyCode.O))
            {
                StartWallRun();
            }
        }

        private void StartWallRun()
        {
            _isWallRunning = true;
            _wallRunTimer = WallRunDuration;
            _verticalVelocity = 0f;

            if (_hasAnimator)
                _animator.SetBool(_animIDWallRun, true);
        }

        private void EndWallRun()
        {
            _isWallRunning = false;
            if (_hasAnimator)
                _animator.SetBool(_animIDWallRun, false);

            _targetYRotation += WallRunRotationAngle;
            StartCoroutine(SmoothRotate(_targetYRotation, WallRunSmoothTime));
        }

        private IEnumerator DelayWallRun()
        {
            yield return new WaitForSeconds(wallRunDelay);
            StartWallRun();
            _wallRunPending = false;
        }

        private IEnumerator SmoothRotate(float targetAngle, float duration)
        {
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(transform.eulerAngles.x, targetAngle, transform.eulerAngles.z);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = endRotation;
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            if (_hasAnimator)
                _animator.SetBool(_animIDGrounded, Grounded);
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
        private void OnGUI()
        {
            // Solo mostrar si el juego no ha comenzado
            if (_gameStarted) return;

            int panelWidth = 400;
            int panelHeight = 360;
            int centerX = Screen.width / 2 - panelWidth / 2;
            int centerY = Screen.height / 2 - panelHeight / 2;

            // Fondo negro completo
            GUI.color = startMenuBackgroundColor;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Panel principal con borde fino
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            GUI.DrawTexture(new Rect(centerX, centerY, panelWidth, panelHeight), Texture2D.whiteTexture);

            // Borde del panel
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.4f);
            int borderWidth = 1;
            GUI.DrawTexture(new Rect(centerX - borderWidth, centerY - borderWidth, panelWidth + borderWidth * 2, borderWidth), Texture2D.whiteTexture); // superior
            GUI.DrawTexture(new Rect(centerX - borderWidth, centerY + panelHeight, panelWidth + borderWidth * 2, borderWidth), Texture2D.whiteTexture); // inferior
            GUI.DrawTexture(new Rect(centerX - borderWidth, centerY, borderWidth, panelHeight), Texture2D.whiteTexture); // izquierdo
            GUI.DrawTexture(new Rect(centerX + panelWidth, centerY, borderWidth, panelHeight), Texture2D.whiteTexture); // derecho
            GUI.color = Color.white;

            // Título
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(centerX, centerY + 20, panelWidth, 40), "Run to the Melody", titleStyle);

            // Subtítulo
            GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            GUI.Label(new Rect(centerX, centerY + 60, panelWidth, 30), "Presiona W para comenzar", subtitleStyle);

            // Separador
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            GUI.DrawTexture(new Rect(centerX + 40, centerY + 100, panelWidth - 80, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Configuraciones
            GUIStyle optionLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            GUI.Label(new Rect(centerX, centerY + 110, panelWidth, 30), "CONFIGURACIÓN", optionLabelStyle);

            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 15,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            // Personalizar el estilo del toggle
            toggleStyle.overflow = new RectOffset(0, 0, 0, 0);
            toggleStyle.margin = new RectOffset(10, 10, 4, 4);
            toggleStyle.padding = new RectOffset(20, 0, 0, 0);

            // Opciones
            int toggleY = centerY + 145;
            startAutoCam = GUI.Toggle(new Rect(centerX + 80, toggleY, panelWidth - 160, 25), startAutoCam, "Cámara automática", toggleStyle);
            startMuted = GUI.Toggle(new Rect(centerX + 80, toggleY + 30, panelWidth - 160, 25), startMuted, "Silenciar audio", toggleStyle);

            // Separador
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            GUI.DrawTexture(new Rect(centerX + 40, toggleY + 65, panelWidth - 80, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Controles
            GUI.Label(new Rect(centerX, toggleY + 75, panelWidth, 30), "CONTROLES", optionLabelStyle);

            GUIStyle controlsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            // Grid de controles
            int gridY = toggleY + 105;
            int rowHeight = 24;
            int col1 = centerX + 80;
            int col2 = centerX + 200;

            // Teclas - Columna 1
            GUI.Label(new Rect(col1, gridY, 120, rowHeight), "A / D o FLECHAS", controlsStyle);
            GUI.Label(new Rect(col1, gridY + rowHeight, 120, rowHeight), "CTRL / S", controlsStyle);
            GUI.Label(new Rect(col1, gridY + rowHeight * 2, 120, rowHeight), "ESPACIO", controlsStyle);
            GUI.Label(new Rect(col1, gridY + rowHeight * 3, 120, rowHeight), "ESC", controlsStyle);

            // Acciones - Columna 2
            GUI.Label(new Rect(col2, gridY, 120, rowHeight), "Movimiento", controlsStyle);
            GUI.Label(new Rect(col2, gridY + rowHeight, 120, rowHeight), "Deslizar", controlsStyle);
            GUI.Label(new Rect(col2, gridY + rowHeight * 2, 120, rowHeight), "Saltar", controlsStyle);
            GUI.Label(new Rect(col2, gridY + rowHeight * 3, 120, rowHeight), "Pausa", controlsStyle);

            // Procesar tecla W para comenzar
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W)
            {
                // Aplicar opciones
                LockCameraPosition = startAutoCam;
                _input.cursorInputForLook = !startAutoCam;
                _input.cursorLocked = !startAutoCam;
                Cursor.lockState = !startAutoCam ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = startAutoCam;

                // Configurar audio
                AudioListener.volume = startMuted ? 0f : 1f;

                // Iniciar juego
                StartMovement();
            }
        }

        // Función para dibujar un panel con bordes redondeados
        private void DrawRoundedPanel(Rect position, Color color, float borderWidth = 0f)
        {
            // Panel principal
            GUI.color = color;
            GUI.DrawTexture(position, Texture2D.whiteTexture);

            // Borde
            if (borderWidth > 0)
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

                // Borde superior
                GUI.DrawTexture(new Rect(position.x, position.y, position.width, borderWidth), Texture2D.whiteTexture);
                // Borde inferior
                GUI.DrawTexture(new Rect(position.x, position.y + position.height - borderWidth, position.width, borderWidth), Texture2D.whiteTexture);
                // Borde izquierdo
                GUI.DrawTexture(new Rect(position.x, position.y, borderWidth, position.height), Texture2D.whiteTexture);
                // Borde derecho
                GUI.DrawTexture(new Rect(position.x + position.width - borderWidth, position.y, borderWidth, position.height), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        // Función para dibujar un encabezado con degradado
        private void DrawGradientHeader(Rect position, Color startColor, Color endColor)
        {
            // Color inicial
            GUI.color = startColor;
            GUI.DrawTexture(position, Texture2D.whiteTexture);

            // Simular un degradado vertical simple
            int steps = 10;
            float stepHeight = position.height / steps;

            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / steps;
                GUI.color = Color.Lerp(startColor, endColor, t);
                GUI.DrawTexture(new Rect(position.x, position.y + i * stepHeight, position.width, stepHeight), Texture2D.whiteTexture);
            }

            // Restablecer color
            GUI.color = Color.white;
        }

        // Función para dibujar un separador horizontal
        private void DrawSeparator(Rect position, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(position, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }


       /* private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
            Gizmos.color = Grounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        } */

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.gameObject.CompareTag("Wall Run") && !_isWallRunning && !_wallRunPending)
            {
                _wallRunPending = true;
                StartCoroutine(DelayWallRun());
            }

            if (hit.gameObject.CompareTag("HitLegs") && !_isHit)
            {
                float verticalComponent = Vector3.Dot(hit.normal, Vector3.up);
                if (verticalComponent <= 0.05f)
                {
                    _isHit = true;
                    _animator.SetBool(_animIDHitLegs, true);
                    _hitStopTimer = hitStopDelay;
                }
            }

            if (hit.gameObject.CompareTag("HitShelf") && !_isShelfHit)
            {
                _isShelfHit = true;
                _animator.SetBool(_animIDHitShelf, true);
                _hitStopTimer = hitStopDelay;
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            // El Tag debe coincidir exactamente con el que usás en tu zona de Wall Run
            if (other.CompareTag("Wall Run") && !_isWallRunning && !_wallRunPending)
            {
                _wallRunPending = true;
                StartCoroutine(DelayWallRun());
            }
        }
    }
}
