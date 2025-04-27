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
        private PanelType _currentPanel = PanelType.MainMenu;

        [Header("Scene Switching")]
        [SerializeField] private float sceneSwitchDelay = 1f;

        [SerializeField] private int gameSceneIndex = 1;
        private bool _isTutorialSelected;

        // Key for PlayerPrefs
        public static readonly string tutorialPreferenceKey = "StartTutorial";

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

        public void SwitchPanel(int panelType) {
            ButtonClicked();
            _currentPanel = (PanelType)panelType;
            switch (_currentPanel) {
                case PanelType.MainMenu:
                    mainMenuPanel.SetActive(true);
                    creditsPanel.SetActive(false);
                    quitPanel.SetActive(false);
                    playPanel.SetActive(false);
                    _currentPanel = PanelType.MainMenu;
                    break;
                case PanelType.Credits:
                    creditsPanel.SetActive(true);
                    quitPanel.SetActive(false);
                    mainMenuPanel.SetActive(false);
                    playPanel.SetActive(false);
                    _currentPanel = PanelType.Credits;
                    break;
                case PanelType.Quit:
                    quitPanel.SetActive(true);
                    mainMenuPanel.SetActive(false);
                    creditsPanel.SetActive(false);
                    playPanel.SetActive(false);
                    _currentPanel = PanelType.Quit;
                    break;
                case PanelType.Play:
                    playPanel.SetActive(true);
                    mainMenuPanel.SetActive(false);
                    creditsPanel.SetActive(false);
                    quitPanel.SetActive(false);
                    _currentPanel = PanelType.Play;
                    break;
            }
        }

        public void SetTutorialSelected(bool selected) {
            _isTutorialSelected = selected;
            ButtonClicked();
            Debug.Log("Tutorial Selected: " + _isTutorialSelected); // Added for debugging
        }

        public void SwitchScenes() {
            ButtonClicked();

            // Save the tutorial preference before loading the scene
            PlayerPrefs.SetInt(tutorialPreferenceKey, _isTutorialSelected ? 1 : 0);
            PlayerPrefs.Save(); // Ensure it's saved immediately
            Debug.Log("Saved Tutorial Preference: " + (_isTutorialSelected ? 1 : 0)); // Added for debugging

            StartCoroutine(nameof(LoadSceneAtIndex));
        }

        private IEnumerator LoadSceneAtIndex() {
            yield return new WaitForSeconds(sceneSwitchDelay);
            // Always load the game scene
            SceneManager.LoadScene(gameSceneIndex, LoadSceneMode.Single);
        }

        public void ButtonClicked() {
            if (audioSource == null || audioClips == null || audioClips.Count == 0) return;

            audioSource.Stop(); // Stop any sound in case there is one playing
            int clipIndex = Random.Range(0, audioClips.Count); // get a random clip to play
            audioSource.clip = audioClips[clipIndex];
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y); // Randomly vary the pitch
            audioSource.Play();
        }
    }
}