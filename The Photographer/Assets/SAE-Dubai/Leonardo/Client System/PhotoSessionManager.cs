using System;
using System.Collections.Generic;
using System.Linq;
using SAE_Dubai.JW;
using UnityEngine;
using SAE_Dubai.Leonardo.CameraSys;


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

        [Header("- Photo Evaluation Settings")]
        [SerializeField] private LayerMask portraitSubjectLayer = default;

        [SerializeField] private float maxDetectionDistance = 20f;
        [SerializeField] [Range(0f, 0.5f)] private float detectionRadius = 0.1f;
        [SerializeField] private bool showEvaluatorDebugInfo = true;
        [SerializeField] private string cameraManagerTag = "CameraManager";

        [SerializeField] private CameraManager cameraManager;
        private CameraSystem currentCameraSystem;

        private List<PhotoSession> activeSessions = new();
        public event Action OnSessionsChanged;
        private bool isTraveling = false;


        private void Awake() {
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }

            Debug.Log("[PhotoSessionManager] Awake: Finding CameraManager...");
            cameraManager = GameObject.FindWithTag(cameraManagerTag)?.GetComponent<CameraManager>();
            if (cameraManager == null) {
                Debug.LogError($"[PhotoSessionManager] Awake: CameraManager with tag '{cameraManagerTag}' not found!",
                    this);
            }
        }

        private void Start() {
            Debug.Log("[PhotoSessionManager] Start: Initializing...");
            FindAnyObjectByType<ComputerUI>();

            if (locationNames == null || locationNames.Count != photoLocations.Count) {
                Debug.LogWarning(
                    "PhotoSessionManager: Location names list is empty or doesn't match photoLocations count. Generating default names.");
                locationNames = new List<string>();
                for (int i = 0; i < photoLocations.Count; i++) {
                    string name = (photoLocations[i] != null) ? photoLocations[i].gameObject.name : $"Location {i + 1}";
                    locationNames.Add(name);
                }
            }


            if (cameraManager != null) {
                SubscribeToActiveCamera();
            }
            else {
                Debug.LogError(
                    "[PhotoSessionManager] Start: Cannot subscribe to camera events, CameraManager not found.");
            }

            if (portraitSubjectLayer == 0)
                Debug.LogWarning("[PhotoSessionManager] Portrait Subject Layer not assigned.", this);
            if (clientPrefab == null) Debug.LogError("[PhotoSessionManager] Client Prefab not assigned!", this);
            if (clientArchetypes == null || clientArchetypes.Count == 0)
                Debug.LogError("[PhotoSessionManager] Client Archetypes not assigned!", this);
            if (photoLocations == null || photoLocations.Count == 0)
                Debug.LogError("[PhotoSessionManager] Photo Locations not assigned!", this);
            if (playerObject == null) Debug.LogError("[PhotoSessionManager] Player Object not assigned!", this);
        }

        private void Update() {
            if (cameraManager != null && (currentCameraSystem == null ||
                                          (currentCameraSystem != null &&
                                           !currentCameraSystem.gameObject.activeInHierarchy))) {
                // Debug.Log("[PhotoSessionManager] Update: Checking camera subscription."); // Can be spammy
                SubscribeToActiveCamera();
            }
        }

        void OnDestroy() {
            if (currentCameraSystem != null) {
                Debug.Log($"[PhotoSessionManager] OnDestroy: Unsubscribing from {currentCameraSystem?.name ?? "NULL"}");
                try {
                    currentCameraSystem.OnPhotoCapture -= HandlePhotoCaptured;
                }
                catch {
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
                Debug.Log($"[PhotoSessionManager] Added new session for {session.clientName}");
            }
            else {
                Debug.LogWarning(
                    $"[PhotoSessionManager] Cannot add session for {session.clientName}. Max sessions reached.");
            }
        }

        public void RemoveSession(PhotoSession session) {
            if (activeSessions.Contains(session)) {
                activeSessions.Remove(session);
                OnSessionsChanged?.Invoke();
                Debug.Log($"[PhotoSessionManager] Removed session for {session.clientName}");
            }
        }

        public List<PhotoSession> GetActiveSessions() {
            return activeSessions;
        }


        public void RequestTravelToLocation(int locationIndex) {
            if (isTraveling) {
                Debug.LogWarning("[PhotoSessionManager] Already traveling.");
                return;
            }

            if (locationIndex < 0 || locationIndex >= photoLocations.Count || photoLocations[locationIndex] == null) {
                Debug.LogError($"[PhotoSessionManager] Invalid or null location index {locationIndex}");
                return;
            }

            if (TravelCostManager.Instance != null &&
                TravelCostManager.Instance.AttemptTravelPayment((int)travelCost)) {
                isTraveling = true;
                ScreenFader fader = ScreenFader.Instance;
                if (fader != null) {
                    Debug.Log("[PhotoSessionManager] Starting fade out for travel...");
                    fader.StartFadeOut(onComplete: () => TeleportAndFadeIn(locationIndex));
                }
                else {
                    Debug.LogError("[PhotoSessionManager] ScreenFader instance not found! Teleporting instantly.");
                    PerformTeleportationAndSpawning(locationIndex);
                    isTraveling = false;
                }
            }
            else {
                Debug.Log("[PhotoSessionManager] Travel prevented (cost check failed or TravelCostManager missing).");
            }
        }

        private void TeleportAndFadeIn(int locationIndex) {
            Debug.Log("[PhotoSessionManager] Fade out complete. Performing teleport and spawning...");
            PerformTeleportationAndSpawning(locationIndex);
            ScreenFader fader = ScreenFader.Instance;
            if (fader != null) {
                Debug.Log("[PhotoSessionManager] Starting fade in...");
                fader.StartFadeIn(onComplete: () => {
                    isTraveling = false;
                    Debug.Log("[PhotoSessionManager] Fade in complete. Travel finished.");
                });
            }
            else {
                isTraveling = false;
                Debug.LogWarning("[PhotoSessionManager] ScreenFader instance missing for FadeIn. Travel finished.");
            }
        }

        private void PerformTeleportationAndSpawning(int locationIndex) {
            if (locationIndex < 0 || locationIndex >= photoLocations.Count || photoLocations[locationIndex] == null) {
                Debug.LogError($"[PhotoSessionManager] Cannot perform teleport/spawn, invalid index {locationIndex}");
                return;
            }

            Transform locationTransform = photoLocations[locationIndex];
            Transform playerSpawnPoint = locationTransform.Find("PlayerSpawnLocation");
            Vector3 targetPosition = playerSpawnPoint != null ? playerSpawnPoint.position : locationTransform.position;
            Quaternion targetRotation =
                playerSpawnPoint != null ? playerSpawnPoint.rotation : locationTransform.rotation;

            if (playerObject != null) {
                CharacterController cc = playerObject.GetComponent<CharacterController>();
                if (cc != null) {
                    cc.enabled = false;
                    playerObject.transform.position = targetPosition;
                    playerObject.transform.rotation = targetRotation;
                    cc.enabled = true;
                }
                else {
                    Rigidbody rb = playerObject.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic) {
                        rb.position = targetPosition;
                        rb.rotation = targetRotation;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    else {
                        playerObject.transform.position = targetPosition;
                        playerObject.transform.rotation = targetRotation;
                    }
                }

                Debug.Log($"[PhotoSessionManager] Player teleported to '{locationNames[locationIndex]}'.");
            }
            else {
                Debug.LogError("[PhotoSessionManager] Player object not assigned! Cannot teleport.");
            }

            PhotoSession sessionToSpawn = null;
            foreach (var session in activeSessions) {
                if (session.locationIndex == locationIndex && !session.isClientSpawned) {
                    sessionToSpawn = session;
                    break;
                }
            }

            if (sessionToSpawn != null) {
                SpawnClientForSession(sessionToSpawn);
            }
            else {
                Debug.Log(
                    $"[PhotoSessionManager] No client needed or client already present for location {locationIndex} ('{locationNames[locationIndex]}').");
            }
        }

        private void SpawnClientForSession(PhotoSession session) {
            if (session == null) {
                Debug.LogError("[PhotoSessionManager] Attempted to spawn client for a null session.");
                return;
            }

            if (session.isClientSpawned) return;
            if (clientPrefab == null) {
                Debug.LogError("[PhotoSessionManager] Client Prefab not assigned!");
                return;
            }

            if (clientArchetypes == null || clientArchetypes.Count == 0) {
                Debug.LogError("[PhotoSessionManager] Client Archetypes not assigned!");
                return;
            }

            if (session.locationIndex < 0 || session.locationIndex >= photoLocations.Count ||
                photoLocations[session.locationIndex] == null) {
                Debug.LogError(
                    $"[PhotoSessionManager] Invalid location index {session.locationIndex} for spawning client {session.clientName}");
                return;
            }

            Transform locationParent = photoLocations[session.locationIndex];
            Transform clientSpawnTransform = locationParent.Find("ClientSpawnLocation") ?? locationParent;

            List<PortraitShotType> requirements = new List<PortraitShotType> { session.requiredShotType };
            ClientData clientArchetype = clientArchetypes[UnityEngine.Random.Range(0, clientArchetypes.Count)];
            GameObject clientObject =
                Instantiate(clientPrefab, clientSpawnTransform.position, clientSpawnTransform.rotation);
            ClientJobController clientController = clientObject.GetComponent<ClientJobController>();

            if (clientController != null) {
                clientController.SetupJob(clientArchetype, requirements, (int)session.reward);
                clientController.OnJobCompleted += (completedClient) => HandleClientJobCompleted(session);
                session.isClientSpawned = true;
                session.ClientReference = clientController;
                OnSessionsChanged?.Invoke();
                Debug.Log(
                    $"[PhotoSessionManager] Spawned client '{session.clientName}' at '{locationNames[session.locationIndex]}'. Required: {session.GetShotTypeName()}");
            }
            else {
                Debug.LogError(
                    $"[PhotoSessionManager] Failed to get ClientJobController component on spawned client prefab '{clientPrefab.name}'.",
                    clientPrefab);
                Destroy(clientObject);
            }
        }

        private void HandleClientJobCompleted(PhotoSession session) {
            if (session != null) {
                session.IsCompleted = true;
                Debug.Log($"[PhotoSessionManager] Job completed for client '{session.clientName}'. Removing session.");
                RemoveSession(session);
            }
            else {
                Debug.LogError("[PhotoSessionManager] HandleClientJobCompleted received a null session!");
            }
        }


        private void SubscribeToActiveCamera() {
            Debug.Log("[PhotoSessionManager] Attempting to subscribe to camera event...");
            if (cameraManager == null) {
                Debug.LogError("[PhotoSessionManager] Cannot subscribe, CameraManager is null.");
                return;
            }

            CameraSystem newActiveCamera = cameraManager.GetActiveCamera();

            // Only unsubscribe/resubscribe if the active camera has actually changed or was null
            if (newActiveCamera != currentCameraSystem) {
                // Unsubscribe from previous camera
                if (currentCameraSystem != null) {
                    try {
                        currentCameraSystem.OnPhotoCapture -= HandlePhotoCaptured;
                        Debug.Log(
                            $"[PhotoSessionManager] Unsubscribed from previous camera: {currentCameraSystem.name}");
                    }
                    catch (System.Exception ex) {
                        Debug.LogWarning(
                            $"[PhotoSessionManager] Error unsubscribing from previous camera: {ex.Message}");
                    }
                }

                // Subscribe to the new camera (if it exists)
                currentCameraSystem = newActiveCamera;
                if (currentCameraSystem != null) {
                    Debug.Log(
                        $"[PhotoSessionManager] Subscribing to OnPhotoCapture event on: {currentCameraSystem.name}");
                    currentCameraSystem.OnPhotoCapture += HandlePhotoCaptured;
                }
                else {
                    Debug.LogWarning(
                        "[PhotoSessionManager] SubscribeToActiveCamera: No active camera found via CameraManager.");
                }
            }
            else {
                // Debug.Log("[PhotoSessionManager] SubscribeToActiveCamera: No change in active camera."); // Can be spammy
            }
        }


        private void HandlePhotoCaptured(CapturedPhoto photo) {
            if (photo == null) {
                Debug.LogError("[PhotoSessionManager] HandlePhotoCaptured received a null photo object!");
                return;
            }

            Debug.LogWarning(">>> [PhotoSessionManager] HandlePhotoCaptured method EXECUTED! <<<");
            Debug.Log($"--- Photo Captured Debug Info (Handled by PhotoSessionManager) ---");

            // 1. Get the active camera.
            Camera activeCamera = null;
            if (currentCameraSystem != null && currentCameraSystem.isCameraOn) {
                activeCamera = currentCameraSystem.usingViewfinder
                    ? currentCameraSystem.viewfinderCamera
                    : currentCameraSystem.cameraRenderer;
            }

            activeCamera ??= Camera.main;

            if (activeCamera == null) {
                Debug.LogError(
                    "[PhotoSessionManager] HandlePhotoCaptured: Could not find any active camera for evaluation.");
                Debug.Log($"Photo Quality (from event): {photo.quality:P0}");
                Debug.Log($"------------------------------------------------------------");
                return;
            }

            // 2. Evaluate composition.
            Transform subjectTransform = null;
            PortraitShotType? evaluatedShotType = null;
            try {
                evaluatedShotType = PhotoCompositionEvaluator.EvaluateComposition(
                    activeCamera, portraitSubjectLayer, maxDetectionDistance, detectionRadius,
                    out subjectTransform, showEvaluatorDebugInfo);
            }
            catch (System.Exception ex) {
                Debug.LogError(
                    $"[PhotoSessionManager] Error during PhotoCompositionEvaluator.EvaluateComposition: {ex.Message}\n{ex.StackTrace}");
                Debug.Log($"------------------------------------------------------------");
                return;
            }

            // 3. Tag the photo object.
            photo.portraitShotType = evaluatedShotType;

            string detectedCompStr = evaluatedShotType.HasValue
                ? PhotoCompositionEvaluator.GetShotTypeDisplayName(evaluatedShotType.Value)
                : "None Detected";
            Debug.Log($"Detected Composition: {detectedCompStr}");
            Debug.Log($"Photo Quality: {photo.quality:P0}");

            // 4. Find which ACTIVE SESSION client (if any) was photographed.
            ClientJobController targetClient = null;
            PhotoSession targetSession = null;

            if (subjectTransform != null) {
                Debug.Log($"Subject Detected: {subjectTransform.name}");
                foreach (var session in activeSessions) {
                    if (session.isClientSpawned && session.ClientReference != null &&
                        session.ClientReference.transform == subjectTransform) {
                        targetClient = session.ClientReference;
                        targetSession = session;
                        Debug.Log($"Match found: Subject is client '{targetClient.clientName}' for active session.");
                        break;
                    }
                }

                if (targetClient != null && targetSession != null) {
                    if (targetClient.requiredShotTypes != null && targetClient.requiredShotTypes.Count > 0) {
                        string requiredCompsStr = string.Join(", ",
                            targetClient.requiredShotTypes.Select(PhotoCompositionEvaluator.GetShotTypeDisplayName));
                        Debug.Log($"Active Client '{targetClient.clientName}' Requires: [{requiredCompsStr}]");
                    }
                    else {
                        Debug.Log($"Active Client '{targetClient.clientName}' has no specific requirements listed.");
                    }

                    // 5. Route the photo.
                    Debug.Log($"Routing photo to client '{targetClient.clientName}' for checking.");
                    targetClient.CheckPhoto(photo);
                }
                else {
                    ClientJobController potentialClient = subjectTransform.GetComponent<ClientJobController>();
                    if (potentialClient != null) {
                        Debug.Log(
                            $"Subject '{subjectTransform.name}' has ClientJobController but does not match any active session client.");
                    }
                    else {
                        Debug.Log(
                            $"Subject '{subjectTransform.name}' detected on layer '{LayerMask.LayerToName(subjectTransform.gameObject.layer)}' but is not a client or not part of an active session.");
                    }
                }
            }
            else {
                Debug.Log("No subject detected on the 'PortraitSubject' layer in the center of the shot.");
            }

            Debug.Log($"------------------------------------------------------------");
        }
    }


    [Serializable]
    public class PhotoSession
    {
        public string clientName;
        public PortraitShotType requiredShotType = PortraitShotType.Undefined;
        public int locationIndex = -1;
        public float reward;

        [NonSerialized] public bool isClientSpawned = false;
        [NonSerialized] public bool IsCompleted = false;
        [NonSerialized] public ClientJobController ClientReference = null;

        public string GetLocationName() {
            if (PhotoSessionManager.Instance != null && PhotoSessionManager.Instance.locationNames != null &&
                locationIndex >= 0 && locationIndex < PhotoSessionManager.Instance.locationNames.Count) {
                return PhotoSessionManager.Instance.locationNames[locationIndex];
            }

            if (locationIndex >= 0) {
                return $"Location {locationIndex + 1}";
            }

            return "Invalid Location";
        }

        public string GetShotTypeName() {
            return PhotoCompositionEvaluator.GetShotTypeDisplayName(requiredShotType);
        }
    }
}