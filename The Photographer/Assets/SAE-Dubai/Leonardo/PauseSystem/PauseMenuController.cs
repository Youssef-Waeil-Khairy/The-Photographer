using System.Collections.Generic;
using SAE_Dubai.Leonardo.Client_System;
using SAE_Dubai.Youssef.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.Leonardo.PauseSystem
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("- UI References")]
        [SerializeField] private GameObject pauseMenuPanel;

        [SerializeField] private Transform activeSessionsContainer;
        [SerializeField] private GameObject sessionInfoPrefab;

        [Header("- Player Controls")]
        [SerializeField] private MovementSystem playerMovement;

        [SerializeField] private MouseController mouseController;

        [Header("- Input")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        private bool _isPaused;
        private PhotoSessionManager _sessionManager;

        void Start() {
            if (pauseMenuPanel != null) {
                pauseMenuPanel.SetActive(false);
            }

            _sessionManager = PhotoSessionManager.Instance;
            if (_sessionManager == null) {
                //Debug.LogError("PauseMenuController: PhotoSessionManager instance not found!");
            }

            if (playerMovement == null) {
                playerMovement = FindFirstObjectByType<MovementSystem>();
                if (playerMovement == null) Debug.LogWarning("PauseMenuController: Player Movement script not found.");
            }

            if (mouseController == null) {
                mouseController = FindFirstObjectByType<MouseController>();
                //if (mouseController == null) Debug.LogWarning("PauseMenuController: Mouse Controller script not found.");
            }
        }

        void Update() {
            if (Input.GetKeyDown(pauseKey)) {
                // If game is already paused, resume it, otherwise pause it
                if (_isPaused) {
                    ResumeGame();
                } else {
                    TogglePause();
                }
            }
        }

        public void TogglePause() {
            _isPaused = !_isPaused;

            if (_isPaused) {
                PauseGame();
            }
            else {
                ResumeGame();
            }
        }

        private void PauseGame() {
            Time.timeScale = 0f;

            if (pauseMenuPanel != null) {
                pauseMenuPanel.SetActive(true);
                UpdateActiveSessionsDisplay();
            }

            if (playerMovement != null) playerMovement.enabled = false;
            if (mouseController != null) mouseController.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _isPaused = true;
            //Debug.Log("PauseMenuController.cs: Game Paused");
        }

        public void ResumeGame() {
            if (!_isPaused && Time.timeScale != 1f) return;

            Time.timeScale = 1f;

            if (pauseMenuPanel != null) {
                pauseMenuPanel.SetActive(false);
            }

            if (playerMovement != null) playerMovement.enabled = true;
            if (mouseController != null) mouseController.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _isPaused = false;
            //Debug.Log("PauseMenuController.cs: Game Resumed");
        }

private void UpdateActiveSessionsDisplay()
    {
        if (_sessionManager == null || activeSessionsContainer == null || sessionInfoPrefab == null)
        {
            //Debug.LogWarning("PauseMenuController: Cannot update active sessions display - References missing.");
            return;
        }

        // Clear previous session items
        foreach (Transform child in activeSessionsContainer)
        {
            Destroy(child.gameObject);
        }

        // Get current active sessions
        List<PhotoSession> activeSessions = _sessionManager.GetActiveSessions();

        if (activeSessions.Count == 0)
        {
            // Optional: Display a message if there are no active sessions
             GameObject noSessionsText = new GameObject("NoSessionsText");
            noSessionsText.transform.SetParent(activeSessionsContainer, false); // Set worldPositionStays to false
            TextMeshProUGUI textComponent = noSessionsText.AddComponent<TextMeshProUGUI>();
            textComponent.text = "No active photo sessions.";
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 18;
            LayoutElement layoutElement = noSessionsText.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 50;
        }
        else
        {
            foreach (PhotoSession session in activeSessions)
            {
                GameObject sessionItemGO = Instantiate(sessionInfoPrefab, activeSessionsContainer);

                PauseSessionItemUI itemUI = sessionItemGO.GetComponent<PauseSessionItemUI>();

                if (itemUI != null)
                {
                    itemUI.Setup(session);
                }
                else
                {
                    //Debug.LogError($"PauseMenuController: Prefab '{sessionInfoPrefab.name}' is missing the PauseSessionItemUI script!", sessionInfoPrefab);
                    // Optionally destroy the problematic instance
                    // Destroy(sessionItemGO);
                }
            }
        }
    }
        /// <summary>
        /// Quits the game, this won't work on the editor lol.
        /// </summary>
        public void QuitGame() {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}