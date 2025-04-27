using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SAE_Dubai.JW;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.Leonardo.Client_System
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
        private readonly List<PhotoSession> _availableSessions = new();
        private PhotoSessionManager _sessionManager;

        [Header("- Feedback")]
        public TMP_Text feedbackText;

        private void Start() {
            _sessionManager = PhotoSessionManager.Instance;
            if (_sessionManager == null) {
                Debug.LogError("PhotoSessionManager not found! Make sure it exists in the scene.");
                return;
            }

            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
            
            // Setup event listeners.
            refreshButton.onClick.AddListener(GenerateNewSessions);
            _sessionManager.OnSessionsChanged += UpdateActiveSessionsUI;

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

        private void OnDestroy() {
            // Unsubscribe from events.
            if (_sessionManager != null)
                _sessionManager.OnSessionsChanged -= UpdateActiveSessionsUI;
        }

        public void GenerateNewSessions() {
            _availableSessions.Clear();

            // Generate all valid shot types once (outside the loop)
            PortraitShotType[] validShotTypes = System.Enum.GetValues(typeof(PortraitShotType))
                .Cast<PortraitShotType>()
                .Where(type => type != PortraitShotType.Undefined)
                .ToArray();

            // Generate random sessions.
            for (int i = 0; i < maxAvailableSessions; i++) {
                // Generate a client name from the available predefined client names
                string clientName = GenerateClientNameFromClientData();

                // Create the session.
                PhotoSession session = new PhotoSession {
                    clientName = clientName,
                    // Select a random valid shot type
                    requiredShotType = validShotTypes[Random.Range(0, validShotTypes.Length)],
                    locationIndex = Random.Range(0, _sessionManager.photoLocations.Count),
                    reward = Mathf.Round(Random.Range(minReward, maxReward))
                };
                _availableSessions.Add(session);
            }

            UpdateAvailableSessionsUI();
            UpdateSessionCountText();
        }

        private string GenerateClientNameFromClientData() {
            // Get a random name from the ClientData scriptable objects
            if (_sessionManager == null || _sessionManager.clientArchetypes == null ||
                _sessionManager.clientArchetypes.Count == 0) {
                // Fallback to old method if client data isn't available
                return GenerateFallbackName();
            }

            // Get a random client archetype
            ClientData clientData =
                _sessionManager.clientArchetypes[Random.Range(0, _sessionManager.clientArchetypes.Count)];

            if (clientData == null || clientData.possibleNames == null || clientData.possibleNames.Count == 0) {
                return GenerateFallbackName();
            }

            // Return a name from the possible names in ClientData
            return clientData.GetRandomName();
        }

        // Keeping the old method as fallback
        private string GenerateFallbackName() {
            Debug.LogWarning("Using fallback name generation because ClientData was not available");
            string[] firstNames = { "Bab", "Dud", "Jan", "Bob" }; // Match the default names in ClientData.cs

            return firstNames[Random.Range(0, firstNames.Length)];
        }

        private void UpdateAvailableSessionsUI() {
            // Clear existing buttons.
            foreach (Transform child in availableSessionsContent) {
                Destroy(child.gameObject);
            }

            // Create new buttons for each available session.
            foreach (var session in _availableSessions) {
                GameObject buttonObj = Instantiate(sessionButtonPrefab, availableSessionsContent);
                Button button = buttonObj.GetComponent<Button>();
                TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();

                buttonText.text = $"<b>{session.clientName}</b>\n" +
                                  $"Location: {session.GetLocationName()}\n" +
                                  $"Shot Type: {session.GetShotTypeName()}\n" +
                                  $"Reward: ${session.reward}\n" +
                                  $"Travel Cost: ${_sessionManager.travelCost}";

                button.onClick.AddListener(() => AcceptSession(session));

                // Disable button if max sessions reached.
                button.interactable = _sessionManager.CanAddNewSession();
            }
        }

        private void UpdateActiveSessionsUI() {
            // Clear existing UI elements.
            foreach (Transform child in activeSessionsContent) {
                Destroy(child.gameObject);
            }

            // Get active sessions from manager.
            List<PhotoSession> activeSessions = _sessionManager.GetActiveSessions();

            // Create UI elements for each active session.
            foreach (var session in activeSessions) {
                GameObject sessionObj = Instantiate(activeSessionPrefab, activeSessionsContent);

                // Get the ActiveSessionItem component and initialize it
                ActiveSessionItem sessionItem = sessionObj.GetComponent<ActiveSessionItem>();
                if (sessionItem != null) {
                    sessionItem.Initialize(session, _sessionManager);
                }
                else {
                    Debug.LogError("ActiveSessionItem component not found on prefab!");

                    // Fallback to the old way if the component isn't found
                    TMP_Text sessionText = sessionObj.GetComponentInChildren<TMP_Text>();
                    Button travelButton = sessionObj.GetComponentInChildren<Button>();

                    // Status text.
                    string status = session.isClientSpawned
                        ? "Status: <color=green>Client Ready</color>"
                        : "Status: <color=yellow>Not Visited</color>";

                    sessionText.text = $"<b>{session.clientName}</b>\n" +
                                       $"Location: {session.GetLocationName()}\n" +
                                       $"Shot Type: {session.GetShotTypeName()}\n" +
                                       $"Reward: ${session.reward}\n" +
                                       $"{status}";

                    // Setup travel button.
                    travelButton.onClick.AddListener(() => TravelToSession(session));
                }
            }

            UpdateSessionCountText();
        }

        private void UpdateSessionCountText() {
            if (sessionCountText != null) {
                int activeCount = _sessionManager.GetActiveSessions().Count;
                sessionCountText.text = $"Sessions: {activeCount}/{_sessionManager.maxActiveSessions}";
            }
        }

        private void AcceptSession(PhotoSession session) {
            // Check if the location is already occupied
            if (_sessionManager.IsLocationOccupied(session.locationIndex)) {
                // Show feedback to the player
                Debug.Log(
                    $"Cannot accept session: Location '{session.GetLocationName()}' already has an active session.");

                // You could add a UI feedback element here
                // For example, you could add a feedback text field to the class and show a message
                if (feedbackText != null) {
                    feedbackText.text = $"Location '{session.GetLocationName()}' already occupied!";
                    feedbackText.gameObject.SetActive(true);
                    // Optionally hide after some time
                    StartCoroutine(HideFeedbackAfterDelay(3.0f));
                }

                return;
            }

            // Original implementation continues below...
            if (_sessionManager.CanAddNewSession()) {
                _sessionManager.AddNewSession(session);
                _availableSessions.Remove(session);
                UpdateAvailableSessionsUI();
                UpdateActiveSessionsUI();

                // Auto switch to active tab.
                SwitchTab(false);
            }
        }

        // Helper method for hiding feedback.
        private IEnumerator HideFeedbackAfterDelay(float delay) {
            yield return new WaitForSeconds(delay);
            if (feedbackText != null) {
                feedbackText.gameObject.SetActive(false);
            }
        }

        private void TravelToSession(PhotoSession session) {
            _sessionManager.RequestTravelToLocation(session.locationIndex);
            FindFirstObjectByType<ComputerUI>()?.ToggleComputerVision();
        }

        private void SwitchTab(bool showAvailableTab) {
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