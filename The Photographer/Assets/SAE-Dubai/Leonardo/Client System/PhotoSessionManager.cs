using System;
using System.Collections.Generic;
using System.Linq;
using SAE_Dubai.JW;
using UnityEngine;
using SAE_Dubai.Leonardo.CameraSys;
using Random = UnityEngine.Random;

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
        
        [Header("- Travel Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip travelSound;
        [SerializeField, Range(0f, 1f)] private float travelVolume = 0.8f;

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
            //Debug.Log("[PhotoSessionManager] Start: Initializing...");

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
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
            // Validate that the shot type is not Undefined.
            if (session.requiredShotType == PortraitShotType.Undefined) {
                Debug.LogWarning(
                    $"Attempting to add a session with Undefined shot type. Fixing to a random valid type.");

                // Fix the invalid shot type with a random valid one.
                var validShotTypes = System.Enum.GetValues(typeof(PortraitShotType))
                    .Cast<PortraitShotType>()
                    .Where(type => type != PortraitShotType.Undefined)
                    .ToArray();

                session.requiredShotType = validShotTypes[Random.Range(0, validShotTypes.Length)];
            }

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
                
                // Play travel sound when successfully paying for travel
                if (audioSource != null && travelSound != null)
                {
                    audioSource.volume = travelVolume;
                    audioSource.PlayOneShot(travelSound);
                }
                
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
                TutorialManager.Instance?.NotifyFirstJobCompleted();
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

            // ! --- Determine active camera using the reference from the photo data --- (FIX FOR PHOTO COMPOSITION THINGY, DON'T CHANGE!)
            Camera activeCamera = photo.CapturingCamera;

            if (activeCamera == null) {
                // ? If the capturing camera wasn't set in the photo object, maybe fallback or log error
                Debug.LogError(
                    "[PhotoSessionManager] HandlePhotoCaptured: CapturingCamera in photo data is null! Falling back to Camera.main.");
                activeCamera = Camera.main; // Keep fallback for safety, but debug of why null.
                if (activeCamera == null) {
                    Debug.LogError(
                        "[PhotoSessionManager] HandlePhotoCaptured: Fallback Camera.main is also null! Cannot evaluate.");
                    return;
                }
            }

            // !--- Log the camera being used ---
            Debug.Log(
                $"[PhotoSessionManager] Evaluating photo using camera: {activeCamera.name} (GameObject: {activeCamera.gameObject.name})");

            // !--- Identify target client ---
            PortraitShotType detectedShotType = PortraitShotType.Undefined;
            ClientJobController targetClient = null;
            Ray centerRay = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            bool hitAnything = Physics.Raycast(centerRay, out RaycastHit centerHit, 100f, portraitSubjectLayer);
            Transform subjectTransform = null;

            if (hitAnything && centerHit.transform != null) {
                subjectTransform = centerHit.transform;
                targetClient = subjectTransform.GetComponentInParent<ClientJobController>();
            }

            // !Fallback/Verification
            if (targetClient != null) {
                bool clientIsActiveSession =
                    activeSessions.Any(s => s.isClientSpawned && s.ClientReference == targetClient);
                if (!clientIsActiveSession) {
                    targetClient = null;
                }
            }

            // * --- Marker Visibility Check using WorldToScreenPoint (Uses the confirmed activeCamera) ---
            if (targetClient != null) {
                List<Transform> bodyMarkers = targetClient.GetOrderedBodyMarkers();

                // Check visibility and store results directly in the photo object.
                photo.HeadVisible = bodyMarkers.Count > 0 && bodyMarkers[0] != null &&
                                    PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                        bodyMarkers[0].position);

                photo.ChestVisible = bodyMarkers.Count > 1 && bodyMarkers[1] != null &&
                                     PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                         bodyMarkers[1].position);

                photo.HipVisible = bodyMarkers.Count > 2 && bodyMarkers[2] != null &&
                                   PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                       bodyMarkers[2].position);

                photo.KneesVisible = bodyMarkers.Count > 3 && bodyMarkers[3] != null &&
                                     PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                         bodyMarkers[3].position);

                photo.FeetVisible = bodyMarkers.Count > 4 && bodyMarkers[4] != null &&
                                    PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                        bodyMarkers[4].position);

                photo.AboveHeadVisible = targetClient.aboveHeadMarker != null &&
                                         PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                             targetClient.aboveHeadMarker.position);

                photo.BelowFeetVisible = targetClient.belowFeetMarker != null &&
                                         PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                             targetClient.belowFeetMarker.position);

                // Check new facial and shoulder markers
                photo.EyesVisible = targetClient.eyesMarker != null &&
                                    PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                        targetClient.eyesMarker.position);

                photo.MouthVisible = targetClient.mouthMarker != null &&
                                     PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                         targetClient.mouthMarker.position);

                photo.ChinVisible = targetClient.chinMarker != null &&
                                    PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                        targetClient.chinMarker.position);

                photo.ShoulderLVisible = targetClient.shoulderLMarker != null &&
                                         PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                             targetClient.shoulderLMarker.position);

                photo.ShoulderRVisible = targetClient.shoulderRMarker != null &&
                                         PhotoCompositionEvaluator.IsMarkerVisibleOnScreen(activeCamera,
                                             targetClient.shoulderRMarker.position);

                detectedShotType = PortraitShotType.Undefined; // * Default.
                bool shouldersVisible = photo.ShoulderLVisible && photo.ShoulderRVisible; 
                
                // ECU
                if (photo.EyesVisible && !photo.HeadVisible) {
                    detectedShotType = PortraitShotType.ExtremeCloseUp;
                }
                // BCU
                else if (photo.HeadVisible && photo.ChinVisible && !photo.ChestVisible && !shouldersVisible) {
                    detectedShotType = PortraitShotType.BigCloseUp;
                }
                // CU
                else if (photo.HeadVisible && photo.ChinVisible && !photo.ChestVisible) {
                    detectedShotType = PortraitShotType.CloseUp;
                }
                // MCU
                else if (photo.HeadVisible && photo.ChestVisible && shouldersVisible && !photo.HipVisible) {
                    detectedShotType = PortraitShotType.MediumCloseUp;
                }
                // MS
                else if (photo.HeadVisible && photo.HipVisible && !photo.KneesVisible) {
                    detectedShotType = PortraitShotType.MidShot;
                }
                // MLS
                else if (photo.HeadVisible && photo.KneesVisible && !photo.FeetVisible) {
                    detectedShotType = PortraitShotType.MediumLongShot;
                }
                // LS, VLS, XLS - Differentiating Long Shots
                else if (photo.HeadVisible && photo.FeetVisible)
                {
                    if (photo.AboveHeadVisible && photo.BelowFeetVisible) {
                        detectedShotType = PortraitShotType.ExtremeLongShot;
                    }
                    else if (photo.AboveHeadVisible) {
                        detectedShotType = PortraitShotType.VeryLongShot;
                    }
                    else {
                        detectedShotType = PortraitShotType.LongShot;
                    }
                }
                else {
                    detectedShotType = PortraitShotType.Undefined;
                }

                Debug.Log(
                    $"[Photo Eval WTS] Result: Head={photo.HeadVisible}, Eyes={photo.EyesVisible}, Mouth={photo.MouthVisible}, Chin={photo.ChinVisible}, Chest={photo.ChestVisible}, Hip={photo.HipVisible}, Knees={photo.KneesVisible}, Feet={photo.FeetVisible}, Above={photo.AboveHeadVisible}, Below={photo.BelowFeetVisible}, ShL={photo.ShoulderLVisible}, ShR={photo.ShoulderRVisible} => Type={detectedShotType}");
            }
            else {
                // !No Client Detected.
                // Debug.Log("[Photo Eval WTS] No client subject detected.");
                photo.HeadVisible = false;
                photo.ChestVisible = false;
                photo.HipVisible = false;
                photo.KneesVisible = false;
                photo.FeetVisible = false;
                photo.AboveHeadVisible = false;
                photo.BelowFeetVisible = false;
                photo.EyesVisible = false;
                photo.MouthVisible = false;
                photo.ChinVisible = false;
                photo.ShoulderLVisible = false;
                photo.ShoulderRVisible = false;
                detectedShotType = PortraitShotType.Undefined;
            }

            photo.portraitShotType = detectedShotType;

            // Final Logging & Client Check.
            string detectedCompStr = PhotoCompositionEvaluator.GetShotTypeDisplayName(detectedShotType);
            Debug.Log($"Determined Composition (WTS): {detectedCompStr}");
            Debug.Log($"Photo Quality: {photo.quality:P0}");

            if (targetClient != null) {
                targetClient.CheckPhoto(photo);
            }

            // Debug.Log($"------------------------------------------------------------");
        }

        public void HandlePhotoCapturedDirectly(CapturedPhoto photo) {
            HandlePhotoCaptured(photo);
        }

        public bool IsLocationOccupied(int locationIndex) {
            return activeSessions.Any(session => session.locationIndex == locationIndex);
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