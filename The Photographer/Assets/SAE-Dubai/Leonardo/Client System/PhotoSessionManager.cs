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

        [SerializeField] public LayerMask occlusionCheckMask;
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
                newActiveCamera = FindFirstObjectByType<CameraSystem>();
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
            if (photo == null) {
                Debug.LogError("[PhotoSessionManager] HandlePhotoCaptured received a null photo object!");
                return;
            }

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
                return;
            }

            PortraitShotType detectedShotType = PortraitShotType.Undefined;
            ClientJobController targetClient = null;

            Ray centerRay = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            bool hitAnything = Physics.Raycast(centerRay, out RaycastHit centerHit, 100f, portraitSubjectLayer);
            Transform subjectTransform = null;

            if (hitAnything) {
                subjectTransform = centerHit.transform;
                targetClient = subjectTransform.GetComponentInParent<ClientJobController>();
            }

            if (targetClient != null) {
                bool clientIsActiveSession =
                    activeSessions.Any(s => s.isClientSpawned && s.ClientReference == targetClient);
                if (!clientIsActiveSession) {
                    targetClient = null;
                }
            }


            if (targetClient != null) {
                int subjectLayer = targetClient.gameObject.layer;
                List<Transform> bodyMarkers = targetClient.GetOrderedBodyMarkers();

                bool headVisible = false;
                bool chestVisible = false;
                bool hipVisible = false;
                bool kneesVisible = false;
                bool feetVisible = false;

                if (bodyMarkers.Count > 0 && bodyMarkers[0] != null && PhotoCompositionEvaluator.IsMarkerVisible(
                        activeCamera, bodyMarkers[0].position, occlusionCheckMask, subjectLayer,
                        drawVisibilityDebugLines))
                    headVisible = true;
                if (bodyMarkers.Count > 1 && bodyMarkers[1] != null && PhotoCompositionEvaluator.IsMarkerVisible(
                        activeCamera, bodyMarkers[1].position, occlusionCheckMask, subjectLayer,
                        drawVisibilityDebugLines))
                    chestVisible = true;
                if (bodyMarkers.Count > 2 && bodyMarkers[2] != null && PhotoCompositionEvaluator.IsMarkerVisible(
                        activeCamera, bodyMarkers[2].position, occlusionCheckMask, subjectLayer,
                        drawVisibilityDebugLines))
                    hipVisible = true;
                if (bodyMarkers.Count > 3 && bodyMarkers[3] != null && PhotoCompositionEvaluator.IsMarkerVisible(
                        activeCamera, bodyMarkers[3].position, occlusionCheckMask, subjectLayer,
                        drawVisibilityDebugLines))
                    kneesVisible = true;
                if (bodyMarkers.Count > 4 && bodyMarkers[4] != null && PhotoCompositionEvaluator.IsMarkerVisible(
                        activeCamera, bodyMarkers[4].position, occlusionCheckMask, subjectLayer,
                        drawVisibilityDebugLines))
                    feetVisible = true;

                bool aboveHeadVisible = targetClient.aboveHeadMarker != null &&
                                        PhotoCompositionEvaluator.IsMarkerVisible(activeCamera,
                                            targetClient.aboveHeadMarker.position, occlusionCheckMask, subjectLayer,
                                            drawVisibilityDebugLines);
                bool belowFeetVisible = targetClient.belowFeetMarker != null &&
                                        PhotoCompositionEvaluator.IsMarkerVisible(activeCamera,
                                            targetClient.belowFeetMarker.position, occlusionCheckMask, subjectLayer,
                                            drawVisibilityDebugLines);

                detectedShotType = PortraitShotType.Undefined;
                bool fullBodyVisible = headVisible && feetVisible;

                // ! SHOT: Extreme Wide Shot.
                // ? Condition: Head-to-Feet visible AND Above-Head visible AND Below-Feet visible.
                if (fullBodyVisible && aboveHeadVisible && belowFeetVisible) {
                    detectedShotType = PortraitShotType.ExtremeWide;
                }
                // ! SHOT: Medium Wide Shot (Specific Definition).
                // ? Condition: Head-to-Feet visible AND EITHER Above-Head OR Below-Feet is visible (but NOT both).
                else if (fullBodyVisible &&
                         ((aboveHeadVisible && !belowFeetVisible) || (!aboveHeadVisible && belowFeetVisible))) {
                    detectedShotType = PortraitShotType.MediumWide;
                }
                // ! SHOT: Wide Shot.
                // ? Condition: Head-to-Feet visible, BUT NEITHER Above-Head NOR Below-Feet is visible.
                
                else if (fullBodyVisible && !aboveHeadVisible && !belowFeetVisible) {
                    detectedShotType = PortraitShotType.Wide;
                }
                // ! SHOT: Medium Wide Shot
                // ? Condition: Head visible AND Knees visible (implies feet are likely cut off).
                else if (headVisible && kneesVisible) {
                    detectedShotType = PortraitShotType.MediumWide;
                }
                // ! SHOT : Medium Shot.
                // ? Condition: Head visible AND Hip visible (implies knees/feet are cut off).
                else if (headVisible && hipVisible) {
                    detectedShotType = PortraitShotType.Medium;
                }
                // ! SHOT: Medium Close-Up Shot.
                // ? Condition: Head visible AND Chest visible (implies hip/knees/feet are cut off).
                else if (headVisible && chestVisible) {
                    detectedShotType = PortraitShotType.MediumCloseUp;
                }
                // ! SHOT: Close-Up / Extreme Close-Up Shot
                // ? Condition: Only Head visible (implies chest and lower body are cut off).
                else if (headVisible) {
                    detectedShotType = PortraitShotType.CloseUp;
                }

                Debug.Log(
                    $"[Photo Eval] Result: Head={headVisible}, Chest={chestVisible}, Hip={hipVisible}, Knees={kneesVisible}, Feet={feetVisible}, Above={aboveHeadVisible}, Below={belowFeetVisible} => Type={detectedShotType}");
            }
            else {
                Debug.Log("[Photo Eval] No client subject detected.");
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