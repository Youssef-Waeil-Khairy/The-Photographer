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
            OnJobCompleted?.Invoke(this);
            Destroy(gameObject, 3.0f); // Destroy after delay.
        }

        public List<Transform> GetOrderedBodyMarkers() {
            // Order matters: from top to bottom.
            // Only return the main vertical body markers
            return new List<Transform> { headMarker, chestMarker, hipMarker, kneesMarker, feetMarker }
                .Where(t => t != null).ToList();
        }
    }
}