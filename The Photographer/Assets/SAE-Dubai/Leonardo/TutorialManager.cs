using System;
using System.Collections;
using System.Collections.Generic;
using SAE_Dubai.JW;
using SAE_Dubai.Leonardo.CameraSys;
using SAE_Dubai.Leonardo.Client_System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.Leonardo
{
    /// <summary>
    /// Manages the tutorial flow and objectives for new players.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("- UI References")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private Button skipTutorialButton;
        [SerializeField] private Image progressBar;

        [Header("- Tutorial Settings")]
        [SerializeField] private bool enableTutorial = true;
        [SerializeField] private float objectiveCompleteDelay = 1.5f;

        private Hotbar.Hotbar _playerHotbar;
        private CameraManager _cameraManager;
        private PhotoSessionManager _sessionManager;
        private ComputerUI _computerUI;

        private int _currentObjectiveIndex = -1;
        private bool _tutorialActive;
        private bool _currentObjectiveComplete;

        private readonly List<TutorialObjective> _objectives = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (PlayerPrefs.HasKey("TutorialCompleted") && PlayerPrefs.GetInt("TutorialCompleted") == 1)
            {
                enableTutorial = false;
            }
        }

        private void Start()
        {
            _playerHotbar = FindFirstObjectByType<Hotbar.Hotbar>();
            _cameraManager = FindFirstObjectByType<CameraManager>();
            _sessionManager = FindFirstObjectByType<PhotoSessionManager>();
            _computerUI = FindFirstObjectByType<ComputerUI>();

            if (skipTutorialButton != null)
            {
                skipTutorialButton.onClick.AddListener(SkipTutorial);
            }

            SetupTutorialObjectives();

            if (enableTutorial)
            {
                StartTutorial();
            }
            else
            {
                HideTutorialPanel();
            }
        }

        private void SetupTutorialObjectives()
        {
            _objectives.Clear();

            _objectives.Add(new TutorialObjective(
                "Find Your Computer",
                "Look around your room and locate your computer. Approach it and press E to interact.",
                () => _computerUI != null && _computerUI.IsPlayerUsingComputer(),
                "Movement: WASD | Look: Mouse | Interact: E"
            ));

            _objectives.Add(new TutorialObjective(
                "Visit Camera Shop",
                "Click on the Camera Shop tab in your computer interface.",
                () => _computerUI != null && _computerUI.IsShopTabActive(),
                "Navigate to the Camera Shop tab in the computer interface"
            ));

            _objectives.Add(new TutorialObjective(
                "Purchase Your First Camera",
                "Select a camera and click 'Purchase' to buy it.",
                () => _cameraManager != null && _cameraManager.GetCameraCount() > 0,
                "Select a camera model and click the Purchase button"
            ));

            _objectives.Add(new TutorialObjective(
                "Pick Up Your Camera",
                "Exit the computer (ESC or E) and approach your new camera. Hold E to pick it up.",
                () => _playerHotbar != null && _playerHotbar.HasAnyEquipment(),
                "Exit: ESC or E | To pick up: look at camera and hold E"
            ));

            _objectives.Add(new TutorialObjective(
                "Equip Your Camera",
                "Select your camera from the hotbar using number keys (1-5) or mouse wheel.",
                () => _playerHotbar.GetSelectedEquipment() != "" && _cameraManager.GetActiveCamera() != null,
                "Select item: Number keys 1-5 or Mouse wheel"
            ));

            _objectives.Add(new TutorialObjective(
                "Turn On Your Camera",
                "Press C to turn your camera on.",
                () => _cameraManager.GetActiveCamera() != null && _cameraManager.GetActiveCamera().isCameraOn,
                "Turn camera on/off: C"
            ));

            _objectives.Add(new TutorialObjective(
                "Focus and Take a Photo",
                "Right-click to focus, then left-click to take a photo.",
                () => _cameraManager.GetActiveCamera() != null && _cameraManager.GetActiveCamera().GetPhotoCount() > 0,
                "Focus: Right Mouse Button | Take photo: Left Mouse Button"
            ));

            _objectives.Add(new TutorialObjective(
                "Return to Computer",
                "Go back to your computer and press E to use it.",
                () => _computerUI != null && _computerUI.IsPlayerUsingComputer(),
                "Move back to your computer and press E"
            ));

            _objectives.Add(new TutorialObjective(
                "Find Photo Sessions",
                "Click on the Photo Sessions tab in your computer.",
                () => _computerUI != null && _computerUI.IsSessionsTabActive(),
                "Click on the Photo Sessions tab"
            ));

            _objectives.Add(new TutorialObjective(
                "Accept a Photo Session",
                "Select a client job and click 'Accept' to start the session.",
                () => _sessionManager != null && _sessionManager.GetActiveSessions().Count > 0,
                "Review available clients and click Accept on one you like"
            ));

            _objectives.Add(new TutorialObjective(
                "Complete Tutorial",
                "Congratulations! You've completed the tutorial. You can now travel to your client by clicking 'Travel'.",
                () => true, // ! Auto-complete this step.
                "Press ESC to open pause menu anytime to see your objectives"
            ));
        }

        private void Update()
        {
            if (!_tutorialActive || _currentObjectiveIndex < 0 || _currentObjectiveIndex >= _objectives.Count)
                return;

            TutorialObjective currentObjective = _objectives[_currentObjectiveIndex];
            if (!_currentObjectiveComplete && currentObjective.IsComplete())
            {
                _currentObjectiveComplete = true;
                StartCoroutine(AdvanceToNextObjective());
            }

            UpdateTutorialUI();
        }

        private IEnumerator AdvanceToNextObjective()
        {
            // * Wait a moment before advancing to next objective.
            yield return new WaitForSeconds(objectiveCompleteDelay);
            
            if (_currentObjectiveIndex < _objectives.Count - 1)
            {
                _currentObjectiveIndex++;
                _currentObjectiveComplete = false;
            }
            else
            {
                CompleteTutorial();
            }
        }

        private void UpdateTutorialUI()
        {
            if (_currentObjectiveIndex < 0 || _currentObjectiveIndex >= _objectives.Count)
                return;

            TutorialObjective objective = _objectives[_currentObjectiveIndex];
            
            if (objectiveText != null)
                objectiveText.text = objective.Title;
                
            if (instructionText != null)
                instructionText.text = objective.Instructions;

            // Update progress bar.
            if (progressBar != null)
            {
                float progress = (float)(_currentObjectiveIndex) / (float)(_objectives.Count - 1);
                progressBar.fillAmount = progress;
            }
        }

        public void StartTutorial()
        {
            _tutorialActive = true;
            _currentObjectiveIndex = 0;
            _currentObjectiveComplete = false;
            ShowTutorialPanel();
            UpdateTutorialUI();
        }

        public void CompleteTutorial()
        {
            _tutorialActive = false;
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();
            HideTutorialPanel();
        }

        public void SkipTutorial()
        {
            CompleteTutorial();
        }

        private void ShowTutorialPanel()
        {
            if (tutorialPanel != null)
                tutorialPanel.SetActive(true);
        }

        private void HideTutorialPanel()
        {
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }

        public void ResetTutorial()
        {
            PlayerPrefs.DeleteKey("TutorialCompleted");
            _currentObjectiveIndex = 0;
            _currentObjectiveComplete = false;
            _tutorialActive = true;
            ShowTutorialPanel();
            UpdateTutorialUI();
        }
    }

    /// <summary>
    /// Represents a single tutorial objective with completion criteria.
    /// </summary>
    [Serializable]
    public class TutorialObjective
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Instructions { get; private set; }
        private Func<bool> CompletionCheck { get; set; }

        public TutorialObjective(string title, string description, Func<bool> completionCheck, string instructions)
        {
            Title = title;
            Description = description;
            CompletionCheck = completionCheck;
            Instructions = instructions;
        }

        public bool IsComplete()
        {
            return CompletionCheck != null && CompletionCheck();
        }
    }
}