using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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
        [SerializeField] private float sceneSwitchDelay = 1f;
        [SerializeField] private int tutorialSceneIndex = 1;
        [SerializeField] private int gameSceneIndex = 2;
        private bool isTutorialSelected = false;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private List<AudioClip> audioClips;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

        private enum PanelType
        {
            MainMenu = 0,
            Credits = 1,
            Quit = 2,
            Play = 3,
        }

        public void SwitchPanel(int panelType)
        {
            ButtonClicked();
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
            ButtonClicked();
        }

        public void SwitchScenes()
        {
            ButtonClicked();

            StartCoroutine(nameof(LoadSceneAtIndex));
        }

        private IEnumerator LoadSceneAtIndex()
        {
            yield return new WaitForSeconds(sceneSwitchDelay);
            SceneManager.LoadScene(isTutorialSelected ? tutorialSceneIndex : gameSceneIndex, LoadSceneMode.Single);
        }

        public void ButtonClicked()
        {
            audioSource.Stop(); // Stop any sound in case there is one playing
            int clipIndex = Random.Range(0, audioClips.Count); // get a random clip to play
            audioSource.clip = audioClips[clipIndex];
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y); // Randomly vary the pitch
            audioSource.Play();
        }
    }
}