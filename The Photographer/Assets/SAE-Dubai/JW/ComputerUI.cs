using System;
using SAE_Dubai.Leonardo;
using TMPro;
using UnityEngine;

public class ComputerUI : MonoBehaviour
{
    [SerializeField] private MouseController  mouseController;
    [SerializeField] Camera computerCamera;
    [SerializeField] Camera playerCamera;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleComputerVision();
        }
    }

    private void ToggleComputerVision()
    {
        if (computerCamera.enabled)
        {
            computerCamera.enabled = false;
            playerCamera.enabled = true;
            mouseController.DisableFreeMouse();
        }
        else
        {
            computerCamera.enabled = true;
            playerCamera.enabled = false;
            mouseController.EnableFreeMouse();
        }
    }
}
