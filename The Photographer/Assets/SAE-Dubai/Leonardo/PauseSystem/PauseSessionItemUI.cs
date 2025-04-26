using SAE_Dubai.Leonardo.Client_System;
using TMPro;
using UnityEngine;

namespace SAE_Dubai.Leonardo.PauseSystem
{
    public class PauseSessionItemUI : MonoBehaviour
    {
        [Header("- UI Element References")]
        [SerializeField] private TextMeshProUGUI clientNameText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI objectiveText;

        /// <summary>
        /// Populates the UI elements with data from the given PhotoSession.
        /// This method will be called by the PauseMenuController.
        /// </summary>
        /// <param name="session">The PhotoSession data to display.</param>
        public void Setup(PhotoSession session)
        {
            if (session == null)
            {
                Debug.LogError("PauseSessionItemUI: Received null session data.");
                if (clientNameText) clientNameText.text = "Error";
                if (rewardText) rewardText.text = "Error";
                if (objectiveText) objectiveText.text = "Error";
                return;
            }

            if (clientNameText != null)
            {
                clientNameText.text = $"Client: {session.clientName}";
            }
            else
            {
                Debug.LogWarning($"PauseSessionItemUI: ClientNameText not assigned in the prefab for {session.clientName}");
            }

            if (rewardText != null)
            {
                rewardText.text = $"Reward: ${session.reward}";
            }
            else
            {
                Debug.LogWarning($"PauseSessionItemUI: RewardText not assigned in the prefab for {session.clientName}");
            }

            if (objectiveText != null)
            {
                objectiveText.text = $"Objective: {session.GetShotTypeName()}";
            }
            else
            {
                Debug.LogWarning($"PauseSessionItemUI: ObjectiveText not assigned in the prefab for {session.clientName}");
            }
        }
    }
}