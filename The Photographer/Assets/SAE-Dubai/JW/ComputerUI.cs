using SAE_Dubai.JW;
using SAE_Dubai.Leonardo;
using SAE_Dubai.Leonardo.CameraSys.Client_System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComputerUI : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private MouseController mouseController;
    [SerializeField] Camera computerCamera;
    [SerializeField] Camera playerCamera;
    [SerializeField] KeyCode interactComputerKeycode = KeyCode.Tab;

    [Header("UI Elements")]
    [SerializeField] TMP_Text balanceText;
    [SerializeField] GameObject photoSessionsPanel;
    
    [Header("Navigation")]
    [SerializeField] Button homeTabButton;
    [SerializeField] Button photoSessionsTabButton;
    
    [SerializeField] GameObject homePanel;

    private void Start()
    {
        // Setup tab navigation
        if (homeTabButton != null)
            homeTabButton.onClick.AddListener(() => SwitchTab(TabType.Home));
            
        if (photoSessionsTabButton != null)
            photoSessionsTabButton.onClick.AddListener(() => SwitchTab(TabType.PhotoSessions));
        
        // Show home tab by default
        SwitchTab(TabType.Home);
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(interactComputerKeycode))
        {
            ToggleComputerVision();
        }

        // Update balance display
        if (balanceText != null && PlayerBalance.Instance != null)
        {
            balanceText.text = $"Balance: ${PlayerBalance.Instance.Balance}";
        }
    }

    public void ToggleComputerVision()
    {
        if (computerCamera.enabled)
        {
            // Exiting computer view
            computerCamera.enabled = false;
            playerCamera.enabled = true;
            mouseController.DisableFreeMouse();
            
            // Optional: play sound effect 
            // AudioManager.Instance?.PlaySound("ComputerExit");
        }
        else
        {
            // Entering computer view
            computerCamera.enabled = true;
            playerCamera.enabled = false;
            mouseController.EnableFreeMouse();
            
            // Optional: play sound effect
            // AudioManager.Instance?.PlaySound("ComputerEnter");
        }
    }

    public enum TabType
    {
        Home,
        PhotoSessions,
        // Add more tabs as needed (Equipment, Gallery, etc.)
    }
    
    public void SwitchTab(TabType tabType)
    {
        // Hide all panels first
        if (homePanel != null) 
            homePanel.SetActive(false);
            
        if (photoSessionsPanel != null)
            photoSessionsPanel.SetActive(false);
        
        // Show the selected panel
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
        }
        
        // Update button visuals if needed
        if (homeTabButton != null)
            homeTabButton.interactable = tabType != TabType.Home;
            
        if (photoSessionsTabButton != null)
            photoSessionsTabButton.interactable = tabType != TabType.PhotoSessions;
    }
}
