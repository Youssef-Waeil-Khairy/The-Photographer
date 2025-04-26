using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SAE_Dubai.Leonardo.CameraSys; // Ensure CameraSys namespace is included
using SAE_Dubai.Leonardo.Client_System; // Ensure Client_System namespace is included

public class PhotoCompositionDebugPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI detectedShotText;
    public TextMeshProUGUI markerVisibilityText;
    public TextMeshProUGUI cameraInfoText;
    public Image panelBackground;
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public Color successColor = new Color(0.2f, 0.8f, 0.2f);
    public Color failureColor = new Color(0.8f, 0.2f, 0.2f);
    public float displayDuration = 5f;
    public KeyCode toggleDebugPanelKey = KeyCode.F3;
    [Tooltip("How often (in seconds) to check for active camera changes")]
    public float cameraCheckInterval = 0.5f; // Check twice per second

    private float _displayTimer;
    private bool _isDisplaying;
    private CameraSystem _subscribedCameraSystem = null; // Track the current camera
    private float _cameraCheckTimer = 0f;

    private void Start()
    {
        // Make sure the canvas group is assigned
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Start invisible but active
        canvasGroup.alpha = 0;
        _isDisplaying = false; // Ensure it starts hidden

        // Set initial texts
        if (titleText) titleText.text = "No Photo Taken";
        if (detectedShotText) detectedShotText.text = "Waiting for Camera...";
        if (markerVisibilityText) markerVisibilityText.text = "No marker data";
        if (cameraInfoText) cameraInfoText.text = "No camera data";

        // Initial check for camera
        CheckForActiveCameraChange();
    }

    private void OnDestroy()
    {
        // IMPORTANT: Unsubscribe when the debug panel is destroyed
        if (_subscribedCameraSystem != null)
        {
            try {
                 _subscribedCameraSystem.OnPhotoCapture -= HandlePhotoCapture;
            } catch (System.Exception ex) {
                 Debug.LogWarning($"Error unsubscribing from camera on destroy: {ex.Message}");
            }
            _subscribedCameraSystem = null;
        }
    }

    private void Update()
    {
        // Handle display timer for the panel visibility
        if (_isDisplaying)
        {
            _displayTimer -= Time.deltaTime;
            if (_displayTimer <= 0)
            {
                _isDisplaying = false;
                canvasGroup.alpha = 0; // Hide the panel
            }
        }

        // Toggle debug panel visibility with key press
        if (Input.GetKeyDown(toggleDebugPanelKey))
        {
            if (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha = 0; // Hide
                _isDisplaying = false;
            }
            else
            {
                canvasGroup.alpha = 1; // Show immediately when toggled on
                // Optionally reset timer or keep it running from HandlePhotoCapture
                // If you want it to show for 'displayDuration' after toggle:
                // _isDisplaying = true;
                // _displayTimer = displayDuration;
            }
        }

        // --- Optimized Camera Subscription Check ---
        _cameraCheckTimer -= Time.deltaTime;
        if (_cameraCheckTimer <= 0f)
        {
            CheckForActiveCameraChange();
            _cameraCheckTimer = cameraCheckInterval; // Reset timer
        }
    }

    private void CheckForActiveCameraChange()
    {
        CameraSystem currentActiveCamera = CameraManager.Instance?.GetActiveCamera();

        // Check if the active camera has changed
        if (currentActiveCamera != _subscribedCameraSystem)
        {
            // Unsubscribe from the previous camera, if any
            if (_subscribedCameraSystem != null)
            {
                 try {
                     _subscribedCameraSystem.OnPhotoCapture -= HandlePhotoCapture;
                     Debug.Log($"DebugPanel: Unsubscribed from {_subscribedCameraSystem.name}");
                 } catch (System.Exception ex) {
                     Debug.LogWarning($"Error unsubscribing from old camera: {ex.Message}");
                 }
            }

            // Subscribe to the new camera, if it exists
            _subscribedCameraSystem = currentActiveCamera;
            if (_subscribedCameraSystem != null)
            {
                 try {
                    _subscribedCameraSystem.OnPhotoCapture += HandlePhotoCapture;
                    Debug.Log($"DebugPanel: Subscribed to {_subscribedCameraSystem.name}");
                    if (detectedShotText) detectedShotText.text = "Take a photo to see composition details"; // Update status
                 } catch (System.Exception ex) {
                     Debug.LogError($"Error subscribing to new camera: {ex.Message}");
                 }
            }
            else
            {
                // No active camera currently
                 if (detectedShotText) detectedShotText.text = "Waiting for Camera...";
            }
        }
    }


    private void HandlePhotoCapture(CapturedPhoto photo) // This is called by the event subscription
    {
        if (photo == null)
        {
            Debug.LogWarning("DebugPanel: HandlePhotoCapture received null photo.");
            return;
        }
        DisplayDebugInfoFromPhoto(photo);
    }
    
        private void DisplayDebugInfoFromPhoto(CapturedPhoto photo)
    {
         // No need for yield return anymore as data is ready

        // --- Get Client Info (Still need to find the relevant client) ---
        ClientJobController targetClient = FindTargetClient(photo); // You'll need a helper method for this
        string clientName = "Unknown Client";
        string requiredShotType = "N/A";
        bool isSuccess = false;

        if (targetClient != null)
        {
            clientName = targetClient.clientName;
             if (targetClient.requiredShotTypes != null && targetClient.requiredShotTypes.Count > 0)
             {
                requiredShotType = string.Join(", ",
                                              targetClient.requiredShotTypes.ConvertAll(
                                                  type => PhotoCompositionEvaluator.GetShotTypeDisplayName(type)));
             }
             isSuccess = photo.portraitShotType.HasValue &&
                         targetClient.requiredShotTypes.Contains(photo.portraitShotType.Value);
        }

        // --- Build Marker Info String from Stored Data ---
        string markerInfo = "";
        // Define the order and names for display
        var markerDisplayOrder = new Dictionary<string, bool> {
            {"Head", photo.HeadVisible},
            {"Chest", photo.ChestVisible},
            {"Hip", photo.HipVisible},
            {"Knees", photo.KneesVisible},
            {"Feet", photo.FeetVisible},
            {"Above Head", photo.AboveHeadVisible},
            {"Below Feet", photo.BelowFeetVisible}
        };

        foreach (var kvp in markerDisplayOrder)
        {
            markerInfo += kvp.Key + ": " + (kvp.Value ?
                "<color=#00FF00>VISIBLE</color>" : "<color=#FF0000>NOT VISIBLE</color>") + "\n";
        }

        // --- Get Detected Shot Type Name ---
        string shotTypeText = "Undefined";
        if (photo.portraitShotType.HasValue)
        {
            shotTypeText = PhotoCompositionEvaluator.GetShotTypeDisplayName(photo.portraitShotType.Value);
        }

        // --- Get Camera Info (Still need to retrieve this) ---
        string cameraInfo = GetCameraInfoString(); // Use a helper method

        // --- Display the Info ---
        string title = $"Client: {clientName}";
        string shotText = $"Detected Shot: {shotTypeText}\nRequired Shot(s): {requiredShotType}";

        DisplayInfo(title, shotText, markerInfo, cameraInfo, isSuccess);
    }

     // Helper method to find the client (similar logic to before)
     private ClientJobController FindTargetClient(CapturedPhoto photo)
     {
         // You might need the camera that took the photo if raycasting is still desired
         // For simplicity, let's assume we find the client linked to the photo somehow,
         // or perhaps find the one that PhotoSessionManager likely targeted.
         // This part might need refinement depending on your exact game logic.

         // Example: Find based on active sessions (if possible)
         PhotoSessionManager sessionManager = PhotoSessionManager.Instance;
         if(sessionManager != null) {
             foreach(var session in sessionManager.GetActiveSessions()) {
                 if(session.ClientReference != null && session.ClientReference.isJobActive) {
                     // Maybe check if this photo matches the requirements? Difficult.
                     // Simplest for now: find *any* active client, assuming only one is usually framed.
                     // Or, pass the client reference along with the photo event if possible.
                     // Fallback: Find closest active client to camera.
                     Camera currentCam = CameraManager.Instance?.GetActiveCamera()?.cameraRenderer ?? Camera.main;
                     if (currentCam != null) {
                        ClientJobController[] clients = FindObjectsOfType<ClientJobController>();
                        float closestDist = float.MaxValue;
                        ClientJobController closestClient = null;
                         foreach (var client in clients)
                         {
                            if (!client.isJobActive) continue;
                             float dist = Vector3.Distance(currentCam.transform.position, client.transform.position);
                             if (dist < closestDist)
                             {
                                 closestDist = dist;
                                 closestClient = client;
                             }
                         }
                         return closestClient; // Return the closest active client
                     }
                 }
             }
         }
         return null; // No suitable client found
     }

    // Helper method to get camera info string (similar logic to before)
    private string GetCameraInfoString()
    {
        Camera activeCamera = null;
        string cameraInfo = "No camera system active or camera off";
        CameraSystem camSys = _subscribedCameraSystem; // Use the subscribed one

        if (camSys != null && camSys.isCameraOn)
        {
             activeCamera = camSys.usingViewfinder ? camSys.viewfinderCamera : camSys.cameraRenderer;
             cameraInfo = $"Camera: {camSys.name} ({ (camSys.usingViewfinder ? "Viewfinder" : "Screen") })\nGameObj: {activeCamera?.name ?? "N/A"}";
             cameraInfo += $"\nPosition: {activeCamera.transform.position.ToString("F2")}";
             cameraInfo += $"\nForward: {activeCamera.transform.forward.ToString("F2")}";
             cameraInfo += $"\nFOV: {activeCamera.fieldOfView:F1}°";
        }
         else
         {
             // Fallback maybe?
             activeCamera = Camera.main;
             if (activeCamera != null) cameraInfo = $"Camera: Main Camera\nGameObj: {activeCamera.name}";
         }
         // Could add distance to target client here if FindTargetClient returns one
        return cameraInfo;
    }

    // --- Keep your existing Coroutine and Display methods ---
    private System.Collections.IEnumerator GatherAndDisplayPhotoInfo(CapturedPhoto photo)
    {
        // Wait a small delay to allow the PhotoSessionManager to process the photo
        yield return new WaitForSeconds(0.1f);

        // Try to find which client was captured
        ClientJobController[] clients = FindObjectsOfType<ClientJobController>();

        if (clients.Length == 0)
        {
            DisplayInfo("No clients found in scene", "Unable to determine shot type", "No markers to check", "No camera info");
            yield break;
        }

        // Get the active camera (use the one we are subscribed to for consistency)
        Camera activeCamera = null;
        string cameraInfo = "No camera system active or camera off";

        if (_subscribedCameraSystem != null && _subscribedCameraSystem.isCameraOn)
        {
             activeCamera = _subscribedCameraSystem.usingViewfinder ? _subscribedCameraSystem.viewfinderCamera : _subscribedCameraSystem.cameraRenderer;
             cameraInfo = $"Camera: {_subscribedCameraSystem.name} ({(_subscribedCameraSystem.usingViewfinder ? "Viewfinder" : "Screen")})\nGameObj: {activeCamera?.name ?? "N/A"}";
        }
        else if (CameraManager.Instance?.GetActiveCamera() != null) {
             // Fallback if subscribedCameraSystem is null but CameraManager has one (edge case)
             CameraSystem tempCamSys = CameraManager.Instance.GetActiveCamera();
             if (tempCamSys.isCameraOn) {
                 activeCamera = tempCamSys.usingViewfinder ? tempCamSys.viewfinderCamera : tempCamSys.cameraRenderer;
                 cameraInfo = $"Camera: {tempCamSys.name} ({ (tempCamSys.usingViewfinder ? "Viewfinder" : "Screen") })\nGameObj: {activeCamera?.name ?? "N/A"}";
             }
        }
         else {
             // Absolute fallback
             activeCamera = Camera.main;
             if (activeCamera != null) cameraInfo = $"Camera: Main Camera\nGameObj: {activeCamera.name}";
         }


        if (activeCamera == null)
        {
            DisplayInfo("No usable camera found", "Unable to determine shot type", "No visible markers", cameraInfo);
            yield break;
        }

        // Add camera position and rotation info
        cameraInfo += $"\nPosition: {activeCamera.transform.position.ToString("F2")}";
        cameraInfo += $"\nForward: {activeCamera.transform.forward.ToString("F2")}";
        cameraInfo += $"\nFOV: {activeCamera.fieldOfView:F1}°";


        // Cast ray to detect which client we're looking at
        Ray centerRay = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        int portraitLayer = LayerMask.NameToLayer("PortraitSubject");
        LayerMask portraitLayerMask = (portraitLayer != -1) ? 1 << portraitLayer : 0;

        ClientJobController targetClient = null;
        if (portraitLayerMask != 0 && Physics.Raycast(centerRay, out RaycastHit hit, 100f, portraitLayerMask))
        {
            targetClient = hit.transform.GetComponentInParent<ClientJobController>(); // Use GetComponentInParent for flexibility
        }

        // Fallback: Find closest client if no direct hit or layer issue
        if (targetClient == null)
        {
            float closestDistanceSqr = float.MaxValue;
            Vector3 cameraPos = activeCamera.transform.position;
            foreach (var client in clients)
            {
                if (!client.isJobActive) continue; // Skip inactive clients
                float distanceSqr = (client.transform.position - cameraPos).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    targetClient = client;
                }
            }
            // Optional: Add a max distance check for this fallback
             // if (closestDistanceSqr > someMaxDistance * someMaxDistance) targetClient = null;
        }

        if (targetClient == null)
        {
            DisplayInfo("No active client detected", "Unable to determine shot type", "No visible markers", cameraInfo);
            yield break;
        }

        // Add distance to client info
        float clientDistance = Vector3.Distance(activeCamera.transform.position, targetClient.transform.position);
        cameraInfo += $"\nDistance to Client: {clientDistance:F2}m";

        // Now check which markers are visible for this client
        string markerInfo = "";
        Dictionary<string, bool> markerVisibility = new Dictionary<string, bool>();

        List<Transform> bodyMarkers = targetClient.GetOrderedBodyMarkers();
        int subjectLayer = targetClient.gameObject.layer; // Get the actual subject layer
        LayerMask occlusionMask = PhotoSessionManager.Instance?.occlusionCheckMask ?? 0; // Use the manager's mask

        for (int i = 0; i < bodyMarkers.Count; i++)
        {
            if (bodyMarkers[i] != null)
            {
                bool isVisible = PhotoCompositionEvaluator.IsMarkerVisible(
                    activeCamera, bodyMarkers[i].position, occlusionMask, subjectLayer, false); // Pass actual subject layer

                string markerName = GetMarkerName(i, bodyMarkers[i].name);
                markerVisibility[markerName] = isVisible;
            }
        }

        // Check special markers
        if (targetClient.aboveHeadMarker != null)
        {
            bool isVisible = PhotoCompositionEvaluator.IsMarkerVisible(
                activeCamera, targetClient.aboveHeadMarker.position, occlusionMask, subjectLayer, false);
            markerVisibility["Above Head"] = isVisible;
        }

        if (targetClient.belowFeetMarker != null)
        {
            bool isVisible = PhotoCompositionEvaluator.IsMarkerVisible(
                activeCamera, targetClient.belowFeetMarker.position, occlusionMask, subjectLayer, false);
            markerVisibility["Below Feet"] = isVisible;
        }

        // Build marker visibility text
        foreach (var marker in markerVisibility)
        {
            markerInfo += marker.Key + ": " + (marker.Value ?
                "<color=#00FF00>VISIBLE</color>" : "<color=#FF0000>NOT VISIBLE</color>") + "\n";
        }

        // Determine the shot type
        string shotTypeText = "Unknown";
        if (photo.portraitShotType.HasValue)
        {
            shotTypeText = PhotoCompositionEvaluator.GetShotTypeDisplayName(photo.portraitShotType.Value);
        }

        // Show required shot type
        string requiredShotType = "N/A";
        if (targetClient.requiredShotTypes != null && targetClient.requiredShotTypes.Count > 0)
        {
            requiredShotType = string.Join(", ",
                                          targetClient.requiredShotTypes.ConvertAll(
                                              type => PhotoCompositionEvaluator.GetShotTypeDisplayName(type)));
        }

        bool isMatchingShot = photo.portraitShotType.HasValue &&
                             targetClient.requiredShotTypes.Contains(photo.portraitShotType.Value);

        // Display the gathered information
        string title = $"Client: {targetClient.clientName}";
        string shotText = $"Detected Shot: {shotTypeText}\nRequired Shot(s): {requiredShotType}";

        DisplayInfo(title, shotText, markerInfo, cameraInfo, isMatchingShot);
    }

    private string GetMarkerName(int index, string defaultName)
    {
        // Convert index to named marker for clarity
        switch (index)
        {
            case 0: return "Head";
            case 1: return "Chest";
            case 2: return "Hip";
            case 3: return "Knees";
            case 4: return "Feet";
            default: return defaultName; // Fallback to the transform name
        }
    }

    private void DisplayInfo(string title, string shotText, string markerInfo, string cameraInfo, bool isSuccess = false)
    {
        // Update UI elements
        if (titleText) titleText.text = title;
        if (detectedShotText) detectedShotText.text = shotText;
        if (markerVisibilityText) markerVisibilityText.text = markerInfo;
        if (cameraInfoText) cameraInfoText.text = cameraInfo;

        // Set panel color based on success
        if (panelBackground)
        {
            Color color = isSuccess ? successColor : failureColor;
            color.a = panelBackground.color.a; // Preserve original alpha or set explicitly
            panelBackground.color = color;
        }

        // Show panel and start timer ONLY if the panel isn't already visible via toggle key
        if (canvasGroup.alpha == 0) { // Only auto-show if hidden
             canvasGroup.alpha = 1;
        }
        _isDisplaying = true; // Keep track that we want it visible
        _displayTimer = displayDuration; // Reset timer every time info is displayed
    }
}