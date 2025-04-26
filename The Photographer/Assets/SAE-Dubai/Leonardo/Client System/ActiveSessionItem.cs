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
            
            if (travelButton != null)
                travelButton.onClick.AddListener(Travel);
                
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);
                
            if (costText != null && sessionManager != null)
                costText.text = $"Travel Cost: ${sessionManager.travelCost}";
            
            if (TravelCostManager.Instance != null)
                TravelCostManager.Instance.OnTravelPaymentProcessed += HandleTravelPaymentResult;
                
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            if (TravelCostManager.Instance != null)
                TravelCostManager.Instance.OnTravelPaymentProcessed -= HandleTravelPaymentResult;
        }
        
        public void UpdateUI()
        {
            if (linkedSession == null || sessionInfoText == null)
                return;
                
            string status = linkedSession.isClientSpawned ? 
                "Status: <color=green>Client Ready</color>" : 
                "Status: <color=yellow>Not Visited</color>";
            
            sessionInfoText.text = $"<b>{linkedSession.clientName}</b>\n" +
                            $"Location: {linkedSession.GetLocationName()}\n" +
                            $"Shot Type: {linkedSession.GetShotTypeName()}\n" +
                            $"Reward: ${linkedSession.reward}\n" +
                            $"{status}";
                            
            if (cancelButton != null)
                cancelButton.interactable = !linkedSession.isClientSpawned;
        }
        
        private void Travel()
        {
            if (sessionManager != null && linkedSession != null)
            {
                sessionManager.RequestTravelToLocation(linkedSession.locationIndex);
                FindFirstObjectByType<ComputerUI>()?.ToggleComputerVision();
            }
        }
        
        private void Cancel()
        {
            if (sessionManager != null && linkedSession != null && !linkedSession.isClientSpawned)
            {
                sessionManager.RemoveSession(linkedSession);
            }
        }
        
        /// <summary>
        /// Handles the feedback display when travel payment is processed.
        /// </summary>
        private void HandleTravelPaymentResult(bool success, int cost, string message)
        {
            if (sessionManager != null && linkedSession != null)
            {
                ShowFeedback(message, success ? Color.green : Color.red);
            }
        }
        
        /// <summary>
        /// Shows feedback message to the user.
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