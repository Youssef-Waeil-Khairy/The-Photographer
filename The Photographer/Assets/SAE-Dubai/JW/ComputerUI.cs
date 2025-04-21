using System;
using SAE_Dubai.JW;
using SAE_Dubai.Leonardo;
using TMPro;
using UnityEngine;

public class ComputerUI : MonoBehaviour
{
    [SerializeField] private MouseController  mouseController;
    [SerializeField] Camera computerCamera;
    [SerializeField] Camera playerCamera;
    [SerializeField] TMP_Text balanceText;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleComputerVision();
        }

        balanceText.text = $"Balance: {PlayerBalance.Instance.Balance}";
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
