using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.Leonardo.CameraSys
{
    public class CameraSystem : MonoBehaviour
    {
        [Header("- Camera Setup")] public CameraSettings CameraSettings;

        [Header("- Rendering")]
        public Camera screenCamera;
        public bool usingViewfinder = false;
        public Renderer screenRenderer;
        public Material screenOnMaterial;
        public Material screenOffMaterial;
        
        [Header("- Current Settings")] [Range(100, 12800)]
        public int currentISO = 100;

        [Range(1.4f, 22f)] public float currentAperture = 5.6f;

        [Tooltip("- Current shutter speed in seconds")]
        public float currentShutterSpeed = 1 / 100f;

        [Tooltip("- Current focal length in mm")]
        public float currentFocalLength = 50f;

        [Header("- Mode Settings")] public bool autoExposure = true;
        public bool autoFocus = true;
        public bool flashEnabled = false;
        [Range(0.1f, 1f)] public float exposurePrecision = 0.8f;

        [Header("- State")] public bool isInPhotoMode = false;
        private bool _isFocusing = false;
        private bool _isCapturing = false;

        [Header("- UI References")] public GameObject photographyUI;
        public TextMeshProUGUI isoText;
        public TextMeshProUGUI apertureText;
        public TextMeshProUGUI shutterSpeedText;
        public TextMeshProUGUI focalLengthText;
        public Image exposureMeter;

        [Header("- References")] public AudioSource audioSource;
        public Camera mainCamera;
        private float _defaultFOV;

        [Header("- Photo Storage")] public int maxPhotoCapacity = 50; 
        public int remainingPhotos;
        public List<CapturedPhoto> photoAlbum = new List<CapturedPhoto>();

        [Header("- Quality Settings")] [Range(0.1f, 1f)]
        public float focusPrecision = 0.9f;
        
        [Header("- Camera Componentes")]
        public Camera viewfinderCamera;
        public Canvas settingsCanvas;
        public bool useViewfinder; 


        /// <summary>
        /// The input keys are just for now. TODO: Later we'll make the player actually click on the camera buttons as if physical.
        /// </summary>
        [Header("- Input keys")] public KeyCode enterPhotoModeKey = KeyCode.C;

        public KeyCode takePhotoKey = KeyCode.Mouse0;
        public KeyCode focusKey = KeyCode.Mouse1;
        public KeyCode increaseApertureKey = KeyCode.O;
        public KeyCode decreaseApertureKey = KeyCode.P;
        public KeyCode increaseShutterSpeedKey = KeyCode.K;
        public KeyCode decreaseShutterSpeedKey = KeyCode.L;
        public KeyCode increaseIsoKey = KeyCode.I;
        public KeyCode decreaseIsoKey = KeyCode.U;

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            _defaultFOV = mainCamera.fieldOfView;

            // Initialize remaining photos.
            remainingPhotos = maxPhotoCapacity;

            // Hide photography UI at start
            if (photographyUI != null)
            {
                photographyUI.SetActive(false);
            }

            // Initialize camera settings based on type
            InitializeCameraSettings();
        }

        private void Update()
        {
            if (Input.GetKeyDown(enterPhotoModeKey))
            {
                TogglePhotoMode();
            }
    
            if (Input.GetKeyDown(KeyCode.V) && isInPhotoMode)
            {
                ToggleViewMode();
            }

            if (!isInPhotoMode)
                return;

            HandlePhotoModeInputs();
    
            UpdatePhysicalCameraSettings();

            UpdatePhotoUI();
        }

        // TODO: add the actual settings -----------------------------------------------------------------------------------------------------------------------
        private void InitializeCameraSettings()
        {
            // Set defaults based on camera type
            switch (CameraSettings.cameraType)
            {
                case CameraModel.Beginner:
                    autoExposure = true;
                    autoFocus = true;

                    // TODO: MORE AUTOMATIC SETTINGS HERE

                    break;

                case CameraModel.Intermediate:
                    autoExposure = false;

                    autoFocus = true;
                    // TODO: ADJUST MANUAL OPTIONS

                    break;

                case CameraModel.Professional:
                    autoExposure = false;
                    autoFocus = false;
                    break;
            }

            currentISO = CameraSettings.baseISO;
            currentAperture = (CameraSettings.minAperture + CameraSettings.maxAperture) / 2;
            currentShutterSpeed =
                1 / 125f; // This is a standard value used to take pictures, it's usually the minimum shutter speed photographers go.
            currentFocalLength = CameraSettings.focalLengthMin;
        }

        public void TogglePhotoMode()
        {
            isInPhotoMode = !isInPhotoMode;

            if (isInPhotoMode)
            {
                Debug.Log("CameraSystem.cs: Entered photography mode");
        
                if (usingViewfinder)
                {
                    if (viewfinderCamera != null) viewfinderCamera.enabled = true;
                    if (screenCamera != null) screenCamera.enabled = false;
            
                    if (screenRenderer != null && screenOffMaterial != null)
                    {
                        screenRenderer.material = screenOffMaterial;
                    }
                }
                else
                {
                    if (viewfinderCamera != null) viewfinderCamera.enabled = false;
                    if (screenCamera != null) screenCamera.enabled = true;
            
                    if (screenRenderer != null && screenOnMaterial != null)
                    {
                        screenRenderer.material = screenOnMaterial;
                    }
                }
        
                if (photographyUI != null)
                {
                    photographyUI.SetActive(true);
                }
            }
            else
            {
                Debug.Log("CameraSystem.cs: Exited photography mode");
        
                if (viewfinderCamera != null) viewfinderCamera.enabled = false;
                if (screenCamera != null) screenCamera.enabled = false;
        
                if (screenRenderer != null && screenOffMaterial != null)
                {
                    screenRenderer.material = screenOffMaterial;
                }
        
                if (photographyUI != null)
                {
                    photographyUI.SetActive(false);
                }
        
                mainCamera.fieldOfView = _defaultFOV;
            }
        }        
        
        public void ToggleViewMode()
        {
            usingViewfinder = !usingViewfinder;
    
            if (usingViewfinder)
            {
                // Using viewfinder, disable screen camera.
                if (viewfinderCamera != null) viewfinderCamera.enabled = true;
                if (screenCamera != null) screenCamera.enabled = false;
        
                // Turn off screen (idk if this helps with performance but I'm doing it anyways).
                if (screenRenderer != null && screenOffMaterial != null)
                {
                    screenRenderer.material = screenOffMaterial;
                }
            }
            else
            {
                // Using screen, disable viewfinder camera.
                if (viewfinderCamera != null) viewfinderCamera.enabled = false;
                if (screenCamera != null) screenCamera.enabled = true;
        
                // Turn on screen.
                if (screenRenderer != null && screenOnMaterial != null)
                {
                    screenRenderer.material = screenOnMaterial;
                }
            }
        }
        
        private void UpdatePhysicalCameraSettings()
        {
            if (screenCamera != null)
            {
                screenCamera.iso = currentISO;
        
                screenCamera.aperture = currentAperture;
        
                screenCamera.focalLength = currentFocalLength;
        
                // TODO: Unity's value is in seconds, so a conversion might be needed if things are looking weird.
                screenCamera.shutterSpeed = currentShutterSpeed;
            }
    
            // Same for viewfinder.
            if (viewfinderCamera != null)
            {
                viewfinderCamera.iso = currentISO;
                viewfinderCamera.aperture = currentAperture;
                viewfinderCamera.focalLength = currentFocalLength;
                viewfinderCamera.shutterSpeed = currentShutterSpeed;
            }
        }
        
        private void HandlePhotoModeInputs()
        {

            // Take Photo.
            if (Input.GetKeyDown(takePhotoKey) && !_isCapturing)
            {
                CapturePhoto();
            }
        }
        private void CapturePhoto()
        {
            if (remainingPhotos <= 0)
            {
                Debug.Log("No remaining photos!");
                return;
            }

            _isCapturing = true;

            // Play shutter sound
            if (CameraSettings.shutterSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(CameraSettings.shutterSound);
            }

            // Capture the photo
            StartCoroutine(CapturePhotoEffect());
        }

        private IEnumerator CapturePhotoEffect()
        {
            // Flash effect if enabled
            if (flashEnabled && CameraSettings.hasBuiltInFlash)
            {
                // TODO: Add flash effect
            }

            // Calculate photo quality
            //float photoQuality = CalculatePhotoQuality();

            // Create new photo data
            CapturedPhoto newPhoto = new CapturedPhoto
            {
                TimeStamp = System.DateTime.Now,
                iso = currentISO,
                aperture = currentAperture,
                shutterSpeed = currentShutterSpeed,
                focalLength = currentFocalLength,
                //quality = photoQuality
            };

            // Add to album
            photoAlbum.Add(newPhoto);

            // Decrease remaining photos
            remainingPhotos--;

            // Visual effect - brief screen darkening
            // TODO: Add screen darkening effect

            yield return new WaitForSeconds(0.5f);

            _isCapturing = false;

            Debug.Log($"Photo captured, {remainingPhotos} photos remaining.");
        }
    

        private void UpdatePhotoUI()
        {
            if (photographyUI == null)
                return;

            // Update text displays.
            if (isoText != null)
                isoText.text = $"ISO: {currentISO}";

            if (apertureText != null)
                apertureText.text = $"F-Stop: f/{currentAperture}";

            if (shutterSpeedText != null)
            {
                string speedDisplay = currentShutterSpeed < 1f
                    ? $"1/{Mathf.Round(1f / currentShutterSpeed)}"
                    : $"{currentShutterSpeed}s";
                shutterSpeedText.text = $"Shutter: {speedDisplay}";
            }

            if (focalLengthText != null)
                focalLengthText.text = $"Focal Length: {currentFocalLength}mm";
        }
    }
}