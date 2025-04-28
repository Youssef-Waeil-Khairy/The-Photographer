using System.Collections.Generic;
using UnityEngine;
using SAE_Dubai.Leonardo.CameraSys;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

namespace SAE_Dubai.JW.UI
{
    /// <summary>
    /// Populates the camera shop with cameras based on the CameraSettings ScriptableObjects.
    /// Uses Odin Inspector for enhanced editor functionality.
    /// </summary>
    public class ShopCameraPopulator : MonoBehaviour
    {
        [FoldoutGroup("References")]
        [Required]
        [Tooltip("The parent transform where camera buttons will be instantiated")]
        [SerializeField] private Transform cameraButtonsContainer;
        
        [FoldoutGroup("References")]
        [Required]
        [Tooltip("The camera button prefab that will be instantiated for each camera")]
        [SerializeField] private GameObject cameraButtonPrefab;
        
        [FoldoutGroup("References")]
        [Required]
        [Tooltip("Reference to the camera info panel that will display camera details")]
        [SerializeField] private CameraInfoPanel cameraInfoPanel;

        [FoldoutGroup("Camera Data")]
        [TableList(ShowIndexLabels = true)]
        [Tooltip("List of available camera settings to populate the shop with")]
        [SerializeField] private List<CameraSettings> availableCameras = new List<CameraSettings>();
        
        [FoldoutGroup("Settings")]
        [Tooltip("If true, the shop will be populated automatically on start")]
        [SerializeField] private bool populateOnStart = true;

        [FoldoutGroup("Settings")]
        [Tooltip("Default camera prefab to use if none is specified in a CameraSettings")]
        [SerializeField] private GameObject defaultCameraPrefab;
        
#if UNITY_EDITOR
        [FoldoutGroup("Editor Tools")]
        [ReadOnly]
        [ShowInInspector]
        private List<CameraSettings> scannedCameras = new List<CameraSettings>();
        
        [FoldoutGroup("Editor Tools")]
        [Button("Scan Project for Cameras")]
        private void ScanForCameras()
        {
            scannedCameras.Clear();
            string[] guids = AssetDatabase.FindAssets("t:CameraSettings");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CameraSettings cameraSettings = AssetDatabase.LoadAssetAtPath<CameraSettings>(path);
                
                if (cameraSettings != null)
                {
                    scannedCameras.Add(cameraSettings);
                }
            }
        }
        
        [FoldoutGroup("Editor Tools")]
        [Button("Add Selected Cameras to Shop")]
        private void AddScannedCamerasToShop()
        {
            foreach (var camera in scannedCameras)
            {
                if (!availableCameras.Contains(camera))
                {
                    availableCameras.Add(camera);
                }
            }
        }

        [FoldoutGroup("Editor Tools")]
        [Button("Add All Scanned Cameras That Don't Exist")]
        private void AddMissingCamerasToShop()
        {
            bool added = false;
            foreach (var camera in scannedCameras)
            {
                if (!availableCameras.Contains(camera))
                {
                    availableCameras.Add(camera);
                    added = true;
                }
            }
            
            if (added)
            {
                PopulateShop();
            }
        }
#endif

        [Button("Populate Shop Now")]
        [PropertyTooltip("Manually refresh the camera shop with current settings")]
        public void PopulateShop()
        {
            if (cameraButtonsContainer == null)
            {
                Debug.LogError("ShopCameraPopulator: Camera buttons container not assigned!", this);
                return;
            }

            if (cameraButtonPrefab == null)
            {
                Debug.LogError("ShopCameraPopulator: Camera button prefab not assigned!", this);
                return;
            }

            if (cameraInfoPanel == null)
            {
                Debug.LogWarning("ShopCameraPopulator: Camera info panel not assigned!", this);
            }

            // Clear existing camera buttons
            foreach (Transform child in cameraButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            // Add new camera buttons
            foreach (CameraSettings cameraSettings in availableCameras)
            {
                if (cameraSettings == null) continue;

                // Create button
                GameObject buttonObj = Instantiate(cameraButtonPrefab, cameraButtonsContainer);
                CameraButton cameraButton = buttonObj.GetComponent<CameraButton>();

                if (cameraButton != null)
                {
                    // Initialize the button with camera data
                    cameraButton.Initialize(cameraSettings, cameraInfoPanel);
                }
                else
                {
                    Debug.LogError($"ShopCameraPopulator: Button prefab doesn't have CameraButton component!", this);
                }
            }

            // Adjust container layout if needed
            if (cameraButtonsContainer.TryGetComponent<UnityEngine.UI.LayoutGroup>(out var layoutGroup))
            {
                Canvas.ForceUpdateCanvases();
                layoutGroup.enabled = false;
                layoutGroup.enabled = true;
            }
        }
        
        private void Start()
        {
            if (populateOnStart)
            {
                PopulateShop();
            }
        }

        /// <summary>
        /// Adds a new camera to the available cameras list and updates the shop.
        /// </summary>
        /// <param name="cameraSettings">The camera settings to add</param>
        /// <param name="updateShop">Whether to update the shop display immediately</param>
        [FoldoutGroup("Runtime API")]
        [Button("Add Camera")]
        public void AddCamera(CameraSettings cameraSettings, bool updateShop = true)
        {
            if (cameraSettings == null) return;

            if (!availableCameras.Contains(cameraSettings))
            {
                availableCameras.Add(cameraSettings);
                
                if (updateShop)
                {
                    PopulateShop();
                }
            }
        }

        /// <summary>
        /// Removes a camera from the available cameras list and updates the shop.
        /// </summary>
        /// <param name="cameraSettings">The camera settings to remove</param>
        /// <param name="updateShop">Whether to update the shop display immediately</param>
        [FoldoutGroup("Runtime API")]
        [Button("Remove Camera")]
        public void RemoveCamera(CameraSettings cameraSettings, bool updateShop = true)
        {
            if (cameraSettings == null) return;

            if (availableCameras.Contains(cameraSettings))
            {
                availableCameras.Remove(cameraSettings);
                
                if (updateShop)
                {
                    PopulateShop();
                }
            }
        }

        /// <summary>
        /// Gets the list of available cameras.
        /// </summary>
        /// <returns>List of available camera settings</returns>
        public List<CameraSettings> GetAvailableCameras()
        {
            return new List<CameraSettings>(availableCameras);
        }
        
        /// <summary>
        /// Clears all cameras from the shop
        /// </summary>
        [FoldoutGroup("Runtime API")]
        [Button("Clear All Cameras", ButtonSizes.Large), GUIColor(1, 0.5f, 0.5f)]
        public void ClearAllCameras()
        {
            availableCameras.Clear();
            PopulateShop();
        }
    }
}