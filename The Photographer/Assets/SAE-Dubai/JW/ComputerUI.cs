using SAE_Dubai.Leonardo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SAE_Dubai.JW
{
    /// <summary>
    /// Manages the player's interaction with a computer interface.
    /// Controls camera switching, mouse behavior, and UI navigation.
    /// </summary>
    public class ComputerUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("- Camera Settings")]
        [SerializeField] private MouseController mouseController;
        [SerializeField] private Camera computerCamera;
        [SerializeField] private Camera playerCamera;
        
        [Header("- Interaction Settings")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionLayer;
        [SerializeField] private GameObject interactionText;
        [SerializeField] private float interactionCooldown = 0.5f;
 
        [Header("- UI Elements")]
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private GameObject photoSessionsPanel;
        [SerializeField] private GameObject cameraShopPanel;
    
        [Header("- Navigation")]
        [SerializeField] private Button homeTabButton;
        [SerializeField] private Button photoSessionsTabButton;
        [SerializeField] private Button cameraShopTabButton;

        #endregion

        #region Private Variables

        private bool _canInteract;
        private float _lastInteractionTime;
        private bool _isTransitioning;
        private bool _wasKeyPressed;
        private bool _isUsingComputer = false;
        private TabType _currentActiveTab = TabType.Home;

        #endregion

        #region Unity Lifecycle Methods
        
        private void Start()
        {
            InitializeUI();
            InitializeState();
        }
    
        private void Update()
        {
            CheckForInteraction();
            HandleInput();
            UpdateBalanceDisplay();
        }

        #endregion

        #region Initialization Methods
        
        private void InitializeUI()
        {
            // Set up tab navigation.
            if (homeTabButton != null)
                homeTabButton.onClick.AddListener(() => SwitchTab(TabType.Home));
            
            if (photoSessionsTabButton != null)
                photoSessionsTabButton.onClick.AddListener(() => SwitchTab(TabType.PhotoSessions));
            
            if (cameraShopTabButton != null)
                cameraShopTabButton.onClick.AddListener(() => SwitchTab(TabType.CameraShop));
            
            // Show home tab by default.
            SwitchTab(TabType.Home);
        }

        private void InitializeState()
        {
            // Make sure interaction text is disabled at start.
            if (interactionText != null)
                interactionText.SetActive(false);

            // Make sure computer view is disabled at start.
            if (computerCamera != null)
                computerCamera.enabled = false;
                
            // Initialize the interaction time to allow immediate first use.
            _lastInteractionTime = -interactionCooldown;
            
            // Initialize state flags.
            _isTransitioning = false;
            _wasKeyPressed = false;
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Prevent actions during transitions.
            if (_isTransitioning)
                return;
                
            // Get cooldown state.
            bool cooledDown = Time.time - _lastInteractionTime >= interactionCooldown;
            if (!cooledDown)
                return;
                
            // Detect key press (not held down).
            bool keyDown = Input.GetKey(interactKey);
            bool keyPressed = keyDown && !_wasKeyPressed;
            _wasKeyPressed = keyDown;
            
            // Handle opening computer.
            if (_canInteract && keyPressed && !computerCamera.enabled)
            {
                _isTransitioning = true;
                StartCoroutine(TransitionToComputerView());
                _lastInteractionTime = Time.time;
            }
            
            // Handle exiting computer.
            if (computerCamera.enabled && (keyPressed || Input.GetKeyDown(KeyCode.Escape)))
            {
                _isTransitioning = true;
                StartCoroutine(TransitionToPlayerView());
                _lastInteractionTime = Time.time;
            }
        }

        private void CheckForInteraction()
        {
            // Skip if already using computer.
            if (computerCamera != null && computerCamera.enabled)
                return;

            _canInteract = false;
                
            // Make sure a camera is available.
            Camera rayCamera = playerCamera != null ? playerCamera : Camera.main;
            if (rayCamera == null)
                return;
                
            // Cast a ray from the camera center.
            Ray ray = rayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            // Use the interaction layer mask for the raycast.
            if (Physics.Raycast(ray, out hit, interactionRange, interactionLayer))
            {
                // Check if hit object is this computer.
                if (hit.collider.gameObject == gameObject)
                {
                    _canInteract = true;
                    
                    // Show interaction prompt.
                    if (interactionText != null)
                    {
                        interactionText.SetActive(true);
                    }
                }
                else
                {
                    if (interactionText != null)
                        interactionText.SetActive(false);
                }
            }
            else
            {
                if (interactionText != null)
                    interactionText.SetActive(false);
            }
        }

        #endregion

        #region View Transitions

        private IEnumerator TransitionToComputerView()
        {
            // Small delay to ensure no double toggles occur.
            yield return new WaitForEndOfFrame();
            
            // Entering computer view.
            computerCamera.enabled = true;
            playerCamera.enabled = false;
            
            if (mouseController != null)
                mouseController.EnableFreeMouse();
        
            // Hide interaction prompt.
            if (interactionText != null)
                interactionText.SetActive(false);
                
            // Allow input again after short delay.
            yield return new WaitForSeconds(0.1f);
            _isTransitioning = false;
        }
        
        private IEnumerator TransitionToPlayerView()
        {
            // Small delay to ensure no double toggles occur.
            yield return new WaitForEndOfFrame();
            
            // Exiting computer view.
            computerCamera.enabled = false;
            playerCamera.enabled = true;
            
            if (mouseController != null)
                mouseController.DisableFreeMouse();
            
            // Allow input again after short delay.
            yield return new WaitForSeconds(0.1f);
            _isTransitioning = false;
            
            // Double check if still looking at the computer.
            CheckForInteraction();
        }
        
        #endregion

        #region UI Management

        // ! THIS METHOD IS TECHNICALLY ONLY FOR BACKWARDS COMPATIBILITY WITH OTHER SCRIPTS.
        /// <summary>
        /// Public method for backward compatibility with other scripts that call ToggleComputerVision
        /// </summary>
        public void ToggleComputerVision()
        {
            if (_isTransitioning)
                return;
        
            // Check if we need to exit or enter.
            if (computerCamera != null && computerCamera.enabled)
            {
                // We're in computer view, so exit.
                _isTransitioning = true;
                StartCoroutine(TransitionToPlayerView());
            }
            else
            {
                // We're not in computer view, try to enter.
                // Only enter if player is looking at the computer or if called externally.
                // ? The frameCount check allows external calls to work.
                if (_canInteract || Time.frameCount > 10) 
                {
                    _isTransitioning = true;
                    StartCoroutine(TransitionToComputerView());
                }
            }
        }
        
        private void UpdateBalanceDisplay()
        {
            if (balanceText != null && PlayerBalance.Instance != null)
            {
                balanceText.text = $"Balance: ${PlayerBalance.Instance.Balance}";
            }
        }

        public enum TabType
        {
            Home,
            CameraShop,
            PhotoSessions
        }

        private void SwitchTab(TabType tabType)
        {
            // Hide all panels first.
            if (photoSessionsPanel != null)
                photoSessionsPanel.SetActive(false);
    
            if (cameraShopPanel != null)
                cameraShopPanel.SetActive(false);

            // Track which tab is active.
            _currentActiveTab = tabType;
    
            // Show the selected panel.
            switch (tabType)
            {
                case TabType.PhotoSessions:
                    if (photoSessionsPanel != null)
                        photoSessionsPanel.SetActive(true);
                    break;
        
                case TabType.CameraShop:
                    if (cameraShopPanel != null)
                        cameraShopPanel.SetActive(true);
                    break;
        
                // ! Home tab (default) doesn't have a dedicated panel.
            }
        }

        #endregion

        #region Tutorial Methods
        
        /// <summary>
        /// Returns whether the player is currently using the computer.
        /// Used by the tutorial system to track progress.
        /// </summary>
        public bool IsPlayerUsingComputer()
        {
            return computerCamera != null && computerCamera.enabled;
        }

        /// <summary>
        /// Returns whether the Camera Shop tab is currently active.
        /// Used by the tutorial system to track progress.
        /// </summary>
        public bool IsShopTabActive()
        {
            return IsPlayerUsingComputer() && _currentActiveTab == TabType.CameraShop;
        }

        /// <summary>
        /// Returns whether the Photo Sessions tab is currently active.
        /// Used by the tutorial system to track progress.
        /// </summary>
        public bool IsSessionsTabActive()
        {
            return IsPlayerUsingComputer() && _currentActiveTab == TabType.PhotoSessions;
        }

        #endregion
    }
}