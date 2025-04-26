using System.Collections.Generic;
using SAE_Dubai.Leonardo.Client_System;
using TMPro;
using UnityEngine;

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
                Debug.LogError("PauseMenuController: PhotoSessionManager instance not found!");
            }

            if (playerMovement == null) {
                playerMovement = FindObjectOfType<MovementSystem>();
                if (playerMovement == null) Debug.LogWarning("PauseMenuController: Player Movement script not found.");
            }

            if (mouseController == null) {
                mouseController = FindObjectOfType<MouseController>();
                if (mouseController == null)
                    Debug.LogWarning("PauseMenuController: Mouse Controller script not found.");
            }
        }

        void Update() {
            if (Input.GetKeyDown(pauseKey)) {
                TogglePause();
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

            Debug.Log("Game Paused");
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
            Debug.Log("Game Resumed");
        }

        private void UpdateActiveSessionsDisplay() {
            if (_sessionManager == null || activeSessionsContainer == null || sessionInfoPrefab == null) {
                Debug.LogWarning("Cannot update active sessions display: References missing.");
                return;
            }

            foreach (Transform child in activeSessionsContainer) {
                Destroy(child.gameObject);
            }

            List<PhotoSession> activeSessions = _sessionManager.GetActiveSessions();

            if (activeSessions.Count == 0) {
                GameObject noSessionsText = new GameObject("NoSessionsText");
                noSessionsText.transform.SetParent(activeSessionsContainer);
                TextMeshProUGUI textComponent = noSessionsText.AddComponent<TextMeshProUGUI>();
                textComponent.text = "No active photo sessions.";
                textComponent.alignment = TextAlignmentOptions.Center;
                textComponent.fontSize = 18;
            }
            else {
                foreach (PhotoSession session in activeSessions) {
                    GameObject sessionItemGo = Instantiate(sessionInfoPrefab, activeSessionsContainer);
                    TextMeshProUGUI clientNameText =
                        sessionItemGo.transform.Find("ClientNameText")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI compositionText =
                        sessionItemGo.transform.Find("CompositionText")?.GetComponent<TextMeshProUGUI>();

                    if (clientNameText != null) {
                        clientNameText.text = $"Client: {session.clientName}";
                    }

                    if (compositionText != null) {
                        compositionText.text = $"Wants: {session.GetShotTypeName()}";
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