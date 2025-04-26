using System.Collections;
using SAE_Dubai.JW;
using SAE_Dubai.Leonardo.CameraSys;
using SAE_Dubai.Leonardo.Items;
using SAE_Dubai.Leonardo.Items.PickUpables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.JW.UI
{
    /// <summary>
    /// Manages the camera shop functionality, including camera purchases and spawning.
    /// This class follows the Singleton pattern for easy access from UI components.
    /// </summary>
    public class CameraShopManager : MonoBehaviour
    {
        public static CameraShopManager Instance { get; private set; }

        [Header("- Shop Settings")]
        [Tooltip("Where purchased cameras will be spawned")]
        public Transform cameraSpawnPoint;
        [Tooltip("The layer to assign to pickupable cameras")]
        public LayerMask pickupableLayer;

        [Header("- UI Elements")]
        [Tooltip("Panel shown to confirm purchase")]
        public GameObject purchaseConfirmationPanel;
        [Tooltip("Text element showing purchase details")]
        public TextMeshProUGUI confirmationText;
        [Tooltip("Button to confirm purchase")]
        public Button confirmButton;
        [Tooltip("Button to cancel purchase")]
        public Button cancelButton;
        [Tooltip("Text showing feedback after purchase")]
        public TextMeshProUGUI feedbackText;
        [Tooltip("How long feedback is displayed")]
        public float feedbackDisplayTime = 3f;

        private GameObject _currentCameraPrefab;
        private float _currentCameraPrice;
        private CameraSettings _currentCameraSettings;
        private bool _isPurchaseInProgress;

        private void Awake()
        {
            // !Singleton pattern setup.
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Hook up button listeners.
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmPurchase);
                
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelPurchase);
                
            // Hide UI elements.
            if (purchaseConfirmationPanel != null)
                purchaseConfirmationPanel.SetActive(false);
                
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
                
            // Validate references.
            if (cameraSpawnPoint == null)
            {
                Debug.LogWarning("CameraShopManager: No camera spawn point assigned, using this transform instead.");
                cameraSpawnPoint = transform;
            }
        }

        /// <summary>
        /// Starts the purchase process for a camera.
        /// </summary>
        /// <param name="cameraPrefab">The camera prefab to instantiate.</param>
        /// <param name="price">The price of the camera.</param>
        /// <param name="settings">The camera's settings.</param>
        public void StartPurchase(GameObject cameraPrefab, float price, CameraSettings settings)
        {
            if (_isPurchaseInProgress)
                return;
        
            // Store the purchase details - use the prefab from settings instead of the passed parameter.
            _currentCameraPrefab = settings.cameraPrefab;
            _currentCameraPrice = price;
            _currentCameraSettings = settings;
            _isPurchaseInProgress = true;
    
            // * Show confirmation dialog if available.
            if (purchaseConfirmationPanel != null)
            {
                purchaseConfirmationPanel.SetActive(true);
        
                if (confirmationText != null)
                {
                    confirmationText.text = $"Purchase {settings.modelName}\nPrice: ${price}\n\nAre you sure?";
                }
            }
            else
            {
                // If no confirmation panel, proceed directly.
                // ! I'm adding this just so we don't run into problems later in development.
                ConfirmPurchase();
            }
        }

        /// <summary>
        /// Cancels the current purchase.
        /// </summary>
        public void CancelPurchase()
        {
            if (purchaseConfirmationPanel != null)
                purchaseConfirmationPanel.SetActive(false);
                
            _isPurchaseInProgress = false;
            ShowFeedback("Purchase canceled.", Color.yellow);
        }

        /// <summary>
        /// Finalizes the purchase, deducts money and spawns the camera.
        /// </summary>
        public void ConfirmPurchase()
        {
            // Check for valid purchase.
            if (_currentCameraPrefab == null || _currentCameraSettings == null)
            {
                ShowFeedback("Purchase failed: Invalid camera data", Color.red);
                return;
            }
            
            // Check player balance.
            if (PlayerBalance.Instance == null)
            {
                Debug.LogError("Cannot complete purchase: PlayerBalance not found.");
                ShowFeedback("Purchase failed: System error", Color.red);
                return;
            }
            
            // Check if player can afford it.
            if (!PlayerBalance.Instance.HasSufficientBalance((int)_currentCameraPrice))
            {
                ShowFeedback("Insufficient funds!", Color.red);
                CancelPurchase();
                return;
            }
            
            // Process payment.
            PlayerBalance.Instance.DeductBalance((int)_currentCameraPrice);
            
            // Spawn the camera as a pickupable item.
            SpawnCamera();
            
            // Hide confirmation panel.
            if (purchaseConfirmationPanel != null)
                purchaseConfirmationPanel.SetActive(false);
                
            ShowFeedback($"Purchased {_currentCameraSettings.modelName}!", Color.green);
            _isPurchaseInProgress = false;
        }

        private void SpawnCamera()
        {
            if (_currentCameraSettings == null || cameraSpawnPoint == null)
            {
                Debug.LogError("Cannot spawn camera: missing settings or spawn point");
                return;
            }

            if (_currentCameraSettings.cameraPrefab == null)
            {
                Debug.LogError($"Cannot spawn camera: {_currentCameraSettings.modelName} has no prefab assigned!");
                return;
            }
        
            // Instantiate the camera at the spawn point.
            GameObject newCamera = Instantiate(_currentCameraSettings.cameraPrefab, cameraSpawnPoint.position, cameraSpawnPoint.rotation);
    
            // Make sure the GameObject is active.
            newCamera.SetActive(true);
    
            // ? Set the layer to Pickupable (dummy proofing)
            if (pickupableLayer.value > 0)
            {
                int layerIndex = (int)Mathf.Log(pickupableLayer.value, 2);
                newCamera.layer = layerIndex;
            }
    
            PickupableCamera pickupable = newCamera.GetComponent<PickupableCamera>();
    
            if (pickupable == null)
            {
                Debug.LogWarning($"Camera prefab {_currentCameraSettings.modelName} doesn't have a PickupableCamera component!");
            }
            else
            {
                pickupable.cameraSettings = _currentCameraSettings;
            }
    
            Debug.Log($"Spawned camera: {_currentCameraSettings.modelName}" +
                      $"\nAt position: {cameraSpawnPoint.position}" +
                      $"\nParent object: {cameraSpawnPoint.gameObject.name}" +
                      $"\nCamera active: {newCamera.activeSelf}" +
                      $"\nCamera layer: {LayerMask.LayerToName(newCamera.layer)}");
        }
        /// <summary>
        /// Shows feedback text for a specified duration.
        /// </summary>
        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText == null)
                return;
                
            feedbackText.text = message;
            feedbackText.color = color;
            feedbackText.gameObject.SetActive(true);
            
            // Hide after delay.
            StartCoroutine(HideFeedback());
        }

        private IEnumerator HideFeedback()
        {
            yield return new WaitForSeconds(feedbackDisplayTime);
            
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }
    }
}