using System;
using System.Collections.Generic;
using SAE_Dubai.JW;
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

        private List<PhotoSession> activeSessions = new();

        // Event for when active sessions change.
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
            FindAnyObjectByType<ComputerUI>();

            // ? Initialize location names if needed.
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

            if (TravelCostManager.Instance != null && TravelCostManager.Instance.AttemptTravelPayment((int)travelCost)) {
                // Get the main location transform.
                Transform locationTransform = photoLocations[locationIndex];
        
                // ! Find the player spawn point (child transform).
                    Transform playerSpawnPoint = locationTransform.Find("PlayerSpawnLocation");
        
                if (playerSpawnPoint != null) {
                    // Teleport player to the player spawn point
                    playerObject.transform.position = playerSpawnPoint.position;
                    playerObject.transform.rotation = playerSpawnPoint.rotation;
                } else {
                    // Fallback to the main location transform if no specific spawn point exists
                    playerObject.transform.position = locationTransform.position;
                    playerObject.transform.rotation = locationTransform.rotation;
                }

                // Find the session for this location and spawn the client
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

            // Get location transform.
            Transform spawnLocation = photoLocations[session.locationIndex];

            // Create client requirements based on session.
            List<PortraitShotType> requirements = new List<PortraitShotType> {
                session.requiredShotType
            };

            // Select a random client archetype.
            ClientData clientArchetype = clientArchetypes[UnityEngine.Random.Range(0, clientArchetypes.Count)];

            // Instantiate the client.
            GameObject clientObject = Instantiate(clientPrefab, spawnLocation.position, spawnLocation.rotation);
            ClientJobController clientController = clientObject.GetComponent<ClientJobController>();

            if (clientController != null) {
                // Setup the client with the session data.
                clientController.SetupJob(clientArchetype, requirements, (int)session.reward);

                // Subscribe to the job completed event.
                clientController.OnJobCompleted +=
                    (completedClient) => HandleClientJobCompleted(session);

                // Mark as spawned.
                session.isClientSpawned = true;
                session.ClientReference = clientController;

                OnSessionsChanged?.Invoke();
            }
            else {
                Debug.LogError(
                    "PhotoSessionManager.cs: Failed to setup client controller. Make sure the client prefab has a ClientJobController component.");
                Destroy(clientObject);
            }
        }

        private void HandleClientJobCompleted(PhotoSession session) {
            // Mark session as completed.
            session.IsCompleted = true;

            // Remove session from active list.
            RemoveSession(session);

            // Notify UI about change.
            OnSessionsChanged?.Invoke();
        }
    }

    [Serializable]
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