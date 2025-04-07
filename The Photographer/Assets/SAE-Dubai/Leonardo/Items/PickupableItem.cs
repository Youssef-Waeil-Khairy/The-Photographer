using UnityEngine;

namespace SAE_Dubai.Leonardo.Items
{
    public class PickupableItem : MonoBehaviour, IPickupable
    {
        [Header("Item Settings")]
        public string itemName = "Item";
        public bool destroyOnPickup = true;
        public AudioClip pickupSound;
    
        public string GetItemName()
        {
            return itemName;
        }
    
        public void OnPickup()
        {
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
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