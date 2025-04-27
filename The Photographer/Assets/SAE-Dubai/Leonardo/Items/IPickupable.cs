using UnityEngine;

namespace SAE_Dubai.Leonardo.Items
{
    public interface IPickupable
    {
        /// <summary>
        /// Returns the name of the pickupable item.
        /// </summary>
        public string GetItemName();

        /// <summary>
        /// Returns the icon sprite for the pickupable item.
        /// Can return null if no icon is available.
        /// </summary>
        public Sprite GetItemIcon();

        /// <summary>
        /// Called when the player picks up this item.
        /// </summary>
        public void OnPickup();
        
        /// <summary>
        /// Called when the player drops this item.
        /// </summary>
        public void OnDrop();
    }
}