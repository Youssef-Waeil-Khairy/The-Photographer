using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE_Dubai.JW.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject quitPanel;
        [SerializeField] private GameObject playPanel;
        private PanelType currentPanel = PanelType.MainMenu;
        

        [Header("Scene Switching")] 
        [SerializeField] private int tutorialSceneIndex = 1;
        [SerializeField] private int gameSceneIndex = 2;
        private bool isTutorialSelected = false;

        private enum PanelType
        {
            MainMenu = 0,
            Credits = 1,
            Quit = 2,
            Play = 3,
        }

        public void SwitchPanel(int panelType)
        {
            currentPanel = (PanelType)panelType;
            switch (currentPanel)
            {
                case PanelType.MainMenu:
                    mainMenuPanel.SetActive(true);
                    creditsPanel.SetActive(false);
                    quitPanel.SetActive(false);
                    playPanel.SetActive(false);
                    currentPanel = PanelType.MainMenu;
                    break;
                case PanelType.Credits:
                    creditsPanel.SetActive(true);
                    quitPanel.SetActive(false);
                    mainMenuPanel.SetActive(false);
                    playPanel.SetActive(false);
                    currentPanel = PanelType.Credits;
                    break;
                case PanelType.Quit:
                    quitPanel.SetActive(true);
                    mainMenuPanel.SetActive(false);
                    creditsPanel.SetActive(false);
                    playPanel.SetActive(false);
                    currentPanel = PanelType.Quit;
                    break;
                case PanelType.Play:
                    playPanel.SetActive(true);
                    mainMenuPanel.SetActive(false);
                    creditsPanel.SetActive(false);
                    quitPanel.SetActive(false);
                    currentPanel = PanelType.Play;
                    break;
            }
        }

        public void SetTutorialSelected(bool selected)
        {
            isTutorialSelected = selected;
        }

        public void SwitchScenes()
        {
            SceneManager.LoadScene(isTutorialSelected ? tutorialSceneIndex : gameSceneIndex, LoadSceneMode.Single);
        }
    }
}