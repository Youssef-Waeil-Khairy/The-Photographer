using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    /// <summary>
    /// Provides static methods to evaluate photo composition based on subject framing.
    /// </summary>
    public static class PhotoCompositionEvaluator
    {
        // Comp tolerance thresholds.
        private const float SIZE_CLOSE_UP = 0.7f;
        private const float SIZE_EXTREME_CLOSE_UP = 0.9f;
        private const float SIZE_MEDIUM_CLOSE_UP = 0.5f;
        private const float SIZE_MEDIUM = 0.35f;
        private const float SIZE_MEDIUM_WIDE = 0.25f;

        private const float SIZE_WIDE = 0.15f;
        // Anything below SIZE_WIDE is considered ExtremeWide.

        /// <summary>
        /// Evaluates the composition of a potential photo based on subject framing.
        /// </summary>
        /// <param name="camera">The camera attempting the shot.</param>
        /// <param name="subjectLayer">The layer mask containing portrait subjects.</param>
        /// <param name="maxDistance">Maximum distance to detect subjects.</param>
        /// <param name="detectionRadius">Viewport radius (0-0.5) for multi-raycast detection.</param>
        /// <param name="detectedSubject">Output: The transform of the detected subject, if any.</param>
        /// <param name="showDebugInfo">Optional: Log debug messages.</param>
        /// <returns>The evaluated PortraitShotType, or null if no valid subject was framed correctly.</returns>
        public static PortraitShotType? EvaluateComposition(
            Camera camera,
            LayerMask subjectLayer,
            float maxDistance,
            float detectionRadius,
            out Transform detectedSubject,
            bool showDebugInfo = false) {
            detectedSubject = null;
            float subjectSize = 0f;

            if (camera == null) {
                if (showDebugInfo) Debug.LogError("EvaluateComposition: Camera is null.");
                return null;
            }

            // --- Detection Logic (Multi-Raycast) ---
            Vector3 centerPoint = new Vector3(0.5f, 0.5f, 0);
            Vector3[] rayPoints = new Vector3[] {
                centerPoint,
                new Vector3(0.5f - detectionRadius, 0.5f + detectionRadius, 0), // TL.
                new Vector3(0.5f + detectionRadius, 0.5f + detectionRadius, 0), // TR.
                new Vector3(0.5f - detectionRadius, 0.5f - detectionRadius, 0), // BL.
                new Vector3(0.5f + detectionRadius, 0.5f - detectionRadius, 0) // BR.
            };

            RaycastHit hit;
            for (int i = 0; i < rayPoints.Length; i++) {
                Vector3 clampedRayPoint = rayPoints[i];
                clampedRayPoint.x = Mathf.Clamp01(clampedRayPoint.x);
                clampedRayPoint.y = Mathf.Clamp01(clampedRayPoint.y);
                Ray ray = camera.ViewportPointToRay(clampedRayPoint);

                if (Physics.Raycast(ray, out hit, maxDistance, subjectLayer)) {
                    Collider subjectCollider = hit.collider;
                    if (CalculateSubjectSize(camera, subjectCollider, out subjectSize)) {
                        detectedSubject = hit.transform; // Found a valid subject.
                        if (showDebugInfo)
                            Debug.Log(
                                $"EvaluateComposition: Subject {detectedSubject.name} detected via ray {i}, Size: {subjectSize:P0}");
                        break; // Use the first valid subject found.
                    }

                    if (showDebugInfo) {
                        Debug.Log(
                            $"EvaluateComposition: Ray {i} hit {hit.transform.name}, but size calculation failed or subject not visible.");
                    }
                }
            }
            // --- End Detection Logic ---

            if (detectedSubject == null || subjectSize <= 0) {
                if (showDebugInfo && detectedSubject == null)
                    Debug.Log("EvaluateComposition: No valid subject detected.");
                return null; // No valid subject found and sized.
            }

            // --- Size to ShotType Mapping ---
            PortraitShotType shotType;
            if (subjectSize > SIZE_EXTREME_CLOSE_UP) {
                shotType = PortraitShotType.ExtremeCloseUp;
            }
            else if (subjectSize > SIZE_CLOSE_UP) {
                shotType = PortraitShotType.CloseUp;
            }
            else if (subjectSize > SIZE_MEDIUM_CLOSE_UP) {
                shotType = PortraitShotType.MediumCloseUp;
            }
            else if (subjectSize > SIZE_MEDIUM) {
                shotType = PortraitShotType.Medium;
            }
            else if (subjectSize > SIZE_MEDIUM_WIDE) {
                shotType = PortraitShotType.MediumWide;
            }
            else if (subjectSize > SIZE_WIDE) {
                shotType = PortraitShotType.Wide;
            }
            else {
                shotType = PortraitShotType.ExtremeWide;
            }
            // --- End Mapping ---

            if (showDebugInfo) Debug.Log($"EvaluateComposition: Determined ShotType: {shotType}");
            return shotType;
        }

        /// <summary>
        /// Calculates the subject's size in the viewport.
        /// </summary>
        /// <returns>True if size calculation was successful and size > 0, false otherwise.</returns>
        private static bool CalculateSubjectSize(Camera targetCamera, Collider subjectCollider, out float size) {
            size = 0f;
            if (subjectCollider == null) return false;

            Bounds bounds = subjectCollider.bounds;
            if (bounds.size == Vector3.zero) return false;

            Vector3 screenMin = targetCamera.WorldToViewportPoint(bounds.min);
            Vector3 screenMax = targetCamera.WorldToViewportPoint(bounds.max);

            bool partlyInFront = screenMin.z > -0.1f || screenMax.z > -0.1f; // Allow slightly behind near plane.
            bool partlyVisibleX = screenMin.x < 1.05f && screenMax.x > -0.05f; // Allow slightly off screen.
            bool partlyVisibleY = screenMin.y < 1.05f && screenMax.y > -0.05f; // Allow slightly off screen.

            if (!partlyInFront || !partlyVisibleX || !partlyVisibleY) return false;

            float minX = Mathf.Clamp01(screenMin.x);
            float minY = Mathf.Clamp01(screenMin.y);
            float maxX = Mathf.Clamp01(screenMax.x);
            float maxY = Mathf.Clamp01(screenMax.y);

            float width = Mathf.Abs(maxX - minX);
            float height = Mathf.Abs(maxY - minY);

            size = Mathf.Max(width, height);
            return size > Mathf.Epsilon;
        }

        /// <summary>
        /// Gets a friendly display name for the shot type for the player to understand better.
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
    }
}