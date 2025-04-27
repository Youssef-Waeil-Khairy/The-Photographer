using System.Collections.Generic;
using UnityEngine;

namespace SAE_Dubai.Leonardo.CameraSys
{
    /// <summary>
    /// Central manager for all camera instances in the game. Responsible for:
    /// 1. Maintaining a registry of all cameras the player has picked up.
    /// 2. Activating/deactivating cameras based on hotbar selection.
    /// 3. Providing the transform location where cameras should be held.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("- References")]
        [Tooltip("Transform where camera will be positioned when in use")]
        public Transform cameraHoldPosition;

        [Tooltip("Reference to the overlay UI used in viewfinder mode")]
        public GameObject overlayUI;

        private Dictionary<string, GameObject> _cameraInstances = new();
        [SerializeField] private CameraSystem _activeCamera;
        private Hotbar.Hotbar _hotbar;

        #region Unity Lifecycle Methods
        private void Awake() {
            if (overlayUI == null) {
                overlayUI = GameObject.FindWithTag("OverlayCameraUI");
                if (overlayUI == null) {
                    Debug.LogWarning("CameraManager.cs: No overlay UI found with tag 'OverlayCameraUI'");
                }
                else {
                    overlayUI.SetActive(false);
                }
            }
        }

        private void Start() {
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }

            _hotbar = FindFirstObjectByType<Hotbar.Hotbar>();

            if (_hotbar == null) {
                Debug.LogError("CameraManager.cs: No Hotbar found in the scene!");
            }

            if (cameraHoldPosition == null) {
                Debug.LogWarning("CameraManager.cs: No camera hold position assigned. Creating a default one.");
                GameObject holder = new GameObject("CameraHolder");
                holder.transform.SetParent(transform);
                cameraHoldPosition = holder.transform;
            }
        }

        private void Update() {
            if (_hotbar != null) {
                string selectedItem = _hotbar.GetSelectedEquipment();

                if (_cameraInstances.ContainsKey(selectedItem)) {
                    ShowCamera(selectedItem);
                }
                else if (_activeCamera != null) {
                    HideActiveCamera();
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Registers a camera instance with the manager, making it available for use.
        /// </summary>
        /// <param name="cameraName">The unique name of the camera.</param>
        /// <param name="cameraInstance">The GameObject containing the camera components.</param>
        public void RegisterCamera(string cameraName, GameObject cameraInstance) {
            if (string.IsNullOrEmpty(cameraName) || cameraInstance == null) {
                Debug.LogError("CameraManager.cs: Cannot register camera with null name or instance");
                return;
            }

            cameraInstance.SetActive(false);
            cameraInstance.transform.SetParent(cameraHoldPosition);
            cameraInstance.transform.localPosition = Vector3.zero;
            cameraInstance.transform.localRotation = Quaternion.identity;

            if (!_cameraInstances.ContainsKey(cameraName)) {
                _cameraInstances.Add(cameraName, cameraInstance);
                //Debug.Log($"CameraManager.cs: Registered new camera: {cameraName}");
            }
            else {
                Destroy(_cameraInstances[cameraName]);
                _cameraInstances[cameraName] = cameraInstance;
                Debug.Log($"CameraManager.cs: Replaced existing camera: {cameraName}");
            }
        }

        public CameraSystem GetActiveCamera() {
            return _activeCamera;
        }
        
        /// <summary>
        /// Returns the number of cameras registered with the manager.
        /// Used by the tutorial system to check if the player has purchased a camera.
        /// </summary>
        public int GetCameraCount()
        {
            return _cameraInstances.Count;
        }
        
        #endregion

        #region Private Methods
        private void ShowCamera(string cameraName) {
            if (_activeCamera != null && _activeCamera.cameraSettings.modelName != cameraName) {
                HideActiveCamera();
            }

            if (_cameraInstances.TryGetValue(cameraName, out GameObject cameraObj)) {
                if (!cameraObj.activeSelf) {
                    cameraObj.SetActive(true);
                    _activeCamera = cameraObj.GetComponent<CameraSystem>();
                    //Debug.Log($"CameraManager.cs: Activated camera: {cameraName}");
                }
            }
        }

        private void HideActiveCamera() {
            if (_activeCamera != null) {
                if (_activeCamera.isCameraOn) {
                    _activeCamera.TogglePhotoMode();
                }

                _activeCamera.gameObject.SetActive(false);
                _activeCamera = null;
                //Debug.Log("CameraManager.cs: Deactivated camera");
            }
        }
        #endregion
    }
}