using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
        [Tooltip("Whether to show the tutorial")] [SerializeField]
        private bool enableTutorial = true;

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

        private Hotbar.Hotbar playerHotbar;
        private CameraManager cameraManager;
        private PhotoSessionManager sessionManager;
        private ComputerUI computerUI;

        private int currentObjectiveIndex = -1;
        private bool tutorialActive;
        private bool tutorialCompleted;
        private bool currentObjectiveComplete;
        private bool isPanelVisible;
        private RectTransform panelRectTransform;
        private bool boughtCamera;
        private bool hasCompletedFirstJob = false;
        private bool hasReturnedToApartment = false;
        
        private List<TutorialObjective> objectives = new();

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }

            if (tutorialPanel != null) {
                panelRectTransform = tutorialPanel.GetComponent<RectTransform>();
            }

            if (panelCanvasGroup == null && tutorialPanel != null) {
                panelCanvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null) {
                    panelCanvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
                }
            }
        }

        private void Start() {
            playerHotbar = FindObjectOfType<Hotbar.Hotbar>();
            cameraManager = FindObjectOfType<CameraManager>();
            sessionManager = FindObjectOfType<PhotoSessionManager>();
            computerUI = FindObjectOfType<ComputerUI>();

            if (skipTutorialButton != null) {
                skipTutorialButton.onClick.AddListener(SkipTutorial);
            }

            SetupTutorialObjectives();

            if (tutorialPanel != null) {
                tutorialPanel.SetActive(false);
                isPanelVisible = false;
            }

            if (enableTutorial) {
                StartTutorial();
            }

            Debug.Log($"ComputerUI found: {computerUI != null}");
        }

        private void SetupTutorialObjectives() {
            // ! Tutorials must be in sequential order.
            objectives.Clear();

            //* Done.
            objectives.Add(new TutorialObjective(
                "Find Your Computer",
                () => computerUI != null && computerUI.IsPlayerUsingComputer(),
                "Movement: WASD | Look: Mouse | Interact: E"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Visit Camera Shop",
                () => computerUI != null && computerUI.IsShopTabActive(),
                "Navigate to the Camera Shop tab in the computer interface"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Purchase Your First Camera",
                () => boughtCamera,
                "Select a camera model and click the Purchase button"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Exit Computer",
                () => computerUI != null && !computerUI.IsPlayerUsingComputer(),
                "Confirm the purchase and press E or ESC to exit the computer interface"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Pick Up Your Camera",
                () => playerHotbar != null && playerHotbar.HasAnyEquipment(),
                "Approach the camera and hold E to pick it up"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Turn On The Camera",
                () => cameraManager.GetActiveCamera() != null && Input.GetKeyDown(KeyCode.C),
                "Press C to turn on the camera while equipped."
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Adjust ISO",
                () => {
                    CameraSystem activeCam = cameraManager?.GetActiveCamera();
                    return activeCam != null && activeCam.isCameraOn && activeCam.GetCurrentISO() >= 6400;
                },
                "Use the I key to increase ISO until it reaches 6400. Use U to decrease if you go too far."
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Adjust Aperture",
                () => {
                    CameraSystem activeCam = cameraManager?.GetActiveCamera();
                    return activeCam != null && activeCam.isCameraOn &&
                           Mathf.Abs(activeCam.GetCurrentAperture() - 5.6f) > 0.1f;
                },
                "Press O for a smaller opening (higher f-number, more in focus) or P for a larger opening (lower f-number, shallower focus). Try changing it!"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Adjust Shutter Speed",
                () => {
                    CameraSystem activeCam = cameraManager?.GetActiveCamera();
                    return activeCam != null && activeCam.isCameraOn &&
                           Mathf.Abs(activeCam.GetCurrentShutterSpeed() - (1f / 125f)) > 0.001f;
                },
                "Press K for a slower speed (more light/blur) or L for a faster speed (less light/blur). Give it a try!"
            ));
            
            //* Done.
            objectives.Add(new TutorialObjective(
                "Focus and Take a Photo",
                () => cameraManager.GetActiveCamera() != null && cameraManager.GetActiveCamera().GetPhotoCount() > 0,
                "Focus: Right Mouse Button | Take photo: Left Mouse Button"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Return to Computer",
                () => computerUI != null && computerUI.IsPlayerUsingComputer(),
                "Move back to your computer and press E"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Find Photo Sessions",
                () => computerUI != null && computerUI.IsSessionsTabActive(),
                "Go back to the menu and click on the Customer App in your computer."
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Accept a Photo Session",
                () => sessionManager != null && sessionManager.GetActiveSessions().Count > 0,
                "Review available clients and click Accept on one you like. Remember to check each clients necessities!"
            ));

            //* Done.
            objectives.Add(new TutorialObjective(
                "Travel To The Client's Location",
                () => {
                    if (sessionManager == null) return false;

                    List<PhotoSession> activeSessions = sessionManager.GetActiveSessions();

                    if (activeSessions.Count == 0) return false;

                    PhotoSession tutorialSession = activeSessions[0];

                    return tutorialSession != null && tutorialSession.isClientSpawned;
                },
                "Click on the travel button in the Active Sessions tab in your customer app to travel to where the client is waiting for you! DON'T FORGET YOUR CAMERA!"
            ));
            
            //* Done.
            objectives.Add(new TutorialObjective(
                "Check Your GuideBook",
                () => Input.GetKeyDown(FindFirstObjectByType<GuideBookController>().toggleKey), // ? Dummy proofing again, probably not optimized.
                $"You've arrived! Press '{FindFirstObjectByType<GuideBookController>().toggleKey}' at any time to open your GuideBook for reminders on controls or shot types."
            ));

            objectives.Add(new TutorialObjective(
                "Complete the Photo Session",
                // ! Completion Check: Wait for the flag to be set by PhotoSessionManager.
                () => hasCompletedFirstJob,
                "Approach the client. Use your camera ('C'), adjust settings if needed (I/U, O/P, K/L), focus (Right Mouse), and take the required photo (Left Mouse). Check needs via Pause Menu (ESC) or GuideBook (G)."
            ));

            // --- NEW OBJECTIVE: Return Home ---
            objectives.Add(new TutorialObjective(
                "Return to Your Apartment",
                // ! Completion Check: Wait for the flag to be set by the Teleporter.
                () => hasReturnedToApartment,
                "Great job! Find the Teleporter nearby (look for the interaction prompt) and press E to return to your apartment."
            ));
            
            // ? Auto-complete this step? .
            // Todo: "Complete tutorial button".
            objectives.Add(new TutorialObjective(
                "Complete Tutorial",
                () => true,
                "Press ESC to open pause menu anytime to see your objectives\nPress " + toggleTutorialKey +
                " to show/hide this panel"
            ));
        }

        private void Update() {
            if (Input.GetKeyDown(toggleTutorialKey)) {
                ToggleTutorialPanel();
            }

            if (!tutorialActive || currentObjectiveIndex < 0 || currentObjectiveIndex >= objectives.Count)
                return;

            // Check if current objective is complete.
            TutorialObjective currentObjective = objectives[currentObjectiveIndex];
            if (!currentObjectiveComplete && currentObjective.IsComplete()) {
                currentObjectiveComplete = true;
                StartCoroutine(AdvanceToNextObjective());
            }

            // Update UI.
            UpdateTutorialUI();
        }

        private IEnumerator AdvanceToNextObjective() {
            // Wait a moment before advancing to next objective
            yield return new WaitForSeconds(objectiveCompleteDelay);

            if (currentObjectiveIndex < objectives.Count - 1) {
                currentObjectiveIndex++;
                currentObjectiveComplete = false;

                // Show panel with new objective
                if (!isPanelVisible) {
                    ShowTutorialPanel();
                }

                // Flash the panel to indicate a new objective
                if (panelCanvasGroup != null) {
                    // Brief flash effect
                    panelCanvasGroup.DOKill();
                    Sequence flashSequence = DOTween.Sequence();
                    flashSequence.Append(panelCanvasGroup.DOFade(0.2f, 0.2f))
                        .Append(panelCanvasGroup.DOFade(1f, 0.3f));
                }
            }
            else {
                CompleteTutorial();
            }
        }

        private void UpdateTutorialUI() {
            if (currentObjectiveIndex < 0 || currentObjectiveIndex >= objectives.Count)
                return;

            TutorialObjective objective = objectives[currentObjectiveIndex];

            if (objectiveText != null)
                objectiveText.text = objective.Title;

            if (instructionText != null)
                instructionText.text = objective.Instructions;

            // Update progress bar.
            if (progressBar != null) {
                float targetProgress = (float)(currentObjectiveIndex) / (float)(objectives.Count - 1);
                // Smooth progress bar animation.
                DOTween.To(() => progressBar.fillAmount,
                        x => progressBar.fillAmount = x,
                        targetProgress, 0.5f)
                    .SetEase(Ease.OutQuad);
            }
        }

        public void StartTutorial() {
            tutorialActive = true;
            tutorialCompleted = false;
            currentObjectiveIndex = 0;
            currentObjectiveComplete = false;
            ShowTutorialPanel();
            UpdateTutorialUI();

            Debug.Log("Tutorial started");
        }

        public void CompleteTutorial() {
            tutorialActive = false;
            tutorialCompleted = true;

            if (hideAfterCompleting) {
                HideTutorialPanel();
            }

            Debug.Log("Tutorial completed");
        }

        public void SkipTutorial() {
            CompleteTutorial();
        }
        
        /// <summary>
        /// Called by PhotoSessionManager when the first tutorial job is completed.
        /// </summary>
        public void NotifyFirstJobCompleted()
        {
            if (tutorialActive && !hasCompletedFirstJob)
            {
                Debug.Log("TutorialManager: First job completed flag set.");
                hasCompletedFirstJob = true;
            }
        }

        /// <summary>
        /// Called by the specific Teleporter that returns the player home during the tutorial.
        /// </summary>
        public void NotifyReturnedToApartment()
        {
            if (tutorialActive && hasCompletedFirstJob && !hasReturnedToApartment)
            {
                Debug.Log("TutorialManager: Returned to apartment flag set.");
                hasReturnedToApartment = true;
            }
        }

        private void ShowTutorialPanel() {
            if (tutorialPanel == null) return;

            // Ensure the panel is active before animating
            tutorialPanel.SetActive(true);

            // Reset position if using slide animation
            if (panelRectTransform != null) {
                Vector2 originalPosition = panelRectTransform.anchoredPosition;
                Vector2 startPosition = originalPosition + slideOffset;
                panelRectTransform.anchoredPosition = startPosition;

                // Animate slide in
                panelRectTransform.DOAnchorPos(originalPosition, fadeInDuration)
                    .SetEase(fadeInEase);
            }

            // Fade in
            if (panelCanvasGroup != null) {
                panelCanvasGroup.alpha = 0;
                panelCanvasGroup.DOFade(1f, fadeInDuration)
                    .SetEase(fadeInEase);
            }

            isPanelVisible = true;
        }

        private void HideTutorialPanel() {
            if (tutorialPanel == null) return;

            // Animate slide out
            if (panelRectTransform != null) {
                Vector2 endPosition = panelRectTransform.anchoredPosition + slideOffset;
                panelRectTransform.DOAnchorPos(endPosition, fadeOutDuration)
                    .SetEase(fadeOutEase);
            }

            // Fade out and deactivate at the end
            if (panelCanvasGroup != null) {
                panelCanvasGroup.DOFade(0f, fadeOutDuration)
                    .SetEase(fadeOutEase)
                    .OnComplete(() => tutorialPanel.SetActive(false));
            }
            else {
                // If no canvas group, deactivate after animation time
                StartCoroutine(DeactivateAfterDelay(fadeOutDuration));
            }

            isPanelVisible = false;
        }

        private IEnumerator DeactivateAfterDelay(float delay) {
            yield return new WaitForSeconds(delay);
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }

        public void ToggleTutorialPanel() {
            if (isPanelVisible) {
                HideTutorialPanel();
            }
            else {
                ShowTutorialPanel();
            }
        }

        // Toggle tutorial visibility (can be called from other scripts if needed)
        public void SetTutorialEnabled(bool enabled) {
            enableTutorial = enabled;

            if (enabled && !tutorialActive && !tutorialCompleted) {
                StartTutorial();
            }
            else if (!enabled && tutorialActive) {
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
        public string Instructions { get; private set; }
        private Func<bool> CompletionCheck { get; set; }

        public TutorialObjective(string title, Func<bool> completionCheck, string instructions) {
            Title = title;
            CompletionCheck = completionCheck;
            Instructions = instructions;
        }

        public bool IsComplete() {
            return CompletionCheck != null && CompletionCheck();
        }
    }
}