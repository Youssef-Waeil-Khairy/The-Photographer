using UnityEngine;

namespace SAE_Dubai.Leonardo.Items
{
    public class ItemDatabase : MonoBehaviour
    {
        public ItemData[] Items;
    
        public ItemData GetItemByName(string itemName)
        {
            foreach (ItemData item in Items)
            {
                if (item.ItemName == itemName)
                {
                    return item;
                }
            }
        
            return null;
        }
    }
}
