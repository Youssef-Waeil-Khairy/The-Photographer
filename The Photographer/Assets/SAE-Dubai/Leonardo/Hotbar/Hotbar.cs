using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private string[] _equipmentSlots;

        #region Unity Methods

        private void Start()
        {
            _equipmentSlots = new string[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                _equipmentSlots[i] = "";
            }

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

                slotNumber.text = (i + 1).ToString();

                itemName.text = _equipmentSlots[i];

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

            /*if (!string.IsNullOrEmpty(_equipmentSlots[selectedSlot]))
            {
                Debug.Log($"Hotbar.cs: selected: {_equipmentSlots[selectedSlot]}");
            }
            else
            {
                Debug.Log($"Hotbar.cs: selected empty slot: {selectedSlot}");
            }*/
        }

        private void UpdateSlotUi(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots) return;

            Transform slotTrasnform = slotsParent.GetChild(slotIndex);
            Image slotImage = slotTrasnform.GetComponent<Image>();

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

            Transform slotTrasnform = slotsParent.GetChild(slotIndex);
            TextMeshProUGUI itemName = slotTrasnform.Find("ItemName").GetComponent<TextMeshProUGUI>();
            itemName.text = equipmentName;

            UpdateSlotUi(slotIndex);
            return true;
        }

        private int FindEmptySlot()
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

            Transform slotTrasnform = slotsParent.GetChild(slotIndex);
            TextMeshProUGUI itemName = slotTrasnform.Find("ItemName").GetComponent<TextMeshProUGUI>();
            itemName.text = "";
        }

        public void UpgradeHotbarSize(int newSize)
        {
            if (newSize <= maxSlots) return;

            string[] oldEquipment = _equipmentSlots;

            maxSlots = newSize;
            _equipmentSlots = new string[maxSlots];

            for (int i = 0; i < oldEquipment.Length; i++)
            {
                _equipmentSlots[i] = oldEquipment[i];
            }

            for (int i = oldEquipment.Length; i < maxSlots; i++)
            {
                _equipmentSlots[i] = "";
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