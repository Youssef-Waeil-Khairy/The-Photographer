using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace SAE_Dubai.Leonardo.CameraSys
{
    /// <summary>
    /// Handles the adjustment of camera parameters (ISO, aperture, shutter speed, focal length).
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera References")]
        public Camera physicalCamera;

        [Header("Camera Parameters")]
        [Range(100, 12800)]
        public int baseISO = 100;
        [Range(100, 12800)]
        public int currentISO = 100;
        public int[] isoStops = { 100, 200, 400, 800, 1600, 3200, 6400, 12800 };
        
        [Range(1.4f, 22f)]
        public float minAperture = 1.4f;
        [Range(1.4f, 22f)]
        public float maxAperture = 16f;
        [Range(1.4f, 22f)]
        public float currentAperture = 5.6f;
        public float[] apertureStops = { 1.4f, 2f, 2.8f, 4f, 5.6f, 8f, 11f, 16f, 22f };
        
        [Tooltip("Shutter speed in seconds")]
        public float currentShutterSpeed = 0.01f; // 1/100 second
        public float[] shutterSpeedStops = { 1/4000f, 1/2000f, 1/1000f, 1/500f, 1/250f, 1/125f, 1/60f, 1/30f, 1/15f, 1/8f, 1/4f, 0.5f, 1f, 2f, 4f, 8f, 15f, 30f };
        
        [Range(18f, 200f)]
        public float minFocalLength = 18f;
        [Range(18f, 200f)]
        public float maxFocalLength = 70f;
        [Range(18f, 200f)]
        public float currentFocalLength = 50f;
        
        [Header("Control Keys")]
        public KeyCode increaseISOKey = KeyCode.I;
        public KeyCode decreaseISOKey = KeyCode.U;
        public KeyCode increaseApertureKey = KeyCode.O;
        public KeyCode decreaseApertureKey = KeyCode.P;
        public KeyCode increaseShutterSpeedKey = KeyCode.K;
        public KeyCode decreaseShutterSpeedKey = KeyCode.L;
        public KeyCode increaseFocalLengthKey = KeyCode.Period;
        public KeyCode decreaseFocalLengthKey = KeyCode.Comma;
        
        [Header("UI References")]
        public TextMeshProUGUI isoText;
        public TextMeshProUGUI apertureText;
        public TextMeshProUGUI shutterSpeedText;
        public TextMeshProUGUI focalLengthText;
        public Image exposureMeter;
        
        [Header("Mode Settings")]
        public bool autoExposure = true;
        [Range(0.1f, 1f)]
        public float exposurePrecision = 0.8f;
        
        // Keep track of indices in our stops arrays
        private int _currentISOIndex = 0;
        private int _currentApertureIndex = 4; // Start at f/5.6
        private int _currentShutterSpeedIndex = 5; // Start at 1/125
        private float _zoomSpeed = 5f;
        
        private bool _isActive = false;
        
        private void Start()
        {
            if (physicalCamera == null)
            {
                physicalCamera = GetComponent<Camera>();
                
                if (physicalCamera == null)
                {
                    Debug.LogError("CameraController: No camera found!");
                    enabled = false;
                    return;
                }
            }
            
            // Make sure the camera is set to physical mode
            physicalCamera.usePhysicalProperties = true;
            
            // Initialize indices
            _currentISOIndex = System.Array.IndexOf(isoStops, currentISO);
            if (_currentISOIndex < 0) _currentISOIndex = 0;
            
            _currentApertureIndex = System.Array.IndexOf(apertureStops, currentAperture);
            if (_currentApertureIndex < 0) _currentApertureIndex = 4; // Default to f/5.6
            
            // Find closest shutter speed
            _currentShutterSpeedIndex = 5; // Default to 1/125
            float closestDiff = Mathf.Abs(shutterSpeedStops[_currentShutterSpeedIndex] - currentShutterSpeed);
            for (int i = 0; i < shutterSpeedStops.Length; i++)
            {
                float diff = Mathf.Abs(shutterSpeedStops[i] - currentShutterSpeed);
                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    _currentShutterSpeedIndex = i;
                }
            }
            
            // Apply initial settings
            UpdateCameraParameters();
        }
        
        private void Update()
        {
            if (!_isActive) return;
            
            // Handle ISO changes
            if (Input.GetKeyDown(increaseISOKey))
            {
                IncreaseISO();
            }
            else if (Input.GetKeyDown(decreaseISOKey))
            {
                DecreaseISO();
            }
            
            // Handle Aperture changes
            if (Input.GetKeyDown(increaseApertureKey))
            {
                IncreaseAperture();
            }
            else if (Input.GetKeyDown(decreaseApertureKey))
            {
                DecreaseAperture();
            }
            
            // Handle Shutter Speed changes
            if (Input.GetKeyDown(increaseShutterSpeedKey))
            {
                IncreaseShutterSpeed();
            }
            else if (Input.GetKeyDown(decreaseShutterSpeedKey))
            {
                DecreaseShutterSpeed();
            }
            
            // Handle Focal Length changes (zoom)
            if (Input.GetKey(increaseFocalLengthKey))
            {
                currentFocalLength = Mathf.Min(currentFocalLength + _zoomSpeed * Time.deltaTime, maxFocalLength);
                UpdateCameraParameters();
            }
            else if (Input.GetKey(decreaseFocalLengthKey))
            {
                currentFocalLength = Mathf.Max(currentFocalLength - _zoomSpeed * Time.deltaTime, minFocalLength);
                UpdateCameraParameters();
            }
            
            // Update UI
            UpdateUI();
            
            // Auto exposure adjustment if enabled
            if (autoExposure)
            {
                AdjustAutoExposure();
            }
        }
        
        /// <summary>
        /// Activates or deactivates the camera controller
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
            
            // Update UI visibility
            if (isoText != null) isoText.transform.parent.gameObject.SetActive(active);
            
            // Make sure we apply parameters even when deactivating
            if (!active)
            {
                UpdateCameraParameters();
            }
        }
        
        #region Camera Parameter Adjustments
        
        public void IncreaseISO()
        {
            if (_currentISOIndex < isoStops.Length - 1)
            {
                _currentISOIndex++;
                currentISO = isoStops[_currentISOIndex];
                UpdateCameraParameters();
                Debug.Log($"ISO increased to {currentISO}");
            }
        }
        
        public void DecreaseISO()
        {
            if (_currentISOIndex > 0)
            {
                _currentISOIndex--;
                currentISO = isoStops[_currentISOIndex];
                UpdateCameraParameters();
                Debug.Log($"ISO decreased to {currentISO}");
            }
        }
        
        public void IncreaseAperture()
        {
            // Remember: Higher f-number = smaller aperture
            if (_currentApertureIndex < apertureStops.Length - 1)
            {
                _currentApertureIndex++;
                currentAperture = apertureStops[_currentApertureIndex];
                UpdateCameraParameters();
                Debug.Log($"Aperture changed to f/{currentAperture}");
            }
        }
        
        public void DecreaseAperture()
        {
            // Remember: Lower f-number = larger aperture
            if (_currentApertureIndex > 0)
            {
                _currentApertureIndex--;
                currentAperture = apertureStops[_currentApertureIndex];
                UpdateCameraParameters();
                Debug.Log($"Aperture changed to f/{currentAperture}");
            }
        }
        
        public void IncreaseShutterSpeed()
        {
            // Remember: Increasing index = slower shutter (more light)
            if (_currentShutterSpeedIndex < shutterSpeedStops.Length - 1)
            {
                _currentShutterSpeedIndex++;
                currentShutterSpeed = shutterSpeedStops[_currentShutterSpeedIndex];
                UpdateCameraParameters();
                Debug.Log($"Shutter speed changed to {FormatShutterSpeed(currentShutterSpeed)}");
            }
        }
        
        public void DecreaseShutterSpeed()
        {
            // Remember: Decreasing index = faster shutter (less light)
            if (_currentShutterSpeedIndex > 0)
            {
                _currentShutterSpeedIndex--;
                currentShutterSpeed = shutterSpeedStops[_currentShutterSpeedIndex];
                UpdateCameraParameters();
                Debug.Log($"Shutter speed changed to {FormatShutterSpeed(currentShutterSpeed)}");
            }
        }
        
        #endregion
        
        /// <summary>
        /// Apply all camera parameters to the physical camera
        /// </summary>
        private void UpdateCameraParameters()
        {
            if (physicalCamera == null) return;
            
            physicalCamera.iso = currentISO;
            physicalCamera.aperture = currentAperture;
            physicalCamera.shutterSpeed = currentShutterSpeed;
            physicalCamera.focalLength = currentFocalLength;
        }
        
        /// <summary>
        /// Update all UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (isoText != null)
                isoText.text = $"ISO: {currentISO}";
                
            if (apertureText != null)
                apertureText.text = $"f/{currentAperture}";
                
            if (shutterSpeedText != null)
                shutterSpeedText.text = FormatShutterSpeed(currentShutterSpeed);
                
            if (focalLengthText != null)
                focalLengthText.text = $"{Mathf.Round(currentFocalLength)}mm";
                
            // Update exposure meter if we have one
            if (exposureMeter != null)
            {
                // Calculate exposure value (simplified)
                float ev = Mathf.Log(currentAperture * currentAperture / currentShutterSpeed / (currentISO / 100f), 2);
                
                // Map to meter range -2 to +2
                float normalizedEV = Mathf.InverseLerp(-2, 2, ev);
                exposureMeter.fillAmount = normalizedEV;
                
                // Color the meter based on exposure
                if (normalizedEV < 0.4f)
                    exposureMeter.color = Color.blue; // Underexposed
                else if (normalizedEV > 0.6f)
                    exposureMeter.color = Color.red;  // Overexposed
                else
                    exposureMeter.color = Color.green; // Good exposure
            }
        }
        
        /// <summary>
        /// Formats shutter speed as a fraction for UI display
        /// </summary>
        private string FormatShutterSpeed(float shutterSpeed)
        {
            if (shutterSpeed >= 1f)
                return $"{shutterSpeed}s";
            else
                return $"1/{Mathf.Round(1f / shutterSpeed)}";
        }
        
        /// <summary>
        /// Automatically adjusts exposure settings based on scene brightness
        /// </summary>
        private void AdjustAutoExposure()
        {
            // This is a simplified auto-exposure - in a real implementation,
            // you would analyze the scene brightness using a render texture
            
            // For demo purposes, just detect if we're looking at something bright
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 100))
            {
                // Get renderer and check if it has an emissive material
                Renderer renderer = hit.transform.GetComponent<Renderer>();
                if (renderer != null && renderer.material.IsKeywordEnabled("_EMISSION"))
                {
                    // Looking at something bright - decrease exposure
                    if (_currentISOIndex > 0 || _currentShutterSpeedIndex > 0 || _currentApertureIndex < apertureStops.Length - 1)
                    {
                        // First try to increase f-stop (smaller aperture)
                        if (_currentApertureIndex < apertureStops.Length - 1)
                        {
                            IncreaseAperture();
                        }
                        // Then try faster shutter
                        else if (_currentShutterSpeedIndex > 0)
                        {
                            DecreaseShutterSpeed();
                        }
                        // Finally lower ISO
                        else if (_currentISOIndex > 0)
                        {
                            DecreaseISO();
                        }
                    }
                }
                else
                {
                    // Looking at something normal/dark - increase exposure
                    if (_currentISOIndex < isoStops.Length - 1 || _currentShutterSpeedIndex < shutterSpeedStops.Length - 1 || _currentApertureIndex > 0)
                    {
                        // First try to decrease f-stop (larger aperture)
                        if (_currentApertureIndex > 0)
                        {
                            DecreaseAperture();
                        }
                        // Then try slower shutter
                        else if (_currentShutterSpeedIndex < shutterSpeedStops.Length - 1)
                        {
                            IncreaseShutterSpeed();
                        }
                        // Finally increase ISO (last resort as it adds noise)
                        else if (_currentISOIndex < isoStops.Length - 1)
                        {
                            IncreaseISO();
                        }
                    }
                }
            }
        }
    }
}