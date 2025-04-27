using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SAE_Dubai.JW;
using SAE_Dubai.JW.UI;
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
        [SerializeField] private float objectiveCompleteDelay = 1.5f;

        [Header("Objective Completion Feedback")]
        [Tooltip("The Image component for the tutorial panel's background.")]
        [SerializeField] private Image tutorialBackground;
        [Tooltip("AudioSource to play tutorial sounds.")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Sound effect played when an objective is completed.")]
        [SerializeField] private AudioClip objectiveCompleteSound;
        [Tooltip("The color the background flashes to on completion.")]
        [SerializeField] private Color flashColor = new Color(0.1f, 0.5f, 0.1f, 1f);
        [Tooltip("How long the color flash effect takes (total in and out).")]
        [SerializeField] private float flashDuration = 0.6f;
        [Tooltip("How long to keep the panel visible after the final step.")]
        [SerializeField] private float completionDisplayDuration = 5.0f;

        private Color originalBackgroundColor;
        private Coroutine hideCoroutine;

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
        private bool tutorialActive = false;
        private bool tutorialCompleted = true;
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
            playerHotbar = FindFirstObjectByType<Hotbar.Hotbar>();
            cameraManager = FindFirstObjectByType<CameraManager>();
            sessionManager = FindFirstObjectByType<PhotoSessionManager>();
            computerUI = FindFirstObjectByType<ComputerUI>();

            if (skipTutorialButton != null) {
                skipTutorialButton.onClick.AddListener(SkipTutorial);
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }

            if (tutorialBackground != null)
            {
                originalBackgroundColor = tutorialBackground.color;
            }
            else
            {
                Debug.LogWarning("TutorialManager: Tutorial Background Image not assigned.", this);
            }

            SetupTutorialObjectives();

            if (tutorialPanel != null) {
                tutorialPanel.SetActive(false);
                isPanelVisible = false;
            }

            bool shouldStartTutorial = PlayerPrefs.GetInt(MainMenuUI.tutorialPreferenceKey, 0) == 1;
            Debug.Log("TutorialManager Read Preference: " + shouldStartTutorial);

            if (shouldStartTutorial) {
                StartTutorial();
            } else {
                tutorialActive = false;
                tutorialCompleted = true;
                if (tutorialPanel != null) tutorialPanel.SetActive(false);
                isPanelVisible = false;
                Debug.Log("Tutorial explicitly disabled by Main Menu selection.");
            }

            PlayerPrefs.DeleteKey(MainMenuUI.tutorialPreferenceKey);
            PlayerPrefs.Save();


            Debug.Log($"ComputerUI found: {computerUI != null}");
        }


        private void SetupTutorialObjectives() {
            // ! Tutorials must be in sequential order.
            objectives.Clear();

            objectives.Add(new TutorialObjective(
                "Find Your Computer",
                () => computerUI != null && computerUI.IsPlayerUsingComputer(),
                "Movement: WASD | Look: Mouse | Interact: E"
            ));

            objectives.Add(new TutorialObjective(
                "Visit Camera Shop",
                () => computerUI != null && computerUI.IsShopTabActive(),
                "Navigate to the Camera Shop tab in the computer interface"
            ));

            objectives.Add(new TutorialObjective(
                "Purchase Your First Camera",
                () => boughtCamera,
                "Select a camera model and click the Purchase button"
            ));

            objectives.Add(new TutorialObjective(
                "Exit Computer",
                () => computerUI != null && !computerUI.IsPlayerUsingComputer(),
                "Confirm the purchase and press E or ESC to exit the computer interface"
            ));

            objectives.Add(new TutorialObjective(
                "Pick Up Your Camera",
                () => playerHotbar != null && playerHotbar.HasAnyEquipment(),
                "Approach the camera and hold E to pick it up"
            ));

            objectives.Add(new TutorialObjective(
                "Turn On The Camera",
                () => cameraManager.GetActiveCamera() != null && Input.GetKeyDown(KeyCode.C),
                "Press C to turn on the camera while equipped."
            ));

            objectives.Add(new TutorialObjective(
                "Toggle Camera Viewfinder",
                () => {
                    CameraSystem activeCam = cameraManager?.GetActiveCamera();
                    return activeCam != null && activeCam.isCameraOn && activeCam.usingViewfinder;
                },
                "Press V to toggle the viewfinder mode. This gives you a more immersive view through the camera lens."
            ));

            objectives.Add(new TutorialObjective(
                "Adjust ISO",
                () => {
                    CameraSystem activeCam = cameraManager?.GetActiveCamera();
                    return activeCam != null && activeCam.isCameraOn && activeCam.GetCurrentISO() >= 6400;
                },
                "Use the I key to increase ISO until it reaches 6400. Use U to decrease if you go too far."
            ));

            objectives.Add(new TutorialObjective(
                "Adjust Aperture",
                () => {
                    CameraSystem activeCam = cameraManager?.GetActiveCamera();
                    return activeCam != null && activeCam.isCameraOn &&
                           Mathf.Abs(activeCam.GetCurrentAperture() - 5.6f) > 0.1f;
                },
                "Press O for a smaller opening (higher f-number, more in focus) or P for a larger opening (lower f-number, shallower focus). Try changing it!"
            ));

            objectives.Add(new TutorialObjective(
                "Adjust Shutter Speed",
                () => {
                    CameraSystem activeCam = cameraManager?.GetActiveCamera();
                    return activeCam != null && activeCam.isCameraOn &&
                           Mathf.Abs(activeCam.GetCurrentShutterSpeed() - (1f / 125f)) > 0.001f;
                },
                "Press K for a slower speed (more light/blur) or L for a faster speed (less light/blur). Give it a try!"
            ));

            // Todo: ZOOOOOOOOOOOOOOOOOOOOOOOOM

            objectives.Add(new TutorialObjective(
                "Focus and Take a Photo",
                () => cameraManager.GetActiveCamera() != null && cameraManager.GetActiveCamera().GetPhotoCount() > 0,
                "Focus: Right Mouse Button | Take photo: Left Mouse Button"
            ));

            objectives.Add(new TutorialObjective(
                "Return to Computer",
                () => computerUI != null && computerUI.IsPlayerUsingComputer(),
                "Move back to your computer and press E"
            ));

            objectives.Add(new TutorialObjective(
                "Find Photo Sessions",
                () => computerUI != null && computerUI.IsSessionsTabActive(),
                "Go back to the menu and click on the Customer App in your computer."
            ));

            objectives.Add(new TutorialObjective(
                "Accept a Photo Session",
                () => sessionManager != null && sessionManager.GetActiveSessions().Count > 0,
                "Review available clients and click Accept on one you like. Remember to check each clients necessities!"
            ));

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

             var guideBookController = FindFirstObjectByType<GuideBookController>();
             objectives.Add(new TutorialObjective(
                 "Check Your GuideBook",
                 () => guideBookController != null && Input.GetKeyDown(guideBookController.toggleKey),
                 $"You've arrived! Press '{ (guideBookController != null ? guideBookController.toggleKey.ToString() : "G") }' at any time to open your GuideBook for reminders on controls or shot types."
             ));

            objectives.Add(new TutorialObjective(
                "Complete the Photo Session",
                // ! Completion Check: Wait for the flag to be set by PhotoSessionManager.
                () => hasCompletedFirstJob,
                "Approach the client. Use your camera ('C'), adjust settings if needed (I/U, O/P, K/L), focus (Right Mouse), and take the required photo (Left Mouse). Check needs via Pause Menu (ESC) or GuideBook (G)."
            ));

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
                "Tutorial finished! Press ESC to open pause menu anytime to see your objectives\nPress " + toggleTutorialKey +
                " to show/hide this panel"
            ));
        }


        private void Update() {
            if (Input.GetKeyDown(toggleTutorialKey)) {
                ToggleTutorialPanel();
            }

             // Only run update logic if the tutorial is actually active.
             if (!tutorialActive || tutorialCompleted || currentObjectiveIndex < 0 || currentObjectiveIndex >= objectives.Count)
                 return;


            // Check if current objective is complete.
            TutorialObjective currentObjective = objectives[currentObjectiveIndex];
            if (!currentObjectiveComplete && currentObjective.IsComplete()) {
                currentObjectiveComplete = true;
                StartCoroutine(AdvanceToNextObjective());
            }

            // Update UI (only if active and not completed).
            UpdateTutorialUI();
        }

        private IEnumerator AdvanceToNextObjective()
        {
            if (currentObjectiveIndex < objectives.Count - 1)
            {
                PlayObjectiveCompleteFeedback();
            }

            yield return new WaitForSeconds(objectiveCompleteDelay);

            if (currentObjectiveIndex < objectives.Count - 1)
            {
                currentObjectiveIndex++;
                currentObjectiveComplete = false;

                if (!isPanelVisible) {
                    ShowTutorialPanel();
                }
                UpdateTutorialUI();

                if (panelCanvasGroup != null) {
                    panelCanvasGroup.DOKill();
                    Sequence flashSequence = DOTween.Sequence();
                    flashSequence.Append(panelCanvasGroup.DOFade(0.7f, 0.2f))
                                 .Append(panelCanvasGroup.DOFade(1f, 0.3f));
                }
            }
            else
            {
                CompleteTutorial();
            }
        }

        private void PlayObjectiveCompleteFeedback()
        {
            if (audioSource != null && objectiveCompleteSound != null)
            {
                audioSource.PlayOneShot(objectiveCompleteSound);
            }

            if (tutorialBackground != null)
            {
                tutorialBackground.DOKill();

                Sequence flashSequence = DOTween.Sequence();
                flashSequence.Append(tutorialBackground.DOColor(flashColor, flashDuration / 2).SetEase(Ease.OutQuad));
                flashSequence.Append(tutorialBackground.DOColor(originalBackgroundColor, flashDuration / 2).SetEase(Ease.InQuad));
                flashSequence.Play();
            }
        }

        private void UpdateTutorialUI() {
             // Ensure tutorial is active and we have valid objectives,,
             if (!tutorialActive || currentObjectiveIndex < 0 || currentObjectiveIndex >= objectives.Count)
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

         // Modified StartTutorial to set flags correctly.
         public void StartTutorial() {
             if (tutorialActive) return; // Don't restart if already active.

             tutorialActive = true;
             tutorialCompleted = false;
             currentObjectiveIndex = 0;
             currentObjectiveComplete = false;
             boughtCamera = false; // Reset specific tutorial flags if needed.
             hasCompletedFirstJob = false;
             hasReturnedToApartment = false;
             ShowTutorialPanel();
             UpdateTutorialUI();

             Debug.Log("Tutorial started");
         }

        public void CompleteTutorial()
        {
            if (!tutorialActive || tutorialCompleted) return; // Prevent multiple completions.

            tutorialActive = false;
            tutorialCompleted = true;

            PlayObjectiveCompleteFeedback();

            // Update UI one last time to show completion state or final message.
             if (objectives.Count > 0) {
                 currentObjectiveIndex = objectives.Count - 1; // Ensure progress bar is full.
                 UpdateTutorialUI();
                 if (objectiveText != null) objectiveText.text = "Tutorial Complete!";
                 if (instructionText != null) instructionText.text = "You're ready to explore! Press " + toggleTutorialKey + " to hide this.";
             }

            if (!isPanelVisible && tutorialPanel != null)
            {
                ShowTutorialPanel();
            }

            Debug.Log("Tutorial completed");

            if (hideAfterCompleting)
            {
                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                }
                hideCoroutine = StartCoroutine(HidePanelAfterDelay(completionDisplayDuration));
            }
        }

        private IEnumerator HidePanelAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

             // Only hide if still completed and visible.
             if (hideAfterCompleting && tutorialCompleted && isPanelVisible)
             {
                 HideTutorialPanel();
             }

            hideCoroutine = null;
        }

        public void SkipTutorial()
        {
            if (!tutorialCompleted)
            {
                currentObjectiveIndex = objectives.Count - 1;
                CompleteTutorial();
                Debug.Log("Tutorial skipped (completion sequence initiated)");
            }
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
            if (tutorialPanel == null || isPanelVisible) return;

            // Ensure the panel is active before animating.
            tutorialPanel.SetActive(true);

            // Reset position if using slide animation.
            if (panelRectTransform != null) {
                Vector2 originalPosition = panelRectTransform.anchoredPosition;
                Vector2 startPosition = originalPosition + slideOffset;
                panelRectTransform.anchoredPosition = startPosition;

                // Animate slide in.
                panelRectTransform.DOAnchorPos(originalPosition, fadeInDuration)
                    .SetEase(fadeInEase);
            }

            // Fade in.
            if (panelCanvasGroup != null) {
                panelCanvasGroup.alpha = 0;
                panelCanvasGroup.DOFade(1f, fadeInDuration)
                    .SetEase(fadeInEase);
            }

            isPanelVisible = true;
        }

        private void HideTutorialPanel() {
             if (tutorialPanel == null || !isPanelVisible) return;


            if (panelRectTransform != null) {
                Vector2 endPosition = panelRectTransform.anchoredPosition + slideOffset;
                panelRectTransform.DOAnchorPos(endPosition, fadeOutDuration)
                    .SetEase(fadeOutEase);
            }

            if (panelCanvasGroup != null) {
                panelCanvasGroup.DOFade(0f, fadeOutDuration)
                    .SetEase(fadeOutEase)
                    .OnComplete(() => {
                         // Only set inactive if still hidden and not meant to be visible.
                         if (!isPanelVisible && tutorialPanel != null)
                             tutorialPanel.SetActive(false);
                     });

            }
            else {
                StartCoroutine(DeactivateAfterDelay(fadeOutDuration));
            }

            if (tutorialBackground != null)
            {
                tutorialBackground.DOKill();
                tutorialBackground.color = originalBackgroundColor;
            }

            isPanelVisible = false;        }

        private IEnumerator DeactivateAfterDelay(float delay) {
            yield return new WaitForSeconds(delay);
             if (tutorialPanel != null && !isPanelVisible) // Check visibility flag again.
                 tutorialPanel.SetActive(false);

        }

        public void ToggleTutorialPanel()
        {
            if (isPanelVisible)
            {
                HideTutorialPanel();
                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                    hideCoroutine = null;
                }
            }
            else
            {
                // Only show if the tutorial is active or completed (to allow viewing final state).
                 if (tutorialActive || tutorialCompleted)
                 {
                     ShowTutorialPanel();
                 }

            }
        }

        public void SetCameraBoughtFlag() {
             if (tutorialActive)
             {
                 boughtCamera = true;
             }

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