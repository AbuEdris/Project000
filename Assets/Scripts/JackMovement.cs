using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Advanced player controller for "Dreams Lost" - Project 000
/// Handles movement, mental state effects, reality transitions, and camera control
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    #region Components
    private CharacterController controller;
    private Camera playerCamera;
    private AudioSource audioSource;
    private Animator animator;
    #endregion

    #region Movement Parameters
    [Header("Basic Movement")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 4.0f;
    [SerializeField] private float crouchSpeed = 1.0f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float airControlFactor = 0.5f;

    [Header("Health & Mental State")]
    [Range(0, 100)]
    [SerializeField] private float mentalHealth = 100f;
    [SerializeField] private float mentalHealthDecayRate = 0.05f;
    [SerializeField] private float healthyMovementModifier = 1.2f;
    [SerializeField] private float criticalHealthThreshold = 20f;
    [SerializeField] private float breathingIntensity = 0.3f;
    
    [Header("Reality Transition")]
    [SerializeField] private float realityBlendSpeed = 2.0f;
    [SerializeField] private List<RealityState> realityStates = new List<RealityState>();
    [SerializeField] private int currentRealityIndex = 0;
    [SerializeField] private float trembleFrequency = 0.5f;
    [SerializeField] private float trembleAmplitude = 0.3f;
    
    [Header("Camera Controls")]
    [SerializeField] private float lookSensitivity = 2.0f;
    [SerializeField] private float maxLookAngle = 80.0f;
    [SerializeField] private float cameraTiltSpeed = 2.0f;
    [SerializeField] private float maxCameraTilt = 5.0f;
    [SerializeField] private float headbobSpeed = 4.0f;
    [SerializeField] private float headbobAmount = 0.05f;
    
    [Header("Advanced Movement")]
    [SerializeField] private float momentumFactor = 0.1f;
    [SerializeField] private float staminaMax = 100f;
    [SerializeField] private float staminaRecoveryRate = 10f;
    [SerializeField] private float staminaDepletionRate = 20f;
    [SerializeField] private float wallSlideSpeed = 2.0f;
    #endregion

    #region State Variables
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private Vector3 momentum = Vector3.zero;
    private float currentSpeed;
    private float verticalRotation = 0f;
    private float headbobCycle = 0f;
    private float footstepTimer = 0f;
    private float cameraBaseline;
    private float stamina;
    private float currentCameraTilt = 0f;
    private float blendFactor = 0f;
    private float realityTransitionTimer = 0f;
    
    private bool isGrounded;
    private bool isRunning;
    private bool isCrouching;
    private bool isBreathingHeavily;
    private bool isHallucinating;
    private bool isInTransition;
    private bool isDualControlActive;
    private bool isWallSliding;
    
    private Vector3 normalCameraPosition;
    private Quaternion normalCameraRotation;
    
    private RealityState currentReality;
    private RealityState targetReality;
    
    // For dual shadow syndrome mechanic
    private Transform shadowSelfTransform;
    #endregion

    #region Sound Effects
    [Header("Audio")]
    [SerializeField] private AudioClip[] footstepSoundsNormal;
    [SerializeField] private AudioClip[] footstepSoundsDream;
    [SerializeField] private AudioClip[] breathingSounds;
    [SerializeField] private AudioClip[] hallucinationSounds;
    [SerializeField] private AudioClip heartbeatSound;
    [SerializeField] private AudioClip realityShiftSound;
    #endregion

    #region Particle Effects
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem mentalDecayParticles;
    [SerializeField] private ParticleSystem realityTransitionParticles;
    [SerializeField] private GameObject shadowTrailPrefab;
    [SerializeField] private Material standardWorldMaterial;
    [SerializeField] private Material dreamWorldMaterial;
    [SerializeField] private Material stationWorldMaterial;
    #endregion

    #region Custom Classes
    [System.Serializable]
    public class RealityState
    {
        public string realityName;
        public float gravityModifier = 1f;
        public float movementSpeedModifier = 1f;
        public float cameraEffectsIntensity = 0f;
        public Color ambientLightColor = Color.white;
        public PostProcessingProfile postProcessingProfile;
        public AudioClip ambientSound;
        
        [Header("Visual Distortions")]
        public float chromaticAberration = 0f;
        public float vignette = 0f;
        public float grainIntensity = 0f;
        
        [Header("Movement Modifiers")]
        public bool allowDoubleJump = false;
        public bool allowWallSlide = false;
        public bool invertControls = false;
        public bool gravitationalShifts = false;
        public float timeScale = 1f;
    }
    
    // Simple class to represent post-processing profiles
    // In a real project, you'd use Unity's post-processing system
    [System.Serializable]
    public class PostProcessingProfile
    {
        public float bloom;
        public float contrast;
        public float colorGrading;
    }
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        // Set up camera
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        normalCameraPosition = playerCamera.transform.localPosition;
        normalCameraRotation = playerCamera.transform.localRotation;
        cameraBaseline = playerCamera.transform.localPosition.y;
        
        // Initial state
        currentSpeed = walkSpeed;
        stamina = staminaMax;
        
        // Set up reality states if not already defined
        if (realityStates.Count == 0)
        {
            SetupDefaultRealityStates();
        }
        
        currentReality = realityStates[currentRealityIndex];
        targetReality = currentReality;
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        InitializeShadowSelf();
    }

    private void Update()
    {
        // Calculate ground state
        isGrounded = controller.isGrounded;
        
        // Handle all input
        HandleInput();
        
        // Update mental state
        UpdateMentalState();
        
        // Handle movement
        HandleMovement();
        
        // Handle camera effects
        HandleCameraEffects();
        
        // Handle reality transitions
        HandleRealityTransition();
        
        // Apply all movement
        ApplyFinalMovement();
        
        // Update UI elements if needed
        UpdateUI();
    }

    private void LateUpdate()
    {
        // Camera movement that should happen after character movement
        ApplyCameraEffects();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Check for wall sliding
        if (!isGrounded && hit.normal.y < 0.1f && currentReality.allowWallSlide)
        {
            isWallSliding = true;
            velocity.y = -wallSlideSpeed;
            // Generate wall dust particles
            if (mentalDecayParticles != null)
            {
                mentalDecayParticles.transform.position = hit.point;
                mentalDecayParticles.transform.rotation = Quaternion.LookRotation(hit.normal);
                mentalDecayParticles.Emit(5);
            }
        }
        else
        {
            isWallSliding = false;
        }
    }
    #endregion

    #region Initialization Methods
    private void SetupDefaultRealityStates()
    {
        // Reality - Normal world
        RealityState reality = new RealityState
        {
            realityName = "Reality",
            gravityModifier = 1f,
            movementSpeedModifier = 1f,
            cameraEffectsIntensity = 0f,
            ambientLightColor = new Color(0.9f, 0.9f, 1f),
            postProcessingProfile = new PostProcessingProfile { bloom = 0.2f, contrast = 0.5f, colorGrading = 0.5f },
            allowDoubleJump = false,
            allowWallSlide = false,
            invertControls = false,
            timeScale = 1f
        };
        
        // Dream - Nightmare world
        RealityState dream = new RealityState
        {
            realityName = "Nightmare",
            gravityModifier = 0.8f,
            movementSpeedModifier = 0.7f,
            cameraEffectsIntensity = 0.5f,
            ambientLightColor = new Color(0.6f, 0.2f, 0.2f),
            postProcessingProfile = new PostProcessingProfile { bloom = 0.8f, contrast = 0.7f, colorGrading = 0.2f },
            chromaticAberration = 0.4f,
            vignette = 0.6f,
            grainIntensity = 0.3f,
            allowDoubleJump = true,
            allowWallSlide = true,
            invertControls = false,
            gravitationalShifts = true,
            timeScale = 0.9f
        };
        
        // Station - The in-between space
        RealityState station = new RealityState
        {
            realityName = "Station",
            gravityModifier = 0.5f,
            movementSpeedModifier = 1.2f,
            cameraEffectsIntensity = 0.3f,
            ambientLightColor = new Color(1f, 1f, 1.2f),
            postProcessingProfile = new PostProcessingProfile { bloom = 0.9f, contrast = 0.3f, colorGrading = 0.8f },
            chromaticAberration = 0.2f,
            vignette = 0.4f,
            grainIntensity = 0.1f,
            allowDoubleJump = true,
            allowWallSlide = true,
            invertControls = false,
            gravitationalShifts = false,
            timeScale = 1.1f
        };
        
        // Hospital - Final chapter
        RealityState hospital = new RealityState
        {
            realityName = "Hospital",
            gravityModifier = 1.1f,
            movementSpeedModifier = 0.8f,
            cameraEffectsIntensity = 0.2f,
            ambientLightColor = new Color(0.9f, 0.95f, 1f),
            postProcessingProfile = new PostProcessingProfile { bloom = 0.5f, contrast = 0.6f, colorGrading = 0.7f },
            chromaticAberration = 0.1f,
            vignette = 0.2f,
            grainIntensity = 0.05f,
            allowDoubleJump = false,
            allowWallSlide = false,
            invertControls = false,
            gravitationalShifts = false,
            timeScale = 1f
        };
        
        realityStates.Add(reality);
        realityStates.Add(dream);
        realityStates.Add(station);
        realityStates.Add(hospital);
    }

    private void InitializeShadowSelf()
    {
        // Create the "other self" for the Dual Shadow Syndrome mechanic
        GameObject shadowSelf = Instantiate(new GameObject("ShadowSelf"), transform.position, transform.rotation);
        shadowSelfTransform = shadowSelf.transform;
        
        // Add visual representation
        if (shadowTrailPrefab != null)
        {
            Instantiate(shadowTrailPrefab, shadowSelfTransform);
        }
        
        // Initially disable
        shadowSelf.SetActive(false);
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        // Movement Input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Apply control inversion if the current reality requires it
        if (currentReality.invertControls)
        {
            horizontal = -horizontal;
            vertical = -vertical;
        }
        
        // Create movement vector
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        
        // Transform direction based on camera look direction
        if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + playerCamera.transform.eulerAngles.y;
            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            
            // Apply momentum
            momentum = Vector3.Lerp(momentum, moveDirection * currentSpeed, momentumFactor * Time.deltaTime);
        }
        else
        {
            // Slow down gradually
            momentum = Vector3.Lerp(momentum, Vector3.zero, momentumFactor * 2f * Time.deltaTime);
            moveDirection = Vector3.zero;
        }
        
        // Handle running
        HandleRunning();
        
        // Handle crouching
        HandleCrouching();
        
        // Handle jumping
        HandleJumping();
        
        // Mouse Look
        HandleMouseLook();
        
        // Reality shift (for testing or special moments)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleReality();
        }
        
        // Dual shadow control toggle (for special game mechanics)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleDualControl();
        }
    }

    private void HandleRunning()
    {
        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && stamina > 0)
        {
            isRunning = true;
            currentSpeed = runSpeed * currentReality.movementSpeedModifier;
            stamina -= staminaDepletionRate * Time.deltaTime;
            
            // Start heavy breathing when stamina is low
            if (stamina < staminaMax * 0.3f && !isBreathingHeavily)
            {
                StartCoroutine(HeavyBreathingCoroutine());
            }
        }
        else
        {
            isRunning = false;
            currentSpeed = isCrouching ? crouchSpeed : walkSpeed;
            currentSpeed *= currentReality.movementSpeedModifier;
            
            // Recover stamina
            if (!isRunning)
            {
                stamina = Mathf.Min(staminaMax, stamina + staminaRecoveryRate * Time.deltaTime);
            }
        }
        
        // Modify speed based on mental health
        if (mentalHealth > 80)
        {
            currentSpeed *= healthyMovementModifier;
        }
        else if (mentalHealth < criticalHealthThreshold)
        {
            currentSpeed *= 0.7f; // Slow when critical
        }
    }

    private void HandleCrouching()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
            
            // Change collider height and camera position
            if (isCrouching)
            {
                controller.height = 1.0f;
                controller.center = new Vector3(0, -0.5f, 0);
                playerCamera.transform.localPosition = new Vector3(
                    normalCameraPosition.x,
                    normalCameraPosition.y - 0.5f,
                    normalCameraPosition.z
                );
                currentSpeed = crouchSpeed * currentReality.movementSpeedModifier;
            }
            else
            {
                controller.height = 2.0f;
                controller.center = Vector3.zero;
                playerCamera.transform.localPosition = normalCameraPosition;
                currentSpeed = isRunning ? runSpeed : walkSpeed;
                currentSpeed *= currentReality.movementSpeedModifier;
            }
            
            // Update animator
            if (animator != null)
            {
                animator.SetBool("IsCrouching", isCrouching);
            }
        }
    }

    private void HandleJumping()
    {
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity * currentReality.gravityModifier);
            
            // Update animator
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
            
            // Play sound
            PlayJumpSound();
        }
        else if (!isGrounded && Input.GetButtonDown("Jump") && currentReality.allowDoubleJump)
        {
            // Double jump in dream world or station
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity * currentReality.gravityModifier) * 0.8f;
            
            // Visual effect for double jump
            if (realityTransitionParticles != null)
            {
                realityTransitionParticles.Emit(20);
            }
        }
    }

    private void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
        
        // Add "shakiness" based on mental health
        if (mentalHealth < 50)
        {
            float shakeFactor = Mathf.Lerp(0, 0.5f, (50 - mentalHealth) / 50);
            mouseX += Mathf.Sin(Time.time * trembleFrequency * 10) * trembleAmplitude * shakeFactor;
            mouseY += Mathf.Cos(Time.time * trembleFrequency * 10) * trembleAmplitude * shakeFactor;
        }
        
        // Rotate the player left/right
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate the camera up/down
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, currentCameraTilt);
    }

    private void CycleReality()
    {
        if (isInTransition) return;
        
        // Play reality shift sound
        if (audioSource != null && realityShiftSound != null)
        {
            audioSource.PlayOneShot(realityShiftSound);
        }
        
        // Start transition
        currentRealityIndex = (currentRealityIndex + 1) % realityStates.Count;
        targetReality = realityStates[currentRealityIndex];
        isInTransition = true;
        blendFactor = 0f;
        
        // Visual effect
        if (realityTransitionParticles != null)
        {
            realityTransitionParticles.Play();
        }
    }

    private void ToggleDualControl()
    {
        // This will be used in later chapters when the player can switch between the two versions of Jack
        isDualControlActive = !isDualControlActive;
        
        if (shadowSelfTransform != null)
        {
            shadowSelfTransform.gameObject.SetActive(isDualControlActive);
            
            if (isDualControlActive)
            {
                // Position shadow self opposite to player
                shadowSelfTransform.position = transform.position - transform.forward * 2f;
                shadowSelfTransform.rotation = transform.rotation;
            }
        }
    }
    #endregion

    #region Movement Handling
    private void HandleMovement()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small constant to ensure grounding
        }
        
        // Apply reality's gravity
        velocity.y += gravity * currentReality.gravityModifier * Time.deltaTime;
        
        // Handle gravitational shifts in nightmare world
        if (currentReality.gravitationalShifts)
        {
            float gravitationalShift = Mathf.Sin(Time.time * 0.5f) * 0.2f;
            velocity.y += gravitationalShift;
        }
        
        // Add movement direction to velocity
        Vector3 move = moveDirection * currentSpeed;
        
        // Add momentum
        move += momentum;
        
        // Reduce air control
        if (!isGrounded)
        {
            move *= airControlFactor;
        }
        
        // Trigger footstep sounds
        if (isGrounded && moveDirection.magnitude > 0.1f)
        {
            footstepTimer += Time.deltaTime * currentSpeed;
            if (footstepTimer >= 0.5f)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
        
        // Update animator
        if (animator != null)
        {
            animator.SetFloat("Speed", moveDirection.magnitude);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsRunning", isRunning);
        }
    }

    private void ApplyFinalMovement()
    {
        // Combine directional movement and gravity
        Vector3 finalMovement = moveDirection * currentSpeed + Vector3.up * velocity.y;
        
        // Apply time scale from current reality
        finalMovement *= currentReality.timeScale;
        
        // Move the character controller
        controller.Move(finalMovement * Time.deltaTime);
    }
    #endregion

    #region Camera Effects
    private void HandleCameraEffects()
    {
        // Camera tilt based on movement
        float targetTilt = 0f;
        if (moveDirection.magnitude > 0.1f)
        {
            // Tilt in the direction of horizontal movement
            targetTilt = -moveDirection.x * maxCameraTilt;
        }
        currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetTilt, cameraTiltSpeed * Time.deltaTime);
        
        // Apply headbob effect when moving
        if (isGrounded && moveDirection.magnitude > 0.1f)
        {
            // Calculate headbob
            headbobCycle += Time.deltaTime * headbobSpeed * (isRunning ? 1.5f : 1.0f);
            float headbobOffset = Mathf.Sin(headbobCycle) * headbobAmount;
            
            // Increase headbob in dream world
            if (currentReality.realityName == "Nightmare")
            {
                headbobOffset *= 1.5f;
            }
            
            // Mental state affects headbob
            if (mentalHealth < 50)
            {
                float mentalFactor = (50 - mentalHealth) / 50;
                headbobOffset *= (1 + mentalFactor * 0.5f);
            }
        }
    }

    private void ApplyCameraEffects()
    {
        // Apply camera shake based on mental state
        if (mentalHealth < 50)
        {
            float shakeFactor = Mathf.Lerp(0, 1.0f, (50 - mentalHealth) / 50);
            float shakeX = Mathf.Sin(Time.time * trembleFrequency) * trembleAmplitude * shakeFactor;
            float shakeY = Mathf.Cos(Time.time * trembleFrequency * 1.3f) * trembleAmplitude * shakeFactor;
            
            playerCamera.transform.localPosition = new Vector3(
                normalCameraPosition.x + shakeX,
                normalCameraPosition.y + shakeY,
                normalCameraPosition.z
            );
            
            // Apply chromatic aberration and other post-processing effects here
            // In a real project, you'd use Unity's post-processing system
            // For this example code, we just mention it
        }
    }
    #endregion

    #region State Management
    private void UpdateMentalState()
    {
        // Mental health decreases over time
        mentalHealth -= mentalHealthDecayRate * Time.deltaTime;
        mentalHealth = Mathf.Clamp(mentalHealth, 0, 100);
        
        // Trigger hallucinations at low mental health
        if (mentalHealth < 30 && !isHallucinating && Random.value < 0.0005f)
        {
            StartCoroutine(HallucinationCoroutine());
        }
        
        // Update mental decay particles
        if (mentalDecayParticles != null)
        {
            var emission = mentalDecayParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0, 20, (100 - mentalHealth) / 100);
        }
    }

    private void HandleRealityTransition()
    {
        if (isInTransition)
        {
            // Increase blend factor
            blendFactor += realityBlendSpeed * Time.deltaTime;
            
            if (blendFactor >= 1.0f)
            {
                // Transition complete
                currentReality = targetReality;
                isInTransition = false;
                blendFactor = 0f;
                
                // Stop transition particles
                if (realityTransitionParticles != null && realityTransitionParticles.isPlaying)
                {
                    realityTransitionParticles.Stop();
                }
            }
            else
            {
                // Apply transitional effects
                // Like blending between reality materials, post-processing, etc.
                // This would tie into your shader system
            }
        }
    }

    // Updates any UI elements that show player state
    private void UpdateUI()
    {
        // This would connect to your UI system
        // For example updating mental health bar, stamina indicator, etc.
    }
    #endregion

    #region Audio
    private void PlayFootstepSound()
    {
        if (audioSource == null) return;
        
        AudioClip[] currentFootstepSounds = currentReality.realityName == "Nightmare" ? 
            footstepSoundsDream : footstepSoundsNormal;
        
        if (currentFootstepSounds.Length > 0)
        {
            AudioClip clip = currentFootstepSounds[Random.Range(0, currentFootstepSounds.Length)];
            audioSource.PlayOneShot(clip, isRunning ? 1.0f : 0.7f);
        }
    }

    private void PlayJumpSound()
    {
        // Jump sound would go here
    }
    #endregion

    #region Coroutines
    private IEnumerator HeavyBreathingCoroutine()
    {
        isBreathingHeavily = true;
        
        if (breathingSounds.Length > 0 && audioSource != null)
        {
            AudioClip breathClip = breathingSounds[Random.Range(0, breathingSounds.Length)];
            audioSource.PlayOneShot(breathClip, breathingIntensity);
        }
        
        yield return new WaitForSeconds(3.0f);
        isBreathingHeavily = false;
    }

    private IEnumerator HallucinationCoroutine()
    {
        isHallucinating = true;
        
        // Visual hallucination effect
        float originalFov = playerCamera.fieldOfView;
        float targetFov = originalFov + 15f;
        
        // Audio effect
        if (hallucinationSounds.Length > 0 && audioSource != null)
        {
            AudioClip hallucinationClip = hallucinationSounds[Random.Range(0, hallucinationSounds.Length)];
            audioSource.PlayOneShot(hallucinationClip);
        }
        
        // Increase FOV
        float elapsed = 0f;
        float duration = 2.0f;
        while (elapsed < duration)
        {
            playerCamera.fieldOfView = Mathf.Lerp(originalFov, targetFov, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Hold for a moment
        yield return new WaitForSeconds(1.0f);
        
        // Return to normal
        elapsed = 0f;
        while (elapsed < duration)
        {
            playerCamera.fieldOfView = Mathf.Lerp(targetFov, originalFov, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        playerCamera.fieldOfView = originalFov;
        
        // Potential to spawn hallucination enemy or object here
        
        isHallucinating = false;
    }
    #endregion

    #region Public Methods
    public void SetReality(int realityIndex)
    {
        if (realityIndex >= 0 && realityIndex < realityStates.Count && !isInTransition)
        {
            currentRealityIndex = realityIndex;
            targetReality = realityStates[currentRealityIndex];
            isInTransition = true;
            blendFactor = 0f;
            
            // Play transition effect
            if (realityTransitionParticles != null)
            {
                realityTransitionParticles.Play();
            }
            
            // Play sound
            if (audioSource != null && realityShiftSound != null)
            {
                audioSource.PlayOneShot(realityShiftSound);
            }
        }
    }

    public void ModifyMentalHealth(float amount)
    {
        mentalHealth = Mathf.Clamp(mentalHealth + amount, 0, 100);
        
        // Visual feedback
        if (amount < 0 && mentalDecayParticles != null)
        {
            mentalDecayParticles.Emit(Mathf.Abs(Mathf.RoundToInt(amount) * 2));
        }
        
        // Audio feedback for significant changes
        if (amount < -10 && audioSource != null && heartbeatSound != null)
        {
        audioSource.PlayOneShot(heartbeatSound);
        }
    }

    public float GetMentalHealth()
    {
        return mentalHealth;
    }

    public void ResetStamina()
    {
        stamina = staminaMax;
    }

    public void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        // Disable controller temporarily to avoid physics issues
        controller.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        controller.enabled = true;
        
        // Reset velocity to prevent unexpected movement
        velocity = Vector3.zero;
        momentum = Vector3.zero;
        
        // Visual effect for teleportation
        if (realityTransitionParticles != null)
        {
            realityTransitionParticles.Emit(30);
        }
    }

    public string GetCurrentRealityName()
    {
        return currentReality.realityName;
    }

    public void SetMovementEnabled(bool enabled)
    {
        // Used for cutscenes or story moments
        if (!enabled)
        {
            moveDirection = Vector3.zero;
            momentum = Vector3.zero;
        }
        
        // Disable input handling if needed
        this.enabled = enabled;
    }
    #endregion

    #region Shadow Self Mechanics
    private void UpdateShadowSelf()
    {
        if (!isDualControlActive || shadowSelfTransform == null)
            return;
            
        // In the dual shadow mode, the shadow self moves with delay or mirrored
        // depending on the specific chapter's mechanics

        // For mirrored movement (early chapters)
        if (currentReality.realityName == "Reality" || currentReality.realityName == "Hospital")
        {
            // Mirror position across an invisible plane
            Vector3 mirrorPosition = transform.position + 2f * (transform.position - shadowSelfTransform.position);
            shadowSelfTransform.position = Vector3.Lerp(shadowSelfTransform.position, mirrorPosition, 0.02f);
            
            // Shadow looks at player
            Vector3 lookDirection = transform.position - shadowSelfTransform.position;
            if (lookDirection != Vector3.zero)
            {
                shadowSelfTransform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
        // For delayed following (nightmare/station chapters)
        else
        {
            // Record player positions and replay them with delay
            shadowSelfTransform.position = Vector3.Lerp(shadowSelfTransform.position, transform.position, 0.05f);
            shadowSelfTransform.rotation = Quaternion.Lerp(shadowSelfTransform.rotation, transform.rotation, 0.05f);
        }
        
        // Generate trail effect
        if (shadowTrailPrefab != null && Time.frameCount % 5 == 0)
        {
            GameObject trail = Instantiate(shadowTrailPrefab, shadowSelfTransform.position, shadowSelfTransform.rotation);
            Destroy(trail, 2.0f); // Destroy trail after 2 seconds
        }
    }
    
    public void SwapWithShadow()
    {
        if (!isDualControlActive || shadowSelfTransform == null)
            return;
            
        // Swap positions with shadow self
        Vector3 tempPosition = transform.position;
        Quaternion tempRotation = transform.rotation;
        
        // Teleport player to shadow position
        TeleportPlayer(shadowSelfTransform.position, shadowSelfTransform.rotation);
        
        // Move shadow to player's old position
        shadowSelfTransform.position = tempPosition;
        shadowSelfTransform.rotation = tempRotation;
        
        // Mental health cost for swapping
        ModifyMentalHealth(-5f);
        
        // Visual and audio effect
        if (realityTransitionParticles != null)
        {
            realityTransitionParticles.transform.position = transform.position;
            realityTransitionParticles.Emit(50);
            
            realityTransitionParticles.transform.position = shadowSelfTransform.position;
            realityTransitionParticles.Emit(50);
        }
        
        if (audioSource != null && realityShiftSound != null)
        {
            audioSource.PlayOneShot(realityShiftSound, 0.7f);
        }
    }
    #endregion
    
    #region Environmental Interactions
    public void HandleInteractionTrigger(InteractionType type, float intensity)
    {
        switch (type)
        {
            case InteractionType.MemoryFlashback:
                StartCoroutine(MemoryFlashbackEffect(intensity));
                break;
                
            case InteractionType.RealityDistortion:
                StartCoroutine(RealityDistortionEffect(intensity));
                break;
                
            case InteractionType.MentalHealing:
                ModifyMentalHealth(intensity);
                break;
                
            case InteractionType.MentalDamage:
                ModifyMentalHealth(-intensity);
                break;
        }
    }
    
    private IEnumerator MemoryFlashbackEffect(float intensity)
    {
        // Flashback effect - intense white fade and audio
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.3f; // Slow motion effect
        
        // White fade effect
        // In a real project, you would use a post-processing volume or UI overlay
        // For this example, we just describe the effect
        
        // Audio effect - high pitch ringing
        if (audioSource != null && hallucinationSounds.Length > 0)
        {
            AudioClip clip = hallucinationSounds[Random.Range(0, hallucinationSounds.Length)];
            audioSource.pitch = 0.5f;
            audioSource.PlayOneShot(clip, intensity);
        }
        
        yield return new WaitForSecondsRealtime(intensity * 2);
        
        // Return to normal
        Time.timeScale = originalTimeScale;
        audioSource.pitch = 1.0f;
        
        // Memory effect can slightly restore mental health
        ModifyMentalHealth(intensity * 2);
    }
    
    private IEnumerator RealityDistortionEffect(float intensity)
    {
        // Reality warping effect
        float duration = intensity * 3.0f;
        float elapsed = 0f;
        
        // Original camera settings
        float originalFOV = playerCamera.fieldOfView;
        Vector3 originalPos = playerCamera.transform.localPosition;
        Quaternion originalRot = playerCamera.transform.localRotation;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float warpFactor = Mathf.Sin(t * Mathf.PI);
            
            // Distort camera
            playerCamera.fieldOfView = originalFOV + warpFactor * 30f;
            
            // Pulse chromatic aberration effect
            // This would connect to your post-processing system
            
            // Camera position distortion
            Vector3 distortedPos = originalPos + new Vector3(
                Mathf.Sin(Time.time * 10) * 0.05f * warpFactor,
                Mathf.Cos(Time.time * 8) * 0.05f * warpFactor,
                0
            );
            playerCamera.transform.localPosition = distortedPos;
            
            // Apply camera tilt based on reality distortion
            float tiltAmount = Mathf.Sin(Time.time * 3) * 10f * warpFactor;
            playerCamera.transform.localRotation = originalRot * Quaternion.Euler(0, 0, tiltAmount);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restore camera
        playerCamera.fieldOfView = originalFOV;
        playerCamera.transform.localPosition = originalPos;
        playerCamera.transform.localRotation = originalRot;
        
        // Reality distortion damages mental health
        ModifyMentalHealth(-intensity * 5);
    }
    
    public enum InteractionType
    {
        MemoryFlashback,
        RealityDistortion,
        MentalHealing,
        MentalDamage
    }
    #endregion
    
    #region Journal System
    // Dream journal entries - key storytelling mechanic
    private List<JournalEntry> journalEntries = new List<JournalEntry>();
    
    [System.Serializable]
    public class JournalEntry
    {
        public string title;
        public string content;
        public bool isCorrupted; // Some entries will be corrupted in the nightmare world
        public string realityOfOrigin; // Which reality this memory belongs to
    }
    
    public void AddJournalEntry(string title, string content, bool isCorrupted = false)
    {
        JournalEntry entry = new JournalEntry
        {
            title = title,
            content = content,
            isCorrupted = isCorrupted,
            realityOfOrigin = currentReality.realityName
        };
        
        journalEntries.Add(entry);
        
        // Visual feedback
        if (realityTransitionParticles != null)
        {
            realityTransitionParticles.transform.position = playerCamera.transform.position + playerCamera.transform.forward;
            realityTransitionParticles.Emit(10);
        }
        
        // Mental effect - journal entries slightly restore mental health
        if (!isCorrupted)
        {
            ModifyMentalHealth(5f);
        }
        else
        {
            // Corrupted entries slightly damage mental health
            ModifyMentalHealth(-2f);
        }
    }
    
    public List<JournalEntry> GetJournalEntries()
    {
        return journalEntries;
    }
    
    public void ClearCorruptedEntries()
    {
        // Used in specific story moments to "clear" Jack's mind
        journalEntries.RemoveAll(entry => entry.isCorrupted);
        ModifyMentalHealth(10f); // Mental boost from clearing corruption
    }
    #endregion
    
    #region Advanced Game-Specific Mechanics
    private void HandleAdvancedMechanics()
    {
        // This section would implement game-specific mechanics
        // like reality anchors, echo memories, and distortion fields
        
        // For example: Reality Anchors (objects that ground the player in a specific reality)
        CheckForRealityAnchors();
        
        // Echo Memories (when the player can see/hear past events)
        UpdateEchoMemories();
        
        // Handle the shadow self updates
        UpdateShadowSelf();
    }
    
    private void CheckForRealityAnchors()
    {
        // Find reality anchors in proximity
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5f);
        bool foundAnchor = false;
        
        foreach (var collider in hitColliders)
        {
            RealityAnchor anchor = collider.GetComponent<RealityAnchor>();
            if (anchor != null)
            {
                foundAnchor = true;
                
                // Reality anchors can stabilize mental health
                if (anchor.realityType == currentReality.realityName)
                {
                    ModifyMentalHealth(0.02f * Time.deltaTime);
                }
                
                // If we're close to an anchor that doesn't match our reality,
                // it might pull us into its reality
                else if (Random.value < 0.0001f && !isInTransition)
                {
                    int targetIndex = realityStates.FindIndex(r => r.realityName == anchor.realityType);
                    if (targetIndex >= 0)
                    {
                        SetReality(targetIndex);
                    }
                }
                
                break;
            }
        }
        
        // If no anchors nearby and in Station or Nightmare, slowly drift toward reality
        if (!foundAnchor && 
            (currentReality.realityName == "Nightmare" || currentReality.realityName == "Station") && 
            !isInTransition && Random.value < 0.0005f)
        {
            int realityIndex = realityStates.FindIndex(r => r.realityName == "Reality");
            if (realityIndex >= 0)
            {
                SetReality(realityIndex);
            }
        }
    }
    
    private void UpdateEchoMemories()
    {
        // Echo memories are remnants of past events that the player can witness
        // This would integrate with a separate EchoMemory system
        
        // For example: detect if player is in an echo zone
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 3f);
        
        foreach (var collider in hitColliders)
        {
            EchoMemory echo = collider.GetComponent<EchoMemory>();
            if (echo != null && !echo.hasBeenTriggered)
            {
                // Trigger the echo memory
                echo.TriggerEcho();
                
                // Add to journal
                AddJournalEntry("Echo: " + echo.echoTitle, echo.echoContent, currentReality.realityName == "Nightmare");
                
                break;
            }
        }
    }
    
    // These would be defined in separate classes
    // Just showing the interface here
    public class RealityAnchor : MonoBehaviour
    {
        public string realityType;
        public float stabilizationStrength = 1f;
    }
    
    public class EchoMemory : MonoBehaviour
    {
        public string echoTitle;
        public string echoContent;
        public bool hasBeenTriggered = false;
        
        public void TriggerEcho()
        {
            // Play visual/audio effects
            hasBeenTriggered = true;
        }
    }
    #endregion
}