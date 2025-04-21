using System;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.JW.UI
{
    /// <summary>
    /// This will go on the App UI prefab to allow it to open up and come back
    /// </summary>
    public class AppButton : MonoBehaviour
    {
        [SerializeField] private GameObject appToOpenPanel;
        [SerializeField] private GameObject appPanel;
        [SerializeField] private Button appButton;
        [SerializeField] private Button homeButton;

        private void Start()
        {
            // Subsribe the app open to the button click
        }

        private void AppClick()
        {
            if (appToOpenPanel != null)
            {
                appToOpenPanel.SetActive(true);
                appPanel.SetActive(false);
            }
        }
    }
}