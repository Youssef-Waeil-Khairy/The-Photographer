using SAE_Dubai.Leonardo.CameraSys;
using SAE_Dubai.Leonardo.Items;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Items.PickUpables
{
    /// <summary>
    /// Implements the IPickupable interface for camera items in the game.
    /// When picked up, this creates a usable camera instance and registers it with the CameraManager.
    /// The camera's behavior and capabilities are defined by the referenced CameraSettings.
    /// </summary>
    public class PickupableCamera : MonoBehaviour, IPickupable
    {
        [Header("- Camera Configuration")]
        [Tooltip("The ScriptableObject containing all settings for this camera model")]
        public CameraSettings cameraSettings;

        [Tooltip("Sound played when picking up this camera")]
        public AudioClip pickupSound;

        [Tooltip("Whether to destroy this object after pickup or just disable it")]
        public bool destroyOnPickup = true;

        /// <summary>
        /// Returns the name of the item as defined in the camera settings.
        /// </summary>
        public string GetItemName() {
            return cameraSettings != null ? cameraSettings.modelName : "Unknown Camera";
        }

        /// <summary>
        /// Called when the player picks up this camera. Creates a usable camera instance
        /// and registers it with the CameraManager.
        /// </summary>
        public void OnPickup() {
            // Play pickup sound if available.
            if (pickupSound != null) {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // Clone this camera for the player to use.
            GameObject cameraClone = Instantiate(gameObject);

            // Remove the IPickupable component so it can't be picked up again.
            Destroy(cameraClone.GetComponent<PickupableCamera>());

            // Remove any collider to prevent physics interactions.
            Collider collider = cameraClone.GetComponent<Collider>();
            if (collider != null) {
                Destroy(collider);
            }

            // Register with camera manager.
            CameraManager cameraManager = CameraManager.Instance;

            if (cameraManager != null) {
                // Pass the camera settings during registration.
                string cameraName = GetItemName();
                cameraManager.RegisterCamera(cameraName, cameraClone);

                // Log successful registration
                //Debug.Log($"Camera '{cameraName}' registered with camera manager");
            }
            else {
                Debug.LogError("PickupableCamera: No CameraManager found in the scene!");
                Destroy(cameraClone);
            }

            // Handle the original pickup object
            if (destroyOnPickup) {
                Destroy(gameObject);
            }
            else {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called when the player drops this camera. Not currently implemented.
        /// </summary>
        public void OnDrop() {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Validates that this camera has all required components and settings.
        /// </summary>
        private void OnValidate() {
            if (cameraSettings == null) {
                Debug.LogWarning(
                    "PickupableCamera.cs: No camera settings assigned. Please assign a CameraSettingsSO asset.", this);
            }
        }
    }
}