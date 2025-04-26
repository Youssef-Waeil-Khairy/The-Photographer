using System;
using System.Collections.Generic;
using System.Linq;
using SAE_Dubai.JW;
using SAE_Dubai.Leonardo.CameraSys;
using UnityEngine;
using DG.Tweening;


namespace SAE_Dubai.Leonardo.Client_System
{
    [RequireComponent(typeof(PortraitSubject))]
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

        private void Start() {
            mainCanvas = GameObject.FindGameObjectWithTag("MainCanvas")?.GetComponent<Canvas>();
        }

        public void SetupJob(ClientData archetype, List<PortraitShotType> specificRequirements, int reward,
            string predefinedClientName = null) {
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

        public void CheckPhoto(CapturedPhoto photo) {
            if (!isJobActive || !photo.portraitShotType.HasValue ||
                photo.portraitShotType.Value == PortraitShotType.Undefined) {
                Debug.Log(
                    $"<color=yellow>[{clientName} Photo Debug]</color> No valid composition detected or job not active");
                return; // ! Job not active or photo has no valid evaluated composition.
            }

            PortraitShotType capturedType = photo.portraitShotType.Value;

            Debug.Log($"<color=cyan>=== PHOTO COMPOSITION DEBUG: {clientName} ===</color>");
            Debug.Log(
                $"<color=cyan>Photo Captured:</color> {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}");
            Debug.Log(
                $"<color=cyan>Required Shot Types:</color> {string.Join(", ", requiredShotTypes.Select(PhotoCompositionEvaluator.GetShotTypeDisplayName))}");
            Debug.Log(
                $"<color=cyan>Already Completed:</color> {string.Join(", ", completedShotTypes.Select(PhotoCompositionEvaluator.GetShotTypeDisplayName))}");
            Debug.Log(
                $"<color=cyan>Match Found:</color> {requiredShotTypes.Contains(capturedType) && !completedShotTypes.Contains(capturedType)}");

            if (requiredShotTypes.Contains(capturedType) && !completedShotTypes.Contains(capturedType)) {
                completedShotTypes.Add(capturedType);
                Debug.Log(
                    $"<color=green>[SUCCESS]</color> Client '{clientName}' received required shot: {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}. Progress: {completedShotTypes.Count}/{requiredShotTypes.Count}");

                if (completedShotTypes.Count >= requiredShotTypes.Count) {
                    Debug.Log(
                        $"<color=green>[JOB COMPLETE]</color> All required shots for '{clientName}' have been captured!");
                    CompleteJob();
                }
            }
            else if (completedShotTypes.Contains(capturedType)) {
                Debug.Log(
                    $"<color=yellow>[DUPLICATE]</color> Client '{clientName}' already received this shot type: {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}");
            }
            else {
                Debug.Log(
                    $"<color=red>[MISMATCH]</color> Client '{clientName}' received photo type {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}, but needs {string.Join(", ", requiredShotTypes.Except(completedShotTypes).Select(PhotoCompositionEvaluator.GetShotTypeDisplayName))}");
            }
        }

        // private void UpdateUI() TODO

        private void CompleteJob() {
            isJobActive = false;
            Debug.Log($"Client '{clientName}' job completed! Reward: {rewardAmount}");
            PlayerBalance.Instance?.AddBalance(rewardAmount);

            // Play completion sound effect
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null && completionSound != null) {
                audioSource.PlayOneShot(completionSound);
            }
            else if (completionSound != null) {
                // Fallback if no AudioSource on this object
                AudioSource.PlayClipAtPoint(completionSound, transform.position);
            }

            // Spawn particle effect
            if (completionParticles != null) {
                Instantiate(completionParticles, transform.position + Vector3.up, Quaternion.identity);
            }

            // Show reward text
            ShowRewardText();

            OnJobCompleted?.Invoke(this);
            Destroy(gameObject, 3.0f); // Destroy after delay.
        }

        private void ShowRewardText() {
            if (rewardTextPrefab != null) {
                // Find the main canvas if not already assigned
                if (mainCanvas == null) {
                    mainCanvas = FindObjectOfType<Canvas>();
                    if (mainCanvas == null) {
                        Debug.LogError("No Canvas found in scene for reward text");
                        return;
                    }
                }

                // Instantiate the panel as a child of the main canvas
                GameObject rewardPanel = Instantiate(rewardTextPrefab, mainCanvas.transform);

                // Get the RectTransform to position it properly
                RectTransform rectTransform = rewardPanel.GetComponent<RectTransform>();
                if (rectTransform == null) {
                    Debug.LogError("Reward text prefab has no RectTransform component");
                    Destroy(rewardPanel);
                    return;
                }

                // Reset the transform properties first
                rectTransform.localPosition = Vector3.zero;
                rectTransform.anchoredPosition = Vector2.zero;

                // Position directly on client in screen space
                if (Camera.main != null) {
                    // Convert the world position to screen position
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1f);

                    // Adjust for any canvas scaling
                    if (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                        rectTransform.position = screenPos;
                    }
                    else if (mainCanvas.renderMode == RenderMode.ScreenSpaceCamera) {
                        // Convert screen position to local position in the canvas
                        Vector2 viewportPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
                        rectTransform.anchorMin = viewportPos;
                        rectTransform.anchorMax = viewportPos;
                        rectTransform.anchoredPosition = Vector2.zero;
                    }

                    // Make sure it's centered on its position
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }

                // Find the text component and set it
                TMPro.TextMeshProUGUI textComponent = rewardPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null) {
                    textComponent.text = $"+${rewardAmount}";
                }

                // Get or add the canvas group for fading
                CanvasGroup canvasGroup = rewardPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null) {
                    canvasGroup = rewardPanel.AddComponent<CanvasGroup>();
                }

                // Start with zero scale
                rectTransform.localScale = Vector3.zero;

                // Create a sequence of animations
                Sequence rewardSequence = DOTween.Sequence();

                // Pop-in animation
                rewardSequence.Append(rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

                // Wait a bit before moving
                rewardSequence.AppendInterval(0.3f);

                // Move upward in screen space
                Vector3 startPos = rectTransform.position;
                rewardSequence.Append(
                    rectTransform.DOMove(
                        new Vector3(startPos.x, startPos.y + 50f, startPos.z),
                        rewardTextDuration
                    ).SetEase(Ease.OutCubic)
                );

                // Fade out
                rewardSequence.Join(
                    canvasGroup.DOFade(0, rewardTextDuration * 0.5f)
                        .SetDelay(rewardTextDuration * 0.5f)
                );

                // Destroy after animation completes
                rewardSequence.OnComplete(() => { Destroy(rewardPanel); });
            }
        }

        public List<Transform> GetOrderedBodyMarkers() {
            // Order matters: from top to bottom.
            // Only return the main vertical body markers
            return new List<Transform> { headMarker, chestMarker, hipMarker, kneesMarker, feetMarker }
                .Where(t => t != null).ToList();
        }
    }
}