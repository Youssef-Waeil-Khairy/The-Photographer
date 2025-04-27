using DG.Tweening;
using UnityEngine;

namespace SAE_Dubai.Leonardo
{
    [RequireComponent(typeof(AudioSource))]
    public class GuideBookController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Assign the main Guide Book Panel GameObject here.")]
        [SerializeField] private GameObject guideBookPanel;

        [Header("Settings")]
        [Tooltip("The key to press to open/close the guide book.")]
        [SerializeField] public KeyCode toggleKey = KeyCode.G;
        [Tooltip("How long the fade animation takes.")]
        [SerializeField] private float fadeDuration = 0.3f;
        [Tooltip("Should opening the guide pause the game time?")]
        [SerializeField] private bool pauseTime = false;

        [Header("Audio Effects")]
        [Tooltip("Sound effect to play when opening the guide book.")]
        [SerializeField] private AudioClip openSound;
        [Tooltip("Sound effect to play when closing the guide book.")]
        [SerializeField] private AudioClip closeSound;

        [Header("Player Control References (Optional)")]
        [Tooltip("Assign the player's MovementSystem script here (optional).")]
        [SerializeField] private MovementSystem playerMovement;
        [Tooltip("Assign the player's MouseController script here (optional).")]
        [SerializeField] private MouseController mouseController;

        private CanvasGroup _panelCanvasGroup;
        private AudioSource _audioSource;
        private bool _isPanelVisible;
        private float _previousTimeScale = 1f;

        void Awake()
        {
            if (guideBookPanel == null)
            {
                Debug.LogError("GuideBookController: guideBookPanel is not assigned!", this);
                enabled = false;
                return;
            }

            _panelCanvasGroup = guideBookPanel.GetComponent<CanvasGroup>();
            if (_panelCanvasGroup == null)
            {
                _panelCanvasGroup = guideBookPanel.AddComponent<CanvasGroup>();
            }

            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            if (playerMovement == null) playerMovement = FindObjectOfType<MovementSystem>();
            if (mouseController == null) mouseController = FindObjectOfType<MouseController>();
        }

        void Start()
        {
            guideBookPanel.SetActive(false);
            _panelCanvasGroup.alpha = 0f;
            _panelCanvasGroup.interactable = false;
            _panelCanvasGroup.blocksRaycasts = false;
            _isPanelVisible = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                TogglePanel();
            }

            if (_isPanelVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                 HidePanel();
            }
        }

        public void TogglePanel()
        {
            if (_isPanelVisible)
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
            if (guideBookPanel == null || _panelCanvasGroup == null || _isPanelVisible) return;

            _isPanelVisible = true;

            if (openSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(openSound);
            }

            guideBookPanel.SetActive(true);

            _panelCanvasGroup.DOKill();
            _panelCanvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);

            _panelCanvasGroup.interactable = true;
            _panelCanvasGroup.blocksRaycasts = true;

            SetPlayerControlsActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (pauseTime)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            Debug.Log("GuideBook Opened");
        }

        private void HidePanel()
        {
            if (guideBookPanel == null || _panelCanvasGroup == null || !_isPanelVisible) return;

            _isPanelVisible = false;

            if (closeSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(closeSound);
            }

            _panelCanvasGroup.interactable = false;
            _panelCanvasGroup.blocksRaycasts = false;

            _panelCanvasGroup.DOKill();
            _panelCanvasGroup.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => {
                     if (!_isPanelVisible)
                     {
                         guideBookPanel.SetActive(false);
                     }
                });


            SetPlayerControlsActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (pauseTime)
            {
                Time.timeScale = _previousTimeScale;
            }

            Debug.Log("GuideBook Closed");
        }

         private void SetPlayerControlsActive(bool isActive)
        {
            if (playerMovement != null) playerMovement.enabled = isActive;
            if (mouseController != null) mouseController.enabled = isActive;
        }
    }
}