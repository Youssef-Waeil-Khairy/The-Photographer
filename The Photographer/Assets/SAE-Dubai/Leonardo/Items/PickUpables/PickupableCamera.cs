using SAE_Dubai.Leonardo.CameraSys;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Items.PickUpables
{
    public class PickupableCamera : MonoBehaviour, IPickupable
    {
        [Header("- Camera Settings")]
        public string cameraName = "Basic Camera";
        public AudioClip pickupSound;
        public bool destroyOnPickup = true;
        
        public string GetItemName()
        {
            return cameraName;
        }
        
        public void OnPickup()
        {
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            
            // Clone this camera and register it with the camera manager (prolly useful in the future).
            GameObject cameraClone = Instantiate(gameObject);
            
            // Remove the IPickupable component so it can't be picked up again.
            Destroy(cameraClone.GetComponent<PickupableCamera>());
            
            // If there's a collider, remove it too.
            Collider collider = cameraClone.GetComponent<Collider>();
            if (collider != null) {
                Destroy(collider);
            }
            
            // Register with camera manager.
            CameraManager cameraManager = CameraManager.Instance;
                
            if (cameraManager != null)
            {
                cameraManager.RegisterCamera(cameraName, cameraClone);
            }
            else
            {
                Debug.LogError("PickupableCamera.cs: No CameraManager found in the scene!");
                Destroy(cameraClone);
            }
            
            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void OnDrop()
        {
            throw new System.NotImplementedException();
        }
    }
}