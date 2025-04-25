using System;
using System.Collections.Generic;
using SAE_Dubai.JW;
using SAE_Dubai.Leonardo.Client_System.ClientUI;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    public class PhotoSessionManager : MonoBehaviour
    {
        public static PhotoSessionManager Instance { get; private set; }

        [Header("- Session Settings")]
        public int maxActiveSessions = 3;

        public float travelCost = 25f;

        [Header("- Locations")]
        public List<Transform> photoLocations;

        public List<string> locationNames;

        [Header("- Player Object")]
        public GameObject playerObject;

        [Header("- Client References")]
        public GameObject clientPrefab;

        public List<ClientData> clientArchetypes;

        private List<PhotoSession> activeSessions = new List<PhotoSession>();
        private ComputerUI computerUI;
        private TravelCostManager _travelCostManagerScript;

        // Event for when active sessions change
        public event Action OnSessionsChanged;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else {
                Destroy(gameObject);
            }
        }

        private void Start() {
            computerUI = FindAnyObjectByType<ComputerUI>();
            _travelCostManagerScript = FindAnyObjectByType<TravelCostManager>();

            // Initialize location names if needed
            if (locationNames == null || locationNames.Count == 0) {
                locationNames = new List<string>();
                for (int i = 0; i < photoLocations.Count; i++) {
                    locationNames.Add($"Location {i + 1}");
                }
            }
        }

        public bool CanAddNewSession() {
            return activeSessions.Count < maxActiveSessions;
        }

        public void AddNewSession(PhotoSession session) {
            if (CanAddNewSession()) {
                activeSessions.Add(session);
                OnSessionsChanged?.Invoke();
            }
        }

        public void RemoveSession(PhotoSession session) {
            if (activeSessions.Contains(session)) {
                activeSessions.Remove(session);
                OnSessionsChanged?.Invoke();
            }
        }

        public List<PhotoSession> GetActiveSessions() {
            return activeSessions;
        }

        public bool TravelToLocation(int locationIndex) {
            if (locationIndex < 0 || locationIndex >= photoLocations.Count)
                return false;

            if (_travelCostManagerScript.AttemptTravelPayment((int)travelCost)) {
                
                // Teleport player to location.
                playerObject.transform.position = photoLocations[locationIndex].position;
                playerObject.transform.rotation = photoLocations[locationIndex].rotation;

                // Find the session for this location and spawn the client.
                foreach (var session in activeSessions) {
                    if (session.locationIndex == locationIndex && !session.isClientSpawned) {
                        SpawnClientForSession(session);
                        return true;
                    }
                }

                return true;
            }

            return false;
        }

        private void SpawnClientForSession(PhotoSession session) {
            if (session.isClientSpawned)
                return;

            // Get location transform
            Transform spawnLocation = photoLocations[session.locationIndex];

            // Create client requirements based on session
            List<PortraitShotType> requirements = new List<PortraitShotType> {
                session.requiredShotType
            };

            // Select a random client archetype
            ClientData clientArchetype = clientArchetypes[UnityEngine.Random.Range(0, clientArchetypes.Count)];

            // Instantiate the client
            GameObject clientObject = Instantiate(clientPrefab, spawnLocation.position, spawnLocation.rotation);
            ClientJobController clientController = clientObject.GetComponent<ClientJobController>();

            if (clientController != null) {
                // Setup the client with the session data
                clientController.SetupJob(clientArchetype, requirements, (int)session.reward);

                // Subscribe to the job completed event
                clientController.OnJobCompleted +=
                    (completedClient) => HandleClientJobCompleted(completedClient, session);

                // Mark as spawned
                session.isClientSpawned = true;
                session.ClientReference = clientController;

                OnSessionsChanged?.Invoke();
            }
            else {
                Debug.LogError(
                    "Failed to setup client controller. Make sure the client prefab has a ClientJobController component.");
                Destroy(clientObject);
            }
        }

        private void HandleClientJobCompleted(ClientJobController completedClient, PhotoSession session) {
            // Mark session as completed
            session.IsCompleted = true;

            // Remove session from active list
            RemoveSession(session);

            // Notify UI about change
            OnSessionsChanged?.Invoke();
        }
    }

    [System.Serializable]
    public class PhotoSession
    {
        public string clientName;
        public PortraitShotType requiredShotType;
        public int locationIndex;
        public float reward;

        [NonSerialized] public bool isClientSpawned;
        [NonSerialized] public bool IsCompleted;
        [NonSerialized] public ClientJobController ClientReference;

        public string GetLocationName() {
            if (PhotoSessionManager.Instance != null &&
                PhotoSessionManager.Instance.locationNames != null &&
                locationIndex < PhotoSessionManager.Instance.locationNames.Count) {
                return PhotoSessionManager.Instance.locationNames[locationIndex];
            }

            return $"Location {locationIndex}";
        }

        public string GetShotTypeName() {
            return PhotoCompositionEvaluator.GetShotTypeDisplayName(requiredShotType);
        }
    }
}