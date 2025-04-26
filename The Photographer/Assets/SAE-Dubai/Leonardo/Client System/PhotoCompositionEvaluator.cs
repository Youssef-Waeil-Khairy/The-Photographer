// In SAE-Dubai/Leonardo/Client System/PhotoCompositionEvaluator.cs

using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    /// <summary>
    /// Provides static helper methods for photo composition evaluation,
    /// including display names and screen visibility checks for markers.
    /// </summary>
    public static class PhotoCompositionEvaluator
    {
        /// <summary>
        /// Gets a friendly display name for the specified portrait shot type.
        /// </summary>
        /// <param name="shotType">The shot type enum value.</param>
        /// <returns>A user-readable string representation of the shot type.</returns>
        public static string GetShotTypeDisplayName(PortraitShotType shotType)
        {
            switch (shotType)
            {
                case PortraitShotType.ExtremeCloseUp: return "Extreme Close-Up";
                case PortraitShotType.CloseUp: return "Close-Up";
                case PortraitShotType.MediumCloseUp: return "Medium Close-Up";
                case PortraitShotType.Medium: return "Medium Shot";
                case PortraitShotType.MediumWide: return "Medium-Wide Shot"; // Covers Cowboy shot etc.
                case PortraitShotType.Wide: return "Wide Shot"; // Full body typically
                case PortraitShotType.ExtremeWide: return "Extreme Wide Shot"; // Subject small in frame
                case PortraitShotType.Undefined: return "Undefined";
                default: return "Unknown Shot Type"; // Fallback for unexpected values
            }
        }

        /// <summary>
        /// Checks if a specific world-space point (marker) projects within the camera's screen bounds.
        /// This method does NOT check for occlusion by other objects.
        /// </summary>
        /// <param name="cam">The camera performing the check.</param>
        /// <param name="markerPosition">The world position of the marker to check.</param>
        /// <returns>True if the marker projects within the screen boundaries and is in front of the near clip plane, false otherwise.</returns>
        public static bool IsMarkerVisibleOnScreen(Camera cam, Vector3 markerPosition)
        {
            if (cam == null)
            {
                 // Log error only once or use a more robust error handling if needed
                 // Debug.LogError("IsMarkerVisibleOnScreen: Camera reference is null!");
                 return false;
            }

            // Convert world position to screen coordinates
            Vector3 screenPos = cam.WorldToScreenPoint(markerPosition);

            // Check if the point is within the screen pixel coordinates (X: 0 to width, Y: 0 to height)
            // and also ensure it's in front of the camera's near clipping plane (Z > nearClipPlane)
            bool onScreen = screenPos.x >= 0 && screenPos.x <= Screen.width &&
                              screenPos.y >= 0 && screenPos.y <= Screen.height &&
                              screenPos.z > cam.nearClipPlane;

            return onScreen;
        }
    }
}