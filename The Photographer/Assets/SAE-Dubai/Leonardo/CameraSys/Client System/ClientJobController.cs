using System.Collections.Generic;
using System.Linq;
using SAE_Dubai.JW;
using UnityEngine;

namespace SAE_Dubai.Leonardo.CameraSys.Client_System
{
    [RequireComponent(typeof(PortraitSubject))]
    public class ClientJobController : MonoBehaviour
    {
        [Header("- Client Instance Info")]
        public string clientName = "Default Client";

        public int rewardAmount = 100;
        public bool isJobActive;

        // Populated by ClientSpawner.
        [HideInInspector] public List<PortraitShotType> requiredShotTypes = new();
        [HideInInspector] public List<PortraitShotType> completedShotTypes = new();

        // TODO: Reference to a world-space UI above the client's head ------------------------------------ASDSAKFHDSJFDSKLAHFJDALSHFIKHJDFLASJDVMPFRWOIEAPVMWsddddaaaaAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
        // public TMPro.TextMeshProUGUI requirementsTextUI;

        public delegate void JobCompletedAction(ClientJobController completedClient);

        public event JobCompletedAction OnJobCompleted;

        /// <summary>
        /// Initializes this client instance with specific job details, called by the ClientSpawner.
        /// </summary>
        public void SetupJob(ClientData archetype, List<PortraitShotType> specificRequirements, int reward) {
            clientName = archetype.GetRandomName();
            // clientDescription = archetype.GetRandomDescription(); // TODO: for descriptions.
            // clientAvatar = archetype.GetRandomAvatar(); // TODO: for avatars (?)

            requiredShotTypes = specificRequirements ?? new List<PortraitShotType>();

            // To ensure no duplicates.
            requiredShotTypes = requiredShotTypes
                .Where(st => st != PortraitShotType.Undefined)
                .Distinct()
                .ToList();

            rewardAmount = reward;
            completedShotTypes = new List<PortraitShotType>();
            isJobActive = true;

            gameObject.name = $"Client_{clientName}"; // Rename instance for clarity in hierarchy.

            Debug.Log($"Client '{clientName}' spawned. Requires: {string.Join(", ", requiredShotTypes)}");
            UpdateUI();
        }

        /// <summary>
        /// Called by the ClientSpawner when a relevant photo is taken. Checks if the photo fulfills one of the requirements.
        /// </summary>
        /// <param name="photo">The captured photo data, tagged with a shot type.</param>
        public void CheckPhoto(CapturedPhoto photo) {
            if (!isJobActive || !photo.portraitShotType.HasValue) {
                // Job not active or photo has no valid composition tag.
                return;
            }

            PortraitShotType capturedType = photo.portraitShotType.Value;

            // Is this shot type required AND not already completed?:
            if (requiredShotTypes.Contains(capturedType) && !completedShotTypes.Contains(capturedType)) {
                completedShotTypes.Add(capturedType);
                Debug.Log(
                    $"Client '{clientName}' received required shot: {capturedType}. Progress: {completedShotTypes.Count}/{requiredShotTypes.Count}");
                UpdateUI();

                // If all requirements are met.
                if (completedShotTypes.Count >= requiredShotTypes.Count) {
                    CompleteJob();
                }
            }
        }

        /// <summary>
        /// Updates any associated UI elements (e.g., text above head).
        /// </summary>
        private void UpdateUI() {
            // TODO: UI AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            // if (requirementsTextUI != null)
            // {
            //     string progress = $"{completedShotTypes.Count}/{requiredShotTypes.Count}";
            //     string requiredList = string.Join("\n- ", requiredShotTypes.Select(st => PhotoCompositionEvaluator.GetShotTypeDisplayName(st)));
            //     requirementsTextUI.text = $"Requires:\n- {requiredList}\nProgress: {progress}";
            // }
        }

        /// <summary>
        /// Called when all required shots have been completed.
        /// </summary>
        private void CompleteJob() {
            isJobActive = false;
            Debug.Log($"Client '{clientName}' job completed! Reward: {rewardAmount}");

            // Connect reward to PlayerBalance.
            PlayerBalance.Instance?.AddBalance(rewardAmount);
        
            // Notify the spawner/manager that this job is done.
            OnJobCompleted?.Invoke(this);
        
            // TODO: Visual feedback (play animation, sound).
            // Disable interaction or destroy after a delay.
            Destroy(gameObject, 5.0f);
        }
    }
}