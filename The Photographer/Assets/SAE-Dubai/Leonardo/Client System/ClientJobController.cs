using System.Collections.Generic;
using System.Linq;
using SAE_Dubai.JW;
using SAE_Dubai.Leonardo.CameraSys;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    [RequireComponent(typeof(PortraitSubject))]
    public class ClientJobController : MonoBehaviour
    {
        [Header("- Client Instance Info")]
        public string clientName = "Default Client";
        public int rewardAmount = 100;
        public bool isJobActive;

        [HideInInspector] public List<PortraitShotType> requiredShotTypes = new();
        [HideInInspector] public List<PortraitShotType> completedShotTypes = new();

        public delegate void JobCompletedAction(ClientJobController completedClient);
        public event JobCompletedAction OnJobCompleted;

        [Header("Composition Markers")]
        [Tooltip("Marker placed slightly above the head.")]
        public Transform aboveHeadMarker;
        [Tooltip("Marker placed at the top/center of the head.")]
        public Transform headMarker;
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
        // Add more markers if needed (e.g., Neck, Shoulders)
        // --- End New ---


        public void SetupJob(ClientData archetype, List<PortraitShotType> specificRequirements, int reward) {
            clientName = archetype.GetRandomName();
            requiredShotTypes = specificRequirements ?? new List<PortraitShotType>();
            requiredShotTypes = requiredShotTypes.Where(st => st != PortraitShotType.Undefined).Distinct().ToList();
            rewardAmount = reward;
            completedShotTypes = new List<PortraitShotType>();
            isJobActive = true;
            gameObject.name = $"Client_{clientName}";
            Debug.Log($"Client '{clientName}' spawned. Requires: {string.Join(", ", requiredShotTypes.Select(PhotoCompositionEvaluator.GetShotTypeDisplayName))}");
            // UpdateUI(); // TODO: Implement client UI if needed
        }

        public void CheckPhoto(CapturedPhoto photo) {
            if (!isJobActive || !photo.portraitShotType.HasValue || photo.portraitShotType.Value == PortraitShotType.Undefined) {
                return; // Job not active or photo has no valid evaluated composition
            }

            PortraitShotType capturedType = photo.portraitShotType.Value;
            if (requiredShotTypes.Contains(capturedType) && !completedShotTypes.Contains(capturedType)) {
                completedShotTypes.Add(capturedType);
                Debug.Log(
                    $"Client '{clientName}' received required shot: {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}. Progress: {completedShotTypes.Count}/{requiredShotTypes.Count}");
                // UpdateUI(); // Update client UI

                if (completedShotTypes.Count >= requiredShotTypes.Count) {
                    CompleteJob();
                }
            } else {
                 Debug.Log($"Client '{clientName}' received photo type {PhotoCompositionEvaluator.GetShotTypeDisplayName(capturedType)}, which was not needed or already completed.");
            }
        }

        // private void UpdateUI() { /* TODO */ }

        private void CompleteJob() {
            isJobActive = false;
            Debug.Log($"Client '{clientName}' job completed! Reward: {rewardAmount}");
            PlayerBalance.Instance?.AddBalance(rewardAmount);
            OnJobCompleted?.Invoke(this);
            // Consider adding feedback before destroying
            Destroy(gameObject, 5.0f); // Destroy after delay
        }

        // This helps determine the highest/lowest visible standard body part
         public List<Transform> GetOrderedBodyMarkers()
         {
              // Order matters: from top to bottom.
              return new List<Transform> { headMarker, chestMarker, hipMarker, kneesMarker, feetMarker }.Where(t => t != null).ToList();
         }
    }
}