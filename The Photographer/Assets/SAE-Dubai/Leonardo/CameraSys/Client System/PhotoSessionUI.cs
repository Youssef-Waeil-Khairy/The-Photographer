using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SAE_Dubai.JW;

namespace SAE_Dubai.Leonardo.CameraSys.Client_System
{
    public class PhotoSessionUI : MonoBehaviour
    {
        [Header("- Available Sessions Tab")]
        public GameObject availableSessionsTab;
        public Transform availableSessionsContent;
        public Button refreshButton;
        public TMP_Text sessionCountText;
        
        [Header("- Active Sessions Tab")]
        public GameObject activeSessionsTab;
        public Transform activeSessionsContent;
        
        [Header("- UI Prefabs")]
        public GameObject sessionButtonPrefab;
        public GameObject activeSessionPrefab;
        
        [Header("- Session Generation")]
        public float minReward = 50f;
        public float maxReward = 200f;
        public int maxAvailableSessions = 5;
        
        [Header("- UI Navigation")]
        public Button availableTabButton;
        public Button activeTabButton;
        
        private List<PhotoSession> availableSessions = new List<PhotoSession>();
        private PhotoSessionManager sessionManager;

        private void Start()
        {
            sessionManager = PhotoSessionManager.Instance;
            if (sessionManager == null)
            {
                Debug.LogError("PhotoSessionManager not found! Make sure it exists in the scene.");
                return;
            }
            
            // Setup event listeners.
            refreshButton.onClick.AddListener(GenerateNewSessions);
            sessionManager.OnSessionsChanged += UpdateActiveSessionsUI;
            
            // Setup tab navigation.
            if (availableTabButton != null)
                availableTabButton.onClick.AddListener(() => SwitchTab(true));
                
            if (activeTabButton != null)
                activeTabButton.onClick.AddListener(() => SwitchTab(false));
            
            // Initial setup.
            GenerateNewSessions();
            UpdateActiveSessionsUI();
            SwitchTab(true); // Start with available sessions tab.
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events.
            if (sessionManager != null)
                sessionManager.OnSessionsChanged -= UpdateActiveSessionsUI;
        }

        public void GenerateNewSessions()
        {
            availableSessions.Clear();
            
            // Generate random sessions.
            for (int i = 0; i < maxAvailableSessions; i++)
            {
                // Create unique client name.
                string clientName = GenerateClientName();
                
                // Create the session.
                PhotoSession session = new PhotoSession
                {
                    clientName = clientName,
                    // Avoid Undefined (index 0).
                    requiredShotType = (PortraitShotType)Random.Range(1, 8),
                    locationIndex = Random.Range(0, sessionManager.photoLocations.Count),
                    reward = Mathf.Round(Random.Range(minReward, maxReward))
                };
                availableSessions.Add(session);
            }
            
            UpdateAvailableSessionsUI();
            UpdateSessionCountText();
        }

        private string GenerateClientName()
        {
            string[] firstNames = { "Alex", "Jamie", "Jordan", "Taylor", "Casey", "Morgan", "Riley", "Quinn", "Bailey", "Avery" };
            string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Garcia", "Wilson", "Anderson" };
            
            return $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
        }

        private void UpdateAvailableSessionsUI()
        {
            // Clear existing buttons.
            foreach (Transform child in availableSessionsContent)
            {
                Destroy(child.gameObject);
            }
        
            // Create new buttons for each available session.
            foreach (var session in availableSessions)
            {
                GameObject buttonObj = Instantiate(sessionButtonPrefab, availableSessionsContent);
                Button button = buttonObj.GetComponent<Button>();
                TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
                
                buttonText.text = $"<b>{session.clientName}</b>\n" +
                                $"Location: {session.GetLocationName()}\n" +
                                $"Shot Type: {session.GetShotTypeName()}\n" +
                                $"Reward: ${session.reward}\n" +
                                $"Travel Cost: ${sessionManager.travelCost}";
        
                button.onClick.AddListener(() => AcceptSession(session));
                
                // Disable button if max sessions reached.
                button.interactable = sessionManager.CanAddNewSession();
            }
        }
        
        private void UpdateActiveSessionsUI()
        {
            // Clear existing UI elements.
            foreach (Transform child in activeSessionsContent)
            {
                Destroy(child.gameObject);
            }
            
            // Get active sessions from manager.
            List<PhotoSession> activeSessions = sessionManager.GetActiveSessions();
            
            // Create UI elements for each active session.
            foreach (var session in activeSessions)
            {
                GameObject sessionObj = Instantiate(activeSessionPrefab, activeSessionsContent);
                TMP_Text sessionText = sessionObj.GetComponentInChildren<TMP_Text>();
                Button travelButton = sessionObj.GetComponentInChildren<Button>();
                
                // Status text.
                string status = session.isClientSpawned ? "Status: <color=green>Client Ready</color>" : "Status: <color=yellow>Not Visited</color>";
                
                sessionText.text = $"<b>{session.clientName}</b>\n" +
                                $"Location: {session.GetLocationName()}\n" +
                                $"Shot Type: {session.GetShotTypeName()}\n" +
                                $"Reward: ${session.reward}\n" +
                                $"{status}";
                
                // Setup travel button.
                travelButton.onClick.AddListener(() => TravelToSession(session));
            }
            
            UpdateSessionCountText();
        }
        
        private void UpdateSessionCountText()
        {
            if (sessionCountText != null)
            {
                int activeCount = sessionManager.GetActiveSessions().Count;
                sessionCountText.text = $"Sessions: {activeCount}/{sessionManager.maxActiveSessions}";
            }
        }

        private void AcceptSession(PhotoSession session)
        {
            if (sessionManager.CanAddNewSession())
            {
                sessionManager.AddNewSession(session);
                availableSessions.Remove(session);
                UpdateAvailableSessionsUI();
                UpdateActiveSessionsUI();
                
                // Auto switch to active tab.
                SwitchTab(false);
            }
        }
        
        private void TravelToSession(PhotoSession session)
        {
            bool success = sessionManager.TravelToLocation(session.locationIndex);
            
            if (success)
            {
                // Close computer UI if successful.
                FindObjectOfType<ComputerUI>()?.ToggleComputerVision();
                UpdateActiveSessionsUI();
            }
        }
        
        private void SwitchTab(bool showAvailableTab)
        {
            availableSessionsTab.SetActive(showAvailableTab);
            activeSessionsTab.SetActive(!showAvailableTab);
            
            // Update button visuals if needed
            if (availableTabButton != null)
                availableTabButton.interactable = !showAvailableTab;
                
            if (activeTabButton != null)
                activeTabButton.interactable = showAvailableTab;
        }
    }
} 