// In SAE-Dubai/Leonardo/Client System/PhotoCompositionEvaluator.cs

using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    /// <summary>
    /// Provides static helper methods for photo composition evaluation and display names.
    /// Now includes methods for checking marker visibility.
    /// </summary>
    public static class PhotoCompositionEvaluator
    {
        // --- Keep the display name method ---
        /// <summary>
        /// Gets a friendly display name for the shot type.
        /// </summary>
        public static string GetShotTypeDisplayName(PortraitShotType shotType) {
            switch (shotType) {
                case PortraitShotType.ExtremeCloseUp: return "Extreme Close-Up";
                case PortraitShotType.CloseUp: return "Close-Up";
                case PortraitShotType.MediumCloseUp: return "Medium Close-Up";
                case PortraitShotType.Medium: return "Medium Shot";
                case PortraitShotType.MediumWide: return "Medium-Wide Shot";
                case PortraitShotType.Wide: return "Wide Shot";
                case PortraitShotType.ExtremeWide: return "Extreme Wide Shot";
                case PortraitShotType.Undefined: return "Undefined";
                default: return "Unknown Shot Type";
            }
        }

        // --- NEW: Marker Visibility Check Method ---
        /// <summary>
        /// Checks if a specific world-space point (marker) is visible to the camera,
        /// considering both viewport bounds and occlusion.
        /// </summary>
        /// <param name="cam">The camera performing the check.</param>
        /// <param name="markerPosition">The world position of the marker to check.</param>
        /// <param name="occlusionCheckMask">LayerMask containing layers that can block line of sight.</param>
        /// <param name="subjectLayer">The layer the subject itself is on (to ignore hitting self).</param>
        /// <param name="debugDraw">Should debug lines be drawn?</param>
        /// <returns>True if the marker is within the viewport and not occluded, false otherwise.</returns>
        public static bool IsMarkerVisible(Camera cam, Vector3 markerPosition, LayerMask occlusionCheckMask, int subjectLayer, bool debugDraw = false)
        {
            if (cam == null) return false;

            // 1. Viewport Check
            Vector3 viewportPos = cam.WorldToViewportPoint(markerPosition);
            bool inViewport = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                              viewportPos.y >= 0 && viewportPos.y <= 1 &&
                              viewportPos.z > cam.nearClipPlane; // Check if in front of near plane

            if (!inViewport)
            {
                if(debugDraw) Debug.DrawLine(cam.transform.position, markerPosition, Color.grey, 0.1f);
                return false; // Not within camera's view bounds
            }

            // 2. Occlusion Check (Raycast)
            Vector3 direction = markerPosition - cam.transform.position;
            float distanceToMarker = direction.magnitude;

            // Raycast from camera towards the marker, up to the marker's distance, hitting only occlusion layers
            if (Physics.Raycast(cam.transform.position, direction.normalized, out RaycastHit hit, distanceToMarker, occlusionCheckMask))
            {
                // We hit something on an occluding layer *before* reaching the marker.
                // Check if the hit object is NOT the subject itself (or part of it)
                 if (hit.transform.gameObject.layer != subjectLayer) // Ensure we didn't hit the subject we are checking
                 {
                      if (debugDraw) Debug.DrawLine(cam.transform.position, hit.point, Color.red, 0.1f); // Draw red line to blocker
                      return false; // Occluded
                 }
                 // If we hit the subject layer first, treat it as visible (ray reached subject)

            }

            // If it's in the viewport and no occluding object was hit first
             if (debugDraw) Debug.DrawLine(cam.transform.position, markerPosition, Color.green, 0.1f); // Draw green line if visible
            return true;
        }


        // --- OLD Method
        /*
        // Comp tolerance thresholds.
        private const float SIZE_CLOSE_UP = 0.7f;
        // ... other size constants ...

        public static PortraitShotType? EvaluateComposition(
            Camera camera, LayerMask subjectLayer, float maxDistance,
            float detectionRadius, out Transform detectedSubject, bool showDebugInfo = false)
        {
            // ... old logic based on bounding box size ...
            detectedSubject = null;
             return null; // Replace with actual old code if you want to keep it commented
        }

        private static bool CalculateSubjectSize(Camera targetCamera, Collider subjectCollider, out float size) {
             // ... old logic ...
             size = 0;
             return false;
        }
        */
    }
}