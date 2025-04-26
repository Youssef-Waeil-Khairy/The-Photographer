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
        public int maxActiveSessions = 3;
        public float travelCost = 25f;
        public List<Transform> photoLocations;
        public List<string> locationNames;
        public GameObject playerObject;
        public GameObject clientPrefab;
        public List<ClientData> clientArchetypes;

        [Header("- Photo Evaluation Settings (Marker Based)")]
        [SerializeField] private LayerMask portraitSubjectLayer;

        [SerializeField] private LayerMask occlusionCheckMask;
        [SerializeField] private bool drawVisibilityDebugLines;
        [SerializeField] private string cameraManagerTag = "CameraManager";

        private CameraManager _cameraManager;
        private CameraSystem _currentCameraSystem;
        private readonly List<PhotoSession> activeSessions = new();
        public event Action OnSessionsChanged;
        private bool _isTraveling;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }
        }

        private void Start() {
            Debug.Log("[PhotoSessionManager] Start: Initializing...");

            _cameraManager = FindFirstObjectByType<CameraManager>();
            if (_cameraManager == null) {
                Debug.LogError("[PhotoSessionManager] Start: Could not find CameraManager in the scene!");
                _cameraManager = GameObject.FindWithTag(cameraManagerTag)?.GetComponent<CameraManager>();
                if (_cameraManager == null) {
                    Debug.LogError("[PhotoSessionManager] Start: Could not find CameraManager by tag either!");
                }
            }

            if (_cameraManager != null) {
                Debug.Log($"[PhotoSessionManager] Start: Found CameraManager: {_cameraManager.name}");
                SubscribeToActiveCamera();
            }
            else {
                Debug.LogError(
                    "[PhotoSessionManager] Start: No CameraManager found! Composition detection will not work.");
                this.enabled = false;
                return;
            }

            FindAnyObjectByType<ComputerUI>();

            _cameraManager = CameraManager.Instance;

            if (_cameraManager == null) {
                Debug.LogError("[PhotoSessionManager] Start: CameraManager.Instance is null!", this);
                this.enabled = false;
                return;
            }
            else {
                Debug.Log("[PhotoSessionManager] Start: Successfully found CameraManager instance.");
                SubscribeToActiveCamera();
            }

            if (locationNames == null || locationNames.Count != photoLocations.Count) {
                Debug.LogWarning("...");
                locationNames = new List<string>();
                for (int i = 0; i < photoLocations.Count; i++) {
                    locationNames.Add((photoLocations[i] != null)
                        ? photoLocations[i].gameObject.name
                        : $"Location {i + 1}");
                }
            }

            if (portraitSubjectLayer == 0)
                Debug.LogWarning("[PhotoSessionManager] Portrait Subject Layer not assigned.", this);
            if (occlusionCheckMask == 0)
                Debug.LogWarning("[PhotoSessionManager] Occlusion Check Mask not assigned.", this);
            if (clientPrefab == null) Debug.LogError("[PhotoSessionManager] Client Prefab not assigned!", this); /*...*/
        }

        private void Update() {
            if (_cameraManager != null && (_currentCameraSystem == null ||
                                           (_currentCameraSystem != null &&
                                            !_currentCameraSystem.gameObject.activeInHierarchy))) {
                SubscribeToActiveCamera();
            }
        }

        void OnDestroy() {
            if (_currentCameraSystem != null) {
                Debug.Log(
                    $"[PhotoSessionManager] OnDestroy: Unsubscribing from {_currentCameraSystem?.name ?? "NULL"}");
                try {
                    _currentCameraSystem.OnPhotoCapture -= HandlePhotoCaptured;
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
                Debug.LogWarning($"...");
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
            if (_isTraveling) return;
            if (locationIndex < 0 || locationIndex >= photoLocations.Count ||
                photoLocations[locationIndex] == null) return;
            if (TravelCostManager.Instance != null &&
                TravelCostManager.Instance.AttemptTravelPayment((int)travelCost)) {
                _isTraveling = true;
                ScreenFader fader = ScreenFader.Instance;
                if (fader != null) fader.StartFadeOut(onComplete: () => TeleportAndFadeIn(locationIndex));
                else {
                    PerformTeleportationAndSpawning(locationIndex);
                    _isTraveling = false;
                }
            }
        }

        private void TeleportAndFadeIn(int locationIndex) {
            PerformTeleportationAndSpawning(locationIndex);
            ScreenFader fader = ScreenFader.Instance;
            if (fader != null) fader.StartFadeIn(onComplete: () => _isTraveling = false);
            else _isTraveling = false;
        }

        private void PerformTeleportationAndSpawning(int locationIndex) {
            if (locationIndex < 0 || locationIndex >= photoLocations.Count ||
                photoLocations[locationIndex] == null) return;
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
            }

            PhotoSession sessionToSpawn = null;
            foreach (var session in activeSessions) {
                if (session.locationIndex == locationIndex && !session.isClientSpawned) {
                    sessionToSpawn = session;
                    break;
                }
            }

            if (sessionToSpawn != null) SpawnClientForSession(sessionToSpawn);
        }

        private void SpawnClientForSession(PhotoSession session) {
            if (session == null || session.isClientSpawned || clientPrefab == null || clientArchetypes == null ||
                clientArchetypes.Count == 0 || session.locationIndex < 0 ||
                session.locationIndex >= photoLocations.Count || photoLocations[session.locationIndex] == null) return;

            Transform locationParent = photoLocations[session.locationIndex];
            Transform clientSpawnTransform = locationParent.Find("ClientSpawnLocation") ?? locationParent;
            List<PortraitShotType> requirements = new List<PortraitShotType> { session.requiredShotType };

            ClientData clientArchetype = clientArchetypes[UnityEngine.Random.Range(0, clientArchetypes.Count)];

            GameObject clientObject =
                Instantiate(clientPrefab, clientSpawnTransform.position, clientSpawnTransform.rotation);
            ClientJobController clientController = clientObject.GetComponent<ClientJobController>();

            if (clientController != null) {
                clientController.clientName = session.clientName;

                clientController.SetupJob(clientArchetype, requirements, (int)session.reward, session.clientName);
                clientController.OnJobCompleted += (completedClient) => HandleClientJobCompleted(session);
                session.isClientSpawned = true;
                session.ClientReference = clientController;
                OnSessionsChanged?.Invoke();
            }
            else {
                Destroy(clientObject);
            }
        }

        private void HandleClientJobCompleted(PhotoSession session) {
            if (session != null) {
                session.IsCompleted = true;
                RemoveSession(session);
            }
        }


        private void SubscribeToActiveCamera() {
            Debug.Log("<color=blue>Attempting to subscribe to camera event...</color>");

            if (_cameraManager == null) {
                Debug.LogError("<color=red>Cannot subscribe to camera events: cameraManager is null!</color>");
                return;
            }

            CameraSystem newActiveCamera = _cameraManager.GetActiveCamera();

            Debug.Log(
                $"<color=blue>Active camera from manager: {(newActiveCamera != null ? newActiveCamera.name : "NULL")}</color>");

            if (newActiveCamera == null) {
                newActiveCamera = FindObjectOfType<CameraSystem>();
                Debug.Log(
                    $"<color=blue>Attempting to find camera directly: {(newActiveCamera != null ? newActiveCamera.name : "NULL")}</color>");
            }

            if (newActiveCamera != _currentCameraSystem) {
                if (_currentCameraSystem != null) {
                    try {
                        Debug.Log(
                            $"<color=blue>Unsubscribing from previous camera: {_currentCameraSystem.name}</color>");
                        _currentCameraSystem.OnPhotoCapture -= HandlePhotoCaptured;
                    }
                    catch (Exception e) {
                        Debug.LogError($"<color=red>Error unsubscribing from camera: {e.Message}</color>");
                    }
                }

                _currentCameraSystem = newActiveCamera;

                if (_currentCameraSystem != null) {
                    try {
                        Debug.Log(
                            $"<color=blue>Subscribing to OnPhotoCapture event on: {_currentCameraSystem.name}</color>");
                        _currentCameraSystem.OnPhotoCapture += HandlePhotoCaptured;

                        Debug.Log("<color=green>Attempted to subscribe to OnPhotoCapture event</color>");
                    }
                    catch (Exception e) {
                        Debug.LogError($"<color=red>Error subscribing to camera: {e.Message}</color>");
                    }
                }
                else {
                    Debug.LogWarning(
                        "<color=orange>[PhotoSessionManager] SubscribeToActiveCamera: No active camera found.</color>");
                }
            }
            else {
                Debug.Log("<color=blue>Current camera is already set correctly</color>");
            }

            Debug.Log(
                $"<color=blue>Current camera set to: {(_currentCameraSystem != null ? _currentCameraSystem.name : "NULL")}</color>");
        }

        private void HandlePhotoCaptured(CapturedPhoto photo) {
            Debug.Log("<color=magenta>PHOTO CAPTURE EVENT RECEIVED IN PHOTOSESSIONMANAGER</color>");

            if (photo == null) {
                Debug.LogError(
                    "<color=red>[PhotoSessionManager] HandlePhotoCaptured received a null photo object!</color>");
                return;
            }

            Debug.LogWarning(">>> [PhotoSessionManager] HandlePhotoCaptured method EXECUTED! <<<");
            Debug.Log($"--- Photo Captured Debug Info (Marker Based) ---");
            Camera activeCamera = null;
            if (_currentCameraSystem != null && _currentCameraSystem.isCameraOn) {
                activeCamera = _currentCameraSystem.usingViewfinder
                    ? _currentCameraSystem.viewfinderCamera
                    : _currentCameraSystem.cameraRenderer;
            }

            activeCamera ??= Camera.main;

            if (activeCamera == null) {
                Debug.LogError(
                    "[PhotoSessionManager] HandlePhotoCaptured: Could not find any active camera for evaluation.");
                Debug.Log($"Photo Quality: {photo.quality:P0}");
                Debug.Log($"------------------------------------------------------------");
                return;
            }

            PortraitShotType detectedShotType = PortraitShotType.Undefined;
            ClientJobController targetClient = null;

            Ray centerRay = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Debug.Log($"<color=yellow>Casting ray from camera {activeCamera.name} to detect subject</color>");
            bool hitAnything = Physics.Raycast(centerRay, out RaycastHit centerHit, 100f, portraitSubjectLayer);
            Debug.Log($"<color=yellow>Ray hit anything on portrait layer: {hitAnything}</color>");
            Transform subjectTransform = null;

            if (hitAnything) {
                subjectTransform = centerHit.transform;
                Debug.Log(
                    $"<color=yellow>Hit object: {subjectTransform.name} on layer {LayerMask.LayerToName(subjectTransform.gameObject.layer)}</color>");
                targetClient = subjectTransform.GetComponent<ClientJobController>();
                Debug.Log($"<color=yellow>Found ClientJobController: {(targetClient != null)}</color>");
            }
            else {
                Debug.Log("[Photo Eval] Initial subject check hit nothing on subject layer.");
            }


            if (targetClient != null) {
                bool clientIsActiveSession =
                    activeSessions.Any(s => s.isClientSpawned && s.ClientReference == targetClient);
                if (!clientIsActiveSession) {
                    Debug.Log(
                        $"[Photo Eval] Subject {targetClient.name} is a client but not part of an active session.");
                    targetClient = null;
                }
            }


            if (targetClient != null) {
                Debug.Log($"[Photo Eval] Checking markers for client: {targetClient.clientName}");
                int subjectLayer = targetClient.gameObject.layer;
                List<Transform> bodyMarkers = targetClient.GetOrderedBodyMarkers();
                int highestVisibleIndex = -1;
                int lowestVisibleIndex = -1;
                bool aboveHeadVisible = false;
                bool belowFeetVisible = false;

                for (int i = 0; i < bodyMarkers.Count; i++) {
                    if (bodyMarkers[i] != null && PhotoCompositionEvaluator.IsMarkerVisible(activeCamera,
                            bodyMarkers[i].position, occlusionCheckMask, subjectLayer, drawVisibilityDebugLines)) {
                        Debug.Log($"[Photo Eval] Visible Marker: {bodyMarkers[i].name}");
                        if (highestVisibleIndex == -1) {
                            highestVisibleIndex = i;
                        }

                        lowestVisibleIndex = i;
                    }
                }

                if (targetClient.aboveHeadMarker != null && PhotoCompositionEvaluator.IsMarkerVisible(activeCamera,
                        targetClient.aboveHeadMarker.position, occlusionCheckMask, subjectLayer,
                        drawVisibilityDebugLines)) {
                    aboveHeadVisible = true;
                    Debug.Log($"[Photo Eval] Visible Marker: Above Head");
                }

                if (targetClient.belowFeetMarker != null && PhotoCompositionEvaluator.IsMarkerVisible(activeCamera,
                        targetClient.belowFeetMarker.position, occlusionCheckMask, subjectLayer,
                        drawVisibilityDebugLines)) {
                    belowFeetVisible = true;
                    Debug.Log($"[Photo Eval] Visible Marker: Below Feet");
                }


                if (highestVisibleIndex != -1) {
                    // Index mapping (based on GetOrderedBodyMarkers): 0:Head, 1:Chest, 2:Hip, 3:Knees, 4:Feet
                    bool headVisible = highestVisibleIndex <= 0;
                    bool chestVisible = lowestVisibleIndex >= 1;
                    bool hipVisible = lowestVisibleIndex >= 2;
                    bool kneesVisible = lowestVisibleIndex >= 3;
                    bool feetVisible = lowestVisibleIndex >= 4;

                    if (aboveHeadVisible || belowFeetVisible ||
                        (headVisible && feetVisible)) // Full body + surroundings
                    {
                        detectedShotType = (aboveHeadVisible && belowFeetVisible)
                            ? PortraitShotType.ExtremeWide
                            : PortraitShotType.Wide;
                    }
                    else if (headVisible && kneesVisible) // Head to at least knees.
                    {
                        detectedShotType = PortraitShotType.MediumWide;
                    }
                    else if (headVisible && hipVisible) // Head to at least hip.
                    {
                        detectedShotType = PortraitShotType.Medium;
                    }
                    else if (headVisible && chestVisible) // Head to at least chest.
                    {
                        detectedShotType = PortraitShotType.MediumCloseUp;
                    }
                    else if (headVisible) // Only head (and maybe neck/shoulders implicitly).
                    {
                        //!  Differentiate CloseUp vs Extreme based on *only* head vs head+chest maybe? Needs tuning.
                        // ? Simplification: if highest is head and lowest is head -> ECU/CU.
                        if (lowestVisibleIndex == 0) {
                            detectedShotType = PortraitShotType.CloseUp; // Could refine to ECU.
                        }
                        else {
                            // This case should be caught by chestVisible? Revisit logic if needed.
                            detectedShotType = PortraitShotType.MediumCloseUp;
                        }
                    }
                    // Add more specific rules if needed (e.g., for ECU based on single head marker).
                }
                else if (aboveHeadVisible || belowFeetVisible) {
                    // Only saw above/below markers, likely very wide.
                    detectedShotType = PortraitShotType.ExtremeWide;
                }
                // If highestVisibleIndex remains -1, type remains Undefined.

                Debug.Log(
                    $"[Photo Eval] Result: Highest Visible Index={highestVisibleIndex}, Lowest Visible Index={lowestVisibleIndex}, Above={aboveHeadVisible}, Below={belowFeetVisible}");
            }
            else {
                Debug.Log("[Photo Eval] No client subject detected by initial raycast.");
            }


            photo.portraitShotType = detectedShotType;

            string detectedCompStr = PhotoCompositionEvaluator.GetShotTypeDisplayName(detectedShotType);
            Debug.Log($"Determined Composition: {detectedCompStr}");
            Debug.Log($"Photo Quality: {photo.quality:P0}");

            if (targetClient != null && detectedShotType != PortraitShotType.Undefined) {
                if (targetClient.requiredShotTypes != null && targetClient.requiredShotTypes.Count > 0) {
                    string requiredCompsStr = string.Join(", ",
                        targetClient.requiredShotTypes.Select(PhotoCompositionEvaluator.GetShotTypeDisplayName));
                    Debug.Log($"Active Client '{targetClient.clientName}' Requires: [{requiredCompsStr}]");
                }
                else {
                    Debug.Log($"Active Client '{targetClient.clientName}' has no specific requirements listed.");
                }

                Debug.Log($"Routing photo to client '{targetClient.clientName}' for checking.");
                targetClient.CheckPhoto(photo);
            }
            else if (targetClient != null && detectedShotType == PortraitShotType.Undefined) {
                Debug.Log($"Photo of client '{targetClient.clientName}' taken, but composition was Undefined.");
            }


            Debug.Log($"------------------------------------------------------------");
        }

        public void HandlePhotoCapturedDirectly(CapturedPhoto photo) {
            HandlePhotoCaptured(photo);
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
            else if (locationIndex >= 0) {
                return $"Location {locationIndex + 1}";
            }
            else {
                return "Invalid Location";
            }
        }

        public string GetShotTypeName() {
            return PhotoCompositionEvaluator.GetShotTypeDisplayName(requiredShotType);
        }
    }
}