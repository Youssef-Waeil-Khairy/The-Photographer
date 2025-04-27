using UnityEngine;

namespace SAE_Dubai.Leonardo.Items
{
    public class PickupableItem : MonoBehaviour, IPickupable
    {
        [Header("- Item Settings")]
        public string itemName = "Item";
        public bool destroyOnPickup = true;
        public AudioClip pickupSound;
        
        [Header("- Item Display")]
        [Tooltip("Icon to display in the hotbar for this item")]
        public Sprite itemIcon;
    
        public string GetItemName()
        {
            return itemName;
        }
        
        public Sprite GetItemIcon()
        {
            return itemIcon;
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