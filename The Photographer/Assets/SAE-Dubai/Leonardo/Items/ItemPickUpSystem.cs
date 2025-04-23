using System;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Items
{
    public class ItemPickUpSystem : MonoBehaviour
    {
        [Header("- Pickup Settings")]
        public float pickupRange = 3f;
        public LayerMask pickupLayer;
        public KeyCode pickupKey = KeyCode.E;
        public float pickupHoldDuration = 0.2f;
    
        [Header("- References")]
        private Camera _playerCamera;
        private Hotbar.Hotbar _hotbar;
        public GameObject pickupPrompt;
        public TMPro.TextMeshProUGUI pickupText;
    
        private IPickupable _currentTarget;
        private float _pickupHoldTimer = 0f;
        private bool _isHoldingPickup = false;
        
        private void Start()
        {
            // Getting references.
            _playerCamera = Camera.main;
            _hotbar = FindObjectOfType<Hotbar.Hotbar>();
        
            // Set the pickup text to false.
            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }
        }

        private void Update()
        {
            LookForItems();
            
            // Added a delay to picking up items.
            if (_currentTarget != null)
            {
                if (Input.GetKey(pickupKey))
                {
                    if (!_isHoldingPickup)
                    {
                        _isHoldingPickup = true;
                        _pickupHoldTimer = 0f;
                    }
                    
                    _pickupHoldTimer += Time.deltaTime;
                    
                    // Update UI to show progress.
                    if (pickupPrompt != null && pickupText != null)
                    {
                        float progress = Mathf.Clamp01(_pickupHoldTimer / pickupHoldDuration);
                        pickupText.text = $"Picking up {_currentTarget.GetItemName()} ({(progress * 100):0}%)";
                    }
                    
                    // Check if held long enough.
                    if (_pickupHoldTimer >= pickupHoldDuration)
                    {
                        PickupItem();
                        _isHoldingPickup = false;
                    }
                }
                else if (_isHoldingPickup)
                {
                    // Player released the key before completing pickup.
                    _isHoldingPickup = false;
                    
                    // Reset UI.
                    if (pickupPrompt != null && pickupText != null)
                    {
                        pickupText.text = $"Press E to pick up {_currentTarget.GetItemName()}";
                    }
                }
            }
            else
            {
                _isHoldingPickup = false;
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
                        if (pickupText != null && !_isHoldingPickup)
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