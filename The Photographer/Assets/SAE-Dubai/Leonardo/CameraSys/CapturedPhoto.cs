using SAE_Dubai.Leonardo.Client_System;
using UnityEngine;

namespace SAE_Dubai.Leonardo.CameraSys
{
    [System.Serializable]
    public class CapturedPhoto
    {
        public PortraitShotType? portraitShotType;
        public System.DateTime TimeStamp;
        public int iso;
        public float aperture;
        public float shutterSpeed;
        public float focalLength;
        public float quality;
        public Texture2D photoTexture; // TODO: Add?
        public bool HeadVisible;
        public bool EyesVisible;
        public bool MouthVisible;
        public bool ChinVisible;
        public bool ChestVisible;
        public bool HipVisible;
        public bool KneesVisible;
        public bool FeetVisible;
        public bool AboveHeadVisible;
        public bool BelowFeetVisible;
        public bool ShoulderLVisible;
        public bool ShoulderRVisible;

        /// <summary>
        /// Reference to the specific Camera component that captured this photo.
        /// </summary>
        [System.NonSerialized] public Camera CapturingCamera;

        public string GetPhotoInfo()
        {
            string shutterText = shutterSpeed >= 1f
                ? $"{shutterSpeed:F1}s"
                : $"1/{Mathf.RoundToInt(1f / shutterSpeed)}";

            string photoInfo = $"Taken: {TimeStamp:yyyy-MM-dd HH:mm:ss}\n" +
                               $"Settings: ISO {iso}, f/{aperture:F1}, {shutterText}, {focalLength:F0}mm\n" +
                               $"Quality: {quality:P0}";

             if (CapturingCamera != null) {
                 photoInfo += $"\nCamera Used: {CapturingCamera.name}";
             }

            if (portraitShotType.HasValue && portraitShotType.Value != PortraitShotType.Undefined)
            {
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