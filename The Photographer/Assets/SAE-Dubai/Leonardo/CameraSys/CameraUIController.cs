using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SAE_Dubai.Leonardo.CameraSys
{
    /// <summary>
    /// Manages the camera's world space UI display, showing camera settings and focus information.
    /// This class updates the UI elements based on the current state of the camera and its settings.
    /// </summary>
    public class CameraUIController : MonoBehaviour
    {
        [Header("- Camera Reference")]
        [Tooltip("Reference to the CameraSystem that this UI displays information for")]
        public CameraSystem cameraSystem;
        
        [Header("- Text Components")]
        [Tooltip("Text that displays the current aperture/f-stop")]
        public TextMeshProUGUI apertureText;
        
        [Tooltip("Text that displays the current shutter speed")]
        public TextMeshProUGUI shutterSpeedText;
        
        [Tooltip("Text that displays the current ISO value")]
        public TextMeshProUGUI isoText;
        
        [Tooltip("Text that displays focus information")]
        public TextMeshProUGUI focusText;
        
        [Tooltip("Text that displays the current focal length")]
        public TextMeshProUGUI focalLengthText;
        
        [FormerlySerializedAs("_focusReticleImage")]
        [Header("- UI Elements")]
        [Tooltip("Focus reticle/indicator in the center of the screen")]
        public Image focusReticleImage;
        
        [Tooltip("Whether the UI is active when the camera is turned off")]
        public bool hideWhenCameraOff = true;
        
        [Header("- Focus Animation")]
        [Tooltip("Color of the focus reticle when focusing")]
        public Color focusingColor = Color.yellow;
        
        [Tooltip("Color of the focus reticle when focused")]
        public Color focusedColor = Color.green;
        
        [Tooltip("Color of the focus reticle when not focused")]
        public Color unfocusedColor = Color.red;
        
        // Reference to the canvas component.
        private Canvas _uiCanvas;
        
        // Store original colors for restoration.
        private Color _originalFocusReticleColor;
        
        // Keeping track of focus state.
        private bool _isFocused;
        private bool _isFocusing;
        
        /// <summary>
        /// Initialize component references and set up the UI.
        /// </summary>
        private void Start()
        {
            // Get the canvas component.
            _uiCanvas = GetComponent<Canvas>();
            
            // If no camera system is assigned, try to find one on the parent.
            if (cameraSystem == null)
            {
                cameraSystem = GetComponentInParent<CameraSystem>();
                
                if (cameraSystem == null)
                {
                    Debug.LogError("CameraUIController.cs: No CameraSystem assigned or found in parent!");
                }
                else
                {
                    // Register with camera system.
                    cameraSystem.UIController = this;
                }
            }
            
            // Initial UI update.
            UpdateUI();
        }
        
        /// <summary>
        /// Update the UI elements based on camera state.
        /// </summary>
        private void Update()
        {
            if (cameraSystem == null) return;
            
            // Just to keep track of the focus from the CameraSys.
            _isFocusing = cameraSystem.IsFocusing();
            _isFocused = cameraSystem.IsFocused();
            
            // Show/hide UI based on camera state.
            if (hideWhenCameraOff && _uiCanvas != null)
            {
                _uiCanvas.enabled = cameraSystem.isCameraOn;
            }
            
            // Only update the UI when the camera is on.
            if (cameraSystem.isCameraOn)
            {
                UpdateUI();
            }
        }
        
        /// <summary>
        /// Updates all UI elements to reflect current camera settings.
        /// </summary>
        private void UpdateUI()
        {
            Debug.Log($"is focused: {_isFocused} is focusing: {_isFocusing}");
            
            if (cameraSystem == null || cameraSystem.cameraSettings == null) return;
            
            // Get current camera settings.
            CameraSettings settings = cameraSystem.cameraSettings;
            
            // Update aperture text.
            if (apertureText != null)
            {
                float aperture = cameraSystem.GetCurrentAperture();
                apertureText.text = $"F: {aperture:F1}";
            }
            
            // Update shutter speed text.
            if (shutterSpeedText != null)
            {
                float shutterSpeed = cameraSystem.GetCurrentShutterSpeed();
                shutterSpeedText.text = FormatShutterSpeed(shutterSpeed);
            }
            
            // Update ISO text.
            if (isoText != null)
            {
                int iso = cameraSystem.GetCurrentISO();
                isoText.text = $"ISO: {iso}";
            }
            
            // Update focal length text if available.
            if (focalLengthText != null)
            {
                float focalLength = cameraSystem.GetCurrentFocalLength();
                focalLengthText.text = $"{focalLength:F0}mm";
            }
            
            // Update focus state.
            _isFocusing = cameraSystem.IsFocusing();
            
            // Update focus UI.
            UpdateFocusUI();
        }
        
        /// <summary>
        /// Updates the focus-related UI elements.
        /// </summary>
        private void UpdateFocusUI()
        {
            // Update focus reticle color based on focus state.
            if (focusReticleImage != null)
            {
                if (_isFocusing)
                {
                    focusReticleImage.color = focusingColor;
                }
                else if (_isFocused)
                {
                    focusReticleImage.color = focusedColor;
                }
                else
                {
                    focusReticleImage.color = unfocusedColor;
                }
            }
    
            // Update focus text.
            if (focusText != null)
            {
                if (_isFocusing)
                {
                    focusText.text = "Focusing...";
                }
                else if (_isFocused)
                {
                    focusText.text = "Focus Locked";
                }
                else
                {
                    focusText.text = "No Focus - Tap to Try Again";
                }
            }
        }
        
        /// <summary>
        /// Called by the CameraSystem when focus is achieved.
        /// </summary>
        public void OnFocusAchieved()
        {
            _isFocused = true;
            _isFocusing = false;
        }
        
        /// <summary>
        /// Called by the CameraSystem when focus is lost.
        /// </summary>
        public void OnFocusLost()
        {
            _isFocused = false;
        }
        
        /// <summary>
        /// Formats shutter speed as a fraction or decimal for UI display.
        /// </summary>
        private string FormatShutterSpeed(float shutterSpeed)
        {
            if (shutterSpeed >= 1f)
                return $"{shutterSpeed}s";
            else
                return $"1/{Mathf.Round(1f / shutterSpeed)}";
        }
    }
}