using System;
using SAE_Dubai.Leonardo.CameraSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.JW.UI
{
    public class CameraButton : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] CameraSettings cameraSettings;

        [Header("Item Shop")] 
        [SerializeField] private GameObject item;
        [SerializeField] private float price = 100f;
        
        [Header("UI")]
        [SerializeField] CameraInfoPanel _cameraInfoPanel;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _price;

        private void Start()
        {
            _name.text = cameraSettings.modelName;
            _price.text = $"Price: ${price}";
        }

        public void SelectCamera()
        {
            if (_cameraInfoPanel != null)
            {
                if (!_cameraInfoPanel.isActiveAndEnabled)
                {
                    _cameraInfoPanel.gameObject.SetActive(true);
                }
            }
            _cameraInfoPanel.SetCameraInfo(cameraSettings, price, item);
        }
    }
}