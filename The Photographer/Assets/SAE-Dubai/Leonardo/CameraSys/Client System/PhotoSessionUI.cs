using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace SAE_Dubai.Leonardo.CameraSys.Client_System
{
    public class PhotoSessionUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject photoSessionTab;
        public Transform sessionListContent;
        public GameObject sessionButtonPrefab;
        public Button refreshButton;
        
        [Header("Session Generation")]
        public float minReward = 50f;
        public float maxReward = 200f;
        
        private List<PhotoSession> availableSessions = new List<PhotoSession>();
        private PhotoSessionManager sessionManager;

        private void Start()
        {
            sessionManager = PhotoSessionManager.Instance;
            refreshButton.onClick.AddListener(GenerateNewSessions);
            GenerateNewSessions();
        }

        public void GenerateNewSessions()
        {
            availableSessions.Clear();
            // Generate 5 random sessions
            for (int i = 0; i < 5; i++)
            {
                PhotoSession session = new PhotoSession
                {
                    clientName = "Client " + Random.Range(1, 100),
                    requiredShotType = (PortraitShotType)Random.Range(0, 7),
                    locationIndex = Random.Range(0, sessionManager.photoLocations.Count),
                    reward = Random.Range(minReward, maxReward)
                };
                availableSessions.Add(session);
            }
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Clear existing buttons
            foreach (Transform child in sessionListContent)
            {
                Destroy(child.gameObject);
            }

            // Create new buttons for each session
            foreach (var session in availableSessions)
            {
                GameObject buttonObj = Instantiate(sessionButtonPrefab, sessionListContent);
                Button button = buttonObj.GetComponent<Button>();
                TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
                
                buttonText.text = $"Client: {session.clientName}\n" +
                                $"Shot Type: {session.requiredShotType}\n" +
                                $"Reward: ${session.reward}\n" +
                                $"Travel Cost: ${sessionManager.travelCost}";

                button.onClick.AddListener(() => AcceptSession(session));
            }
        }

        private void AcceptSession(PhotoSession session)
        {
            if (sessionManager.CanAddNewSession())
            {
                sessionManager.AddNewSession(session);
                availableSessions.Remove(session);
                UpdateUI();
            }
        }
    }
} 