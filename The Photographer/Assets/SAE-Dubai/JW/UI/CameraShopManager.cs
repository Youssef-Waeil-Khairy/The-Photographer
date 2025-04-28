using System.Collections;
using SAE_Dubai.JW;
using SAE_Dubai.Leonardo;
using SAE_Dubai.Leonardo.CameraSys;
using SAE_Dubai.Leonardo.Items;
using SAE_Dubai.Leonardo.Items.PickUpables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SAE_Dubai.JW.UI
{
    /// <summary>
    /// Manages the camera shop functionality, including camera purchases and spawning.
    /// This class follows the Singleton pattern for easy access from UI components.
    /// </summary>
    public class CameraShopManager : MonoBehaviour
    {
        #region Singleton Setup
        [FoldoutGroup("Singleton Setup", false)]
        [ShowInInspector, ReadOnly]
        public static CameraShopManager Instance { get; private set; }
        #endregion

        #region Shop Settings
        [BoxGroup("Shop Settings")]
        [Required("Camera spawn point is required for spawning purchased cameras")]
        [Tooltip("Where purchased cameras will be spawned")]
        public Transform cameraSpawnPoint;
        
        [BoxGroup("Shop Settings")]
        [Tooltip("The layer to assign to pickupable cameras")]
        public LayerMask pickupableLayer;
        
        [BoxGroup("Shop Settings")]
        [Tooltip("The shop camera populator to manage after purchases")]
        [SerializeField] private ShopCameraPopulator shopPopulator;
        
        [BoxGroup("Shop Settings/Debugging")]
        [Tooltip("Set to true to see debug messages in the console")]
        [SerializeField] private bool showDebugMessages = false;
        #endregion

        #region UI Elements
        [FoldoutGroup("UI Elements")]
        [Required("Required for purchase confirmation")]
        [Tooltip("Panel shown to confirm purchase")]
        public GameObject purchaseConfirmationPanel;
        
        [FoldoutGroup("UI Elements")]
        [Required("Required for purchase confirmation text")]
        [Tooltip("Text element showing purchase details")]
        public TextMeshProUGUI confirmationText;
        
        [FoldoutGroup("UI Elements")]
        [Required("Required for confirming purchase")]
        [Tooltip("Button to confirm purchase")]
        public Button confirmButton;
        
        [FoldoutGroup("UI Elements")]
        [Required("Required for canceling purchase")]
        [Tooltip("Button to cancel purchase")]
        public Button cancelButton;
        
        [FoldoutGroup("UI Elements")]
        [Required("Required for purchase feedback")]
        [Tooltip("Text showing feedback after purchase")]
        public TextMeshProUGUI feedbackText;
        
        [FoldoutGroup("UI Elements")]
        [MinValue(0.5f)]
        [Tooltip("How long feedback is displayed in seconds")]
        public float feedbackDisplayTime = 3f;
        #endregion

        #region Private Fields
        [FoldoutGroup("Debug Information", false)]
        [ReadOnly]
        [ShowInInspector]
        private GameObject cameraButtonGO; // GameObject of the selected camera button
        
        [FoldoutGroup("Debug Information", false)]
        [ReadOnly]
        [ShowInInspector]
        private GameObject _currentCameraPrefab;
        
        [FoldoutGroup("Debug Information", false)]
        [ReadOnly]
        [ShowInInspector]
        private float _currentCameraPrice;
        
        [FoldoutGroup("Debug Information", false)]
        [ReadOnly]
        [ShowInInspector]
        private CameraSettings _currentCameraSettings;
        
        [FoldoutGroup("Debug Information", false)]
        [ReadOnly]
        [ShowInInspector]
        private bool _isPurchaseInProgress;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Singleton pattern implementation
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
            InitializeShop();
        }
        #endregion

        #region Shop Initialization
        [Button("Initialize Shop")]
        private void InitializeShop()
        {
            // Hook up button listeners
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmPurchase);
                if (showDebugMessages) Debug.Log("CameraShopManager: Confirm button listener added");
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(CancelPurchase);
                if (showDebugMessages) Debug.Log("CameraShopManager: Cancel button listener added");
            }
            
            // Hide UI elements
            if (purchaseConfirmationPanel != null)
            {
                purchaseConfirmationPanel.SetActive(false);
                if (showDebugMessages) Debug.Log("CameraShopManager: Purchase confirmation panel hidden");
            }
            
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
                if (showDebugMessages) Debug.Log("CameraShopManager: Feedback text hidden");
            }
            
            // Validate references
            if (cameraSpawnPoint == null)
            {
                Debug.LogWarning("CameraShopManager: No camera spawn point assigned, using this transform instead.");
                cameraSpawnPoint = transform;
            }
        }
        #endregion

        #region Purchase Management
        /// <summary>
        /// Starts the purchase process for a camera.
        /// </summary>
        /// <param name="cameraPrefab">The camera prefab to instantiate.</param>
        /// <param name="price">The price of the camera.</param>
        /// <param name="settings">The camera's settings.</param>
        /// <param name="cameraButtonGO">The button GameObject that initiated the purchase.</param>
        public void StartPurchase(GameObject cameraPrefab, float price, CameraSettings settings, GameObject cameraButtonGO)
        {
            if (_isPurchaseInProgress)
            {
                if (showDebugMessages) Debug.Log("CameraShopManager: Purchase already in progress, ignoring request");
                return;
            }
        
            if (settings == null)
            {
                ShowFeedback("Error: Invalid camera settings", Color.red);
                if (showDebugMessages) Debug.LogError("CameraShopManager: Null camera settings in StartPurchase");
                return;
            }
        
            // Store the purchase details - use the prefab from settings instead of the passed parameter
            _currentCameraPrefab = settings.cameraPrefab;
            _currentCameraPrice = price;
            _currentCameraSettings = settings;
            _isPurchaseInProgress = true;
            this.cameraButtonGO = cameraButtonGO;
            
            if (showDebugMessages) Debug.Log($"CameraShopManager: Starting purchase for {settings.modelName} at ${price}");
    
            // Show confirmation dialog if available
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
                // If no confirmation panel, proceed directly
                if (showDebugMessages) Debug.Log("CameraShopManager: No confirmation panel, proceeding directly with purchase");
                ConfirmPurchase();
            }
        }

        /// <summary>
        /// Cancels the current purchase.
        /// </summary>
        [Button("Cancel Current Purchase")]
        public void CancelPurchase()
        {
            if (purchaseConfirmationPanel != null)
                purchaseConfirmationPanel.SetActive(false);
                
            _isPurchaseInProgress = false;
            ShowFeedback("Purchase canceled.", Color.yellow);
            
            if (showDebugMessages) Debug.Log("CameraShopManager: Purchase canceled");
        }

        /// <summary>
        /// Finalizes the purchase, deducts money and spawns the camera.
        /// </summary>
        [Button("Confirm Current Purchase")]
        public void ConfirmPurchase()
        {
            // Check for valid purchase
            if (_currentCameraPrefab == null || _currentCameraSettings == null)
            {
                ShowFeedback("Purchase failed: Invalid camera data", Color.red);
                if (showDebugMessages) Debug.LogError("CameraShopManager: Invalid camera data in ConfirmPurchase");
                return;
            }
            
            // Check player balance
            if (PlayerBalance.Instance == null)
            {
                Debug.LogError("Cannot complete purchase: PlayerBalance not found.");
                ShowFeedback("Purchase failed: System error", Color.red);
                return;
            }
            
            // Check if player can afford it
            if (!PlayerBalance.Instance.HasSufficientBalance((int)_currentCameraPrice))
            {
                ShowFeedback("Insufficient funds!", Color.red);
                CancelPurchase();
                return;
            }
            
            if (showDebugMessages) Debug.Log($"CameraShopManager: Processing payment of ${_currentCameraPrice}");
            
            // Process payment
            PlayerBalance.Instance.DeductBalance((int)_currentCameraPrice);
            
            // Spawn the camera as a pickupable item
            SpawnCamera();
            
            // Notify TutorialManager if it exists
            TutorialManager tutorialManager = FindFirstObjectByType<TutorialManager>();
            if (tutorialManager != null)
            {
                tutorialManager.SetCameraBoughtFlag();
                if (showDebugMessages) Debug.Log("CameraShopManager: Notified TutorialManager of camera purchase");
            }
            
            // Hide confirmation panel
            if (purchaseConfirmationPanel != null)
            {
                purchaseConfirmationPanel.SetActive(false);
            }
                
            // Hide the button if we should disable it after purchase
            if (cameraButtonGO != null)
            {
                cameraButtonGO.SetActive(false); // Stops the camera from being purchased again
                if (showDebugMessages) Debug.Log($"CameraShopManager: Disabled button for {_currentCameraSettings.modelName}");
            }
            
            // Remove the camera from the shop populator if available
            if (shopPopulator != null)
            {
                shopPopulator.RemoveCamera(_currentCameraSettings);
                if (showDebugMessages) Debug.Log($"CameraShopManager: Removed {_currentCameraSettings.modelName} from shop populator");
            }
                
            ShowFeedback($"Purchased {_currentCameraSettings.modelName}!", Color.green);
            _isPurchaseInProgress = false;
        }
        #endregion

        #region Camera Spawning
        [Button("Test Spawn Current Camera")]
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
            
            if (showDebugMessages) Debug.Log($"CameraShopManager: Spawning camera {_currentCameraSettings.modelName}");
        
            // Instantiate the camera at the spawn point
            GameObject newCamera = Instantiate(_currentCameraSettings.cameraPrefab, cameraSpawnPoint.position, cameraSpawnPoint.rotation);
    
            // Make sure the GameObject is active
            newCamera.SetActive(true);
    
            // Set the layer to Pickupable (dummy proofing)
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
            
            if (showDebugMessages)
            {
                Debug.Log($"Spawned camera: {_currentCameraSettings.modelName}" +
                          $"\nAt position: {cameraSpawnPoint.position}" +
                          $"\nParent object: {cameraSpawnPoint.gameObject.name}" +
                          $"\nCamera active: {newCamera.activeSelf}" +
                          $"\nCamera layer: {LayerMask.LayerToName(newCamera.layer)}");
            }
        }
        #endregion

        #region Feedback System
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
            
            // Hide after delay
            StartCoroutine(HideFeedback());
        }

        private IEnumerator HideFeedback()
        {
            yield return new WaitForSeconds(feedbackDisplayTime);
            
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }
        #endregion
    }
}