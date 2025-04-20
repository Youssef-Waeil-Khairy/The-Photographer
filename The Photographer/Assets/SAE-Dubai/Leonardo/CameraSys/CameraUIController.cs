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
        [Header("- Canvas References")]
        [Tooltip("Reference to the screen overlay canvas used when in viewfinder mode.")]
        private GameObject _overlayPanel;

        private TextMeshProUGUI _overlayApertureText;
        private TextMeshProUGUI _overlayShutterSpeedText;
        private TextMeshProUGUI _overlayIsoText;
        private TextMeshProUGUI _overlayFocusText;
        private TextMeshProUGUI _overlayFocalLengthText;
        private Image _overlayFocusReticleImage;

        [Header("- Camera Reference")]
        [Tooltip("Reference to the CameraSystem that this UI displays information for")]
        public CameraSystem cameraSystem;

        [Header("- SCREEN Text Components")]
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

        [Header("- SCREEN UI Elements")]
        [Tooltip("Focus reticle/indicator in the center of the screen")]
        public Image focusReticleImage;

        [Tooltip("Whether the UI is active when the camera is turned off")]
        public bool hideWhenCameraOff = true;

        [Header("- Focus Animation Colors")]
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
        private void Start() {
            // Get the canvas component.
            _uiCanvas = GetComponent<Canvas>();

            // If no camera system is assigned, try to find one on the parent.
            if (cameraSystem == null) {
                cameraSystem = GetComponentInParent<CameraSystem>();

                if (cameraSystem == null) {
                    Debug.LogError("CameraUIController.cs: No CameraSystem assigned or found in parent!");
                }
                else {
                    // Register with camera system.
                    cameraSystem.UIController = this;
                }
            }

            CameraManager manager = FindObjectOfType<CameraManager>();
            _overlayPanel = manager.overlayUI;

            if (_overlayPanel != null) {
                _overlayApertureText = _overlayPanel.transform.Find("ApertureText")?.GetComponent<TextMeshProUGUI>();
                _overlayShutterSpeedText = _overlayPanel.transform.Find("ShutterSpeedText")?.GetComponent<TextMeshProUGUI>();
                _overlayIsoText = _overlayPanel.transform.Find("ISOText")?.GetComponent<TextMeshProUGUI>();
                _overlayFocusText = _overlayPanel.transform.Find("FocusText")?.GetComponent<TextMeshProUGUI>();
                _overlayFocalLengthText = _overlayPanel.transform.Find("FocalLengthText")?.GetComponent<TextMeshProUGUI>();
                _overlayFocusReticleImage = _overlayPanel.transform.Find("FocusReticle")?.GetComponent<Image>();
            }

            // Initial UI update.
            UpdateUI();
        }

        /// <summary>
        /// Update the UI elements based on camera state.
        /// </summary>
        private void Update() {
            if (cameraSystem == null) return;

            // Just to keep track of the focus from the CameraSys.
            _isFocusing = cameraSystem.IsFocusing();
            _isFocused = cameraSystem.IsFocused();

            // Show/hide UI based on camera state.
            if (hideWhenCameraOff && _uiCanvas != null) {
                _uiCanvas.enabled = cameraSystem.isCameraOn;
            }

            // Only update the UI when the camera is on.
            if (cameraSystem.isCameraOn) {
                UpdateUI();
            }
        }

        /// <summary>
        /// Updates all UI elements to reflect current camera settings.
        /// </summary>
        private void UpdateUI() {
            if (cameraSystem == null || cameraSystem.cameraSettings == null) return;

            // Get current camera settings.
            CameraSettings settings = cameraSystem.cameraSettings;

            // Update both world space and overlay UI elements
            UpdateUiElement(apertureText, _overlayApertureText, $"F: {cameraSystem.GetCurrentAperture():F1}");

            float shutterSpeed = cameraSystem.GetCurrentShutterSpeed();
            string shutterText = FormatShutterSpeed(shutterSpeed);
            UpdateUiElement(shutterSpeedText, _overlayShutterSpeedText, shutterText);

            int iso = cameraSystem.GetCurrentISO();
            UpdateUiElement(isoText, _overlayIsoText, $"ISO: {iso}");

            float focalLength = cameraSystem.GetCurrentFocalLength();
            UpdateUiElement(focalLengthText, _overlayFocalLengthText, $"{focalLength:F0}mm");

            // Update focus state.
            _isFocusing = cameraSystem.IsFocusing();
            _isFocused = cameraSystem.IsFocused();

            // Update focus UI.
            UpdateFocusUI();
        }

        // Helper method to update both world and overlay UI elements
        private void UpdateUiElement(TextMeshProUGUI worldText, TextMeshProUGUI overlayText, string text) {
            if (worldText != null) {
                worldText.text = text;
            }

            if (overlayText != null) {
                overlayText.text = text;
            }
        }

        /// <summary>
        /// Updates the focus-related UI elements.
        /// </summary>
        private void UpdateFocusUI() {
            // Update focus reticle colors for both UIs.
            UpdateFocusReticle(focusReticleImage, _isFocusing, _isFocused);
            UpdateFocusReticle(_overlayFocusReticleImage, _isFocusing, _isFocused);

            // Update focus text for both UIs.
            string focusMessage = GetFocusMessage(_isFocusing, _isFocused);
            UpdateUiElement(focusText, _overlayFocusText, focusMessage);
        }

        /// <summary>
        /// Updates the reticle image to the current focus state,
        /// </summary>
        /// <param name="reticle">Reticle image.</param>
        /// <param name="isFocusing">Is camera focusing?</param>
        /// <param name="isFocused">Is image focused?</param>
        private void UpdateFocusReticle(Image reticle, bool isFocusing, bool isFocused) {
            if (reticle == null) return;

            if (isFocusing) {
                reticle.color = focusingColor;
            }
            else if (isFocused) {
                reticle.color = focusedColor;
            }
            else {
                reticle.color = unfocusedColor;
            }
        }

        /// <summary>
        /// Gets the text string of the state of the camera focus.
        /// </summary>
        /// <param name="isFocusing">Is camera focusing?</param>
        /// <param name="isFocused">Is image focused</param>
        /// <returns></returns>
        private string GetFocusMessage(bool isFocusing, bool isFocused) {
            if (isFocusing) {
                return "Focusing...";
            }
            else if (isFocused) {
                return "Focus Locked";
            }
            else {
                return "No Focus";
            }
        }

        /// <summary>
        /// Called by the CameraSystem when focus is achieved.
        /// </summary>
        public void OnFocusAchieved() {
            _isFocused = true;
            _isFocusing = false;
        }

        /// <summary>
        /// Called by the CameraSystem when focus is lost.
        /// </summary>
        public void OnFocusLost() {
            _isFocused = false;
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
    }
}