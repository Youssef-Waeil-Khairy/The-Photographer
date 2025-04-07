using UnityEngine;

namespace SAE_Dubai.Leonardo.CameraSys
{
    public class CameraSettings
    {
        [Header("- Basic Settings")]
        public string modelName = "Default Camera";
        public CameraModel cameraType = CameraModel.Beginner;
        public string manufacturer = "Generic";
        
        [Header("- Technical Specifications")]
        [Range(100, 12800)]
        public int baseISO = 100;
        [Range(1.4f, 22f)]
        public float minAperture = 1.4f; // Lower f-number = wider aperture.
        [Range(1.4f, 22f)]
        public float maxAperture = 16f;
        
        [Header("- Shutter Speed")]
        [Tooltip("Fraction of a second (1/x), smaller values mean faster shutter")]
        public float minShutterSpeedFraction = 4000f; // 1/4000 sec.
        [Tooltip("In seconds, larger values mean slower shutter")]
        public float maxShutterSpeed = 30f; // 30 seconds (this is literally unnecessary and no one will use any settings above 1/125 seconds lol, at least they shouldn't.
        
        [Header("- Lens Information")]
        public bool hasZoom = true;
        [Tooltip("Focal length in millimeters")]
        public float focalLengthMin = 24f;
        [Tooltip("Focal length in millimeters")]
        public float focalLengthMax = 70f;
        
        [Header("- Features")]
        public bool hasAutoFocus = true;
        public bool hasImageStabilization = false;
        public bool hasBuiltInFlash = true;
        
        [Header("- Visual")]
        public Sprite cameraSprite;
        public GameObject cameraModel3D;
        
        [Header("- Audio")]
        public AudioClip shutterSound;
        public AudioClip focusSound;
        public AudioClip zoomSound;
    }
}