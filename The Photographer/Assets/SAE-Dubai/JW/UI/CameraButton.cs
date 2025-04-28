using System;
using SAE_Dubai.Leonardo.CameraSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SAE_Dubai.JW.UI
{
    [HelpURL("https://youtu.be/dQw4w9WgXcQ")]
    [AddComponentMenu("UI/Camera Shop/Camera Button")]
    [RequireComponent(typeof(Button))]
    public class CameraButton : MonoBehaviour
    {
        [BoxGroup("Camera Data")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [Tooltip("Reference to the CameraSettings ScriptableObject for this camera")]
        [SerializeField] public CameraSettings cameraSettings;

        [BoxGroup("Item Shop")] 
        [Tooltip("The prefab to spawn when this camera is purchased (defaults to camera prefab if not set)")]
        [SerializeField] private GameObject itemPrefab;
        
        [FoldoutGroup("UI References")]
        [Required]
        [Tooltip("Reference to the info panel that displays detailed camera information")]
        [SerializeField] private CameraInfoPanel _cameraInfoPanel;
        
        [FoldoutGroup("UI References")]
        [Required]
        [Tooltip("Text component for displaying the camera name")]
        [SerializeField] private TMP_Text _nameText;
        
        [FoldoutGroup("UI References")]
        [Required]
        [Tooltip("Text component for displaying the camera price")]
        [SerializeField] private TMP_Text _priceText;
        
        [FoldoutGroup("UI References")]
        [Tooltip("Image component for displaying the camera icon")]
        [SerializeField] private Image _cameraImage;
        
        [FoldoutGroup("UI References/Optional")]
        [Tooltip("Additional text component for showing camera manufacturer")]
        [SerializeField] private TMP_Text _manufacturerText;
        
        [FoldoutGroup("UI References/Optional")]
        [Tooltip("Text component for showing a brief description")]
        [SerializeField] private TMP_Text _descriptionText;
        
        // Reference to the button component
        private Button _button;

        [ShowInInspector, ReadOnly]
        [BoxGroup("Debug", false)]
        private bool _isInitialized = false;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(SelectCamera);
            }
            
            // If camera settings were assigned in the editor, initialize the UI
            if (cameraSettings != null && !_isInitialized)
            {
                UpdateUIFromSettings();
                _isInitialized = true;
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(SelectCamera);
            }
        }

        [Button("Update UI From Settings")]
        private void UpdateUIFromSettings()
        {
            if (cameraSettings == null) return;
            
            // Update name
            if (_nameText != null)
            {
                _nameText.text = cameraSettings.modelName;
            }
            
            // Update price
            if (_priceText != null)
            {
                _priceText.text = $"Price: ${cameraSettings.price}";
            }
            
            // Update icon
            if (_cameraImage != null && cameraSettings.cameraIcon != null)
            {
                _cameraImage.sprite = cameraSettings.cameraIcon;
                _cameraImage.preserveAspect = true;
            }
            
            // Update manufacturer
            if (_manufacturerText != null)
            {
                _manufacturerText.text = cameraSettings.manufacturer;
            }
            
            // Update description
            if (_descriptionText != null)
            {
                if (!string.IsNullOrEmpty(cameraSettings.description))
                {
                    _descriptionText.gameObject.SetActive(true);
                    _descriptionText.text = cameraSettings.description;
                }
                else
                {
                    _descriptionText.gameObject.SetActive(false);
                }
            }
        }

        public void Initialize(CameraSettings settings, CameraInfoPanel infoPanel)
        {
            cameraSettings = settings;
            _cameraInfoPanel = infoPanel;
            
            if (cameraSettings != null)
            {
                UpdateUIFromSettings();
                _isInitialized = true;
            }
        }

        [Button("Preview In Info Panel")]
        public void SelectCamera()
        {
            if (_cameraInfoPanel != null && cameraSettings != null)
            {
                if (!_cameraInfoPanel.isActiveAndEnabled)
                {
                    _cameraInfoPanel.gameObject.SetActive(true);
                }
                
                GameObject itemToUse = itemPrefab;
                if (itemToUse == null && cameraSettings.cameraPrefab != null)
                {
                    itemToUse = cameraSettings.cameraPrefab;
                }
                
                _cameraInfoPanel.SetCameraInfo(cameraSettings, cameraSettings.price, itemToUse, gameObject);
            }
            else
            {
                if (_cameraInfoPanel == null)
                {
                    Debug.LogWarning("CameraButton: No info panel assigned. Cannot show camera details.", this);
                }
                
                if (cameraSettings == null)
                {
                    Debug.LogWarning("CameraButton: No camera settings assigned. Cannot show camera details.", this);
                }
            }
        }
    }
}