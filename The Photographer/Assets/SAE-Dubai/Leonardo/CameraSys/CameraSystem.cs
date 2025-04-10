using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE_Dubai.Leonardo.CameraSys
{
    public class CameraSystem : MonoBehaviour
    {
        [Header("- Camera Setup")] public CameraSettings CameraSettings;

        [Header("- Camera Controller")] public CameraController cameraController;

        [Header("- Rendering")] public Camera screenCamera;
        public bool usingViewfinder = false;
        public Renderer screenRenderer;
        public Material screenOnMaterial;
        public Material screenOffMaterial;

        [Header("- Mode Settings")] public bool flashEnabled = false;

        [Header("- State")] public bool isInPhotoMode = false;
        private bool _isFocusing = false;
        private bool _isCapturing = false;

        [Header("- UI References")] public GameObject photographyUI;

        [Header("- References")] public AudioSource audioSource;
        public Camera mainCamera;
        private float _defaultFOV;

        [Header("- Photo Storage")] public int maxPhotoCapacity = 50;
        public int remainingPhotos;
        public List<CapturedPhoto> photoAlbum = new List<CapturedPhoto>();

        [Header("- Quality Settings")] [Range(0.1f, 1f)]
        public float focusPrecision = 0.9f;

        [Header("- Camera Components")] public Camera viewfinderCamera;
        public Canvas settingsCanvas;
        public bool useViewfinder;

        [Header("- Input keys")] public KeyCode enterPhotoModeKey = KeyCode.C;
        public KeyCode takePhotoKey = KeyCode.Mouse0;
        public KeyCode focusKey = KeyCode.Mouse1;
        public KeyCode toggleViewfinderKey = KeyCode.V;

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

            remainingPhotos = maxPhotoCapacity;
            
            if (photographyUI != null)
            {
                photographyUI.SetActive(false);
            }

            if (cameraController == null)
            {
                cameraController = GetComponent<CameraController>();
                if (cameraController == null && screenCamera != null)
                {
                    cameraController = screenCamera.GetComponent<CameraController>();
                }

                if (cameraController == null)
                {
                    Debug.LogWarning(
                        "CameraSystem: No CameraController found. Camera parameter adjustment will not work.");
                }
            }

            InitializeCameraSettings();
        }

        private void Update()
        {
            if (Input.GetKeyDown(enterPhotoModeKey))
            {
                TogglePhotoMode();
            }

            if (Input.GetKeyDown(toggleViewfinderKey) && isInPhotoMode)
            {
                ToggleViewMode();
            }

            if (!isInPhotoMode)
                return;

            HandlePhotoModeInputs();
        }

        private void InitializeCameraSettings()
        {
            if (CameraSettings != null)
            {
                switch (CameraSettings.cameraType)
                {
                    case CameraModel.Beginner:
                        if (cameraController != null)
                        {
                            cameraController.autoExposure = true;
                            cameraController.currentISO = CameraSettings.baseISO;
                            cameraController.currentAperture = 5.6f;
                            cameraController.currentShutterSpeed = 1 / 125f;
                            cameraController.currentFocalLength = CameraSettings.focalLengthMin;
                        }

                        break;

                    case CameraModel.Intermediate:
                        if (cameraController != null)
                        {
                            cameraController.autoExposure = false;
                            cameraController.currentISO = CameraSettings.baseISO;
                            cameraController.currentAperture = 4f;
                            cameraController.currentShutterSpeed = 1 / 250f;
                            cameraController.currentFocalLength = CameraSettings.focalLengthMin;
                        }

                        break;

                    case CameraModel.Professional:
                        if (cameraController != null)
                        {
                            cameraController.autoExposure = false;
                            cameraController.currentISO = CameraSettings.baseISO;
                            cameraController.currentAperture = CameraSettings.minAperture;
                            cameraController.currentShutterSpeed = 1 / 500f;
                            cameraController.currentFocalLength = CameraSettings.focalLengthMin;
                        }

                        break;
                }

                if (cameraController != null)
                {
                    cameraController.minAperture = CameraSettings.minAperture;
                    cameraController.maxAperture = CameraSettings.maxAperture;
                    cameraController.minFocalLength = CameraSettings.focalLengthMin;
                    cameraController.maxFocalLength = CameraSettings.focalLengthMax;
                }
            }
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

                if (cameraController != null)
                {
                    cameraController.SetActive(true);
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

                if (cameraController != null)
                {
                    cameraController.SetActive(false);
                }
            }
        }

        public void ToggleViewMode()
        {
            usingViewfinder = !usingViewfinder;

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
        }

        private void HandlePhotoModeInputs()
        {
            if (Input.GetKeyDown(takePhotoKey) && !_isCapturing)
            {
                CapturePhoto();
            }

            if (Input.GetKeyDown(focusKey) && !_isFocusing)
            {
                StartCoroutine(FocusEffect());
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

            if (CameraSettings.shutterSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(CameraSettings.shutterSound);
            }

            StartCoroutine(CapturePhotoEffect());
        }

        private IEnumerator CapturePhotoEffect()
        {
            if (flashEnabled && CameraSettings.hasBuiltInFlash)
            {
                // TODO: Add flash effect (could be a bright white flash overlay)
            }

            CapturedPhoto newPhoto = new CapturedPhoto
            {
                TimeStamp = System.DateTime.Now
            };

            if (cameraController != null)
            {
                newPhoto.iso = cameraController.currentISO;
                newPhoto.aperture = cameraController.currentAperture;
                newPhoto.shutterSpeed = cameraController.currentShutterSpeed;
                newPhoto.focalLength = cameraController.currentFocalLength;

                newPhoto.quality = CalculatePhotoQuality();
            }

            photoAlbum.Add(newPhoto);

            remainingPhotos--;

            yield return new WaitForSeconds(0.5f);

            _isCapturing = false;

            Debug.Log($"Photo captured, quality: {newPhoto.quality:F2}, {remainingPhotos} photos remaining.");
        }

        private IEnumerator FocusEffect()
        {
            _isFocusing = true;

            if (CameraSettings.focusSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(CameraSettings.focusSound);
            }
            
            yield return new WaitForSeconds(0.3f);

            _isFocusing = false;
            Debug.Log("Camera focused");
        }

        private float CalculatePhotoQuality()
        {
            float quality = 0.5f; // A base one.

            if (cameraController != null)
            {

                float isoFactor = 1.0f - Mathf.InverseLerp(100, 12800, cameraController.currentISO);

                float apertureFactor;
                if (cameraController.currentAperture < 4.0f)
                    apertureFactor = Mathf.InverseLerp(1.4f, 4.0f, cameraController.currentAperture);
                else if (cameraController.currentAperture > 11.0f)
                    apertureFactor = 1.0f - Mathf.InverseLerp(11.0f, 22.0f, cameraController.currentAperture);
                else
                    apertureFactor = 1.0f;

                float shutterFactor = cameraController.currentShutterSpeed < 1 / 30f
                    ? 1.0f
                    : Mathf.InverseLerp(1 / 30f, 1.0f, cameraController.currentShutterSpeed);

                quality = 0.4f * isoFactor + 0.4f * apertureFactor + 0.2f * shutterFactor;
                quality = Mathf.Clamp01(quality);
            }

            return quality;
        }
    }
}