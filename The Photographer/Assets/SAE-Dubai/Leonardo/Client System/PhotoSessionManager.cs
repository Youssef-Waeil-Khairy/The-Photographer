// In SAE-Dubai/Leonardo/Client System/PhotoSessionManager.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Needed for Linq
using SAE_Dubai.JW;
using UnityEngine;
using SAE_Dubai.Leonardo.CameraSys; // Required
using SAE_Dubai.Leonardo.Client_System; // Required

namespace SAE_Dubai.Leonardo.Client_System
{
    public class PhotoSessionManager : MonoBehaviour
    {
        // ... (Keep Instance, session settings, locations, player object, client refs etc.) ...
        public static PhotoSessionManager Instance { get; private set; }
        public int maxActiveSessions = 3;
        public float travelCost = 25f;
        public List<Transform> photoLocations;
        public List<string> locationNames;
        public GameObject playerObject;
        public GameObject clientPrefab;
        public List<ClientData> clientArchetypes;

        [Header("- Photo Evaluation Settings (Marker Based)")]
        // Keep portraitSubjectLayer for identifying clients
        [SerializeField] private LayerMask portraitSubjectLayer = default;
        // NEW: LayerMask for objects that block line of sight to markers
        [SerializeField] private LayerMask occlusionCheckMask = default; // Assign Environment/Obstacle layers
        [SerializeField] private bool drawVisibilityDebugLines = false; // Toggle debug lines
        [SerializeField] private string cameraManagerTag = "CameraManager";

        // Remove old evaluator fields if they existed: maxDetectionDistance, detectionRadius
        // Keep showEvaluatorDebugInfo if used elsewhere, or remove

        private CameraManager cameraManager;
        private CameraSystem currentCameraSystem;
        private List<PhotoSession> activeSessions = new();
        public event Action OnSessionsChanged;
        private bool isTraveling = false;

        // ... (Keep Awake, Start, Update, OnDestroy, Session Mgmt, Travel, Client Spawning/Handling Methods AS MODIFIED PREVIOUSLY) ...
         private void Awake() { /* ... Find Camera Manager ... */ if (Instance == null) { Instance = this; } else { Destroy(gameObject); return; } Debug.Log("[PhotoSessionManager] Awake: Finding CameraManager..."); cameraManager = GameObject.FindWithTag(cameraManagerTag)?.GetComponent<CameraManager>(); if (cameraManager == null) { Debug.LogError($"[PhotoSessionManager] Awake: CameraManager with tag '{cameraManagerTag}' not found!", this); } }
         private void Start() { /* ... Get CameraManager Instance, SubscribeToActiveCamera, Init locations, Validate fields... */ Debug.Log("[PhotoSessionManager] Start: Initializing..."); FindAnyObjectByType<ComputerUI>(); cameraManager = CameraManager.Instance; if (cameraManager == null) { Debug.LogError("[PhotoSessionManager] Start: CameraManager.Instance is null!", this); this.enabled = false; return; } else { Debug.Log("[PhotoSessionManager] Start: Successfully found CameraManager instance."); SubscribeToActiveCamera(); } if (locationNames == null || locationNames.Count != photoLocations.Count) { Debug.LogWarning("..."); locationNames = new List<string>(); for (int i = 0; i < photoLocations.Count; i++) { locationNames.Add((photoLocations[i] != null) ? photoLocations[i].gameObject.name : $"Location {i + 1}"); } } if (portraitSubjectLayer == 0) Debug.LogWarning("[PhotoSessionManager] Portrait Subject Layer not assigned.", this); if (occlusionCheckMask == 0) Debug.LogWarning("[PhotoSessionManager] Occlusion Check Mask not assigned.", this); if (clientPrefab == null) Debug.LogError("[PhotoSessionManager] Client Prefab not assigned!", this); /*...*/ }
         private void Update() { if (cameraManager != null && (currentCameraSystem == null || (currentCameraSystem != null && !currentCameraSystem.gameObject.activeInHierarchy))) { SubscribeToActiveCamera(); } }
         void OnDestroy() { if (currentCameraSystem != null) { Debug.Log($"[PhotoSessionManager] OnDestroy: Unsubscribing from {currentCameraSystem?.name ?? "NULL"}"); try { currentCameraSystem.OnPhotoCapture -= HandlePhotoCaptured; } catch {} } }
         public bool CanAddNewSession() { /*...*/ return activeSessions.Count < maxActiveSessions; }
         public void AddNewSession(PhotoSession session) { /*...*/ if (CanAddNewSession()) { activeSessions.Add(session); OnSessionsChanged?.Invoke(); Debug.Log($"[PhotoSessionManager] Added new session for {session.clientName}"); } else { Debug.LogWarning($"..."); } }
         public void RemoveSession(PhotoSession session) { /*...*/ if (activeSessions.Contains(session)) { activeSessions.Remove(session); OnSessionsChanged?.Invoke(); Debug.Log($"[PhotoSessionManager] Removed session for {session.clientName}"); } }
         public List<PhotoSession> GetActiveSessions() { /*...*/ return activeSessions; }
         public void RequestTravelToLocation(int locationIndex) { /* ... includes cost check, calls fader.StartFadeOut(onComplete: () => TeleportAndFadeIn(locationIndex)); ... */ if (isTraveling) return; if (locationIndex < 0 || locationIndex >= photoLocations.Count || photoLocations[locationIndex] == null) return; if (TravelCostManager.Instance != null && TravelCostManager.Instance.AttemptTravelPayment((int)travelCost)) { isTraveling = true; ScreenFader fader = ScreenFader.Instance; if (fader != null) fader.StartFadeOut(onComplete: () => TeleportAndFadeIn(locationIndex)); else { PerformTeleportationAndSpawning(locationIndex); isTraveling = false; } } }
         private void TeleportAndFadeIn(int locationIndex) { /* ... calls PerformTeleportationAndSpawning, then fader.StartFadeIn(...) ... */ PerformTeleportationAndSpawning(locationIndex); ScreenFader fader = ScreenFader.Instance; if (fader != null) fader.StartFadeIn(onComplete: () => isTraveling = false); else isTraveling = false; }
         private void PerformTeleportationAndSpawning(int locationIndex) { /* ... actual teleport and SpawnClientForSession call ... */ if (locationIndex < 0 || locationIndex >= photoLocations.Count || photoLocations[locationIndex] == null) return; Transform locationTransform = photoLocations[locationIndex]; Transform playerSpawnPoint = locationTransform.Find("PlayerSpawnLocation"); Vector3 targetPosition = playerSpawnPoint != null ? playerSpawnPoint.position : locationTransform.position; Quaternion targetRotation = playerSpawnPoint != null ? playerSpawnPoint.rotation : locationTransform.rotation; if (playerObject != null) { CharacterController cc = playerObject.GetComponent<CharacterController>(); if (cc != null) { cc.enabled = false; playerObject.transform.position = targetPosition; playerObject.transform.rotation = targetRotation; cc.enabled = true; } else { Rigidbody rb = playerObject.GetComponent<Rigidbody>(); if (rb != null && !rb.isKinematic) { rb.position = targetPosition; rb.rotation = targetRotation; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; } else { playerObject.transform.position = targetPosition; playerObject.transform.rotation = targetRotation; } } } PhotoSession sessionToSpawn = null; foreach (var session in activeSessions) { if (session.locationIndex == locationIndex && !session.isClientSpawned) { sessionToSpawn = session; break; } } if (sessionToSpawn != null) SpawnClientForSession(sessionToSpawn); }
         private void SpawnClientForSession(PhotoSession session) { /* ... Instantiates client, calls SetupJob, subscribes to OnJobCompleted ... */ if (session == null || session.isClientSpawned || clientPrefab==null || clientArchetypes==null || clientArchetypes.Count==0 || session.locationIndex<0 || session.locationIndex>=photoLocations.Count || photoLocations[session.locationIndex]==null) return; Transform locationParent = photoLocations[session.locationIndex]; Transform clientSpawnTransform = locationParent.Find("ClientSpawnLocation") ?? locationParent; List<PortraitShotType> requirements = new List<PortraitShotType> { session.requiredShotType }; ClientData clientArchetype = clientArchetypes[UnityEngine.Random.Range(0, clientArchetypes.Count)]; GameObject clientObject = Instantiate(clientPrefab, clientSpawnTransform.position, clientSpawnTransform.rotation); ClientJobController clientController = clientObject.GetComponent<ClientJobController>(); if (clientController != null) { clientController.SetupJob(clientArchetype, requirements, (int)session.reward); clientController.OnJobCompleted += (completedClient) => HandleClientJobCompleted(session); session.isClientSpawned = true; session.ClientReference = clientController; OnSessionsChanged?.Invoke(); } else { Destroy(clientObject); } }
         private void HandleClientJobCompleted(PhotoSession session) { /* ... Removes session, invokes OnSessionsChanged ... */ if (session != null) { session.IsCompleted = true; RemoveSession(session); } }


        private void SubscribeToActiveCamera() { /* ... Unsubscribe/Resubscribe logic ... */ Debug.Log("[PhotoSessionManager] Attempting to subscribe to camera event..."); if (cameraManager == null) return; CameraSystem newActiveCamera = cameraManager.GetActiveCamera(); if (newActiveCamera != currentCameraSystem) { if (currentCameraSystem != null) { try { currentCameraSystem.OnPhotoCapture -= HandlePhotoCaptured; Debug.Log($"[PhotoSessionManager] Unsubscribed from previous camera: {currentCameraSystem.name}"); } catch {} } currentCameraSystem = newActiveCamera; if (currentCameraSystem != null) { Debug.Log($"[PhotoSessionManager] Subscribing to OnPhotoCapture event on: {currentCameraSystem.name}"); currentCameraSystem.OnPhotoCapture += HandlePhotoCaptured; } else { Debug.LogWarning("[PhotoSessionManager] SubscribeToActiveCamera: No active camera found."); } } }


        // --- REVISED Photo Capture Event Handler ---
        private void HandlePhotoCaptured(CapturedPhoto photo)
        {
             if (photo == null) { Debug.LogError("[PhotoSessionManager] HandlePhotoCaptured received a null photo object!"); return; }

             Debug.LogWarning(">>> [PhotoSessionManager] HandlePhotoCaptured method EXECUTED! <<<");
             Debug.Log($"--- Photo Captured Debug Info (Marker Based) ---");

             // 1. Get active camera
             Camera activeCamera = null;
             if (currentCameraSystem != null && currentCameraSystem.isCameraOn) {
                 activeCamera = currentCameraSystem.usingViewfinder ? currentCameraSystem.viewfinderCamera : currentCameraSystem.cameraRenderer;
             }
             activeCamera ??= Camera.main;

             if (activeCamera == null) {
                 Debug.LogError("[PhotoSessionManager] HandlePhotoCaptured: Could not find any active camera for evaluation.");
                 Debug.Log($"Photo Quality: {photo.quality:P0}"); // Still log quality
                 Debug.Log($"------------------------------------------------------------");
                 return;
             }

             // --- New Evaluation Logic ---
             PortraitShotType detectedShotType = PortraitShotType.Undefined; // Default to Undefined
             ClientJobController targetClient = null; // The client whose markers we checked

             // Find the potential subject using a simple raycast (or spherecast) first
             // This determines *who* we are looking at before checking markers
             Ray centerRay = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
             Transform subjectTransform = null;
             if(Physics.Raycast(centerRay, out RaycastHit centerHit, 100f, portraitSubjectLayer)) // Use appropriate distance
             {
                  subjectTransform = centerHit.transform;
                  targetClient = subjectTransform.GetComponent<ClientJobController>();
                  Debug.Log($"[Photo Eval] Initial subject check hit: {subjectTransform.name}");
             } else {
                   Debug.Log("[Photo Eval] Initial subject check hit nothing on subject layer.");
             }


             // Only proceed with marker check if we hit a Client
             if (targetClient != null)
             {
                  // Check if this client belongs to an active session
                  bool clientIsActiveSession = activeSessions.Any(s => s.isClientSpawned && s.ClientReference == targetClient);
                  if(!clientIsActiveSession) {
                       Debug.Log($"[Photo Eval] Subject {targetClient.name} is a client but not part of an active session.");
                       targetClient = null; // Don't process markers for non-session clients
                  }
             }


             if (targetClient != null)
             {
                  Debug.Log($"[Photo Eval] Checking markers for client: {targetClient.clientName}");
                  int subjectLayer = targetClient.gameObject.layer; // Get the client's own layer
                  List<Transform> bodyMarkers = targetClient.GetOrderedBodyMarkers();
                  int highestVisibleIndex = -1; // Index in the ordered list
                  int lowestVisibleIndex = -1; // Index in the ordered list
                  bool aboveHeadVisible = false;
                  bool belowFeetVisible = false;

                  // Check standard body markers
                  for(int i = 0; i < bodyMarkers.Count; i++)
                  {
                       if (bodyMarkers[i] != null && PhotoCompositionEvaluator.IsMarkerVisible(activeCamera, bodyMarkers[i].position, occlusionCheckMask, subjectLayer, drawVisibilityDebugLines))
                       {
                           Debug.Log($"[Photo Eval] Visible Marker: {bodyMarkers[i].name}");
                           if (highestVisibleIndex == -1) { highestVisibleIndex = i; } // First visible marker from top
                           lowestVisibleIndex = i; // Last visible marker found
                       }
                  }

                 // Check extra-wide markers
                  if (targetClient.aboveHeadMarker != null && PhotoCompositionEvaluator.IsMarkerVisible(activeCamera, targetClient.aboveHeadMarker.position, occlusionCheckMask, subjectLayer, drawVisibilityDebugLines)) {
                       aboveHeadVisible = true; Debug.Log($"[Photo Eval] Visible Marker: Above Head");
                  }
                   if (targetClient.belowFeetMarker != null && PhotoCompositionEvaluator.IsMarkerVisible(activeCamera, targetClient.belowFeetMarker.position, occlusionCheckMask, subjectLayer, drawVisibilityDebugLines)) {
                       belowFeetVisible = true; Debug.Log($"[Photo Eval] Visible Marker: Below Feet");
                  }

                  // --- Determine Composition based on visible markers ---
                  // This is where you define your rules! Example rules:
                  if (highestVisibleIndex != -1) // At least one standard marker was visible
                  {
                       // Index mapping (based on GetOrderedBodyMarkers): 0:Head, 1:Chest, 2:Hip, 3:Knees, 4:Feet
                       bool headVisible = highestVisibleIndex <= 0; // Head or higher
                       bool chestVisible = lowestVisibleIndex >= 1;
                       bool hipVisible = lowestVisibleIndex >= 2;
                       bool kneesVisible = lowestVisibleIndex >= 3;
                       bool feetVisible = lowestVisibleIndex >= 4;

                       if (aboveHeadVisible || belowFeetVisible || (headVisible && feetVisible)) // Full body + surroundings
                       {
                           detectedShotType = (aboveHeadVisible && belowFeetVisible) ? PortraitShotType.ExtremeWide : PortraitShotType.Wide;
                       }
                       else if (headVisible && kneesVisible) // Head to at least knees
                       {
                           detectedShotType = PortraitShotType.MediumWide;
                       }
                       else if (headVisible && hipVisible) // Head to at least hip
                       {
                            detectedShotType = PortraitShotType.Medium;
                       }
                       else if (headVisible && chestVisible) // Head to at least chest
                       {
                           detectedShotType = PortraitShotType.MediumCloseUp;
                       }
                       else if (headVisible) // Only head (and maybe neck/shoulders implicitly)
                       {
                           // Differentiate CloseUp vs Extreme based on *only* head vs head+chest maybe? Needs tuning.
                           // Simplification: if highest is head and lowest is head -> ECU/CU
                           if (lowestVisibleIndex == 0)
                           {
                                detectedShotType = PortraitShotType.CloseUp; // Could refine to ECU
                           } else {
                               // This case should be caught by chestVisible? Revisit logic if needed.
                                detectedShotType = PortraitShotType.MediumCloseUp;
                           }
                       }
                       // Add more specific rules if needed (e.g., for ECU based on single head marker)
                  }
                  else if (aboveHeadVisible || belowFeetVisible)
                  {
                       // Only saw above/below markers, likely very wide
                       detectedShotType = PortraitShotType.ExtremeWide;
                  }
                  // If highestVisibleIndex remains -1, type remains Undefined

                  Debug.Log($"[Photo Eval] Result: Highest Visible Index={highestVisibleIndex}, Lowest Visible Index={lowestVisibleIndex}, Above={aboveHeadVisible}, Below={belowFeetVisible}");
             }
             else
             {
                  Debug.Log("[Photo Eval] No client subject detected by initial raycast.");
             }

             // --- End New Evaluation Logic ---


             // 3. Tag the photo object
             photo.portraitShotType = detectedShotType; // Assign determined type

             // --- Debug Logging ---
             string detectedCompStr = PhotoCompositionEvaluator.GetShotTypeDisplayName(detectedShotType);
             Debug.Log($"Determined Composition: {detectedCompStr}");
             Debug.Log($"Photo Quality: {photo.quality:P0}");

             // 4. Route to the client IF one was identified and the shot type is valid
             if (targetClient != null && detectedShotType != PortraitShotType.Undefined)
             {
                 // Log Client Requirement
                 if (targetClient.requiredShotTypes != null && targetClient.requiredShotTypes.Count > 0) {
                     string requiredCompsStr = string.Join(", ", targetClient.requiredShotTypes.Select(PhotoCompositionEvaluator.GetShotTypeDisplayName));
                     Debug.Log($"Active Client '{targetClient.clientName}' Requires: [{requiredCompsStr}]");
                 } else { Debug.Log($"Active Client '{targetClient.clientName}' has no specific requirements listed."); }

                 // 5. Route the evaluated photo
                 Debug.Log($"Routing photo to client '{targetClient.clientName}' for checking.");
                 targetClient.CheckPhoto(photo);
             }
             else if (targetClient != null && detectedShotType == PortraitShotType.Undefined)
             {
                 Debug.Log($"Photo of client '{targetClient.clientName}' taken, but composition was Undefined.");
             }


             Debug.Log($"------------------------------------------------------------");
        }

    }
    

    [Serializable]
    public class PhotoSession { /* ... */
         public string clientName;
        public PortraitShotType requiredShotType = PortraitShotType.Undefined;
        public int locationIndex = -1;
        public float reward;
        [NonSerialized] public bool isClientSpawned = false;
        [NonSerialized] public bool IsCompleted = false;
        [NonSerialized] public ClientJobController ClientReference = null;
        public string GetLocationName() { if (PhotoSessionManager.Instance != null && PhotoSessionManager.Instance.locationNames != null && locationIndex >= 0 && locationIndex < PhotoSessionManager.Instance.locationNames.Count) { return PhotoSessionManager.Instance.locationNames[locationIndex]; } else if (locationIndex >= 0) { return $"Location {locationIndex + 1}"; } else { return "Invalid Location"; } }
        public string GetShotTypeName() { return PhotoCompositionEvaluator.GetShotTypeDisplayName(requiredShotType); }
     }

}