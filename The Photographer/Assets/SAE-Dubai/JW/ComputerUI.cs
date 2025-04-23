using SAE_Dubai.Leonardo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.JW
{
    public class ComputerUI : MonoBehaviour
    {
        [Header("- Camera Settings")]
        [SerializeField] private MouseController mouseController;
        [SerializeField] Camera computerCamera;
        [SerializeField] Camera playerCamera;
        [SerializeField] KeyCode interactComputerKeycode = KeyCode.Tab;

        [Header("- UI Elements")]
        [SerializeField] TMP_Text balanceText;
        [SerializeField] GameObject photoSessionsPanel;
        [SerializeField] GameObject cameraShopPanel;
    
        [Header("- Navigation")]
        [SerializeField] Button homeTabButton;
        [SerializeField] Button photoSessionsTabButton;
        [SerializeField] Button cameraShopTabButton;
    
        [SerializeField] GameObject homePanel;

        private void Start()
        {
            // Leo: Setup tab navigation
            if (homeTabButton != null)
                homeTabButton.onClick.AddListener(() => SwitchTab(TabType.Home));
            
            if (photoSessionsTabButton != null)
                photoSessionsTabButton.onClick.AddListener(() => SwitchTab(TabType.PhotoSessions));
            
            if (cameraShopTabButton != null)
                cameraShopTabButton.onClick.AddListener(() => SwitchTab(TabType.CameraShop));
        
            // Leo: Show home tab by default.
            SwitchTab(TabType.Home);
        }
    
        private void Update()
        {
            if (Input.GetKeyDown(interactComputerKeycode))
            {
                ToggleComputerVision();
            }

            // ? Update balance display (we shouldn't probably do this on update lol).
            if (balanceText != null && PlayerBalance.Instance != null)
            {
                balanceText.text = $"Balance: ${PlayerBalance.Instance.Balance}";
            }
        }

        public void ToggleComputerVision()
        {
            if (computerCamera.enabled)
            {
                // Exiting computer view.
                computerCamera.enabled = false;
                playerCamera.enabled = true;
                mouseController.DisableFreeMouse();
            
                // ? Optional: play sound effect 
                // ! AudioManager.Instance?.PlaySound("ComputerExit");
            }
            else
            {
                // Entering computer view
                computerCamera.enabled = true;
                playerCamera.enabled = false;
                mouseController.EnableFreeMouse();
            
                // ? Optional: play sound effect
                // ! AudioManager.Instance?.PlaySound("ComputerEnter");
            }
        }

        /// <summary>
        /// Better way to keep track of tabs.
        /// </summary>
        public enum TabType
        {
            Home,
            CameraShop,
            PhotoSessions
        }

        private void SwitchTab(TabType tabType)
        {
            // Hide all panels first.
            if (homePanel != null) 
                homePanel.SetActive(false);
            
            if (photoSessionsPanel != null)
                photoSessionsPanel.SetActive(false);
            
            if (cameraShopPanel != null)
                cameraShopPanel.SetActive(false);
        
            // Show the selected panel.
            switch (tabType)
            {
                case TabType.Home:
                    if (homePanel != null)
                        homePanel.SetActive(true);
                    break;
                
                case TabType.PhotoSessions:
                    if (photoSessionsPanel != null)
                        photoSessionsPanel.SetActive(true);
                    break;
                
                case TabType.CameraShop:
                    if (cameraShopPanel != null)
                        cameraShopPanel.SetActive(true);
                    break;
            }
        }
    }
}
