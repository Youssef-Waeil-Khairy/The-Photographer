using SAE_Dubai.JW;
using SAE_Dubai.Leonardo.CameraSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.JW.UI
{
    public class CameraInfoPanel : MonoBehaviour
    {
        [Header("- Camera Item")]
        public CameraSettings CamSettings;

        public float CameraPrice = 100f;
        public GameObject CameraItemPrefab;

        [Header("- Text Fields")]
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

        [Header("- Purchase Controls")]
        [SerializeField] private Button purchaseButton;

        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button closeButton;

        private void Start() {
            // Setup the purchase button.
            if (purchaseButton != null) {
                purchaseButton.onClick.AddListener(PurchaseCamera);
            }

            // Setup close button if available.
            if (closeButton != null) {
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }
        }

        public void SetCameraInfo(CameraSettings cameraSettings, float cameraPrice, GameObject cameraItemPrefab) {
            CamSettings = cameraSettings;
            CameraPrice = cameraPrice;
            CameraItemPrefab = cameraItemPrefab;

            ShowCameraInfo();
            UpdatePurchaseUI();
        }

        private void ShowCameraInfo() {
            cameraName.text = CamSettings.modelName;
            cameraManufacturer.text = CamSettings.manufacturer;

            cameraAutoISO.text = CamSettings.hasAutoISO ? "Auto ISO" : "Manual ISO";
            cameraISO.text = $"Base ISO: {CamSettings.baseISO} | Max ISO: {CamSettings.maxISO}";

            cameraHasZoom.text = CamSettings.hasZoom ? "Zoom Enabled" : "Zoom Disabled";
            cameraAutoFocus.text = CamSettings.hasAutoFocus ? "Auto Focus" : "Manual Focus";
            cameraAperture.text = $"Aperture: {CamSettings.minAperture} -> {CamSettings.maxAperture}";

            cameraAutoShutterSpeed.text =
                CamSettings.hasAutoShutterSpeed ? "Auto Shutter Speed" : "Manual Shutter Speed";
            cameraShutterSpeed.text = $"Shutter Speed: {CamSettings.minShutterSpeed} -> {CamSettings.maxShutterSpeed}";

            cameraSensor.text = $"Sensor: {CamSettings.sensorType.ToString()}";
        }

        private void UpdatePurchaseUI() {
            // Update price text.
            if (priceText != null) {
                priceText.text = $"Price: ${CameraPrice}";
            }

            // Enable/disable purchase button based on available funds.
            if (purchaseButton != null && PlayerBalance.Instance != null) {
                bool canAfford = PlayerBalance.Instance.HasSufficientBalance((int)CameraPrice);
                purchaseButton.interactable = canAfford;

                // Optionally change button text based on affordability.
                TMP_Text buttonText = purchaseButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null) {
                    buttonText.text = canAfford ? "Purchase" : "Cannot Afford";
                }
            }
        }

        private void PurchaseCamera() {
            if (CameraItemPrefab == null || CamSettings == null) {
                Debug.LogError("Cannot purchase: Missing camera prefab or settings.");
                return;
            }

            // Use the CameraShopManager to handle the purchase.
            if (CameraShopManager.Instance != null) {
                CameraShopManager.Instance.StartPurchase(CameraItemPrefab, CameraPrice, CamSettings);

                // Close the info panel after starting purchase.
                gameObject.SetActive(false);
            }
            else {
                Debug.LogError("CameraShopManager not found in scene. Make sure it's created.");
            }
        }

        private void OnEnable() {
            // Refresh purchase UI each time panel is shown.
            if (CamSettings != null) {
                UpdatePurchaseUI();
            }
        }
    }
}