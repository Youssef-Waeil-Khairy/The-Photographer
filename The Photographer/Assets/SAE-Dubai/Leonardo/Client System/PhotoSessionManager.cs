using System;
using System.Collections;
using System.Collections.Generic;
using SAE_Dubai.JW; // Namespace for ComputerUI, PlayerBalance etc.
using UnityEngine;

// Ensure you have the namespace for the ClientData, ClientJobController, etc.
using SAE_Dubai.Leonardo.Client_System;

namespace SAE_Dubai.Leonardo.Client_System // Keep your original namespace
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
        public GameObject playerObject; // Assign your Player GameObject here

        [Header("- Client References")]
        public GameObject clientPrefab; // Assign your Client Prefab
        public List<ClientData> clientArchetypes; // Assign your ClientData ScriptableObjects

        private List<PhotoSession> activeSessions = new();
        public event Action OnSessionsChanged;

        // Flag to prevent starting multiple travels at once
        private bool isTraveling = false;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject); // Optional
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Attempt to find ComputerUI to ensure it's loaded or accessible
            FindAnyObjectByType<ComputerUI>(); // Assuming ComputerUI is needed/related

            // Initialize location names if not set in inspector
            if (locationNames == null || locationNames.Count != photoLocations.Count)
            {
                 Debug.LogWarning("PhotoSessionManager: Location names list is empty or doesn't match photoLocations count. Generating default names.");
                locationNames = new List<string>();
                for (int i = 0; i < photoLocations.Count; i++)
                {
                    // Use the GameObject name if available, otherwise generate default
                    string name = (photoLocations[i] != null) ? photoLocations[i].gameObject.name : $"Location {i + 1}";
                    locationNames.Add(name);
                }
            }
        }

        /// <summary>
        /// Checks if a new session can be added based on the limit.
        /// </summary>
        public bool CanAddNewSession()
        {
            return activeSessions.Count < maxActiveSessions;
        }

        /// <summary>
        /// Adds a new photo session to the active list if space is available.
        /// </summary>
        /// <param name="session">The session to add.</param>
        public void AddNewSession(PhotoSession session)
        {
            if (CanAddNewSession())
            {
                activeSessions.Add(session);
                OnSessionsChanged?.Invoke(); // Notify UI or other listeners
                Debug.Log($"PhotoSessionManager: Added new session for {session.clientName}");
            }
             else
            {
                 Debug.LogWarning($"PhotoSessionManager: Cannot add session for {session.clientName}. Max sessions reached.");
            }
        }

        /// <summary>
        /// Removes a specific session from the active list.
        /// </summary>
        /// <param name="session">The session to remove.</param>
        public void RemoveSession(PhotoSession session)
        {
            if (activeSessions.Contains(session))
            {
                activeSessions.Remove(session);
                OnSessionsChanged?.Invoke(); // Notify UI or other listeners
                Debug.Log($"PhotoSessionManager: Removed session for {session.clientName}");
            }
        }

        /// <summary>
        /// Gets the list of currently active photo sessions.
        /// </summary>
        public List<PhotoSession> GetActiveSessions()
        {
            return activeSessions;
        }


        /// <summary>
        /// Initiates the travel sequence to a specific location, including screen fade.
        /// </summary>
        /// <param name="locationIndex">Index of the destination in the photoLocations list.</param>
        public void RequestTravelToLocation(int locationIndex)
        {
            // Prevent starting a new travel if already in progress
            if (isTraveling)
            {
                Debug.LogWarning("PhotoSessionManager: Already traveling.");
                return;
            }

            // Basic validation for location index
            if (locationIndex < 0 || locationIndex >= photoLocations.Count || photoLocations[locationIndex] == null)
            {
                Debug.LogError($"PhotoSessionManager: Invalid or null location index {locationIndex}");
                return;
            }

            // Check cost via TravelCostManager BEFORE starting the fade
            if (TravelCostManager.Instance != null && TravelCostManager.Instance.AttemptTravelPayment((int)travelCost))
            {
                isTraveling = true; // Mark that a travel sequence has started

                // Get the ScreenFader instance (ensure ScreenFader exists in the scene)
                ScreenFader fader = ScreenFader.Instance;
                if (fader != null)
                {
                    // Start Fade Out, and provide the TeleportAndFadeIn method as the callback action
                    // This callback will execute *after* the screen is fully faded out
                    Debug.Log("PhotoSessionManager: Starting fade out for travel...");
                    fader.StartFadeOut(onComplete: () => TeleportAndFadeIn(locationIndex));
                }
                else
                {
                    // Fallback: No fader found, perform teleport instantly (original behavior)
                    Debug.LogError("PhotoSessionManager: ScreenFader instance not found! Teleporting instantly.");
                    PerformTeleportationAndSpawning(locationIndex);
                    isTraveling = false; // Reset flag immediately in fallback case
                }
            }
            else
            {
                 Debug.Log("PhotoSessionManager: Travel prevented (cost check failed or TravelCostManager missing).");
                 // Feedback about insufficient funds is likely handled by TravelCostManager's event
                 // which ActiveSessionItem listens to.
            }
        }

        /// <summary>
        /// Performs the actual teleportation and client spawning, then starts the fade-in.
        /// This method is intended to be called as a callback from ScreenFader *after* fade-out.
        /// </summary>
        /// <param name="locationIndex">The index of the target location.</param>
        private void TeleportAndFadeIn(int locationIndex)
        {
             Debug.Log("PhotoSessionManager: Fade out complete. Performing teleport and spawning...");
             PerformTeleportationAndSpawning(locationIndex);

             // Now that teleport/spawn is done (instantaneously), start fading back in
             ScreenFader fader = ScreenFader.Instance;
             if (fader != null)
             {
                 Debug.Log("PhotoSessionManager: Starting fade in...");
                // Start Fade In, and provide a lambda expression to reset the isTraveling flag once fade-in completes
                fader.StartFadeIn(onComplete: () =>
                {
                    isTraveling = false;
                    Debug.Log("PhotoSessionManager: Fade in complete. Travel finished.");
                });
             }
             else
             {
                 // Should ideally not happen if fade out worked, but handle just in case
                 isTraveling = false;
                 Debug.LogWarning("PhotoSessionManager: ScreenFader instance missing for FadeIn. Travel finished.");
             }
        }

        /// <summary>
        /// Handles the actual player teleportation and client spawning logic.
        /// Separated for clarity and use in the callback.
        /// </summary>
        /// <param name="locationIndex">Index of the target location.</param>
        private void PerformTeleportationAndSpawning(int locationIndex)
        {
             // Double-check index validity
            if (locationIndex < 0 || locationIndex >= photoLocations.Count || photoLocations[locationIndex] == null)
            {
                 Debug.LogError($"PhotoSessionManager: Cannot perform teleport/spawn, invalid index {locationIndex}");
                 return;
            }

            // Determine spawn point (main location or specific child)
            Transform locationTransform = photoLocations[locationIndex];
            Transform playerSpawnPoint = locationTransform.Find("PlayerSpawnLocation"); // Look for a child named this

            Vector3 targetPosition = playerSpawnPoint != null ? playerSpawnPoint.position : locationTransform.position;
            Quaternion targetRotation = playerSpawnPoint != null ? playerSpawnPoint.rotation : locationTransform.rotation;

            // --- Teleport player ---
            if (playerObject != null)
            {
                // Check if using CharacterController for safer teleportation
                 CharacterController cc = playerObject.GetComponent<CharacterController>();
                 if (cc != null)
                 {
                     cc.enabled = false; // IMPORTANT: Disable CC before changing transform directly
                     playerObject.transform.position = targetPosition;
                     playerObject.transform.rotation = targetRotation;
                     cc.enabled = true; // Re-enable CC after teleportation
                 }
                 else
                 {
                      // If not using CharacterController, assume Rigidbody or just Transform modification is okay
                      // Note: Directly setting rigidbody.position might be better if using physics heavily
                      Rigidbody rb = playerObject.GetComponent<Rigidbody>();
                       if (rb != null && !rb.isKinematic)
                       {
                           rb.position = targetPosition; // Use Rigidbody's position if dynamic
                           rb.rotation = targetRotation;
                           rb.linearVelocity = Vector3.zero; // Reset velocity after teleport
                           rb.angularVelocity = Vector3.zero;
                       }
                       else
                       {
                            playerObject.transform.position = targetPosition; // Fallback to transform
                            playerObject.transform.rotation = targetRotation;
                       }
                 }
                Debug.Log($"PhotoSessionManager: Player teleported to '{locationNames[locationIndex]}'.");
            }
            else
            {
                Debug.LogError("PhotoSessionManager: Player object not assigned! Cannot teleport.");
            }


            // --- Find the session for this location and spawn the client if needed ---
            PhotoSession sessionToSpawn = null;
            foreach (var session in activeSessions)
            {
                // Find the active session matching the location index that hasn't spawned its client yet
                if (session.locationIndex == locationIndex && !session.isClientSpawned)
                {
                    sessionToSpawn = session;
                    break;
                }
            }

            if (sessionToSpawn != null)
            {
                 SpawnClientForSession(sessionToSpawn); // Call the spawning logic
            }
            else
            {
                Debug.Log($"PhotoSessionManager: No client needed or client already present for location {locationIndex} ('{locationNames[locationIndex]}').");
            }
        }


        /// <summary>
        /// Instantiates and sets up a client for a specific photo session.
        /// </summary>
        /// <param name="session">The session requiring a client.</param>
        private void SpawnClientForSession(PhotoSession session)
        {
            if (session == null)
            {
                Debug.LogError("PhotoSessionManager: Attempted to spawn client for a null session.");
                return;
            }

            // Don't spawn if already spawned or prefab/locations are missing
            if (session.isClientSpawned) return;
            if (clientPrefab == null) { Debug.LogError("Client Prefab not assigned!"); return; }
            if (clientArchetypes == null || clientArchetypes.Count == 0) { Debug.LogError("Client Archetypes not assigned!"); return; }
             if (session.locationIndex < 0 || session.locationIndex >= photoLocations.Count || photoLocations[session.locationIndex] == null)
             {
                 Debug.LogError($"PhotoSessionManager: Invalid location index {session.locationIndex} for spawning client {session.clientName}");
                 return;
             }


            // Determine client spawn location (can be same as player or different)
            Transform locationParent = photoLocations[session.locationIndex];
            // Look for a specific child object named "ClientSpawnLocation", otherwise use the main location transform
            Transform clientSpawnTransform = locationParent.Find("ClientSpawnLocation") ?? locationParent;


            // Prepare client requirements (currently just the single required shot)
            List<PortraitShotType> requirements = new List<PortraitShotType> { session.requiredShotType };

            // Select a random client archetype (or implement more specific logic if needed)
            ClientData clientArchetype = clientArchetypes[UnityEngine.Random.Range(0, clientArchetypes.Count)];

            // Instantiate the client prefab
            GameObject clientObject = Instantiate(clientPrefab, clientSpawnTransform.position, clientSpawnTransform.rotation);
            ClientJobController clientController = clientObject.GetComponent<ClientJobController>();

            if (clientController != null)
            {
                // Setup the client's job details
                clientController.SetupJob(clientArchetype, requirements, (int)session.reward);

                // Subscribe to the client's completion event
                // Use a lambda to capture the specific 'session' variable for the callback
                clientController.OnJobCompleted += (completedClient) => HandleClientJobCompleted(session);

                // Update the session state
                session.isClientSpawned = true;
                session.ClientReference = clientController; // Store reference to the client controller

                OnSessionsChanged?.Invoke(); // Update UI to show client is ready/spawned

                Debug.Log($"PhotoSessionManager: Spawned client '{session.clientName}' for session at '{locationNames[session.locationIndex]}'. Required: {session.GetShotTypeName()}");
            }
            else
            {
                 Debug.LogError($"PhotoSessionManager: Failed to get ClientJobController component on spawned client prefab '{clientPrefab.name}'.", clientPrefab);
                Destroy(clientObject); // Clean up the failed instance
            }
        }

         /// <summary>
         /// Handles the event when a client's job is completed.
         /// </summary>
         /// <param name="session">The session that was completed.</param>
         private void HandleClientJobCompleted(PhotoSession session)
         {
            if (session != null)
            {
                 session.IsCompleted = true; // Mark session as completed (useful if tracking history)
                 Debug.Log($"PhotoSessionManager: Job completed for client '{session.clientName}'. Removing session.");
                 RemoveSession(session); // Remove from active list (this already invokes OnSessionsChanged)
            }
            else
            {
                 Debug.LogError("PhotoSessionManager: HandleClientJobCompleted received a null session!");
            }
         }
    }


    // --- PhotoSession Class Definition ---
    // (Keep this within the same file or ensure it's accessible in the namespace)
    [Serializable]
    public class PhotoSession
    {
        public string clientName;
        public PortraitShotType requiredShotType = PortraitShotType.Undefined; // Default value
        public int locationIndex = -1; // Default value indicating invalid
        public float reward;

        // These fields should not be serialized if saving/loading sessions directly,
        // as they represent runtime state.
        [NonSerialized] public bool isClientSpawned = false;
        [NonSerialized] public bool IsCompleted = false;
        [NonSerialized] public ClientJobController ClientReference = null; // Reference to the spawned client instance

        /// <summary>
        /// Gets the display name for the session's location.
        /// </summary>
        public string GetLocationName()
        {
            if (PhotoSessionManager.Instance != null &&
                PhotoSessionManager.Instance.locationNames != null &&
                locationIndex >= 0 && // Check if index is valid
                locationIndex < PhotoSessionManager.Instance.locationNames.Count)
            {
                // Return the name from the manager's list
                return PhotoSessionManager.Instance.locationNames[locationIndex];
            }
             else if (locationIndex >= 0)
             {
                  // Fallback if manager or list is unavailable but index is valid
                  return $"Location {locationIndex + 1}"; // Use 1-based index for display
             }
             else
             {
                  return "Invalid Location"; // Indicate if index was bad
             }
        }

        /// <summary>
        /// Gets the display name for the required photo composition.
        /// </summary>
        public string GetShotTypeName()
        {
            // Uses the static method from the evaluator class
            return PhotoCompositionEvaluator.GetShotTypeDisplayName(requiredShotType);
        }
    }
} // End of namespace