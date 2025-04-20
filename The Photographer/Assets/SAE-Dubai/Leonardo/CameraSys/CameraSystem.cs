using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        public bool isCameraOn = false;

        [SerializeField] private bool _isFocusing = false;

        [SerializeField] private bool _isCapturing = false;

        [SerializeField] private bool _isFocused = false;

        [Header("- UI References")]
        [Tooltip("The UI panel containing all photography controls and information.")]
        public GameObject photographyUI;

        private CameraUIController _uiController;

        [Header("- Audio")]
        [Tooltip("Audio source for camera sounds.")]
        public AudioSource audioSource;

        [Tooltip("Reference to the main player camera.")]
        public Camera mainCamera;

        [SerializeField] private float _defaultFOV;

        [Header("- Photo Storage")]
        [Tooltip("Current number of photos remaining in storage.")]
        public int remainingPhotos;

        [Tooltip("Collection of all photos taken with this camera.")]
        public List<CapturedPhoto> photoAlbum = new List<CapturedPhoto>();

        [Header("- Camera Components")]
        [Tooltip("The viewfinder camera (if available).")]
        public Camera viewfinderCamera;

        [Tooltip("Canvas containing camera settings UI.")]
        public Canvas settingsCanvas;

        [Header("- Input Keys")]
        [Tooltip("Key to turn the camera on/off.")]
        public KeyCode turnCameraOnKey = KeyCode.C;

        [Tooltip("Key to take a photo")] public KeyCode takePhotoKey = KeyCode.Mouse0;

        [Tooltip("Key to focus the camera")] public KeyCode focusKey = KeyCode.Mouse1;

        [Tooltip("Key to toggle between viewfinder and screen.")]
        public KeyCode toggleViewfinderKey = KeyCode.V;

        // Internal variables for camera control.
        private int _currentISOIndex = 0;
        private int _currentApertureIndex = 4;
        private int _currentShutterSpeedIndex = 5;
        private float _currentFocalLength;
        private bool _autoExposureEnabled = true;

        /// <summary>
        /// Initializes the camera system with default settings.
        /// </summary>
        private void Start() {
            // Set up camera references
            if (mainCamera == null) {
                mainCamera = Camera.main;
            }

            if (audioSource == null) {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            _defaultFOV = mainCamera.fieldOfView;

            // Initialize camera settings from scriptable object.
            if (cameraSettings != null) {
                remainingPhotos = cameraSettings.photoCapacity;
                _autoExposureEnabled = cameraSettings.hasAutoISO || cameraSettings.hasAutoShutterSpeed ||
                                       cameraSettings.hasAutoAperture;

                // Find initial indices for settings.
                FindInitialSettingsIndices();

                // Set initial focal length.
                _currentFocalLength = cameraSettings.minFocalLength;
            }
            else {
                remainingPhotos = 50;
                Debug.LogWarning("CameraSystem: No camera settings assigned!");
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
            if (_currentApertureIndex < 0) _currentApertureIndex = 4; // Default to middle aperture

            _currentShutterSpeedIndex = 5; // Default to 1/125
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

            if (Input.GetKeyDown(toggleViewfinderKey) && isCameraOn) {
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
                Debug.Log("CameraSystem: Entered photography mode");

                // Set up appropriate camera display.
                if (usingViewfinder) {
                    if (viewfinderCamera != null) viewfinderCamera.enabled = true;
                    if (cameraRenderer != null) cameraRenderer.enabled = false;

                    if (screenRenderer != null && screenOffMaterial != null) {
                        screenRenderer.material = screenOffMaterial;
                    }
                }
                else {
                    if (viewfinderCamera != null) viewfinderCamera.enabled = false;
                    if (cameraRenderer != null) cameraRenderer.enabled = true;

                    if (screenRenderer != null && screenOnMaterial != null) {
                        screenRenderer.material = screenOnMaterial;
                    }
                }

                // Show UI.
                if (photographyUI != null) {
                    photographyUI.SetActive(true);
                }
            }
            else {
                Debug.Log("CameraSystem.cs: Exited photography mode");

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
                mainCamera.fieldOfView = _defaultFOV;
            }
        }

        /// <summary>
        /// Toggles between viewfinder and screen display modes.
        /// </summary>
        public void ToggleViewMode() {
            usingViewfinder = !usingViewfinder;

            if (usingViewfinder) {
                if (viewfinderCamera != null) viewfinderCamera.enabled = true;
                if (cameraRenderer != null) cameraRenderer.enabled = false;

                if (screenRenderer != null && screenOffMaterial != null) {
                    screenRenderer.material = screenOffMaterial;
                }
            }
            else {
                if (viewfinderCamera != null) viewfinderCamera.enabled = false;
                if (cameraRenderer != null) cameraRenderer.enabled = true;

                if (screenRenderer != null && screenOnMaterial != null) {
                    screenRenderer.material = screenOnMaterial;
                }
            }
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
        /// </summary>
        private void CapturePhoto() {
            if (remainingPhotos <= 0) {
                Debug.Log("No remaining photos!");
                return;
            }

            _isCapturing = true;

            // Play shutter sound
            if (cameraSettings != null && cameraSettings.shutterSound != null && audioSource != null) {
                audioSource.PlayOneShot(cameraSettings.shutterSound);
            }

            StartCoroutine(CapturePhotoEffect());
        }

        /// <summary>
        /// Handles the photo capture process and creates a photo record.
        /// </summary>
        private IEnumerator CapturePhotoEffect() {
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
                newPhoto.iso = cameraSettings.availableISOStops[_currentISOIndex];
                newPhoto.aperture = cameraSettings.availableApertureStops[_currentApertureIndex];
                newPhoto.shutterSpeed = cameraSettings.availableShutterSpeedStops[_currentShutterSpeedIndex];
                newPhoto.focalLength = _currentFocalLength;

                // Calculate photo quality based on current settings.
                newPhoto.quality = CalculatePhotoQuality();
            }

            // Add to album and update count.
            photoAlbum.Add(newPhoto);
            remainingPhotos--;

            // Delay before allowing another photo.
            yield return new WaitForSeconds(0.5f);
            _isCapturing = false;

            Debug.Log($"Photo captured, quality: {newPhoto.quality:F2}, {remainingPhotos} photos remaining.");
        }

        /// <summary>
        /// Attempts to focus the camera by casting a ray to determine focus distance.
        /// </summary>
        private void AttemptAutoFocus() {
            if (!cameraSettings.hasAutoFocus || _isFocusing)
                return;

            // Cast a ray from the center of the camera view to find focus distance.
            if (cameraRenderer != null) {
                Ray ray = cameraRenderer.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out RaycastHit hit, 100f)) {
                    // Set focus distance based on hit.
                    cameraRenderer.focusDistance = hit.distance;

                    // Start the focus effect.
                    StartCoroutine(FocusEffect());

                    Debug.Log($"Focusing at distance: {hit.distance}m");
                }
                else {
                    // No object hit, focus at default distance.
                    cameraRenderer.focusDistance = 10f;
                    StartCoroutine(FocusEffect());

                    Debug.Log("Focusing at default distance (10m)");
                }
            }
            else {
                // Just do the focus effect without changing focus distance.
                StartCoroutine(FocusEffect());
            }
        }

        /// <summary>
        /// Simulates the camera focus process.
        /// </summary>
        private IEnumerator FocusEffect() {
            _isFocusing = true;
            _isFocused = false;

            // Notify UI controller
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

            Debug.Log("Camera focused");
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
            if (cameraRenderer == null || cameraSettings == null) return;

            cameraRenderer.iso = cameraSettings.availableISOStops[_currentISOIndex];
            cameraRenderer.aperture = cameraSettings.availableApertureStops[_currentApertureIndex];
            cameraRenderer.shutterSpeed = cameraSettings.availableShutterSpeedStops[_currentShutterSpeedIndex];
            cameraRenderer.focalLength = _currentFocalLength;
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
    }
}