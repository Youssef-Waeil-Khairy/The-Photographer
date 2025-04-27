using System.Collections.Generic;
using System.Linq;
using SAE_Dubai.JW;
using SAE_Dubai.Leonardo.CameraSys;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace SAE_Dubai.Leonardo.Client_System
{
    [RequireComponent(typeof(PortraitSubject), typeof(ClientFeedbackText))]
    public class ClientJobController : MonoBehaviour
    {
        [Header("- Completion Effects")]
        [Tooltip("Sound played when job is completed successfully")]
        public AudioClip completionSound;

        public Canvas mainCanvas;

        [Tooltip("Particle effect spawned when job is completed")]
        public GameObject completionParticles;

        [Header("- UI Feedback")]
        [Tooltip("Prefab for displaying reward amount")]
        public GameObject rewardTextPrefab;

        [Tooltip("How long the reward text should be visible")]
        public float rewardTextDuration = 2f;

        [Tooltip("How high above the client the text should appear")]
        public float rewardTextHeight = 2f;

        // --- Added Reference ---
        [Header("- Feedback System")]
        [Tooltip("Reference to the feedback text display system")]
        [SerializeField] // Optional: SerializeField to assign in Inspector if needed
        private ClientFeedbackText clientFeedback;
        // ---------------------

        [Header("- Client Instance Info")]
        public string clientName = "Default Client";

        public int rewardAmount = 100;
        public bool isJobActive;
        [HideInInspector] public List<PortraitShotType> requiredShotTypes = new();
        [HideInInspector] public List<PortraitShotType> completedShotTypes = new();

        public delegate void JobCompletedAction(ClientJobController completedClient);

        public event JobCompletedAction OnJobCompleted;

        [Header("- Composition Markers")]
        [Tooltip("Marker placed slightly above the head.")]
        public Transform aboveHeadMarker;

        [Tooltip("Marker placed at the top/center of the head.")]
        public Transform headMarker;

        [Tooltip("Marker placed between the eyes or center forehead.")]
        public Transform eyesMarker;

        [Tooltip("Marker placed at the mouth level.")]
        public Transform mouthMarker;

        [Tooltip("Marker placed at the chin level.")]
        public Transform chinMarker;

        [Tooltip("Marker placed at the center of the chest.")]
        public Transform chestMarker;

        [Tooltip("Marker placed around the hip/waist area.")]
        public Transform hipMarker;

        [Tooltip("Marker placed around the knee level.")]
        public Transform kneesMarker;

        [Tooltip("Marker placed at the feet level.")]
        public Transform feetMarker;

        [Tooltip("Marker placed slightly below the feet.")]
        public Transform belowFeetMarker;

        [Tooltip("Marker placed at the left shoulder.")]
        public Transform shoulderLMarker;

        [Tooltip("Marker placed at the right shoulder.")]
        public Transform shoulderRMarker;

        private void Awake()
        {
            clientFeedback = GetComponent<ClientFeedbackText>();
            if (clientFeedback == null)
            {
                Debug.LogError($"ClientFeedbackText component not found on {gameObject.name}. Please add it.", this);
            }
        }

        private void Start()
        {
            if (mainCanvas == null)
            {
                GameObject canvasGO = GameObject.FindWithTag("MainCanvas");
                if (canvasGO != null)
                {
                     mainCanvas = canvasGO.GetComponent<Canvas>();
                }
            }
        }

        public void SetupJob(ClientData archetype, List<PortraitShotType> specificRequirements, int reward,
            string predefinedClientName = null)
        {
            clientName = predefinedClientName ?? archetype.GetRandomName();

            requiredShotTypes = specificRequirements ?? new List<PortraitShotType>();
            requiredShotTypes = requiredShotTypes.Where(st => st != PortraitShotType.Undefined).Distinct().ToList();
            rewardAmount = reward;
            completedShotTypes = new List<PortraitShotType>();
            isJobActive = true;
            gameObject.name = $"Client_{clientName}";
            Debug.Log(
                $"Client '{clientName}' spawned. Requires: {string.Join(", ", requiredShotTypes.Select(PhotoCompositionEvaluator.GetShotTypeDisplayName))}");
            // UpdateUI(); // TODO: Implement client UI.
        }

        public void CheckPhoto(CapturedPhoto photo)
        {
            if (!isJobActive || clientFeedback == null)
            {
                return;
            }

            if (!photo.portraitShotType.HasValue || photo.portraitShotType.Value == PortraitShotType.Undefined)
            {
                Debug.Log($"<color=yellow>[{clientName} Photo Debug]</color> No valid composition detected.");
                // --- Call Feedback for undefined shot ---
                clientFeedback.HandlePhotoResult(photo, false);
                // --------------------------------------
                return;
            }

            PortraitShotType capturedType = photo.portraitShotType.Value;
            bool isCorrectShot = false;

            //Debug.Log($"<color=cyan>=== PHOTO COMPOSITION DEBUG: {clientName} ===</color>");
            //Debug.Log($"<color=cyan>Photo Captured:</color> {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}");

            if (requiredShotTypes.Contains(capturedType) && !completedShotTypes.Contains(capturedType))
            {
                completedShotTypes.Add(capturedType);
                isCorrectShot = true;
                Debug.Log(
                    $"<color=green>[SUCCESS]</color> Client '{clientName}' received required shot: {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}. Progress: {completedShotTypes.Count}/{requiredShotTypes.Count}");

                if (completedShotTypes.Count >= requiredShotTypes.Count)
                {
                    Debug.Log(
                        $"<color=green>[JOB COMPLETE]</color> All required shots for '{clientName}' have been captured!");
                    CompleteJob();
                    return; 
                }
            }
            else if (completedShotTypes.Contains(capturedType))
            {
                isCorrectShot = false;
                Debug.Log(
                    $"<color=yellow>[DUPLICATE]</color> Client '{clientName}' already received this shot type: {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}");
            }
            else
            {
                isCorrectShot = false;
                Debug.Log(
                    $"<color=red>[MISMATCH]</color> Client '{clientName}' received photo type {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}, but needs {string.Join(", ", requiredShotTypes.Except(completedShotTypes).Select(PhotoCompositionEvaluator.GetShotTypeDisplayName))}");
            }

            // --- Call Feedback ---
            // Pass the photo and whether the specific shot composition was correct for the *current* requirements.
            clientFeedback.HandlePhotoResult(photo, isCorrectShot);
            // ---------------------
        }

        private void CompleteJob()
        {
            isJobActive = false;
            Debug.Log($"Client '{clientName}' job completed! Reward: {rewardAmount}");

            if (PlayerBalance.Instance != null)
            {
                PlayerBalance.Instance.AddBalance(rewardAmount);
            }
            else
            {
                Debug.LogWarning("PlayerBalance instance not found. Cannot add reward.");
            }

            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource != null && completionSound != null)
            {
                audioSource.PlayOneShot(completionSound);
            }
            else if (completionSound != null)
            {
                AudioSource.PlayClipAtPoint(completionSound, transform.position);
            }

            if (completionParticles != null)
            {
                Instantiate(completionParticles, transform.position + Vector3.up, Quaternion.identity);
            }

            ShowRewardText();

            OnJobCompleted?.Invoke(this);

            Destroy(gameObject, 3.0f);
        }

        private void ShowRewardText()
        {
            if (rewardTextPrefab != null && mainCanvas != null)
            {
                GameObject rewardPanel = Instantiate(rewardTextPrefab, mainCanvas.transform);
                RectTransform rectTransform = rewardPanel.GetComponent<RectTransform>();

                if (rectTransform == null)
                {
                    Debug.LogError("Reward text prefab has no RectTransform component");
                    Destroy(rewardPanel);
                    return;
                }

                if (Camera.main != null)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * rewardTextHeight); 

                   
                    if (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        rectTransform.position = screenPos;
                    }
                    else 
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            mainCanvas.transform as RectTransform,
                            screenPos,
                            mainCanvas.worldCamera,
                            out Vector2 localPoint);
                        rectTransform.localPosition = localPoint;
                    }
                     // Ensure pivot is centered if needed: rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }

                // Set the text.
                TextMeshProUGUI textComponent = rewardPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"+${rewardAmount}";
                }

                // Animation stuff.
                CanvasGroup canvasGroup = rewardPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = rewardPanel.AddComponent<CanvasGroup>();

                rectTransform.localScale = Vector3.zero;
                canvasGroup.alpha = 1f;

                Sequence rewardSequence = DOTween.Sequence();
                rewardSequence.Append(rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                rewardSequence.Append(rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + 50f, rewardTextDuration).SetEase(Ease.OutCubic)); 
                rewardSequence.Join(canvasGroup.DOFade(0, rewardTextDuration * 0.5f).SetDelay(rewardTextDuration * 0.5f));
                rewardSequence.OnComplete(() => Destroy(rewardPanel));
            }
            else
            {
                 if(rewardTextPrefab == null) Debug.LogWarning("Reward Text Prefab not assigned.");
                 if(mainCanvas == null) Debug.LogWarning("Main Canvas not found for reward text.");
            }
        }

        public List<Transform> GetOrderedBodyMarkers()
        {
            // Order matters: from top to bottom.
            // Only return the main vertical body markers
            return new List<Transform> { headMarker, chestMarker, hipMarker, kneesMarker, feetMarker }
                .Where(t => t != null).ToList();
        }
    }
}