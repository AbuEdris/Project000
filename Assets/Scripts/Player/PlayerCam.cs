using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class AlternativePsychCam : MonoBehaviour
{
    [Header("Camera Settings")]
    public float sensX = 100f;
    public float sensY = 100f;
    public Transform orientation;
    public Transform cameraPosition;
    public KeyCode altLookKey = KeyCode.LeftAlt; // Hold to look around without mouse
    public Vector2 gyroLookSensitivity = new Vector2(2f, 2f); // Arrow key look sensitivity
    
    [Header("Mental State Settings")]
    [Range(0f, 1f)] public float mentalStability = 1f; // 1 = stable, 0 = unstable
    public float stabilityDecayRate = 0.05f; // How fast stability decreases when stressed
    public float stabilityRecoveryRate = 0.02f; // How fast stability recovers when calm
    
    public enum WorldState { Reality, Nightmare, Station, Hospital, VoidRealm }
    public WorldState currentWorld = WorldState.Reality;
    
    [Header("Visual Effects")]
    public PostProcessVolume postProcessVolume;
    public float maxChromaticAberration = 1f;
    public float maxVignette = 0.5f;
    public float maxGrain = 0.5f;
    public float maxDistortion = 5f;
    
    [Header("Camera Movement")]
    public float normalSwayAmount = 0.02f;
    public float mentalInstabilitySwayMultiplier = 3f;
    public float breathingSpeed = 1f;
    public float breathingAmount = 0.02f;
    public float heartbeatAmplitude = 0.01f;
    public float heartbeatFrequency = 1.2f;
    
    [Header("Hallucination Effects")]
    public float hallucinationChance = 0.01f; // Chance per frame of triggering a hallucination
    public float hallucinationDuration = 2f;
    public float hallucinationIntensity = 2f;
    public AnimationCurve hallucinationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Memory Fragments")]
    public GameObject[] memoryPrefabs; // Ghostly objects that can appear when hallucinating
    public float memorySpawnChance = 0.3f; // Chance to spawn memory during hallucination
    public float memoryDuration = 4f;
    public float memoryDistance = 3f;
    
    [Header("Audio")]
    public AudioSource cameraAudioSource;
    public AudioClip[] heartbeatSounds;
    public AudioClip[] hallucinationSounds;
    public AudioClip[] memoryFragmentSounds;
    [Range(0f, 1f)] public float maxHeartbeatVolume = 0.5f;
    
    // Private variables
    private float xRotation;
    private float yRotation;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isHallucinating = false;
    private float timeSinceLastHeartbeat = 0f;
    private float currentHeartRate = 60f; // BPM
    private List<GameObject> activeMemoryFragments = new List<GameObject>();
    
    // Post processing effects
    private ChromaticAberration chromaticAberration;
    private Vignette vignette;
    private Grain grain;
    private LensDistortion lensDistortion;
    private ColorGrading colorGrading;
    
    // Stress detection
    private Vector3 lastPosition;
    private Vector3 lastForward;
    private float movementIntensity;
    private float rotationIntensity;
    private float cumulativeStress;
    
    private void Start()
    {
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Store original transform values
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        lastPosition = transform.position;
        lastForward = transform.forward;
        
        // Get post processing effects
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGetSettings(out chromaticAberration);
            postProcessVolume.profile.TryGetSettings(out vignette);
            postProcessVolume.profile.TryGetSettings(out grain);
            postProcessVolume.profile.TryGetSettings(out lensDistortion);
            postProcessVolume.profile.TryGetSettings(out colorGrading);
        }
        
        // Set initial world-specific settings
        UpdateWorldEffects();
    }
    
    private void Update()
    {
        // Process alternative look controls
        ProcessAlternativeLook();
        
        // Update stress level based on movement and rotation
        UpdateStressLevel();
        
        // Process mental state effects
        ProcessMentalStateEffects();
        
        // Check for hallucination trigger
        CheckForHallucination();
        
        // Handle heartbeat effect
        ProcessHeartbeat();
        
        // Debug controls for testing
        ProcessDebugControls();
    }
    
    private void LateUpdate()
    {
        // Update camera position
        transform.position = cameraPosition.position;
        
        // Apply camera effects (breathing, swaying)
        ApplyCameraEffects();
    }
    
    private void ProcessAlternativeLook()
    {
        float mouseX = 0f;
        float mouseY = 0f;
        
        // Arrow keys for looking when alt is held
        if (Input.GetKey(altLookKey))
        {
            mouseX = Input.GetAxis("Horizontal") * gyroLookSensitivity.x;
            mouseY = Input.GetAxis("Vertical") * gyroLookSensitivity.y;
        }
        // Normal mouse control otherwise
        else
        {
            mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
            mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;
        }
        
        // Apply mental stability effect to sensitivity (more erratic when unstable)
        float stabilityFactor = Mathf.Lerp(1.5f, 1f, mentalStability);
        mouseX *= stabilityFactor;
        mouseY *= stabilityFactor;
        
        // Add random camera drift based on mental instability
        if (mentalStability < 0.8f)
        {
            float driftAmount = (1f - mentalStability) * 0.05f;
            mouseX += Random.Range(-driftAmount, driftAmount);
            mouseY += Random.Range(-driftAmount, driftAmount);
        }
        
        // Calculate rotation
        yRotation += mouseX;
        xRotation -= mouseY;
        
        // Clamp vertical rotation
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        // Apply rotation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }
    
    private void ApplyCameraEffects()
    {
        if (isHallucinating)
            return; // Skip normal effects during hallucination
            
        Vector3 finalPosition = originalPosition;
        
        // Breathing effect
        float breathingValue = Mathf.Sin(Time.time * breathingSpeed) * breathingAmount;
        finalPosition.y += breathingValue;
        
        // Heartbeat effect (stronger when unstable)
        float heartbeatStrength = Mathf.Lerp(0.1f, 1.0f, 1f - mentalStability);
        if (currentHeartRate > 80f) // Only visible when heart rate is elevated
        {
            float heartbeatInterval = 60f / currentHeartRate;
            float heartbeatPhase = (Time.time % heartbeatInterval) / heartbeatInterval;
            
            // Double pulse heartbeat
            float pulse;
            if (heartbeatPhase < 0.1f)
                pulse = Mathf.Sin(heartbeatPhase * 10f * Mathf.PI);
            else if (heartbeatPhase < 0.2f)
                pulse = Mathf.Sin((heartbeatPhase - 0.1f) * 10f * Mathf.PI) * 0.6f;
            else
                pulse = 0f;
                
            finalPosition.z += pulse * heartbeatAmplitude * heartbeatStrength;
        }
        
        // Swaying effect based on mental stability
        float swayIntensity = normalSwayAmount * Mathf.Lerp(1f, mentalInstabilitySwayMultiplier, 1f - mentalStability);
        finalPosition.x += Mathf.Sin(Time.time * 0.5f) * swayIntensity;
        finalPosition.z += Mathf.Sin(Time.time * 0.3f) * swayIntensity;
        
        // Apply subtle rotation sway
        float rotationSway = swayIntensity * 2f;
        Quaternion finalRotation = originalRotation * Quaternion.Euler(
            Mathf.Sin(Time.time * 0.4f) * rotationSway, 
            Mathf.Sin(Time.time * 0.6f) * rotationSway, 
            Mathf.Sin(Time.time * 0.5f) * rotationSway
        );
        
        // Apply effects to camera
        transform.localPosition = finalPosition;
        transform.localRotation *= finalRotation;
    }
    
    private void UpdateStressLevel()
    {
        // Calculate movement intensity
        Vector3 currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        float speed = currentVelocity.magnitude;
        movementIntensity = Mathf.Lerp(movementIntensity, speed, Time.deltaTime * 5f);
        
        // Calculate rotation intensity
        float rotationDelta = Quaternion.Angle(transform.rotation, Quaternion.LookRotation(lastForward));
        rotationIntensity = Mathf.Lerp(rotationIntensity, rotationDelta / Time.deltaTime, Time.deltaTime * 5f);
        
        // Update heartrate based on movement and rotation
        float targetHeartRate = 60f + movementIntensity * 10f + rotationIntensity * 0.5f;
        
        // Additional heart rate increase with lower mental stability
        targetHeartRate += (1f - mentalStability) * 40f;
        
        // Smooth heart rate changes
        currentHeartRate = Mathf.Lerp(currentHeartRate, targetHeartRate, Time.deltaTime * 2f);
        
        // Update mental stability based on stress
        float stressLevel = Mathf.Clamp01((movementIntensity * 0.05f) + (rotationIntensity * 0.005f));
        
        // Accumulate stress
        cumulativeStress += stressLevel * Time.deltaTime;
        
        // Decay cumulative stress over time
        cumulativeStress = Mathf.Max(0, cumulativeStress - (Time.deltaTime * 0.05f));
        
        // Stability decreases when stressed, recovers when calm
        if (stressLevel > 0.2f)
        {
            mentalStability -= stabilityDecayRate * stressLevel * Time.deltaTime;
        }
        else
        {
            mentalStability += stabilityRecoveryRate * Time.deltaTime;
        }
        
        // Clamp mental stability
        mentalStability = Mathf.Clamp01(mentalStability);
        
        // Update last position and rotation
        lastPosition = transform.position;
        lastForward = transform.forward;
    }
    
    private void ProcessMentalStateEffects()
    {
        if (postProcessVolume == null) return;
        
        // Update post-processing effects based on mental state
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = Mathf.Lerp(0f, maxChromaticAberration, 1f - mentalStability);
            
        if (vignette != null)
            vignette.intensity.value = Mathf.Lerp(0.2f, maxVignette, 1f - mentalStability);
            
        if (grain != null)
            grain.intensity.value = Mathf.Lerp(0f, maxGrain, 1f - mentalStability);
            
        if (lensDistortion != null)
            lensDistortion.intensity.value = Mathf.Lerp(0f, maxDistortion, 1f - mentalStability);
            
        // Color grading effects based on mental state
        if (colorGrading != null)
        {
            // Adjust saturation based on mental state (desaturate when unstable)
            colorGrading.saturation.value = Mathf.Lerp(0f, -20f, 1f - mentalStability);
            
            // Add color tint based on current world
            switch (currentWorld)
            {
                case WorldState.Reality:
                    colorGrading.colorFilter.value = Color.Lerp(Color.white, new Color(1.0f, 0.95f, 0.9f), 1f - mentalStability);
                    break;
                case WorldState.Nightmare:
                    colorGrading.colorFilter.value = Color.Lerp(Color.white, new Color(0.8f, 0.6f, 0.6f), 1f - mentalStability * 0.5f);
                    break;
                case WorldState.Hospital:
                    colorGrading.colorFilter.value = Color.Lerp(Color.white, new Color(0.9f, 1.0f, 1.1f), 1f - mentalStability * 0.5f);
                    break;
                case WorldState.Station:
                    colorGrading.colorFilter.value = Color.Lerp(Color.white, new Color(0.7f, 0.9f, 1.1f), 1f - mentalStability * 0.5f);
                    break;
                case WorldState.VoidRealm:
                    colorGrading.colorFilter.value = Color.Lerp(Color.white, new Color(0.5f, 0.5f, 0.8f), 1f - mentalStability * 0.3f);
                    break;
            }
        }
    }
    
    private void ProcessHeartbeat()
    {
        if (cameraAudioSource == null || heartbeatSounds.Length == 0) return;
        
        // Calculate time between heartbeats based on current heart rate
        float heartbeatInterval = 60f / currentHeartRate;
        
        timeSinceLastHeartbeat += Time.deltaTime;
        
        // Time for a heartbeat
        if (timeSinceLastHeartbeat >= heartbeatInterval)
        {
            timeSinceLastHeartbeat = 0f;
            
            // Only play heartbeat sound when heart rate is elevated or mental stability is low
            if (currentHeartRate > 80f || mentalStability < 0.6f)
            {
                // Choose random heartbeat sound
                AudioClip heartbeatClip = heartbeatSounds[Random.Range(0, heartbeatSounds.Length)];
                
                // Calculate volume based on heart rate and mental stability
                float volume = Mathf.Lerp(0.1f, maxHeartbeatVolume, 
                    Mathf.Max((currentHeartRate - 80f) / 60f, (1f - mentalStability)));
                
                // Play heartbeat sound
                cameraAudioSource.PlayOneShot(heartbeatClip, volume);
            }
        }
    }
    
    private void CheckForHallucination()
    {
        // Increase chance of hallucination based on mental instability and stress
        float adjustedChance = hallucinationChance * (1f + (1f - mentalStability) * 5f + cumulativeStress);
        
        // Roll for hallucination
        if (!isHallucinating && Random.value < adjustedChance * Time.deltaTime)
        {
            StartCoroutine(TriggerHallucination());
        }
    }
    
    private IEnumerator TriggerHallucination()
    {
        isHallucinating = true;
        
        // Play hallucination sound if available
        if (cameraAudioSource != null && hallucinationSounds.Length > 0)
        {
            AudioClip hallucinationClip = hallucinationSounds[Random.Range(0, hallucinationSounds.Length)];
            cameraAudioSource.PlayOneShot(hallucinationClip, Mathf.Lerp(0.3f, 1.0f, 1f - mentalStability));
        }
        
        // Store original values
        float originalChromaticValue = chromaticAberration != null ? chromaticAberration.intensity.value : 0f;
        float originalVignetteValue = vignette != null ? vignette.intensity.value : 0f;
        float originalGrainValue = grain != null ? grain.intensity.value : 0f;
        float originalDistortionValue = lensDistortion != null ? lensDistortion.intensity.value : 0f;
        
        // Duration of hallucination build-up
        float buildUpTime = 0.5f;
        float elapsedTime = 0f;
        
        // Possibly spawn a memory fragment
        if (Random.value < memorySpawnChance && memoryPrefabs.Length > 0)
        {
            SpawnMemoryFragment();
        }
        
        // Build up hallucination intensity
        while (elapsedTime < buildUpTime)
        {
            float t = elapsedTime / buildUpTime;
            float curveT = hallucinationCurve.Evaluate(t);
            
            // Apply intense visual effects
            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(originalChromaticValue, maxChromaticAberration * hallucinationIntensity, curveT);
                
            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(originalVignetteValue, maxVignette * hallucinationIntensity, curveT);
                
            if (grain != null)
                grain.intensity.value = Mathf.Lerp(originalGrainValue, maxGrain * hallucinationIntensity, curveT);
                
            if (lensDistortion != null)
                lensDistortion.intensity.value = Mathf.Lerp(originalDistortionValue, maxDistortion * hallucinationIntensity, curveT);
            
            // Apply random camera movement
            float shakeMagnitude = curveT * hallucinationIntensity * 0.2f;
            transform.localPosition = originalPosition + new Vector3(
                Random.Range(-shakeMagnitude, shakeMagnitude),
                Random.Range(-shakeMagnitude, shakeMagnitude),
                Random.Range(-shakeMagnitude, shakeMagnitude)
            );
            
            transform.localRotation = originalRotation * Quaternion.Euler(
                Random.Range(-shakeMagnitude * 10f, shakeMagnitude * 10f),
                Random.Range(-shakeMagnitude * 10f, shakeMagnitude * 10f),
                Random.Range(-shakeMagnitude * 10f, shakeMagnitude * 10f)
            );
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Maintain peak hallucination
        yield return new WaitForSeconds(hallucinationDuration);
        
        // Fade out hallucination
        elapsedTime = 0f;
        float fadeOutTime = 1.0f;
        
        while (elapsedTime < fadeOutTime)
        {
            float t = elapsedTime / fadeOutTime;
            float curveT = hallucinationCurve.Evaluate(1f - t);
            
            // Return to normal values
            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(originalChromaticValue, maxChromaticAberration * hallucinationIntensity, curveT);
                
            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(originalVignetteValue, maxVignette * hallucinationIntensity, curveT);
                
            if (grain != null)
                grain.intensity.value = Mathf.Lerp(originalGrainValue, maxGrain * hallucinationIntensity, curveT);
                
            if (lensDistortion != null)
                lensDistortion.intensity.value = Mathf.Lerp(originalDistortionValue, maxDistortion * hallucinationIntensity, curveT);
            
            // Gradually reduce camera shake
            float shakeMagnitude = curveT * hallucinationIntensity * 0.2f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalPosition,
                t * 0.2f
            );
            
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                originalRotation,
                t * 0.2f
            );
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Reset to normal state
        isHallucinating = false;
    }
    
    private void SpawnMemoryFragment()
    {
        if (memoryPrefabs.Length == 0) return;
        
        // Choose random memory prefab
        GameObject memoryPrefab = memoryPrefabs[Random.Range(0, memoryPrefabs.Length)];
        
        // Calculate spawn position (in front of player)
        Vector3 spawnPosition = transform.position + transform.forward * memoryDistance;
        
        // Add some randomness to position
        spawnPosition += new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-1f, 1f),
            Random.Range(-2f, 2f)
        );
        
        // Spawn memory fragment
        GameObject memoryFragment = Instantiate(memoryPrefab, spawnPosition, Quaternion.identity);
        
        // Make memory fragment look at player
        memoryFragment.transform.LookAt(transform.position);
        
        // Play sound if available
        if (cameraAudioSource != null && memoryFragmentSounds.Length > 0)
        {
            AudioClip memorySound = memoryFragmentSounds[Random.Range(0, memoryFragmentSounds.Length)];
            cameraAudioSource.PlayOneShot(memorySound, 0.7f);
        }
        
        // Add to active fragments list
        activeMemoryFragments.Add(memoryFragment);
        
        // Set up destruction
        StartCoroutine(DestroyMemoryFragment(memoryFragment));
    }
    
    private IEnumerator DestroyMemoryFragment(GameObject fragment)
    {
        // Wait for duration
        yield return new WaitForSeconds(memoryDuration);
        
        // Fade out fragment
        if (fragment != null)
        {
            // Get all renderers in fragment
            Renderer[] renderers = fragment.GetComponentsInChildren<Renderer>();
            
            // Fade out over time
            float fadeTime = 1.5f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime && fragment != null)
            {
                float alpha = 1f - (elapsed / fadeTime);
                
                // Update material alpha for each renderer
                foreach (Renderer rend in renderers)
                {
                    foreach (Material mat in rend.materials)
                    {
                        Color color = mat.color;
                        color.a = alpha;
                        mat.color = color;
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Remove from active list
            activeMemoryFragments.Remove(fragment);
            
            // Destroy fragment
            Destroy(fragment);
        }
    }
    
    public void SetMentalStability(float newStability)
    {
        mentalStability = Mathf.Clamp01(newStability);
    }
    
    public void SetWorldState(WorldState newState)
    {
        currentWorld = newState;
        UpdateWorldEffects();
    }
    
    private void UpdateWorldEffects()
    {
        // Apply specific visual settings based on current world
        switch (currentWorld)
        {
            case WorldState.Reality:
                // Reality: Subtle effects, mostly dependent on mental state
                breathingSpeed = 1f;
                breathingAmount = 0.01f;
                maxChromaticAberration = 0.2f;
                maxVignette = 0.4f;
                maxGrain = 0.2f;
                maxDistortion = 5f;
                break;
                
            case WorldState.Nightmare:
                // Nightmare: Intense visual distortion
                breathingSpeed = 1.5f;
                breathingAmount = 0.03f;
                maxChromaticAberration = 1f;
                maxVignette = 0.7f;
                maxGrain = 0.5f;
                maxDistortion = 20f;
                break;
                
            case WorldState.Station:
                // The Station: Dreamy, floating feeling
                breathingSpeed = 0.5f;
                breathingAmount = 0.05f;
                maxChromaticAberration = 0.6f;
                maxVignette = 0.3f;
                maxGrain = 0.1f;
                maxDistortion = 10f;
                break;
                
            case WorldState.Hospital:
                // Hospital: Clinical, sharp, but with unsettling undertones
                breathingSpeed = 0.8f;
                breathingAmount = 0.02f;
                maxChromaticAberration = 0.3f;
                maxVignette = 0.5f;
                maxGrain = 0.15f;
                maxDistortion = 3f;
                break;
                
            case WorldState.VoidRealm:
                // New realm: Otherworldly, surreal
                breathingSpeed = 0.3f;
                breathingAmount = 0.07f;
                maxChromaticAberration = 0.8f;
                maxVignette = 0.6f;
                maxGrain = 0.3f;
                maxDistortion = 15f;
                break;
        }
    }
    
    private void ProcessDebugControls()
    {
        // Debug controls to manually test features (remove for final version)
        
        // Manual hallucination trigger
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (!isHallucinating)
            {
                StartCoroutine(TriggerHallucination());
            }
        }
        
        // Decrease mental stability
        if (Input.GetKeyDown(KeyCode.End))
        {
            SetMentalStability(mentalStability - 0.1f);
            Debug.Log($"Mental Stability: {mentalStability:F2}");
        }
        
        // Increase mental stability
        if (Input.GetKeyDown(KeyCode.Home))
        {
            SetMentalStability(mentalStability + 0.1f);
            Debug.Log($"Mental Stability: {mentalStability:F2}");
        }
        
        // Toggle through world states
        if (Input.GetKeyDown(KeyCode.Insert))
        {
            currentWorld = (WorldState)(((int)currentWorld + 1) % System.Enum.GetValues(typeof(WorldState)).Length);
            UpdateWorldEffects();
            Debug.Log($"World State: {currentWorld}");
        }
        
        // Spawn memory fragment
        if (Input.GetKeyDown(KeyCode.M) && memoryPrefabs.Length > 0)
        {
            SpawnMemoryFragment();
        }
    }
    
    // Method to trigger a specific hallucination (can be called from other scripts)
    public void TriggerSpecificHallucination(float intensity, float duration)
    {
        if (!isHallucinating)
        {
            StartCoroutine(CustomHallucination(intensity, duration));
        }
    }
    
    private IEnumerator CustomHallucination(float intensity, float duration)
    {
        // Implementation similar to TriggerHallucination but with custom parameters
        isHallucinating = true;
        
        // Store original values
        float originalChromaticValue = chromaticAberration != null ? chromaticAberration.intensity.value : 0f;
        float originalVignetteValue = vignette != null ? vignette.intensity.value : 0f;
        float originalGrainValue = grain != null ? grain.intensity.value : 0f;
        float originalDistortionValue = lensDistortion != null ? lensDistortion.intensity.value : 0f;
        
        // Apply hallucination effects
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = maxChromaticAberration * intensity;
            
        if (vignette != null)
            vignette.intensity.value = maxVignette * intensity;
            
        if (grain != null)
            grain.intensity.value = maxGrain * intensity;
            
        if (lensDistortion != null)
            lensDistortion.intensity.value = maxDistortion * intensity;
        
        // Apply custom camera movement during the hallucination
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            float shakeMagnitude = intensity * 0.2f;
            transform.localPosition = originalPosition + new Vector3(
                Random.Range(-shakeMagnitude, shakeMagnitude),
                Random.Range(-shakeMagnitude, shakeMagnitude),
                Random.Range(-shakeMagnitude, shakeMagnitude)
            );
            
            float rotationIntensity = shakeMagnitude * 10f;
            transform.localRotation = originalRotation * Quaternion.Euler(
                Random.Range(-rotationIntensity, rotationIntensity),
                Random.Range(-rotationIntensity, rotationIntensity),
                Random.Range(-rotationIntensity, rotationIntensity)
            );
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Fade out effects
        float fadeOutTime = 1.0f;
        elapsedTime = 0f;
        
        while (elapsedTime < fadeOutTime)
        {
            float t = elapsedTime / fadeOutTime;
            
            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(maxChromaticAberration * intensity, originalChromaticValue, t);
                
            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(maxVignette * intensity, originalVignetteValue, t);
            if (grain != null)
                grain.intensity.value = Mathf.Lerp(maxGrain * intensity, originalGrainValue, t);
                
            if (lensDistortion != null)
                lensDistortion.intensity.value = Mathf.Lerp(maxDistortion * intensity, originalDistortionValue, t);
            
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, t * 0.2f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRotation, t * 0.2f);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        isHallucinating = false;
    }
    
    // Method to gradually transition between world states
    public void TransitionToWorldState(WorldState targetState, float transitionTime)
    {
        StartCoroutine(WorldTransition(targetState, transitionTime));
    }
    
    private IEnumerator WorldTransition(WorldState targetState, float transitionTime)
    {
        WorldState startState = currentWorld;
        
        // Store starting values
        float startBreathingSpeed = breathingSpeed;
        float startBreathingAmount = breathingAmount;
        float startChromaticMax = maxChromaticAberration;
        float startVignetteMax = maxVignette;
        float startGrainMax = maxGrain;
        float startDistortionMax = maxDistortion;
        
        // Temporarily set target world to get its values
        WorldState originalState = currentWorld;
        currentWorld = targetState;
        UpdateWorldEffects();
        
        // Store target values
        float targetBreathingSpeed = breathingSpeed;
        float targetBreathingAmount = breathingAmount;
        float targetChromaticMax = maxChromaticAberration;
        float targetVignetteMax = maxVignette;
        float targetGrainMax = maxGrain;
        float targetDistortionMax = maxDistortion;
        
        // Reset current world
        currentWorld = originalState;
        
        // Begin transition
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionTime)
        {
            float t = elapsedTime / transitionTime;
            
            // Use curve for smoother transition
            float curveT = hallucinationCurve.Evaluate(t);
            
            // Interpolate all values
            breathingSpeed = Mathf.Lerp(startBreathingSpeed, targetBreathingSpeed, curveT);
            breathingAmount = Mathf.Lerp(startBreathingAmount, targetBreathingAmount, curveT);
            maxChromaticAberration = Mathf.Lerp(startChromaticMax, targetChromaticMax, curveT);
            maxVignette = Mathf.Lerp(startVignetteMax, targetVignetteMax, curveT);
            maxGrain = Mathf.Lerp(startGrainMax, targetGrainMax, curveT);
            maxDistortion = Mathf.Lerp(startDistortionMax, targetDistortionMax, curveT);
            
            // Update post-processing to show changes immediately
            ProcessMentalStateEffects();
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Finalize transition
        currentWorld = targetState;
        UpdateWorldEffects();
    }
    
    // Method to trigger flashback effect
    public void TriggerFlashback(float duration, Color flashbackTint, float disorientation = 1f)
    {
        StartCoroutine(FlashbackEffect(duration, flashbackTint, disorientation));
    }
    
    private IEnumerator FlashbackEffect(float duration, Color flashbackTint, float disorientation)
    {
        if (colorGrading == null) yield break;
        
        // Store original values
        Color originalTint = colorGrading.colorFilter.value;
        float originalTemperature = colorGrading.temperature.value;
        float originalSaturation = colorGrading.saturation.value;
        Vector3 originalPosition = transform.localPosition;
        Quaternion originalRotation = transform.localRotation;
        
        // Quick flash to white
        float flashDuration = 0.2f;
        float flashElapsed = 0f;
        
        while (flashElapsed < flashDuration)
        {
            float t = flashElapsed / flashDuration;
            colorGrading.colorFilter.value = Color.Lerp(originalTint, Color.white, t);
            
            flashElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Apply flashback tint
        colorGrading.colorFilter.value = flashbackTint;
        
        // Modify other color grading params
        colorGrading.temperature.value = 20f; // Warm/cool temperature shift
        colorGrading.saturation.value = 30f; // Oversaturated
        
        // Play with camera during flashback
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Calculate normalized time (0 to 1)
            float normalizedTime = elapsed / duration;
            
            // Apply disorienting camera movement
            float wobbleAmount = disorientation * 0.1f * (1f - normalizedTime);
            transform.localPosition = originalPosition + new Vector3(
                Mathf.Sin(Time.time * 5f) * wobbleAmount,
                Mathf.Sin(Time.time * 3f) * wobbleAmount,
                Mathf.Sin(Time.time * 4f) * wobbleAmount
            );
            
            transform.localRotation = originalRotation * Quaternion.Euler(
                Mathf.Sin(Time.time * 2f) * disorientation * 3f * (1f - normalizedTime),
                Mathf.Sin(Time.time * 1.5f) * disorientation * 3f * (1f - normalizedTime),
                Mathf.Sin(Time.time * 2.5f) * disorientation * 3f * (1f - normalizedTime)
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Return to normal
        float returnDuration = 0.5f;
        elapsed = 0f;
        
        while (elapsed < returnDuration)
        {
            float t = elapsed / returnDuration;
            
            colorGrading.colorFilter.value = Color.Lerp(flashbackTint, originalTint, t);
            colorGrading.temperature.value = Mathf.Lerp(20f, originalTemperature, t);
            colorGrading.saturation.value = Mathf.Lerp(30f, originalSaturation, t);
            
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, t);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRotation, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restore original values
        colorGrading.colorFilter.value = originalTint;
        colorGrading.temperature.value = originalTemperature;
        colorGrading.saturation.value = originalSaturation;
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
    }
    
    // Method to get current mental stability (can be used by other scripts)
    public float GetMentalStability()
    {
        return mentalStability;
    }
    
    // Method to add stress directly (can be called by other scripts when something traumatic happens)
    public void AddStress(float stressAmount)
    {
        cumulativeStress += stressAmount;
        
        // Immediately affect mental stability
        mentalStability -= stressAmount * 0.5f;
        mentalStability = Mathf.Clamp01(mentalStability);
        
        // Increase heart rate
        currentHeartRate += stressAmount * 40f;
        
        // Potentially trigger hallucination if severe stress
        if (stressAmount > 0.3f && !isHallucinating && Random.value < stressAmount)
        {
            StartCoroutine(TriggerHallucination());
        }
    }
    
    // Method to display entities in peripheral vision
    public void ShowPeripheralEntity(GameObject entityPrefab, float duration = 2f, float distance = 8f)
    {
        StartCoroutine(PeripheralEntityRoutine(entityPrefab, duration, distance));
    }
    
    private IEnumerator PeripheralEntityRoutine(GameObject entityPrefab, float duration, float distance)
    {
        if (entityPrefab == null) yield break;
        
        // Calculate a position in the peripheral vision
        // Choose a random side (left or right)
        float angle = Random.Range(70f, 110f) * (Random.value > 0.5f ? 1f : -1f);
        
        // Convert to radians
        float angleRad = angle * Mathf.Deg2Rad;
        
        // Calculate position
        Vector3 spawnPosition = transform.position + 
                               (transform.right * Mathf.Sin(angleRad) * distance) + 
                               (transform.forward * Mathf.Cos(angleRad) * distance);
        
        // Adjust height randomly
        spawnPosition.y += Random.Range(-1f, 1f);
        
        // Spawn entity
        GameObject entity = Instantiate(entityPrefab, spawnPosition, Quaternion.identity);
        
        // Make entity look at player
        entity.transform.LookAt(transform.position);
        
        // Wait for duration
        yield return new WaitForSeconds(duration);
        
        // Fade out entity
        Renderer[] renderers = entity.GetComponentsInChildren<Renderer>();
        float fadeTime = 1f;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            float alpha = 1f - (elapsed / fadeTime);
            
            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Destroy entity
        Destroy(entity);
    }
    
    // Method to add a temporary screen effect
    public void AddScreenEffect(string effectType, float intensity, float duration)
    {
        StartCoroutine(TemporaryScreenEffect(effectType, intensity, duration));
    }
    
    private IEnumerator TemporaryScreenEffect(string effectType, float intensity, float duration)
    {
        float originalValue = 0f;
        
        // Store original value
        switch (effectType.ToLower())
        {
            case "chromatic":
                if (chromaticAberration != null)
                    originalValue = chromaticAberration.intensity.value;
                break;
            case "vignette":
                if (vignette != null)
                    originalValue = vignette.intensity.value;
                break;
            case "grain":
                if (grain != null)
                    originalValue = grain.intensity.value;
                break;
            case "distortion":
                if (lensDistortion != null)
                    originalValue = lensDistortion.intensity.value;
                break;
        }
        
        // Apply effect
        float blendDuration = 0.3f;
        float elapsed = 0f;
        
        // Blend in
        while (elapsed < blendDuration)
        {
            float t = elapsed / blendDuration;
            
            switch (effectType.ToLower())
            {
                case "chromatic":
                    if (chromaticAberration != null)
                        chromaticAberration.intensity.value = Mathf.Lerp(originalValue, intensity, t);
                    break;
                case "vignette":
                    if (vignette != null)
                        vignette.intensity.value = Mathf.Lerp(originalValue, intensity, t);
                    break;
                case "grain":
                    if (grain != null)
                        grain.intensity.value = Mathf.Lerp(originalValue, intensity, t);
                    break;
                case "distortion":
                    if (lensDistortion != null)
                        lensDistortion.intensity.value = Mathf.Lerp(originalValue, intensity, t);
                    break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Hold effect
        yield return new WaitForSeconds(duration - (blendDuration * 2));
        
        // Blend out
        elapsed = 0f;
        
        while (elapsed < blendDuration)
        {
            float t = elapsed / blendDuration;
            
            switch (effectType.ToLower())
            {
                case "chromatic":
                    if (chromaticAberration != null)
                        chromaticAberration.intensity.value = Mathf.Lerp(intensity, originalValue, t);
                    break;
                case "vignette":
                    if (vignette != null)
                        vignette.intensity.value = Mathf.Lerp(intensity, originalValue, t);
                    break;
                case "grain":
                    if (grain != null)
                        grain.intensity.value = Mathf.Lerp(intensity, originalValue, t);
                    break;
                case "distortion":
                    if (lensDistortion != null)
                        lensDistortion.intensity.value = Mathf.Lerp(intensity, originalValue, t);
                    break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restore original value
        switch (effectType.ToLower())
        {
            case "chromatic":
                if (chromaticAberration != null)
                    chromaticAberration.intensity.value = originalValue;
                break;
            case "vignette":
                if (vignette != null)
                    vignette.intensity.value = originalValue;
                break;
            case "grain":
                if (grain != null)
                    grain.intensity.value = originalValue;
                break;
            case "distortion":
                if (lensDistortion != null)
                    lensDistortion.intensity.value = originalValue;
                break;
        }
    }
}