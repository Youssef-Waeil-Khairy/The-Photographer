// Create or Update this file as: CapturedPhoto.cs

using SAE_Dubai.Leonardo.CameraSys.Client_System;
using UnityEngine;
// Make sure the namespace matches the other scripts
namespace SAE_Dubai.Leonardo.CameraSys
{
    /// <summary>
    /// Data structure to store information about photos captured by the player.
    /// </summary>
    [System.Serializable]
    public class CapturedPhoto
    {
        /// <summary>
        /// The evaluated portrait shot type if this is a portrait photo.
        /// Set by the system handling the OnPhotoCapture event (e.g., ClientSpawner).
        /// </summary>
        public PortraitShotType? portraitShotType; // Make sure this uses the enum from PortraitShotType.cs

        // --- Keep your existing fields ---
        public System.DateTime TimeStamp;
        public int iso;
        public float aperture;
        public float shutterSpeed;
        public float focalLength;
        public float quality;
        public Texture2D photoTexture; // Optional: If you store the actual image
        // --- End existing fields ---

        /// <summary>
        /// Returns a formatted string with the camera settings and composition info.
        /// </summary>
        public string GetPhotoInfo()
        {
            string shutterText = shutterSpeed >= 1f
                ? $"{shutterSpeed:F1}s" // Format seconds with one decimal place if needed
                : $"1/{Mathf.RoundToInt(1f / shutterSpeed)}";

            string photoInfo = $"Taken: {TimeStamp:yyyy-MM-dd HH:mm:ss}\n" +
                               $"Settings: ISO {iso}, f/{aperture:F1}, {shutterText}, {focalLength:F0}mm\n" +
                               $"Quality: {quality:P0}";

            // Add portrait information if available, using the new static evaluator method
            if (portraitShotType.HasValue && portraitShotType.Value != PortraitShotType.Undefined)
            {
                // Use the static method from the new evaluator class
                photoInfo += $"\nComposition: {PhotoCompositionEvaluator.GetShotTypeDisplayName(portraitShotType.Value)}";
            }
            else
            {
                 photoInfo += "\nComposition: N/A";
            }

            return photoInfo;
        }
    }
}