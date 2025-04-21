using UnityEngine;

namespace SAE_Dubai.Leonardo.CameraSys
{
    /// <summary>
    /// Data structure to store information about photos captured by the player.
    /// Records technical details of each photo including camera settings and quality.
    /// </summary>
    [System.Serializable]
    public class CapturedPhoto
    {
        /// <summary>
        /// When the photo was taken.
        /// </summary>
        public System.DateTime TimeStamp;
        
        /// <summary>
        /// ISO sensitivity setting used for the photo.
        /// </summary>
        public int iso;
        
        /// <summary>
        /// Aperture f-stop used for the photo.
        /// </summary>
        public float aperture;
        
        /// <summary>
        /// Shutter speed in seconds used for the photo.
        /// </summary>
        public float shutterSpeed;
        
        /// <summary>
        /// Focal length in mm used for the photo.
        /// </summary>
        public float focalLength;
        
        /// <summary>
        /// Calculated quality value of the photo (0.0 to 1.0).
        /// </summary>
        public float quality;
        
        /// <summary>
        /// Optional render texture containing the actual photo image.
        /// </summary>
        public Texture2D photoTexture;
        
        /// <summary>
        /// Returns a formatted string with the camera settings used for this photo.
        /// </summary>
        public string GetPhotoInfo()
        {
            string shutterText = shutterSpeed >= 1f 
                ? $"{shutterSpeed}s" 
                : $"1/{Mathf.Round(1f / shutterSpeed)}";
                
            return $"Photo taken: {TimeStamp.ToString("yyyy-MM-dd HH:mm:ss")}\n" +
                   $"Settings: ISO {iso}, f/{aperture}, {shutterText}, {focalLength}mm\n" +
                   $"Quality: {quality:P0}";
        }
    }
}