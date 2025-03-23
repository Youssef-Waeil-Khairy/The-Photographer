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

        private string[] equipmentSlots;

        #region Unity Methods

        private void Start()
        {
            equipmentSlots = new string[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                equipmentSlots[i] = "";
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

                Text slotNumber = slot.transform.Find("SlotNumber").GetComponent<Text>();
                Text itemName = slot.transform.Find("ItemName").GetComponent<Text>();
                Image slotImage = slot.GetComponent<Image>();

                slotNumber.text = (i + 1).ToString();

                itemName.text = equipmentSlots[i];

                if (i == selectedSlot)
                {
                    slotImage.color = selectedSlotColor;
                }
                else if (string.IsNullOrEmpty(equipmentSlots[i]))
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

            if (!string.IsNullOrEmpty(equipmentSlots[selectedSlot]))
            {
                Debug.Log($"Hotbar.cs: selected: {equipmentSlots[selectedSlot]}");
            }
            else
            {
                Debug.Log($"Hotbar.cs: selected empty slot: {selectedSlot}");
            }
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
            else if (string.IsNullOrEmpty(equipmentSlots[selectedSlot]))
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

        public bool AddEquipment(string equipmentName, int slotIndex = -1)
        {
            if (slotIndex < 0)
            {
                // TODO: Find empty slot
                slotIndex = FindEmptySlot();
                if (slotIndex < 0) return false;
            }

            equipmentSlots[slotIndex] = equipmentName;

            Transform slotTrasnform = slotsParent.GetChild(slotIndex);
            Text itemName = slotTrasnform.Find("ItemName").GetComponent<Text>();
            itemName.text = equipmentName;

            UpdateSlotUi(slotIndex);
            return true;
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                if (string.IsNullOrEmpty(equipmentSlots[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public void RemoveEquipment(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots) return;

            equipmentSlots[slotIndex] = null;

            Transform slotTrasnform = slotsParent.GetChild(slotIndex);
            Text itemName = slotTrasnform.Find("ItemName").GetComponent<Text>();
            itemName.text = "";
        }

        public void UpgradeHotbarSize(int newSize)
        {
            if (newSize <= maxSlots) return;

            string[] oldEquipment = equipmentSlots;

            maxSlots = newSize;
            equipmentSlots = new string[maxSlots];

            for (int i = 0; i < oldEquipment.Length; i++)
            {
                equipmentSlots[i] = oldEquipment[i];
            }

            for (int i = oldEquipment.Length; i < maxSlots; i++)
            {
                equipmentSlots[i] = "";
            }

            CreateHotbarUi();
        }

        public string GetSelectedEquipment()
        {
            if (selectedSlot >= 0 && selectedSlot < equipmentSlots.Length)
            {
                return equipmentSlots[selectedSlot];
            }

            return "";
        }

        #endregion
    }
}