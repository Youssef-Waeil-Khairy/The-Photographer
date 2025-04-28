using UnityEngine;

namespace SAE_Dubai.Leonardo.CameraSys
{
    /// <summary>
    /// Defines the various camera sensor types available in Unity by defautl.
    /// </summary>
    public enum CameraSensorType
    {
        Custom,
        EightMM,
        SuperEightMM,
        SixteenMM,
        SuperSixteenMM,
        ThirtyFiveMM_2Perf,
        ThirtyFiveMM_Academy,
        Super35,
        ThirtyFiveMM_TVProjection,
        ThirtyFiveMM_FullAperture,
        ThirtyFiveMM_185Projection,
        ThirtyFiveMM_Anamorphic,
        SixtyFiveMM_ALEXA,
        SeventyMM,
        SeventyMM_IMAX
    }

    /// <summary>
    /// ScriptableObject that defines all settings and capabilities for a specific camera model.
    /// This allows for creating different camera types as assets in the Unity editor that can
    /// be referenced by camera prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "New Camera Settings", menuName = "Photography/Camera Settings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("- Unity related")]
        public GameObject cameraPrefab;
        
        [Header("- Basic Information")]
        [Tooltip("The name of this camera model")]
        public string modelName = "Default Camera";

        [Tooltip("The manufacturer of this camera")]
        public string manufacturer = "Generic";

        [TextArea(2, 5)] [Tooltip("Description of this camera's features and quality")]
        public string description = "A basic camera.";
        
        [Tooltip("The base price of this camera in the shop")]
        public int price = 100;

        [Header("- Sensor Settings")]
        [Tooltip("The type of camera sensor used in this model")]
        public CameraSensorType sensorType = CameraSensorType.ThirtyFiveMM_FullAperture;

        [Tooltip("Custom sensor size in mm (X = width, Y = height), used only when sensorType is Custom")]
        public Vector2 customSensorSize = new Vector2(36, 24);

        [Header("- ISO Settings")]
        [Tooltip("The base/native ISO value of this camera")]
        public int baseISO = 100;

        [Tooltip("Minimum ISO value this camera supports")]
        public int minISO = 100;

        [Tooltip("Maximum ISO value this camera supports")]
        public int maxISO = 6400;

        [Tooltip("Available ISO stops this camera can be set to")]
        public int[] availableISOStops = { 100, 200, 400, 800, 1600, 3200, 6400 };

        [Tooltip("Whether this camera has auto ISO capability")]
        public bool hasAutoISO = true;

        [Header("- Aperture Settings")]
        [Tooltip("Minimum aperture value (largest opening) this camera supports")] [Range(1.4f, 22f)]
        public float minAperture = 1.4f;

        [Tooltip("Maximum aperture value (smallest opening) this camera supports")] [Range(1.4f, 22f)]
        public float maxAperture = 16f;

        [Tooltip("Available aperture stops this camera can be set to")]
        public float[] availableApertureStops = { 1.4f, 2f, 2.8f, 4f, 5.6f, 8f, 11f, 16f, 22f };

        [Tooltip("Whether this camera has auto aperture capability")]
        public bool hasAutoAperture = true;

        [Header("Shutter Speed Settings")]
        [Tooltip("Fastest shutter speed this camera supports (in seconds)")]
        public float minShutterSpeed = 1 / 4000f;

        [Tooltip("Slowest shutter speed this camera supports (in seconds)")]
        public float maxShutterSpeed = 30f;

        [Tooltip("Available shutter speed stops this camera can be set to")]
        public float[] availableShutterSpeedStops = {
            1 / 4000f, 1 / 2000f, 1 / 1000f, 1 / 500f, 1 / 250f, 1 / 125f, 1 / 60f, 1 / 30f, 1 / 15f, 1 / 8f, 1 / 4f,
            0.5f, 1f, 2f, 4f, 8f, 15f, 30f
        };

        [Tooltip("Whether this camera has auto shutter speed capability")]
        public bool hasAutoShutterSpeed = true;

        [Header("- Lens Settings")]
        [Tooltip("Whether this camera has a zoom lens")]
        public bool hasZoom = true;

        [Tooltip("Minimum focal length in millimeters")] [Range(10f, 200f)]
        public float minFocalLength = 18f;

        [Tooltip("Maximum focal length in millimeters")] [Range(10f, 800f)]
        public float maxFocalLength = 70f;

        [Tooltip("Whether this camera has auto focus capability")]
        public bool hasAutoFocus = true;

        [Header("- Aperture Shape Settings")]
        [Tooltip("Number of aperture blades (affects bokeh shape)")]
        [Range(3, 11)]
        public int apertureBladeCount = 5;

        [Tooltip("Curvature of aperture blades (0 = straight, 1 = circular)")]
        [Range(0f, 1f)]
        public Vector2 apertureBladeCurvature = new Vector2(0.5f, 0.5f);

        [Tooltip("Barrel clipping value for the aperture")]
        [Range(0f, 1f)]
        public float apertureBarrelClipping = 0.25f;
        
        [Tooltip("Anamorphism value for the aperture (creates oval bokeh)")]
        [Range(-1f, 1f)]
        public float apertureAnamorphism = 0f;

        [Header("- Additional Features")]
        [Tooltip("Whether this camera has image stabilization")]
        public bool hasImageStabilization = false;

        [Tooltip("Whether this camera has a built-in flash")]
        public bool hasBuiltInFlash = true;

        [Tooltip("Maximum number of photos this camera can store")]
        public int photoCapacity = 50;

        [Header("Visual Assets")]
        [Tooltip("2D sprite representation of this camera for UI")]
        public Sprite cameraIcon;

        [Header("Audio")]
        [Tooltip("Sound played when taking a photo")]
        public AudioClip shutterSound;

        [Tooltip("Sound played when focusing")]
        public AudioClip focusSound;

        [Tooltip("Sound played when zooming")] public AudioClip zoomSound;

        
        
        /// <summary>
        /// Returns the sensor size for the selected sensor type.
        /// </summary>
        public Vector2 GetSensorSize()
        {
            switch (sensorType)
            {
                case CameraSensorType.EightMM:
                    return new Vector2(4.8f, 3.5f);
                case CameraSensorType.SuperEightMM:
                    return new Vector2(5.79f, 4.01f);
                case CameraSensorType.SixteenMM:
                    return new Vector2(10.26f, 7.49f);
                case CameraSensorType.SuperSixteenMM:
                    return new Vector2(12.52f, 7.41f);
                case CameraSensorType.ThirtyFiveMM_2Perf:
                    return new Vector2(21.95f, 9.35f);
                case CameraSensorType.ThirtyFiveMM_Academy:
                    return new Vector2(21.95f, 16.0f);
                case CameraSensorType.Super35:
                    return new Vector2(24.89f, 18.66f);
                case CameraSensorType.ThirtyFiveMM_TVProjection:
                    return new Vector2(21.95f, 16.46f);
                case CameraSensorType.ThirtyFiveMM_FullAperture:
                    return new Vector2(36.0f, 24.0f);
                case CameraSensorType.ThirtyFiveMM_185Projection:
                    return new Vector2(22.05f, 12.0f);
                case CameraSensorType.ThirtyFiveMM_Anamorphic:
                    return new Vector2(21.95f, 18.59f);
                case CameraSensorType.SixtyFiveMM_ALEXA:
                    return new Vector2(54.12f, 25.59f);
                case CameraSensorType.SeventyMM:
                    return new Vector2(52.63f, 23.01f);
                case CameraSensorType.SeventyMM_IMAX:
                    return new Vector2(69.6f, 48.5f);
                case CameraSensorType.Custom:
                default:
                    return customSensorSize;
            }
        }

        /// <summary>
        /// Initializes a new camera with default settings for its skill level.
        /// </summary>
        /// <param name="initialISO">The starting ISO value.</param>
        /// <param name="initialAperture">The starting aperture value.</param>
        /// <param name="initialShutterSpeed">The starting shutter speed.</param>
        /// <param name="initialFocalLength">The starting focal length.</param>
        public void InitializeDefaultSettings(out int initialISO, out float initialAperture,
            out float initialShutterSpeed, out float initialFocalLength) {
            initialISO = baseISO;
            initialAperture = 5.6f; // Default mid-range aperture.
            initialShutterSpeed = 1 / 125f; // Default balanced shutter speed.
            initialFocalLength = minFocalLength; // Start at widest angle.

            // Ensure settings are within this camera's capabilities.
            initialISO = Mathf.Clamp(initialISO, minISO, maxISO);
            initialAperture = Mathf.Clamp(initialAperture, minAperture, maxAperture);
            initialShutterSpeed = Mathf.Clamp(initialShutterSpeed, minShutterSpeed, maxShutterSpeed);
            initialFocalLength = Mathf.Clamp(initialFocalLength, minFocalLength, maxFocalLength);
        }
    }
}