using System.Collections;
using System.Collections.Generic;
using SAE_Dubai.Leonardo.Client_System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;
using FilmGrain = UnityEngine.Rendering.HighDefinition.FilmGrain;

namespace SAE_Dubai.Leonardo.CameraSys
{
    /// <summary>
    /// The main component that handles all camera functionality when a camera is active.
    /// This includes handling photo mode, managing camera parameters, capturing photos,
    /// and providing user interface feedback. Camera behavior is driven by the attached
    /// CameraSettingsSO scriptable object.
    /// </summary>
    public class CameraSystem : MonoBehaviour
    {
        private CameraManager _manager;

        [Header("- Camera Configuration")]
        [Tooltip("Reference to the scriptable object containing all settings for this camera.")]
        public CameraSettings cameraSettings;


        [Header("- Rendering")]
        [Tooltip("The camera used to render the viewfinder/screen.")]
        public Camera cameraRenderer;

        [Tooltip("Whether this camera is using the viewfinder or screen display.")]
        public bool usingViewfinder;

        [Tooltip("Renderer for the camera's screen.")]
        public Renderer screenRenderer;

        [Tooltip("Material used when the camera screen is on.")]
        public Material screenOnMaterial;

        [Tooltip("Material used when the camera screen is off.")]
        public Material screenOffMaterial;


        [Header("- State")]
        [Tooltip("Whether the camera is currently powered on.")]
        public bool isCameraOn;

        [SerializeField] private bool _isFocusing;
        [SerializeField] private bool _isCapturing;
        [SerializeField] private bool _isFocused;


        [Header("- UI References")]
        [Tooltip("The UI panel containing all photography controls and information.(Screen)")]
        public GameObject photographyUI;

        [FormerlySerializedAs("_overlayUI")] [Tooltip("The UI panel for the OVERLAY UI. (Viewfinder)")] [SerializeField]
        private GameObject overlayUI;

        private CameraUIController _uiController;


        [Header("- Audio")]
        [Tooltip("Audio source for camera sounds.")]
        public AudioSource audioSource;

        [Tooltip("Reference to the main player camera.")]
        public Camera mainCamera;

        [SerializeField] private float defaultFOV;


        [Header("- Photo Storage")]
        [Tooltip("Current number of photos remaining in storage.")]
        public int remainingPhotos;

        [Tooltip("Collection of all photos taken with this camera.")]
        public List<CapturedPhoto> photoAlbum = new List<CapturedPhoto>();


        [Header("- Camera Components")]
        [Tooltip("The viewfinder camera (if available).")]
        public Camera viewfinderCamera;


        [Header("- Camera Effects")]
        [Tooltip("Reference to the Post Processing Volume (or Layer)")] [SerializeField]
        private Volume localCameraVolume;

        private FilmGrain localFilmGrain;
        private DepthOfField localDepthOfField;
        private MotionBlur localMotionBlur;

        [Header("- Input Keys")]
        [Tooltip("Key to turn the camera on/off.")]
        public KeyCode turnCameraOnKey = KeyCode.C;

        [Tooltip("Key to take a photo")] public KeyCode takePhotoKey = KeyCode.Mouse0;

        [Tooltip("Key to focus the camera")] public KeyCode focusKey = KeyCode.Mouse1;

        [FormerlySerializedAs("toggleViewfinderKey")] [Tooltip("Key to toggle between viewfinder and screen.")]
        public KeyCode toggleViewKey = KeyCode.V;

        private int _currentISOIndex = 0;
        private int _currentApertureIndex = 4;
        private int _currentShutterSpeedIndex = 5;
        private float _currentFocalLength;
        private bool _autoExposureEnabled = true;
        private float _currentFocusDistance = 10f;

        /// <summary>
        /// Get a reference to the overlay photo panel to disable it.
        /// </summary>
        private void Awake() {
            _manager = FindFirstObjectByType<CameraManager>();

            overlayUI = _manager.overlayUI;

            if (overlayUI != null) {
                // Initially disable it.
                overlayUI.SetActive(false);
            }
            else {
                Debug.LogWarning("CameraSystem.cs: Could not find UI with tag 'OverlayCameraUI'");
            }
        }

        private void DebugCheckEventConnections() {
            Debug.Log($"<color=cyan>CameraSystem {name} checking event connections</color>");

            PhotoSessionManager sessionManager = FindFirstObjectByType<PhotoSessionManager>();
            if (sessionManager != null) {
                Debug.Log("<color=cyan>Found PhotoSessionManager, attempting manual connection</color>");

                if (OnPhotoCapture == null) {
                    OnPhotoCapture += sessionManager.HandlePhotoCapturedDirectly;
                    Debug.Log(
                        "<color=green>Connected PhotoSessionManager.HandlePhotoCapturedDirectly to OnPhotoCapture event</color>");
                }
                else {
                    Debug.Log("<color=yellow>OnPhotoCapture already has listeners, not modifying</color>");
                }
            }
            else {
                Debug.LogWarning("<color=yellow>Could not find PhotoSessionManager in scene</color>");
            }
        }

        /// <summary>
        /// Initializes the camera system with default settings.
        /// </summary>
        private void Start() {
            if (mainCamera == null) {
                Debug.LogWarning("CameraSystem.cs: mainCamera reference not set, attempting to find Camera.main");
                mainCamera = Camera.main;
                if (mainCamera == null) {
                    Debug.LogError("CameraSystem.cs: Could not find main camera! Some functionality may be limited.");
                }
                else {
                    Debug.Log($"CameraSystem.cs: Successfully found and assigned main camera: {mainCamera.name}");
                }
            }

            if (audioSource == null) {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            defaultFOV = 70;

            if (cameraSettings != null) {
                remainingPhotos = cameraSettings.photoCapacity;
                _autoExposureEnabled = cameraSettings.hasAutoISO || cameraSettings.hasAutoShutterSpeed ||
                                       cameraSettings.hasAutoAperture;

                FindInitialSettingsIndices();

                _currentFocalLength = cameraSettings.minFocalLength;
            }
            else {
                remainingPhotos = 50;
                Debug.LogWarning("CameraSystem.cs: No camera settings assigned!");
            }

            // Initialize physical camera if available.
            if (cameraRenderer != null) {
                cameraRenderer.usePhysicalProperties = true;
                ApplyCameraParameters();
            }

            // Disable UI initially.
            if (photographyUI != null) {
                photographyUI.SetActive(false);
            }

            // Get post-processing object.
            localCameraVolume.profile.TryGet(out localFilmGrain);
            localCameraVolume.profile.TryGet(out localDepthOfField);
            localCameraVolume.profile.TryGet(out localMotionBlur);

            DebugCheckEventConnections();
        }

        /// <summary>
        /// Finds the initial indices for ISO, aperture, and shutter speed based on camera settings.
        /// </summary>
        private void FindInitialSettingsIndices() {
            if (cameraSettings == null) return;

            // Initialize camera with default settings.
            int initialISO;
            float initialAperture;
            float initialShutterSpeed;
            float initialFocalLength;

            cameraSettings.InitializeDefaultSettings(out initialISO, out initialAperture, out initialShutterSpeed,
                out initialFocalLength);

            // Find indices in available stops.
            _currentISOIndex = System.Array.IndexOf(cameraSettings.availableISOStops, initialISO);
            if (_currentISOIndex < 0) _currentISOIndex = 0;

            _currentApertureIndex = System.Array.IndexOf(cameraSettings.availableApertureStops, initialAperture);
            if (_currentApertureIndex < 0) _currentApertureIndex = 4; // Default to middle aperture.

            _currentShutterSpeedIndex = 5; // Default to 1/125 (to not have blurry pictures).
            float closestDiff = Mathf.Abs(cameraSettings.availableShutterSpeedStops[_currentShutterSpeedIndex] -
                                          initialShutterSpeed);

            for (int i = 0; i < cameraSettings.availableShutterSpeedStops.Length; i++) {
                float diff = Mathf.Abs(cameraSettings.availableShutterSpeedStops[i] - initialShutterSpeed);
                if (diff < closestDiff) {
                    closestDiff = diff;
                    _currentShutterSpeedIndex = i;
                }
            }

            _currentFocalLength = initialFocalLength;
        }

        /// <summary>
        /// Handles user input when the camera is active.
        /// </summary>
        private void Update() {
            if (Input.GetKeyDown(turnCameraOnKey)) {
                TogglePhotoMode();
            }

            if (Input.GetKeyDown(toggleViewKey) && isCameraOn) {
                ToggleViewMode();
            }

            if (!isCameraOn)
                return;

            HandlePhotoModeInputs();
        }

        /// <summary>
        /// Toggles the camera between active and inactive states.
        /// </summary>
        public void TogglePhotoMode() {
            isCameraOn = !isCameraOn;

            if (isCameraOn) {
                // Set up appropriate camera display.
                if (usingViewfinder) {
                    if (viewfinderCamera != null) viewfinderCamera.enabled = true;
                    if (cameraRenderer != null) cameraRenderer.enabled = false;
                    if (screenRenderer != null && screenOffMaterial != null) {
                        screenRenderer.material = screenOffMaterial;
                    }

                    // Show overlay UI, hide world space UI.
                    if (photographyUI != null) photographyUI.SetActive(false);
                    if (CameraManager.Instance != null && CameraManager.Instance.overlayUI != null) {
                        CameraManager.Instance.overlayUI.SetActive(true);
                    }
                }
                else {
                    if (viewfinderCamera != null) viewfinderCamera.enabled = false;
                    if (cameraRenderer != null) cameraRenderer.enabled = true;
                    if (screenRenderer != null && screenOnMaterial != null) {
                        screenRenderer.material = screenOnMaterial;
                    }

                    // Show world space UI, hide overlay UI.
                    if (photographyUI != null) photographyUI.SetActive(true);
                    if (CameraManager.Instance != null && CameraManager.Instance.overlayUI != null) {
                        CameraManager.Instance.overlayUI.SetActive(false);
                    }
                }
            }
            else {
                //Debug.Log("CameraSystem.cs: Turned cam off.");

                if (photographyUI != null) {
                    photographyUI.SetActive(false);
                }

                if (_manager != null && _manager.overlayUI != null) {
                    _manager.overlayUI.SetActive(false);
                }

                // Turn off all camera displays
                if (viewfinderCamera != null) viewfinderCamera.enabled = false;
                if (cameraRenderer != null) cameraRenderer.enabled = false;

                if (screenRenderer != null && screenOffMaterial != null) {
                    screenRenderer.material = screenOffMaterial;
                }

                // Hide UI.
                if (photographyUI != null) {
                    photographyUI.SetActive(false);
                }

                // Reset main camera.
                mainCamera.fieldOfView = defaultFOV;
            }
        }

        /// <summary>
        /// Toggles between viewfinder and screen display modes.
        /// </summary>
        public void ToggleViewMode() {
            usingViewfinder = !usingViewfinder;
            // Get the camera manager reference.
            CameraManager manager = CameraManager.Instance;

            // Toggle camera renderers.
            if (viewfinderCamera != null) viewfinderCamera.enabled = usingViewfinder;
            if (cameraRenderer != null) cameraRenderer.enabled = !usingViewfinder;

            // Update screen material.
            if (screenRenderer != null) {
                Material materialToUse = usingViewfinder ? screenOffMaterial : screenOnMaterial;
                if (materialToUse != null) {
                    screenRenderer.material = materialToUse;
                }
            }

            // Toggle UI panels.
            if (photographyUI != null) {
                photographyUI.SetActive(!usingViewfinder);
            }

            if (manager != null && manager.overlayUI != null) {
                manager.overlayUI.SetActive(usingViewfinder);
            }

            //Debug.Log("CameraSystem: Switched to " + (usingViewfinder ? "viewfinder" : "screen") + " view");
        }

        /// <summary>
        /// Handles user input for photography functions.
        /// </summary>
        private void HandlePhotoModeInputs() {
            // Take photo.
            if (Input.GetKeyDown(takePhotoKey) && !_isCapturing) {
                CapturePhoto();
            }

            // Focus camera.
            if (Input.GetKeyDown(focusKey) && !_isFocusing) {
                AttemptAutoFocus();
            }

            // Handle ISO changes.
            if (Input.GetKeyDown(KeyCode.I) && cameraSettings != null) {
                IncreaseISO();
            }
            else if (Input.GetKeyDown(KeyCode.U) && cameraSettings != null) {
                DecreaseISO();
            }

            // Handle Aperture changes.
            if (Input.GetKeyDown(KeyCode.O) && cameraSettings != null) {
                IncreaseAperture();
            }
            else if (Input.GetKeyDown(KeyCode.P) && cameraSettings != null) {
                DecreaseAperture();
            }

            // Handle Shutter Speed changes.
            if (Input.GetKeyDown(KeyCode.K) && cameraSettings != null) {
                IncreaseShutterSpeed();
            }
            else if (Input.GetKeyDown(KeyCode.L) && cameraSettings != null) {
                DecreaseShutterSpeed();
            }

            // Handle Focal Length changes (zoom).
            if (Input.GetKey(KeyCode.Period) && cameraSettings != null && cameraSettings.hasZoom) {
                float zoomSpeed = 5f;
                _currentFocalLength = Mathf.Min(_currentFocalLength + zoomSpeed * Time.deltaTime,
                    cameraSettings.maxFocalLength);
                ApplyCameraParameters();
            }
            else if (Input.GetKey(KeyCode.Comma) && cameraSettings != null && cameraSettings.hasZoom) {
                float zoomSpeed = 5f;
                _currentFocalLength = Mathf.Max(_currentFocalLength - zoomSpeed * Time.deltaTime,
                    cameraSettings.minFocalLength);
                ApplyCameraParameters();
            }
        }

        /// <summary>
        /// Captures a photo with the current camera settings.
        /// Starts the coroutine that handles the capture process.
        /// </summary>
        private void CapturePhoto() {
            if (remainingPhotos <= 0) {
                Debug.Log("No remaining photos!");
                return;
            }

            if (_isCapturing) return; // * To prevent overlapping captures.

            _isCapturing = true;

            // Play shutter sound.
            if (cameraSettings != null && cameraSettings.shutterSound != null && audioSource != null) {
                audioSource.PlayOneShot(cameraSettings.shutterSound);
            }

            // ! --- Determine which camera is rendering NOW ---
            Camera camBeingUsed = null;
            if (isCameraOn) // Check if the system is on
            {
                camBeingUsed = usingViewfinder ? viewfinderCamera : cameraRenderer;
            }

            // ! --- Start the coroutine, passing the camera reference ---
            StartCoroutine(CapturePhotoEffect(camBeingUsed));
        }

        /// <summary>
        /// Handles the photo capture process, creates a photo record, and invokes the event.
        /// Now receives the specific camera that was used.
        /// </summary>
        /// <param name="capturingCamera">The Camera component used for this capture.</param>
        private IEnumerator CapturePhotoEffect(Camera capturingCamera) {
            // Handle flash if enabled.
            if (cameraSettings != null && cameraSettings.hasBuiltInFlash) {
                // Flash effect could be implemented here.
            }

            // Create a new photo record.
            CapturedPhoto newPhoto = new CapturedPhoto {
                TimeStamp = System.DateTime.Now
            };

            if (cameraSettings != null) {
                // Record settings used for the photo.
                newPhoto.iso = GetCurrentISO();
                newPhoto.aperture = GetCurrentAperture();
                newPhoto.shutterSpeed = GetCurrentShutterSpeed();
                newPhoto.focalLength = GetCurrentFocalLength();

                // Calculate photo quality based on current settings.
                newPhoto.quality = CalculatePhotoQuality();
            }

            // --- Assign the capturing camera reference ---
            newPhoto.CapturingCamera = capturingCamera;
            // -------------------------------------------

            // Add to album and update count.
            photoAlbum.Add(newPhoto);
            remainingPhotos--;

            // --- Invoke the event AFTER setting all photo data ---
            Debug.Log(
                $"<color=cyan>CameraSystem {name}: Invoking OnPhotoCapture. Photo captured by: {(capturingCamera != null ? capturingCamera.name : "NULL")}</color>");
            OnPhotoCapture?.Invoke(newPhoto);
            Debug.Log("<color=green>CameraSystem: OnPhotoCapture event invoked</color>");

            // Delay before allowing another photo.
            yield return new WaitForSeconds(0.5f); // Consider if delay is needed
            _isCapturing = false;

            // Debug.Log($"Photo captured, quality: {newPhoto.quality:F2}, {remainingPhotos} photos remaining.");
        }

        /// <summary>
        /// Attempts to focus the camera by casting a ray to determine focus distance.
        /// </summary>
        private void AttemptAutoFocus() {
            if (!cameraSettings.hasAutoFocus || _isFocusing)
                return;

            // Determine which camera to use for focus ray.
            Camera focusCamera = usingViewfinder ? viewfinderCamera : cameraRenderer;

            if (focusCamera == null) {
                // Fall back to screen camera if viewfinder is null.
                focusCamera = cameraRenderer;

                // If both are null, we can't focus.
                if (focusCamera == null) {
                    StartCoroutine(FailedFocusEffect());
                    return;
                }
            }

            // Cast a ray from the center of the camera view to find focus distance.
            Ray ray = focusCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            // Use a longer max distance to allow focusing on distant objects.
            float maxFocusDistance = 100f;
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxFocusDistance)) {
                // Get the focus distance.
                _currentFocusDistance = hit.distance;

                //Debug.Log($"Focus hit: {hit.collider.name} at distance {_currentFocusDistance:F2}m");

                // Apply focus distance to cameras.
                if (cameraRenderer != null) {
                    cameraRenderer.focusDistance = _currentFocusDistance;
                }

                if (viewfinderCamera != null) {
                    viewfinderCamera.focusDistance = _currentFocusDistance;
                }

                // Apply DoF effect immediately.
                ApplyApertureBasedDepthOfField();

                // Start the successful focus effect.
                StartCoroutine(SuccessfulFocusEffect());
            }
            else {
                // No object hit, try to focus at a far distance.
                _currentFocusDistance = 50f;

                Debug.Log($"No direct focus target found, focusing at {_currentFocusDistance:F2}m");

                // Apply the far focus distance to cameras.
                if (cameraRenderer != null) {
                    cameraRenderer.focusDistance = _currentFocusDistance;
                }

                if (viewfinderCamera != null) {
                    viewfinderCamera.focusDistance = _currentFocusDistance;
                }

                // Apply DoF effect immediately.
                ApplyApertureBasedDepthOfField();

                // Use success effect since if focusing at infinity.
                StartCoroutine(SuccessfulFocusEffect());
            }
        }

        /// <summary>
        /// Simulates the camera focus process.
        /// </summary>
        private IEnumerator SuccessfulFocusEffect() {
            _isFocusing = true;
            _isFocused = false;

            // Notify UI controller.
            if (_uiController != null) {
                _uiController.OnFocusLost();
            }

            if (cameraSettings != null && cameraSettings.focusSound != null && audioSource != null) {
                audioSource.PlayOneShot(cameraSettings.focusSound);
            }

            yield return new WaitForSeconds(0.3f);

            _isFocusing = false;
            _isFocused = true;

            // Notify UI controller.
            if (_uiController != null) {
                _uiController.OnFocusAchieved();
            }
        }

        /// <summary>
        /// Enumerator to run if the focus is not possible.
        /// </summary>
        private IEnumerator FailedFocusEffect() {
            _isFocusing = true;
            _isFocused = false;

            // Notify UI controller.
            if (_uiController != null) {
                _uiController.OnFocusLost();
            }

            if (cameraSettings != null && cameraSettings.focusSound != null && audioSource != null) {
                // TODO: Could play a different sound for failed focus if available
                audioSource.PlayOneShot(cameraSettings.focusSound);
            }

            yield return new WaitForSeconds(0.3f);

            _isFocusing = false;
            _isFocused = false;

            // No need to notify UI controller about focus achieved!!!!!!
            // The UI will stay in unfocused state!!!!!
        }

        /// <summary>
        /// Increases the camera's ISO setting.
        /// </summary>
        public void IncreaseISO() {
            if (cameraSettings == null) return;

            if (_currentISOIndex < cameraSettings.availableISOStops.Length - 1) {
                _currentISOIndex++;
                ApplyCameraParameters();
                Debug.Log($"ISO increased to {cameraSettings.availableISOStops[_currentISOIndex]}");
            }
        }

        /// <summary>
        /// Decreases the camera's ISO setting.
        /// </summary>
        public void DecreaseISO() {
            if (cameraSettings == null) return;

            if (_currentISOIndex > 0) {
                _currentISOIndex--;
                ApplyCameraParameters();
                Debug.Log($"ISO decreased to {cameraSettings.availableISOStops[_currentISOIndex]}");
            }
        }

        /// <summary>
        /// Increases the camera's aperture f-number (smaller aperture).
        /// </summary>
        public void IncreaseAperture() {
            if (cameraSettings == null) return;

            // Higher f-number = smaller aperture
            if (_currentApertureIndex < cameraSettings.availableApertureStops.Length - 1) {
                _currentApertureIndex++;
                ApplyCameraParameters();
                Debug.Log($"Aperture changed to f/{cameraSettings.availableApertureStops[_currentApertureIndex]}");
            }
        }

        /// <summary>
        /// Decreases the camera's aperture f-number (larger aperture).
        /// </summary>
        public void DecreaseAperture() {
            if (cameraSettings == null) return;

            // Lower f-number = larger aperture.
            if (_currentApertureIndex > 0) {
                _currentApertureIndex--;
                ApplyCameraParameters();
                Debug.Log($"Aperture changed to f/{cameraSettings.availableApertureStops[_currentApertureIndex]}");
            }
        }

        /// <summary>
        /// Increases the camera's shutter speed (longer exposure).
        /// </summary>
        public void IncreaseShutterSpeed() {
            if (cameraSettings == null) return;

            // Increasing index = slower shutter (more light).
            if (_currentShutterSpeedIndex < cameraSettings.availableShutterSpeedStops.Length - 1) {
                _currentShutterSpeedIndex++;
                ApplyCameraParameters();

                float speed = cameraSettings.availableShutterSpeedStops[_currentShutterSpeedIndex];
                Debug.Log($"Shutter speed changed to {FormatShutterSpeed(speed)}");
            }
        }

        /// <summary>
        /// Returns the number of photos taken with this camera.
        /// Used by the tutorial system to check if the player has taken a photo.
        /// </summary>
        public int GetPhotoCount() {
            return photoAlbum != null ? photoAlbum.Count : 0;
        }


        /// <summary>
        /// Decreases the camera's shutter speed (shorter exposure).
        /// </summary>
        public void DecreaseShutterSpeed() {
            if (cameraSettings == null) return;

            // Decreasing index = faster shutter (less light)
            if (_currentShutterSpeedIndex > 0) {
                _currentShutterSpeedIndex--;
                ApplyCameraParameters();

                float speed = cameraSettings.availableShutterSpeedStops[_currentShutterSpeedIndex];
                Debug.Log($"Shutter speed changed to {FormatShutterSpeed(speed)}");
            }
        }

        /// <summary>
        /// Applies the current camera parameters to the physical camera.
        /// </summary>
        private void ApplyCameraParameters() {
            if (cameraSettings == null) return;

            // Get the current parameter values.
            int iso = cameraSettings.availableISOStops[_currentISOIndex];
            float aperture = cameraSettings.availableApertureStops[_currentApertureIndex];
            float shutterSpeed = cameraSettings.availableShutterSpeedStops[_currentShutterSpeedIndex];
            float focalLength = _currentFocalLength;

            // Get sensor size from camera settings.
            Vector2 sensorSize = cameraSettings.GetSensorSize();

            // Apply to screen camera if available.
            if (cameraRenderer != null) {
                // Enable physical camera .
                cameraRenderer.usePhysicalProperties = true;

                // Basic camera parameters.
                cameraRenderer.iso = iso;
                cameraRenderer.aperture = aperture;
                cameraRenderer.shutterSpeed = shutterSpeed;
                cameraRenderer.focalLength = focalLength;

                // Sensor size.
                cameraRenderer.sensorSize = sensorSize;

                // Apply aperture shape settings.
                cameraRenderer.bladeCount = cameraSettings.apertureBladeCount;
                cameraRenderer.curvature = cameraSettings.apertureBladeCurvature;
                cameraRenderer.barrelClipping = cameraSettings.apertureBarrelClipping;
                cameraRenderer.anamorphism = cameraSettings.apertureAnamorphism;

                // Set focus distance.
                cameraRenderer.focusDistance = _currentFocusDistance;
            }

            // Apply to viewfinder camera if available.
            if (viewfinderCamera != null) {
                // Enable physical camera mode.
                viewfinderCamera.usePhysicalProperties = true;

                // Basic camera parameters.
                viewfinderCamera.iso = iso;
                viewfinderCamera.aperture = aperture;
                viewfinderCamera.shutterSpeed = shutterSpeed;
                viewfinderCamera.focalLength = focalLength;

                // Sensor size.
                viewfinderCamera.sensorSize = sensorSize;

                // Apply aperture shape settings.
                viewfinderCamera.bladeCount = cameraSettings.apertureBladeCount;
                viewfinderCamera.curvature = cameraSettings.apertureBladeCurvature;
                viewfinderCamera.barrelClipping = cameraSettings.apertureBarrelClipping;
                viewfinderCamera.anamorphism = cameraSettings.apertureAnamorphism;

                // Set focus distance.
                viewfinderCamera.focusDistance = _currentFocusDistance;
            }

            // ! Apply post-processing effects for camera settings.
            ApplyIsoBasedGrain();
            ApplyApertureBasedDepthOfField();
            ApplyShutterSpeedBasedMotionBlur(); 
        }

        /// <summary>
        /// Calculates the quality of a photo based on current settings.
        /// </summary>
        private float CalculatePhotoQuality() {
            if (cameraSettings == null) return 0.5f;

            float quality = 0.5f; // Base quality

            // ISO factor (lower ISO = better quality).
            float isoFactor = 1.0f - Mathf.InverseLerp(cameraSettings.minISO, cameraSettings.maxISO,
                cameraSettings.availableISOStops[_currentISOIndex]);

            // Aperture factor (mid-range apertures are sharpest).
            float aperture = cameraSettings.availableApertureStops[_currentApertureIndex];
            float apertureFactor;
            if (aperture < 4.0f)
                apertureFactor = Mathf.InverseLerp(cameraSettings.minAperture, 4.0f, aperture);
            else if (aperture > 11.0f)
                apertureFactor = 1.0f - Mathf.InverseLerp(11.0f, cameraSettings.maxAperture, aperture);
            else
                apertureFactor = 1.0f;

            // Shutter speed factor (too slow can introduce blur).
            float shutterSpeed = cameraSettings.availableShutterSpeedStops[_currentShutterSpeedIndex];
            float shutterFactor = shutterSpeed < 1 / 30f
                ? 1.0f
                : Mathf.InverseLerp(1 / 30f, 1.0f, shutterSpeed);

            // Weight the factors to calculate overall quality.
            quality = 0.4f * isoFactor + 0.4f * apertureFactor + 0.2f * shutterFactor;
            quality = Mathf.Clamp01(quality);

            return quality;
        }

        /// <summary>
        /// Formats shutter speed as a fraction or decimal for UI display.
        /// </summary>
        private string FormatShutterSpeed(float shutterSpeed) {
            if (shutterSpeed >= 1f)
                return $"{shutterSpeed}s";
            else
                return $"1/{Mathf.Round(1f / shutterSpeed)}";
        }

        #region UI Access Methods

        /// <summary>
        /// Returns the current aperture f-number.
        /// </summary>
        public float GetCurrentAperture() {
            if (cameraSettings == null) return 0;
            return cameraSettings.availableApertureStops[_currentApertureIndex];
        }

        /// <summary>
        /// Returns the current ISO value.
        /// </summary>
        public int GetCurrentISO() {
            if (cameraSettings == null) return 0;
            return cameraSettings.availableISOStops[_currentISOIndex];
        }

        /// <summary>
        /// Returns the current shutter speed in seconds.
        /// </summary>
        public float GetCurrentShutterSpeed() {
            if (cameraSettings == null) return 0;
            return cameraSettings.availableShutterSpeedStops[_currentShutterSpeedIndex];
        }

        /// <summary>
        /// Returns the current focal length in mm.
        /// </summary>
        public float GetCurrentFocalLength() {
            return _currentFocalLength;
        }

        /// <summary>
        /// Returns whether the camera is currently focusing.
        /// </summary>
        public bool IsFocusing() {
            return _isFocusing;
        }

        /// <summary>
        /// Returns whether the camera has focus locked.
        /// </summary>
        public bool IsFocused() {
            return _isFocused;
        }

        /// <summary>
        /// Gets or sets the UI controller for this camera
        /// </summary>
        public CameraUIController UIController {
            get { return _uiController; }
            set { _uiController = value; }
        }

        #endregion

        #region Camera Post Processing Effects

        /// <summary>
        /// Adjusts the Film Grain post-processing effect based on ISO.
        /// </summary>
        private void ApplyIsoBasedGrain() {
            if (localFilmGrain == null || cameraSettings == null) return;

            int currentISO = cameraSettings.availableISOStops[_currentISOIndex];
            float isoNormalized = Mathf.InverseLerp(cameraSettings.minISO, cameraSettings.maxISO, currentISO);

            // Control Grain Intensity.
            float minIntensity = 0.0f; // Minimum grain intensity.
            float maxIntensity = 0.5f; // Maximum grain intensity.
            localFilmGrain.intensity.value = Mathf.Lerp(minIntensity, maxIntensity, isoNormalized);

            // Control Grain Response.
            float minResponse = 0.1f; // Minimum grain response.
            float maxResponse = 0.9f; // Maximum grain response.
            localFilmGrain.response.value = Mathf.Lerp(minResponse, maxResponse, isoNormalized);
        }


        /// <summary>
        /// Adjusts the Depth of Field effect based on the current aperture and focus.
        /// This version utilizes Unity's Physical Camera settings for DoF calculations.
        /// </summary>
        private void ApplyApertureBasedDepthOfField() {
            if (localDepthOfField == null || cameraSettings == null) return;
            localDepthOfField.active = true;
            localDepthOfField.focusMode.value = DepthOfFieldMode.UsePhysicalCamera;
            localDepthOfField.focusDistance.value = _currentFocusDistance;

            if (cameraRenderer != null) {
                cameraRenderer.usePhysicalProperties = true;

                float aperture = cameraSettings.availableApertureStops[_currentApertureIndex];
                cameraRenderer.aperture = aperture;
                cameraRenderer.focalLength = _currentFocalLength;
                cameraRenderer.focusDistance = _currentFocusDistance;
            }

            if (viewfinderCamera != null) {
                viewfinderCamera.usePhysicalProperties = true;

                float aperture = cameraSettings.availableApertureStops[_currentApertureIndex];
                viewfinderCamera.aperture = aperture;
                viewfinderCamera.focalLength = _currentFocalLength;
                viewfinderCamera.focusDistance = _currentFocusDistance;
            }

            // Adjust quality settings for DoF based on aperture
            float currentAperture = cameraSettings.availableApertureStops[_currentApertureIndex];

            // ------------------------------------------------------------------------------------------------------
            // Adjust sample count and max radius based on aperture.!!
            // For wide apertures (small f-numbers like f/1.4), higher quality should be used.
            // For narrow apertures (large f-numbers like f/22), lower should be fine.
            // ------------------------------------------------------------------------------------------------------

            // Normalized aperture value (inversed, 0 = widest aperture, 1 = narrowest aperture).
            float apertureNormalized =
                Mathf.InverseLerp(cameraSettings.minAperture, cameraSettings.maxAperture, currentAperture);

            // Adjust max blur radius - wide apertures create more pronounced bokeh.
            float nearMaxRadius = Mathf.Lerp(8f, 3f, apertureNormalized);
            float farMaxRadius = Mathf.Lerp(10f, 4f, apertureNormalized);

            // Apply quality settings.
            localDepthOfField.nearMaxBlur = nearMaxRadius;
            localDepthOfField.farMaxBlur = farMaxRadius;
        }

        /// <summary>
        /// Adjusts the Motion Blur post-processing effect based on shutter speed.
        /// Slower shutter speeds result in higher motion blur intensity.
        /// </summary>
        private void ApplyShutterSpeedBasedMotionBlur() {
            if (localMotionBlur == null || cameraSettings == null ||
                cameraSettings.availableShutterSpeedStops.Length <= 1) {
                if (localMotionBlur != null) localMotionBlur.active = false;
                return;
            }

            // --- Intensity Mapping based on Shutter Speed Index ---
            int maxIndex = cameraSettings.availableShutterSpeedStops.Length - 1;

            // Normalize the index (0 = fastest, 1 = slowest).
            float normalizedIndex = maxIndex > 0 ? (float)_currentShutterSpeedIndex / maxIndex : 0f;

            // Define the min/max intensity range for motion blur.
            // Adjust these values to control the effect strength.
            float minBlurIntensity = 0.0f; // Intensity for the fastest shutter speed.
            float maxBlurIntensity = 20.0f; // Max intensity for the slowest shutter speed.

            // Calculate the target intensity using Lerp.
            float targetIntensity = Mathf.Lerp(minBlurIntensity, maxBlurIntensity, normalizedIndex);
                
            // Apply the intensity to the Motion Blur override.
            localMotionBlur.intensity.value = targetIntensity;

            // Activate the effect only if the intensity is noticeable.
            localMotionBlur.active = targetIntensity > 0.01f;

            // ? Todo: 
            // int baseSampleCount = 8; // Match your Volume Profile setting
            // int maxSampleCount = 12;
            // localMotionBlur.sampleCount.value = Mathf.RoundToInt(Mathf.Lerp(baseSampleCount, maxSampleCount, normalizedIndex));

            // Debug.Log($"Shutter Index: {_currentShutterSpeedIndex}/{maxIndex}, Norm: {normalizedIndex:F2}, Blur Intensity: {targetIntensity:F2}");
        }

        #endregion

        public delegate void PhotoCapturedEvent(CapturedPhoto photo);

        public event PhotoCapturedEvent OnPhotoCapture;
    }
}