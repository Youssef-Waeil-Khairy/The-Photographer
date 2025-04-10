using System.Collections.Generic;
using UnityEngine;

namespace SAE_Dubai.Leonardo.CameraSys
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }
        
        [Header("- References")]
        public Transform cameraHoldPosition;
        
        // Dictionary to store camera instances by name
        private Dictionary<string, GameObject> cameraInstances = new Dictionary<string, GameObject>();
        private CameraSystem activeCamera;
        
        // Reference to the hotbar
        private Hotbar.Hotbar hotbar;
        
        private void Awake()
        {
            // Set up singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Find the hotbar in the scene
            hotbar = FindObjectOfType<Hotbar.Hotbar>();
            
            if (hotbar == null)
            {
                Debug.LogError("CameraManager.cs: No Hotbar found in the scene!");
            }
        }
        
        private void Update()
        {
            // Check if hotbar has selected a camera
            if (hotbar != null)
            {
                string selectedItem = hotbar.GetSelectedEquipment();
                
                // If a camera is selected in the hotbar.
                if (cameraInstances.ContainsKey(selectedItem))
                {
                    // Show this camera if it's not already active.
                    ShowCamera(selectedItem);
                }
                else if (activeCamera != null)
                {
                    // Hide the camera if a non-camera item is selected.
                    HideActiveCamera();
                }
            }
        }
        
        public void RegisterCamera(string cameraName, GameObject cameraInstance)
        {
            // Hide the camera initially.
            cameraInstance.SetActive(false);
            
            // Move it to be a child of the hold position.
            cameraInstance.transform.SetParent(cameraHoldPosition);
            cameraInstance.transform.localPosition = Vector3.zero;
            cameraInstance.transform.localRotation = Quaternion.identity;
            
            // Add to our dictionary.
            if (!cameraInstances.ContainsKey(cameraName))
            {
                cameraInstances.Add(cameraName, cameraInstance);
                Debug.Log($"CameraManager: Registered new camera: {cameraName}");
            }
            else
            {
                // Replace existing entry.
                Destroy(cameraInstances[cameraName]);
                cameraInstances[cameraName] = cameraInstance;
                Debug.Log($"CameraManager: Replaced existing camera: {cameraName}");
            }
        }
        
        private void ShowCamera(string cameraName)
        {
            // First hide the current camera if any.
            if (activeCamera != null && activeCamera.CameraSettings.modelName != cameraName)
            {
                HideActiveCamera();
            }
            
            // Show the selected camera.
            if (cameraInstances.TryGetValue(cameraName, out GameObject cameraObj))
            {
                if (!cameraObj.activeSelf)
                {
                    cameraObj.SetActive(true);
                    activeCamera = cameraObj.GetComponent<CameraSystem>();
                    
                    // Initialize the camera if needed.
                    if (activeCamera != null)
                    {
                        //activeCamera.InitializeCamera();
                        Debug.Log($"CameraManager: Activated camera: {cameraName}");
                    }
                }
            }
        }
        
        private void HideActiveCamera()
        {
            if (activeCamera != null)
            {
                // Exit photo mode if active.
                if (activeCamera.isInPhotoMode)
                {
                    activeCamera.TogglePhotoMode();
                }
                
                // Call deselection method.
                //activeCamera.OnDeselected();
                
                // Hide the camera.
                activeCamera.gameObject.SetActive(false);
                activeCamera = null;
                
                Debug.Log("CameraManager: Deactivated camera");
            }
        }
    }
}