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

        [Header("- Current Settings")] [Range(100, 12800)]
        public int currentISO = 100;
        
        [Header("- Camera Componentes")]
        public Camera viewfinderCamera;
        public Camera lcdScreenCamera;
        public Canvas settingsCanvas;
        public bool useViewfinder; 

        [Range(1.4f, 22f)] public float currentAperture = 5.6f;

        [Tooltip("- Current shutter speed in seconds")]
        public float currentShutterSpeed = 1 / 100f;

        [Tooltip("- Current focal length in mm")]
        public float currentFocalLength = 50f;

        [Header("- Mode Settings")] public bool autoExposure = true;
        public bool autoFocus = true;
        public bool flashEnabled = false;

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

        [Range(0.1f, 1f)] public float exposurePrecision = 0.8f;

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
            // Toggle photo mode
            if (Input.GetKeyDown(enterPhotoModeKey))
            {
                TogglePhotoMode();
            }

            if (!isInPhotoMode)
                return;

            // Handle photo mode inputs
            HandlePhotoModeInputs();

            // Update UI
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
        
                // Enable the correct camera.
                if (viewfinderCamera != null)
                {
                    viewfinderCamera.enabled = useViewfinder;
                }
        
                if (lcdScreenCamera != null)
                {
                    lcdScreenCamera.enabled = !useViewfinder;
                }
        
                // Show UI.
                if (photographyUI != null)
                {
                    photographyUI.SetActive(true);
                }
        
                // TODO: We might want to disable player movement here.
                // DisablePlayerMovement();
            }
            else
            {
                Debug.Log("Exited photography mode");
        
                // Disable camera viewfinder/screen.
                if (viewfinderCamera != null)
                {
                    viewfinderCamera.enabled = false;
                }
        
                if (lcdScreenCamera != null)
                {
                    lcdScreenCamera.enabled = false;
                }
        
                // Hide UI.
                if (photographyUI != null)
                {
                    photographyUI.SetActive(false);
                }
        
                // Reset camera values.
                mainCamera.fieldOfView = _defaultFOV;
        
                // Re-enable player controls if disabled?
                // EnablePlayerMovement();
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