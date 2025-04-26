using SAE_Dubai.Leonardo.CameraSys;
using SAE_Dubai.Leonardo.Items.PickUpables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.JW.UI
{
    /// <summary>
    /// Leo: Manages the camera shop functionality, including purchasing cameras
    /// and spawning them in the world. Took some methods taken from JW's previous scripts and put them here.
    /// </summary>
    public class CameraShopManager : MonoBehaviour
    {
        [Header("- Shop Settings")]
        [SerializeField] private Transform cameraSpawnPoint;
        [SerializeField] private LayerMask pickupableLayer;
        
        [Header("- UI Elements")]
        [SerializeField] private GameObject purchaseConfirmationPanel;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private float feedbackDisplayTime = 3f;
        
        private GameObject _currentCameraItem;
        private float _currentPrice;
        private CameraSettings _currentCameraSettings;
        private bool _isPurchaseInProgress;
        private float _feedbackTimer;
        
        public static CameraShopManager Instance { get; private set; }
        
        private void Awake()
        {
            // ! Singleton.
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Hide UI elements on start.
            if (purchaseConfirmationPanel != null)
                purchaseConfirmationPanel.SetActive(false);
            
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
            
            // Setup UI button listeners.
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmPurchase);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelPurchase);
        }
        
        private void Update()
        {
            if (feedbackText != null && feedbackText.gameObject.activeSelf)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0)
                {
                    feedbackText.gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Initiates the purchase process for a camera.
        /// </summary>
        /// <param name="cameraItem">The camera item prefab to purchase.</param>
        /// <param name="price">The price of the camera.</param>
        /// <param name="cameraSettings">Camera settings reference.</param>
        public void StartPurchase(GameObject cameraItem, float price, CameraSettings cameraSettings)
        {
            _currentCameraItem = cameraItem;
            _currentPrice = price;
            _currentCameraSettings = cameraSettings;
            _isPurchaseInProgress = true;
            
            if (purchaseConfirmationPanel != null)
            {
                purchaseConfirmationPanel.SetActive(true);
                
                if (confirmationText != null)
                {
                    confirmationText.text = $"Purchase {cameraSettings.modelName} for ${price}?";
                }
            }
        }
        
        private void ConfirmPurchase()
        {
            if (!_isPurchaseInProgress || _currentCameraItem == null || _currentCameraSettings == null)
            {
                ShowFeedback("Error: No camera selected for purchase", Color.red);
                return;
            }
            
            // Check if player has enough money.
            if (PlayerBalance.Instance != null && PlayerBalance.Instance.HasSufficientBalance((int)_currentPrice))
            {
                // Take from balance.
                PlayerBalance.Instance.DeductBalance((int)_currentPrice);
                
                // Spawn the camera item.
                SpawnPurchasedCamera();
                
                // Show success message.
                ShowFeedback($"Successfully purchased {_currentCameraSettings.modelName}!", Color.green);
            }
            else
            {
                // Show insufficient funds message.
                ShowFeedback("Insufficient funds to purchase this camera", Color.red);
            }
            
            // Close confirmation panel.
            if (purchaseConfirmationPanel != null)
                purchaseConfirmationPanel.SetActive(false);
            
            // Reset purchase state.
            ResetPurchaseState();
        }
        
        private void CancelPurchase()
        {
            // Hide confirmation panel.
            if (purchaseConfirmationPanel != null)
                purchaseConfirmationPanel.SetActive(false);
            
            // Reset purchase state.
            ResetPurchaseState();
        }
        
        private void ResetPurchaseState()
        {
            _currentCameraItem = null;
            _currentCameraSettings = null;
            _isPurchaseInProgress = false;
        }
        
        private void SpawnPurchasedCamera()
        {
            if (_currentCameraItem == null || cameraSpawnPoint == null)
                return;
            
            //! Determine spawn location (either at the specified point or a fallback method).
            Vector3 spawnPosition = cameraSpawnPoint != null ? 
                cameraSpawnPoint.position : 
                DetermineSpawnPosition();
                
            // Instantiate the camera object.
            GameObject cameraObject = Instantiate(_currentCameraItem, spawnPosition, Quaternion.identity);
            
            // Ensure it's on the pickupable layer if specified.
            if (pickupableLayer != 0)
            {
                int layerIndex = GetFirstLayerFromMask(pickupableLayer);
                if (layerIndex >= 0)
                {
                    cameraObject.layer = layerIndex;
                }
            }
            
            // Set up any pickupable component if needed
            PickupableCamera pickupCamera = cameraObject.GetComponent<PickupableCamera>();
            if (pickupCamera != null && _currentCameraSettings != null)
            {
                pickupCamera.cameraSettings = _currentCameraSettings;
            }
            
            Debug.Log($"CameraShopManager: Spawned {_currentCameraSettings.modelName} at {spawnPosition}");
        }
        
        private Vector3 DetermineSpawnPosition()
        {
            // Fallback method to find a reasonable spawn position if no specific point is set
            // This is just an example - adjust as needed for your game
            
            // Try to find the player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Spawn in front of the player
                return player.transform.position + player.transform.forward * 2f + Vector3.up * 0.5f;
            }
            
            // If all else fails, use scene origin with slight elevation
            return new Vector3(0, 0.5f, 0);
        }
        
        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                feedbackText.gameObject.SetActive(true);
                _feedbackTimer = feedbackDisplayTime;
            }
            
            Debug.Log($"CameraShopManager: {message}");
        }
        
        private int GetFirstLayerFromMask(LayerMask layerMask)
        {
            // Helper to get the first layer index from a layer mask
            int bitmask = layerMask.value;
            
            for (int i = 0; i < 32; i++)
            {
                if (((1 << i) & bitmask) != 0)
                {
                    return i;
                }
            }
            
            return -1;
        }
    }
}