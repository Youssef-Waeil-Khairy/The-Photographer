using SAE_Dubai.Leonardo.CameraSys;
using TMPro;
using UnityEngine;

namespace SAE_Dubai.JW.UI
{
    public class CameraInfoPanel : MonoBehaviour
    {
        [Header("Camera Item")]
        public CameraSettings CamSettings;

        [Header("Text Fields")] 
        [SerializeField] private TMP_Text cameraName;
        [SerializeField] private TMP_Text cameraManufacturer;
        [SerializeField] private TMP_Text cameraAutoISO;
        [SerializeField] private TMP_Text cameraISO;
        [SerializeField] private TMP_Text cameraAutoWB;
        [SerializeField] private TMP_Text cameraAutoFocus;
        [SerializeField] private TMP_Text cameraAperture;
        [SerializeField] private TMP_Text cameraAutoShutterSpeed;
        [SerializeField] private TMP_Text cameraShutterSpeed;
        [SerializeField] private TMP_Text cameraHasZoom;
        [SerializeField] private TMP_Text cameraSensor;

        public void SetCameraInfo(CameraSettings cameraSettings)
        {
            CamSettings = cameraSettings;
            
            ShowCameraInfo();
        }

        private void ShowCameraInfo()
        {
            cameraName.text = CamSettings.modelName;
            cameraManufacturer.text = CamSettings.manufacturer;
            
            cameraAutoISO.text = CamSettings.hasAutoISO ? "Auto ISO" : "Manual ISO";
            cameraISO.text = $"Base ISO: {CamSettings.baseISO} | Max ISO: {CamSettings.maxISO}";
            
            cameraHasZoom.text = CamSettings.hasZoom ? "Zoom Enabled" : "Zoom Disabled";
            cameraAutoFocus.text = CamSettings.hasAutoFocus ? "Auto Focus" : "Manual Focus";
            cameraAperture.text = $"Aperture: {CamSettings.minAperture} -> {CamSettings.maxAperture}";
            
            cameraAutoShutterSpeed.text = CamSettings.hasAutoShutterSpeed ? "Auto Shutter Speed" : "Manual Shutter Speed";
            cameraShutterSpeed.text = $"Shutter Speed: {CamSettings.minShutterSpeed} -> {CamSettings.maxShutterSpeed}";
            
        }
    }
}