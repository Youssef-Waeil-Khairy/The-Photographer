using System;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Items
{
    public class ItemPickUpSystem : MonoBehaviour
    {
        [Header("Pickup Settings")]
        public float pickupRange = 3f;
        public LayerMask pickupLayer;
        public KeyCode pickupKey = KeyCode.E;
    
        [Header("References")]
        private Camera _playerCamera;
        private Hotbar.Hotbar _hotbar;
        public GameObject pickupPrompt;
        public TMPro.TextMeshProUGUI pickupText;
    
        private IPickupable _currentTarget;
        
        private void Start()
        {
            _playerCamera = Camera.main;
            _hotbar = FindObjectOfType<Hotbar.Hotbar>();
        
            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }
        }

        private void Update()
        {
            LookForItems();

            if (_currentTarget != null && Input.GetKeyDown(pickupKey))
            {
                PickupItem();
            }
        }

        private void LookForItems()
        {
            // Refresh the target every time.
            _currentTarget = null;
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
            if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer))
            {
                // Try to get IPickupable interface from the hit object.
                IPickupable pickupable = hit.collider.GetComponent<IPickupable>();
            
                if (pickupable != null)
                {
                    // Object found.
                    _currentTarget = pickupable;
                
                    // Show pickup prompt.
                    if (pickupPrompt != null)
                    {
                        pickupPrompt.SetActive(true);
                        if (pickupText != null)
                        {
                            pickupText.text = $"Press E to pick up {pickupable.GetItemName()}";
                        }
                    }
                }
                else
                {
                    // Hide pickup feedback.
                    if (pickupPrompt != null)
                    {
                        pickupPrompt.SetActive(false);
                    }
                }
            }
            else
            {
                // Hide visual feedback if not looking at anything.
                if (pickupPrompt != null)
                {
                    pickupPrompt.SetActive(false);
                }
            }
        }


        private void PickupItem()
        {
            if (_currentTarget != null && _hotbar != null)
            {
                // Get the item name.
                string itemName = _currentTarget.GetItemName();
                bool added = _hotbar.AddEquipment(itemName);

                if (added)
                {
                    // If item was added to hotbar, handle pickup.
                    _currentTarget.OnPickup();

                    // Hide the prompt.
                    if (pickupPrompt != null)
                    {
                        pickupPrompt.SetActive(false);
                    }

                    // Refresh visual feedback.
                    _currentTarget = null;
                }
                else
                {
                    Debug.Log("Could not add item to hotbar.");
                }
            }
        }
    }
}
