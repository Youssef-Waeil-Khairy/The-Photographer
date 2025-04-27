using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SAE_Dubai.Leonardo.CameraSys;

namespace SAE_Dubai.Leonardo.Hotbar
{
    public class Hotbar : MonoBehaviour
    {
        [Header("- Hotbar Settings")] [SerializeField]
        private int maxSlots = 5;

        [SerializeField] private int selectedSlot = 0;

        [Header("- References")] [SerializeField]
        private GameObject hotbarPanel;

        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform slotsParent;
        [SerializeField] private Color selectedSlotColor = Color.green;
        [SerializeField] private Color emptySlotColor = Color.grey;

        [Header("- Camera Icons")]
        [Tooltip("If true, will display camera icons instead of text when possible")]
        [SerializeField] private bool useIconsForCameras = true;

        private string[] _equipmentSlots;
        private Sprite[] _slotIcons;
        private CameraManager _cameraManager;

        #region Unity Methods

        private void Start()
        {
            _equipmentSlots = new string[maxSlots];
            _slotIcons = new Sprite[maxSlots];
            
            for (int i = 0; i < maxSlots; i++)
            {
                _equipmentSlots[i] = "";
                _slotIcons[i] = null;
            }

            _cameraManager = CameraManager.Instance;

            CreateHotbarUi();
        }

        private void Update()
        {
            for (int i = 0; i < Mathf.Min(maxSlots, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectSlot(i);
                }
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.1f)
            {
                int direction = scroll > 0 ? 1 : -1;
                CycleSlots(direction);
            }
        }

        #endregion

        #region Script Specific

        private void CreateHotbarUi()
        {
            // Clear the slots.
            foreach (Transform child in slotsParent)
            {
                Destroy(child.gameObject);
            }

            // Create the slots.
            for (int i = 0; i < maxSlots; i++)
            {
                GameObject slot = Instantiate(slotPrefab, slotsParent);
                slot.name = "Slot_" + i;

                TextMeshProUGUI slotNumber = slot.transform.Find("SlotNumber").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI itemName = slot.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
                Image slotImage = slot.GetComponent<Image>();
                
                // Try to find the item icon image component (make sure it exists in your prefab)
                Image itemIcon = slot.transform.Find("ItemIcon")?.GetComponent<Image>();
                
                slotNumber.text = (i + 1).ToString();

                // Set up icon or text based on what we have
                if (useIconsForCameras && itemIcon != null && _slotIcons[i] != null)
                {
                    // Use icon and hide text
                    itemIcon.sprite = _slotIcons[i];
                    itemIcon.gameObject.SetActive(true);
                    itemName.gameObject.SetActive(false);
                }
                else
                {
                    // Use text and hide icon if there is one
                    itemName.text = _equipmentSlots[i];
                    if (itemIcon != null)
                    {
                        itemIcon.gameObject.SetActive(false);
                    }
                }

                if (i == selectedSlot)
                {
                    slotImage.color = selectedSlotColor;
                }
                else if (string.IsNullOrEmpty(_equipmentSlots[i]))
                {
                    slotImage.color = emptySlotColor;
                }
            }
        }

        private void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots) return;

            int prevSlot = selectedSlot;
            selectedSlot = slotIndex;

            UpdateSlotUi(prevSlot);
            UpdateSlotUi(selectedSlot);
        }

        private void UpdateSlotUi(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots) return;

            Transform slotTransform = slotsParent.GetChild(slotIndex);
            Image slotImage = slotTransform.GetComponent<Image>();
            TextMeshProUGUI itemName = slotTransform.Find("ItemName").GetComponent<TextMeshProUGUI>();
            Image itemIcon = slotTransform.Find("ItemIcon")?.GetComponent<Image>();

            // Update icon or text based on what we have
            if (useIconsForCameras && itemIcon != null && _slotIcons[slotIndex] != null)
            {
                // Use icon and hide text
                itemIcon.sprite = _slotIcons[slotIndex];
                itemIcon.gameObject.SetActive(true);
                itemName.gameObject.SetActive(false);
            }
            else
            {
                // Use text and hide icon if there is one
                itemName.text = _equipmentSlots[slotIndex];
                if (itemIcon != null)
                {
                    itemIcon.gameObject.SetActive(false);
                }
            }

            if (slotIndex == selectedSlot)
            {
                slotImage.color = selectedSlotColor;
            }
            else if (string.IsNullOrEmpty(_equipmentSlots[slotIndex]))
            {
                slotImage.color = emptySlotColor;
            }
            else
            {
                slotImage.color = Color.white;
            }
        }

        private void CycleSlots(int direction)
        {
            int newSlot = (selectedSlot + direction) % maxSlots;
            if (newSlot < 0) newSlot += maxSlots;

            SelectSlot(newSlot);
        }

        // Methods to call from outside.
        
        public bool AddEquipment(string equipmentName, int slotIndex = -1)
        {
            if (slotIndex < 0)
            {
                slotIndex = FindEmptySlot();
                if (slotIndex < 0) return false;
            }

            _equipmentSlots[slotIndex] = equipmentName;
            
            // Try to get camera icon if it's a camera
            Sprite icon = TryGetCameraIcon(equipmentName);
            _slotIcons[slotIndex] = icon;

            UpdateSlotUi(slotIndex);
            return true;
        }
        
        /// <summary>
        /// Adds an item to the hotbar with its associated icon.
        /// </summary>
        /// <param name="equipmentName">Name of the equipment</param>
        /// <param name="icon">Icon sprite to display</param>
        /// <param name="slotIndex">Target slot, or -1 to find empty slot</param>
        /// <returns>True if added successfully</returns>
        public bool AddItemWithIcon(string equipmentName, Sprite icon, int slotIndex = -1)
        {
            if (slotIndex < 0)
            {
                slotIndex = FindEmptySlot();
                if (slotIndex < 0) return false;
            }

            _equipmentSlots[slotIndex] = equipmentName;
            _slotIcons[slotIndex] = icon;

            UpdateSlotUi(slotIndex);
            return true;
        }

        private Sprite TryGetCameraIcon(string cameraName)
        {
            // Skip if camera manager isn't available
            if (_cameraManager == null)
                return null;
                
            // Try to look up the camera in the manager by name
            CameraSystem camera = _cameraManager.GetActiveCamera();
            if (camera != null && camera.cameraSettings != null && camera.cameraSettings.modelName == cameraName)
            {
                return camera.cameraSettings.cameraIcon;
            }
            
            // If we couldn't find it in the active camera, try to find it in registered cameras 
            // (requires adding a method to CameraManager to check this - placeholder for now)
            
            return null;
        }

        /// <summary>
        /// Finds the first empty slot in the hotbar.
        /// </summary>
        /// <returns>Index of empty slot, or -1 if none available</returns>
        public int FindEmptySlot()
        {
            for (int i = 0; i < _equipmentSlots.Length; i++)
            {
                if (string.IsNullOrEmpty(_equipmentSlots[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public void RemoveEquipment(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots) return;

            _equipmentSlots[slotIndex] = null;
            _slotIcons[slotIndex] = null;

            UpdateSlotUi(slotIndex);
        }

        public void UpgradeHotbarSize(int newSize)
        {
            if (newSize <= maxSlots) return;

            string[] oldEquipment = _equipmentSlots;
            Sprite[] oldIcons = _slotIcons;

            maxSlots = newSize;
            _equipmentSlots = new string[maxSlots];
            _slotIcons = new Sprite[maxSlots];

            for (int i = 0; i < oldEquipment.Length; i++)
            {
                _equipmentSlots[i] = oldEquipment[i];
                _slotIcons[i] = oldIcons[i];
            }

            for (int i = oldEquipment.Length; i < maxSlots; i++)
            {
                _equipmentSlots[i] = "";
                _slotIcons[i] = null;
            }

            CreateHotbarUi();
        }

        public string GetSelectedEquipment()
        {
            if (selectedSlot >= 0 && selectedSlot < _equipmentSlots.Length)
            {
                return _equipmentSlots[selectedSlot];
            }

            return "";
        }

        public int GetSelectedSlotIndex()
        {
            return selectedSlot;
        }
        
        /// <summary>
        /// Checks if the player has any equipment in the hotbar.
        /// Used by the tutorial system to check if the player has picked up a camera.
        /// </summary>
        public bool HasAnyEquipment()
        {
            if (_equipmentSlots == null)
                return false;
        
            foreach (string item in _equipmentSlots)
            {
                if (!string.IsNullOrEmpty(item))
                    return true;
            }
    
            return false;
        }

        #endregion
    }
}