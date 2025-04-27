using DG.Tweening;
using UnityEngine;

namespace SAE_Dubai.Leonardo
{
    public class GuideBookController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Assign the main Guide Book Panel GameObject here.")]
        [SerializeField] private GameObject guideBookPanel;

        [Header("Settings")]
        [Tooltip("The key to press to open/close the guide book.")]
        [SerializeField] private KeyCode toggleKey = KeyCode.G;
        [Tooltip("How long the fade animation takes.")]
        [SerializeField] private float fadeDuration = 0.3f;
        [Tooltip("Should opening the guide pause the game time?")]
        [SerializeField] private bool pauseTime;

        [Header("Player Control References (Optional)")]
        [Tooltip("Assign the player's MovementSystem script here (optional).")]
        [SerializeField] private MovementSystem playerMovement;
        [Tooltip("Assign the player's MouseController script here (optional).")]
        [SerializeField] private MouseController mouseController;


        private CanvasGroup panelCanvasGroup;
        private bool isPanelVisible;
        private float previousTimeScale = 1f;

        void Awake()
        {
            if (guideBookPanel == null)
            {
                Debug.LogError("GuideBookController: guideBookPanel is not assigned!", this);
                enabled = false;
                return;
            }

            panelCanvasGroup = guideBookPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = guideBookPanel.AddComponent<CanvasGroup>();
            }

            if (playerMovement == null)
            {
                playerMovement = FindObjectOfType<MovementSystem>();
                 // if (playerMovement == null) Debug.LogWarning("GuideBookController: Player Movement script not found.");
            }
            if (mouseController == null)
            {
                mouseController = FindObjectOfType<MouseController>();
                // if (mouseController == null) Debug.LogWarning("GuideBookController: Mouse Controller script not found.");.
            }
        }

        void Start()
        {
            // Start with the panel hidden.
            guideBookPanel.SetActive(false);
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            isPanelVisible = false;
        }

        void Update()
        {
            // Check for the toggle key press.
            if (Input.GetKeyDown(toggleKey))
            {
                TogglePanel();
            }

            // Allow closing with ESCAPE if the panel is visible.
            if (isPanelVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                 HidePanel();
            }
        }

        public void TogglePanel()
        {
            if (isPanelVisible)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }

        private void ShowPanel()
        {
            if (guideBookPanel == null || panelCanvasGroup == null) return;

            isPanelVisible = true;
            guideBookPanel.SetActive(true);

            // Fade In Animation.
            panelCanvasGroup.DOKill(); // Kill any previous tween.
            panelCanvasGroup.DOFade(1f, fadeDuration).SetUpdate(true); // Use unscaled time if pausing.

            // Make panel interactive
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;

            // Handle Player Controls & Cursor.
            SetPlayerControlsActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

             // ? Pause time.
            if (pauseTime)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            Debug.Log("GuideBook Opened");
        }

        private void HidePanel()
        {
            if (guideBookPanel == null || panelCanvasGroup == null) return;

            isPanelVisible = false;

            // Fade Out Animation.
            panelCanvasGroup.DOKill();
            panelCanvasGroup.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => {
                     if (!isPanelVisible) 
                     {
                         guideBookPanel.SetActive(false);
                     }
                 });

            // Make panel non-interactive immediately.
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;

            // Handle Player Controls & Cursor.
            SetPlayerControlsActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Optional: Resume time.
            if (pauseTime)
            {
                Time.timeScale = previousTimeScale;
            }

            Debug.Log("GuideBook Closed");
        }

         private void SetPlayerControlsActive(bool isActive)
        {
            // Enable/disable player movement and look scripts if they are assigned.
            if (playerMovement != null) playerMovement.enabled = isActive;
            if (mouseController != null) mouseController.enabled = isActive;

            // Debug.Log($"GuideBook: Player controls set to {isActive}");
        }
    }
}