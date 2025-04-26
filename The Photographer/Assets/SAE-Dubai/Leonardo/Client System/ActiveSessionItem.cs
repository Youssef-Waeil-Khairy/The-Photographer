using SAE_Dubai.JW;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.Leonardo.Client_System
{
    public class ActiveSessionItem : MonoBehaviour
    {
        [Header("- UI References")]
        public TMP_Text sessionInfoText;
        public Button travelButton;
        public Button cancelButton;
        public TMP_Text costText;
        public TMP_Text feedbackText;
        
        [Header("- UI Settings")]
        [SerializeField] private float feedbackDisplayTime = 2f;
        
        private PhotoSession linkedSession;
        private PhotoSessionManager sessionManager;
        private float _feedbackTimer;
        
        private void Start()
        {
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }
        
        private void Update()
        {
            // Handle feedback display timer
            if (feedbackText != null && feedbackText.gameObject.activeSelf)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0)
                {
                    feedbackText.gameObject.SetActive(false);
                }
            }
        }
        
        public void Initialize(PhotoSession session, PhotoSessionManager manager)
        {
            linkedSession = session;
            sessionManager = manager;
            
            // Setup button listeners
            if (travelButton != null)
                travelButton.onClick.AddListener(Travel);
                
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);
                
            // Update cost text if available
            if (costText != null && sessionManager != null)
                costText.text = $"Travel Cost: ${sessionManager.travelCost}";
            
            // Subscribe to travel payment events
            if (TravelCostManager.Instance != null)
                TravelCostManager.Instance.OnTravelPaymentProcessed += HandleTravelPaymentResult;
                
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (TravelCostManager.Instance != null)
                TravelCostManager.Instance.OnTravelPaymentProcessed -= HandleTravelPaymentResult;
        }
        
        public void UpdateUI()
        {
            if (linkedSession == null || sessionInfoText == null)
                return;
                
            // Status text
            string status = linkedSession.isClientSpawned ? 
                "Status: <color=green>Client Ready</color>" : 
                "Status: <color=yellow>Not Visited</color>";
            
            // Format info text
            sessionInfoText.text = $"<b>{linkedSession.clientName}</b>\n" +
                            $"Location: {linkedSession.GetLocationName()}\n" +
                            $"Shot Type: {linkedSession.GetShotTypeName()}\n" +
                            $"Reward: ${linkedSession.reward}\n" +
                            $"{status}";
                            
            // Update button states
            if (cancelButton != null)
                cancelButton.interactable = !linkedSession.isClientSpawned;
        }
        
        private void Travel()
        {
            if (sessionManager != null && linkedSession != null)
            {
                bool success = sessionManager.TravelToLocation(linkedSession.locationIndex);
                
                if (success)
                {
                    // Update UI after traveling
                    UpdateUI();
                    
                    // Close computer UI
                    FindObjectOfType<ComputerUI>()?.ToggleComputerVision();
                }
            }
        }
        
        private void Cancel()
        {
            if (sessionManager != null && linkedSession != null && !linkedSession.isClientSpawned)
            {
                // Only allow cancellation if client hasn't been spawned yet
                sessionManager.RemoveSession(linkedSession);
            }
        }
        
        /// <summary>
        /// Handles the feedback display when travel payment is processed
        /// </summary>
        private void HandleTravelPaymentResult(bool success, int cost, string message)
        {
            // Only display feedback for this session's travel attempts
            if (sessionManager != null && linkedSession != null)
            {
                ShowFeedback(message, success ? Color.green : Color.red);
            }
        }
        
        /// <summary>
        /// Shows feedback message to the user
        /// </summary>
        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                feedbackText.gameObject.SetActive(true);
                _feedbackTimer = feedbackDisplayTime;
            }
        }
    }
}