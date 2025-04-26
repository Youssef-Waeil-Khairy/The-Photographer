using System.Collections.Generic;
using UnityEngine;

namespace SAE_Dubai.Leonardo.CameraSys
{
    /// <summary>
    /// Central manager for all camera instances in the game. Responsible for:
    /// 1. Maintaining a registry of all cameras the player has picked up.
    /// 2. Activating/deactivating cameras based on hotbar selection.
    /// 3. Providing the transform location where cameras should be held.
    /// 
    /// This class follows the Singleton pattern to ensure only one instance exists.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance accessible from anywhere.
        /// </summary>
        public static CameraManager Instance { get; private set; }

        [Header("- References")]
        [Tooltip("Transform where camera will be positioned when in use")]
        public Transform cameraHoldPosition;

        [Tooltip("Reference to the overlay UI used in viewfinder mode")]
        public GameObject overlayUI;

        // Dictionary to store camera instances by name.
        private Dictionary<string, GameObject> _cameraInstances = new Dictionary<string, GameObject>();
        private CameraSystem _activeCamera;

        // Reference to the hotbar.
        private Hotbar.Hotbar _hotbar;

        /// <summary>
        /// Sets up the singleton pattern and ensures only one CameraManager exists.
        /// </summary>
        private void Awake() {
            // ! Singleton setup.
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }

            // Reference to the camera overlay UI (used by viewfinder in all cameras).
            if (overlayUI == null) {
                overlayUI = GameObject.FindWithTag("OverlayCameraUI");
                if (overlayUI == null) {
                    Debug.LogWarning("CameraManager.cs: No overlay UI found with tag 'OverlayCameraUI'");
                }
                else {
                    // Initially set it to inactive
                    overlayUI.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Initializes the connection to the hotbar system.
        /// </summary>
        private void Start() {
            // Find the hotbar in the scene
            _hotbar = Object.FindFirstObjectByType<Hotbar.Hotbar>();

            if (_hotbar == null) {
                Debug.LogError("CameraManager.cs: No Hotbar found in the scene!");
            }

            // Validate camera hold position
            if (cameraHoldPosition == null) {
                Debug.LogWarning("CameraManager.cs: No camera hold position assigned. Creating a default one.");
                GameObject holder = new GameObject("CameraHolder");
                holder.transform.SetParent(transform);
                cameraHoldPosition = holder.transform;
            }
        }

        /// <summary>
        /// Monitors the hotbar to activate/deactivate cameras based on selection.
        /// </summary>
        private void Update() {
            if (_hotbar != null) {
                string selectedItem = _hotbar.GetSelectedEquipment();

                // If a camera is selected in the hotbar.
                if (_cameraInstances.ContainsKey(selectedItem)) {
                    // Show this camera if it's not already active.
                    ShowCamera(selectedItem);
                }
                else if (_activeCamera != null) {
                    // Hide the camera if a non-camera item is selected.
                    HideActiveCamera();
                }
            }
        }

        /// <summary>
        /// Registers a camera instance with the manager, making it available for use.
        /// </summary>
        /// <param name="cameraName">The unique name of the camera.</param>
        /// <param name="cameraInstance">The GameObject containing the camera components.</param>
        public void RegisterCamera(string cameraName, GameObject cameraInstance) {
            // Validate parameters.
            if (string.IsNullOrEmpty(cameraName) || cameraInstance == null) {
                Debug.LogError("CameraManager.cs: Cannot register camera with null name or instance");
                return;
            }

            // Hide the camera initially.
            cameraInstance.SetActive(false);

            // Move it to be a child of the hold position.
            cameraInstance.transform.SetParent(cameraHoldPosition);
            cameraInstance.transform.localPosition = Vector3.zero;
            cameraInstance.transform.localRotation = Quaternion.identity;

            // Add to our dictionary.
            if (!_cameraInstances.ContainsKey(cameraName)) {
                _cameraInstances.Add(cameraName, cameraInstance);
                Debug.Log($"CameraManager.cs: Registered new camera: {cameraName}");
            }
            else {
                // Replace existing entry
                Destroy(_cameraInstances[cameraName]);
                _cameraInstances[cameraName] = cameraInstance;
                Debug.Log($"CameraManager.cs: Replaced existing camera: {cameraName}");
            }
        }

        /// <summary>
        /// Activates a specific camera by name and deactivates any currently active camera.
        /// </summary>
        /// <param name="cameraName">The name of the camera to activate.</param>
        private void ShowCamera(string cameraName) {
            // First hide the current camera if different from the requested one.
            if (_activeCamera != null && _activeCamera.cameraSettings.modelName != cameraName) {
                HideActiveCamera();
            }

            // Show the selected camera.
            if (_cameraInstances.TryGetValue(cameraName, out GameObject cameraObj)) {
                if (!cameraObj.activeSelf) {
                    cameraObj.SetActive(true);
                    _activeCamera = cameraObj.GetComponent<CameraSystem>();

                    // Log activation.
                    Debug.Log($"CameraManager.cs: Activated camera: {cameraName}");
                }
            }
        }

        /// <summary>
        /// Deactivates the currently active camera.
        /// </summary>
        private void HideActiveCamera() {
            if (_activeCamera != null) {
                // Exit photo mode if active.
                if (_activeCamera.isCameraOn) {
                    _activeCamera.TogglePhotoMode();
                }

                // Hide the camera.
                _activeCamera.gameObject.SetActive(false);
                _activeCamera = null;

                Debug.Log("CameraManager.cs: Deactivated camera");
            }
        }

        /// <summary>
        /// Returns the currently active CameraSystem, if any.
        /// </summary>
        public CameraSystem GetActiveCamera() {
            return _activeCamera;
        }
    }
}