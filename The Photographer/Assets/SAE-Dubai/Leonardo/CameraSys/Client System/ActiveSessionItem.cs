using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SAE_Dubai.Leonardo.CameraSys.Client_System
{
    public class ActiveSessionItem : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text sessionInfoText;
        public Button travelButton;
        public Button cancelButton;
        
        private PhotoSession linkedSession;
        private PhotoSessionManager sessionManager;
        
        public void Initialize(PhotoSession session, PhotoSessionManager manager)
        {
            linkedSession = session;
            sessionManager = manager;
            
            // Setup button listeners.
            if (travelButton != null)
                travelButton.onClick.AddListener(Travel);
                
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);
                
            UpdateUI();
        }
        
        public void UpdateUI()
        {
            if (linkedSession == null || sessionInfoText == null)
                return;
                
            // Status text.
            string status = linkedSession.isClientSpawned ? 
                "Status: <color=green>Client Ready</color>" : 
                "Status: <color=yellow>Not Visited</color>";
            
            // Format info text.
            sessionInfoText.text = $"<b>{linkedSession.clientName}</b>\n" +
                            $"Location: {linkedSession.GetLocationName()}\n" +
                            $"Shot Type: {linkedSession.GetShotTypeName()}\n" +
                            $"Reward: ${linkedSession.reward}\n" +
                            $"{status}";
                            
            // Update button states.
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
                    // Update UI after traveling.
                    UpdateUI();
                    
                    // Close computer UI.
                    FindObjectOfType<ComputerUI>()?.ToggleComputerVision();
                }
            }
        }
        
        private void Cancel()
        {
            if (sessionManager != null && linkedSession != null && !linkedSession.isClientSpawned)
            {
                // Only allow cancellation if client hasn't been spawned yet.
                sessionManager.RemoveSession(linkedSession);
            }
        }
    }
}
