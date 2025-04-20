using SAE_Dubai.Leonardo.CameraSys;
using SAE_Dubai.Leonardo.Items;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Hotbar
{
    public class HotbarController : MonoBehaviour
    {
        [Header("- Settings")] public KeyCode useItemKey = KeyCode.Mouse0;

        [Header("- References")] private Hotbar _hotbar;
        private CameraManager cameraManager;
        public ItemDatabase itemDatabase;

        private void Start()
        {
            _hotbar = GetComponent<Hotbar>();

            if (_hotbar == null)
            {
                _hotbar = FindObjectOfType<Hotbar>();
            }

            if (itemDatabase == null)
            {
                itemDatabase = FindObjectOfType<ItemDatabase>();
            }
            
            cameraManager = CameraManager.Instance;

        }


        private void UseSelectedItem()
        {
            string selectedItemName = _hotbar.GetSelectedEquipment();

            if (string.IsNullOrEmpty(selectedItemName))
            {
                return;
            }

            if (itemDatabase != null)
            {
                ItemData itemData = itemDatabase.GetItemByName(selectedItemName);

                if (itemData != null)
                {
                    switch (itemData.ItemType)
                    {
                        case ItemType.Consumable:
                            ConsumeItem(itemData);
                            break;

                        case ItemType.Placeable:
                            PlaceItem(itemData);
                            break;

                        case ItemType.Tool:
                            UseTool(itemData);
                            break;

                        default:
                            Debug.Log($"Used item: {selectedItemName}");
                            break;
                    }
                }
            }
            // TODO: remove later.
            else
            {
                Debug.Log($"Used item: {selectedItemName}");
            }
        }

        private void PlaceItem(ItemData item)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, 5f))
            {
                // Instantiate the item's prefab at the hit location.
                if (item.ItemPrefab != null)
                {
                    Instantiate(item.ItemPrefab, hit.point, Quaternion.identity);
                    Debug.Log($"Placed item: {item.ItemName}");

                    RemoveSelectedItem();
                }
            }
        }

        private void RemoveSelectedItem()
        {
            int selectedSlot = _hotbar.GetSelectedSlotIndex();
            if (selectedSlot >= 0)
            {
                _hotbar.RemoveEquipment(selectedSlot);
            }
        }

        private void UseTool(ItemData item)
        {
            Debug.Log($"Used tool: {item.ItemName}");

            if (item.UseEffect != null)
            {
                Instantiate(item.UseEffect, transform.position + transform.forward, Quaternion.identity);
            }
        }

        private void ConsumeItem(ItemData item)
        {
            Debug.Log($"Consumed item: {item.ItemName}");
            RemoveSelectedItem();
        }
    }
}