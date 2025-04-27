using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using SAE_Dubai.Leonardo.Hotbar;
using SAE_Dubai.Leonardo.CameraSys;
using SAE_Dubai.Leonardo.Client_System;
using SAE_Dubai.JW;

namespace SAE_Dubai.Leonardo.Tutorial
{
    /// <summary>
    /// Manages the tutorial flow and objectives for new players.
    /// Self-contained with DOTween animations and toggle key.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private Button skipTutorialButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Tutorial Settings")]
        [Tooltip("Whether to show the tutorial")]
        [SerializeField] private bool enableTutorial = true;
        [SerializeField] private float objectiveCompleteDelay = 1.5f;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private Ease fadeInEase = Ease.OutQuad;
        [SerializeField] private Ease fadeOutEase = Ease.InQuad;
        [SerializeField] private Vector2 slideOffset = new Vector2(-50f, 0f);
        
        [Header("Toggle Settings")]
        [SerializeField] private KeyCode toggleTutorialKey = KeyCode.F1;
        [SerializeField] private bool hideAfterCompleting = true;

        // Reference to other systems
        private Hotbar.Hotbar playerHotbar;
        private CameraManager cameraManager;
        private PhotoSessionManager sessionManager;
        private ComputerUI computerUI;

        // Tutorial state
        private int currentObjectiveIndex = -1;
        private bool tutorialActive = false;
        private bool tutorialCompleted = false;
        private bool currentObjectiveComplete = false;
        private bool isPanelVisible = false;
        private RectTransform panelRectTransform;
        private bool boughtCamera = false;

        // Define all tutorial objectives
        private List<TutorialObjective> objectives = new List<TutorialObjective>();

        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Get the RectTransform for animations
            if (tutorialPanel != null)
            {
                panelRectTransform = tutorialPanel.GetComponent<RectTransform>();
            }
            
            // Make sure we have a CanvasGroup for fading
            if (panelCanvasGroup == null && tutorialPanel != null)
            {
                panelCanvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                {
                    panelCanvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
                }
            }
        }

        private void Start()
        {
            // Find references to needed components
            playerHotbar = FindObjectOfType<Hotbar.Hotbar>();
            cameraManager = FindObjectOfType<CameraManager>();
            sessionManager = FindObjectOfType<PhotoSessionManager>();
            computerUI = FindObjectOfType<ComputerUI>();

            // Setup skip button
            if (skipTutorialButton != null)
            {
                skipTutorialButton.onClick.AddListener(SkipTutorial);
            }

            // Define tutorial objectives (order matters)
            SetupTutorialObjectives();

            // Initialize UI to hidden state
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
                isPanelVisible = false;
            }

            // Start tutorial if enabled in editor
            if (enableTutorial)
            {
                StartTutorial();
            }
            
            Debug.Log($"ComputerUI found: {computerUI != null}");
        }

        private void SetupTutorialObjectives()
        {
            // ! Tutorials must be in sequential order.
            objectives.Clear();

            //* Done.
            objectives.Add(new TutorialObjective(
                "Find Your Computer",
                "Look around your room and locate your computer. Approach it and press E to interact.",
                () => computerUI != null && computerUI.IsPlayerUsingComputer(),
                "Movement: WASD | Look: Mouse | Interact: E"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Visit Camera Shop",
                "Click on the Camera Shop tab in your computer interface.",
                () => computerUI != null && computerUI.IsShopTabActive(),
                "Navigate to the Camera Shop tab in the computer interface"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Purchase Your First Camera",
                "Select a camera and click 'Purchase' to buy it.",
                () => boughtCamera,
                "Select a camera model and click the Purchase button"
            ));
            
            //* Done.
            objectives.Add(new TutorialObjective(
                "Exit Computer",
                "Close the computer interface to return to the game world.",
                () => computerUI != null && !computerUI.IsPlayerUsingComputer(),
                "Confirm the purchase and press E or ESC to exit the computer interface"
            ));
            
            //* Done.
            objectives.Add(new TutorialObjective(
                "Pick Up Your Camera",
                "Approach your new camera and hold E to pick it up.",
                () => playerHotbar != null && playerHotbar.HasAnyEquipment(),
                "Approach the camera and hold E to pick it up"
            ));

            objectives.Add(new TutorialObjective(
                "Turn On The Camera",
                "To work, the camera needs to be turned on.",
                () => cameraManager.GetActiveCamera() != null && Input.GetKeyDown(KeyCode.C),
                "Press C to turn on the camera while equipped."
            ));
            
            // Todo: Teach ISO, Aperture and Shutter Speed controls.
            
            // Todo: Tell the player that they can open their guide when pressing "J" and add the actual canvas.
            
            objectives.Add(new TutorialObjective(
                "Focus and Take a Photo",
                "Right-click to focus, then left-click to take a photo.",
                () => cameraManager.GetActiveCamera() != null && cameraManager.GetActiveCamera().GetPhotoCount() > 0,
                "Focus: Right Mouse Button | Take photo: Left Mouse Button"
            ));

            objectives.Add(new TutorialObjective(
                "Return to Computer",
                "Go back to your computer and press E to use it.",
                () => computerUI != null && computerUI.IsPlayerUsingComputer(),
                "Move back to your computer and press E"
            ));

            objectives.Add(new TutorialObjective(
                "Find Photo Sessions",
                "Go back to the menu and click on the Photo Sessions tab in your computer.",
                () => computerUI != null && computerUI.IsSessionsTabActive(),
                "Go back to the menu and click on the Customer App in your computer."
            ));

            objectives.Add(new TutorialObjective(
                "Accept a Photo Session",
                "Click on the client to accept the session.",
                () => sessionManager != null && sessionManager.GetActiveSessions().Count > 0,
                "Review available clients and click Accept on one you like. Remember to check each clients necessities!"
            ));
            
            objectives.Add(new TutorialObjective(
                "Travel To The Client's Location",
                "Select a client job and click 'Accept' to start the session.",
                () => {
                    if (sessionManager == null) return false;

                    List<PhotoSession> activeSessions = sessionManager.GetActiveSessions();

                    if (activeSessions.Count == 0) return false;

                    PhotoSession tutorialSession = activeSessions[0];

                    return tutorialSession != null && tutorialSession.isClientSpawned;
                },
                "Click on the travel button in the Active Sessions tab in your customer app to travel to where the client is waiting for you!"
            ));

            // ? Auto-complete this step? .
            // Todo: "Complete tutorial button".
            objectives.Add(new TutorialObjective(
                "Complete Tutorial",
                "Congratulations! You've completed the tutorial. You can now travel to your client by clicking 'Travel'.",
                () => true, 
                "Press ESC to open pause menu anytime to see your objectives\nPress " + toggleTutorialKey + " to show/hide this panel"
            ));
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleTutorialKey))
            {
                ToggleTutorialPanel();
            }
        
            if (!tutorialActive || currentObjectiveIndex < 0 || currentObjectiveIndex >= objectives.Count)
                return;

            // Check if current objective is complete.
            TutorialObjective currentObjective = objectives[currentObjectiveIndex];
            if (!currentObjectiveComplete && currentObjective.IsComplete())
            {
                currentObjectiveComplete = true;
                StartCoroutine(AdvanceToNextObjective());
            }

            // Update UI.
            UpdateTutorialUI();
        }

        private IEnumerator AdvanceToNextObjective()
        {
            // Wait a moment before advancing to next objective
            yield return new WaitForSeconds(objectiveCompleteDelay);
            
            if (currentObjectiveIndex < objectives.Count - 1)
            {
                currentObjectiveIndex++;
                currentObjectiveComplete = false;
                
                // Show panel with new objective
                if (!isPanelVisible)
                {
                    ShowTutorialPanel();
                }
                
                // Flash the panel to indicate a new objective
                if (panelCanvasGroup != null)
                {
                    // Brief flash effect
                    panelCanvasGroup.DOKill();
                    Sequence flashSequence = DOTween.Sequence();
                    flashSequence.Append(panelCanvasGroup.DOFade(0.2f, 0.2f))
                                 .Append(panelCanvasGroup.DOFade(1f, 0.3f));
                }
            }
            else
            {
                CompleteTutorial();
            }
        }

        private void UpdateTutorialUI()
        {
            if (currentObjectiveIndex < 0 || currentObjectiveIndex >= objectives.Count)
                return;

            TutorialObjective objective = objectives[currentObjectiveIndex];
            
            if (objectiveText != null)
                objectiveText.text = objective.Title;
                
            if (instructionText != null)
                instructionText.text = objective.Instructions;

            // Update progress bar.
            if (progressBar != null)
            {
                float targetProgress = (float)(currentObjectiveIndex) / (float)(objectives.Count - 1);
                // Smooth progress bar animation.
                DOTween.To(() => progressBar.fillAmount, 
                           x => progressBar.fillAmount = x, 
                           targetProgress, 0.5f)
                       .SetEase(Ease.OutQuad);  
            }
        }

        public void StartTutorial()
        {
            tutorialActive = true;
            tutorialCompleted = false;
            currentObjectiveIndex = 0;
            currentObjectiveComplete = false;
            ShowTutorialPanel();
            UpdateTutorialUI();
            
            Debug.Log("Tutorial started");
        }

        public void CompleteTutorial()
        {
            tutorialActive = false;
            tutorialCompleted = true;
            
            if (hideAfterCompleting)
            {
                HideTutorialPanel();
            }
            
            Debug.Log("Tutorial completed");
        }

        public void SkipTutorial()
        {
            CompleteTutorial();
        }

        private void ShowTutorialPanel()
        {
            if (tutorialPanel == null) return;
            
            // Ensure the panel is active before animating
            tutorialPanel.SetActive(true);
            
            // Reset position if using slide animation
            if (panelRectTransform != null)
            {
                Vector2 originalPosition = panelRectTransform.anchoredPosition;
                Vector2 startPosition = originalPosition + slideOffset;
                panelRectTransform.anchoredPosition = startPosition;
                
                // Animate slide in
                panelRectTransform.DOAnchorPos(originalPosition, fadeInDuration)
                                  .SetEase(fadeInEase);
            }
            
            // Fade in
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0;
                panelCanvasGroup.DOFade(1f, fadeInDuration)
                                .SetEase(fadeInEase);
            }
            
            isPanelVisible = true;
        }

        private void HideTutorialPanel()
        {
            if (tutorialPanel == null) return;
            
            // Animate slide out
            if (panelRectTransform != null)
            {
                Vector2 endPosition = panelRectTransform.anchoredPosition + slideOffset;
                panelRectTransform.DOAnchorPos(endPosition, fadeOutDuration)
                                 .SetEase(fadeOutEase);
            }
            
            // Fade out and deactivate at the end
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.DOFade(0f, fadeOutDuration)
                                .SetEase(fadeOutEase)
                                .OnComplete(() => tutorialPanel.SetActive(false));
            }
            else
            {
                // If no canvas group, deactivate after animation time
                StartCoroutine(DeactivateAfterDelay(fadeOutDuration));
            }
            
            isPanelVisible = false;
        }
        
        private IEnumerator DeactivateAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }
        
        public void ToggleTutorialPanel()
        {
            if (isPanelVisible)
            {
                HideTutorialPanel();
            }
            else
            {
                ShowTutorialPanel();
            }
        }

        // Toggle tutorial visibility (can be called from other scripts if needed)
        public void SetTutorialEnabled(bool enabled)
        {
            enableTutorial = enabled;
            
            if (enabled && !tutorialActive && !tutorialCompleted)
            {
                StartTutorial();
            }
            else if (!enabled && tutorialActive)
            {
                CompleteTutorial();
            }
        }

        public void SetCameraBoughtFlag() {
            boughtCamera = true;
        }
    }

    /// <summary>
    /// Represents a single tutorial objective with completion criteria
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