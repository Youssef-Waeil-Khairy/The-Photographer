using UnityEngine;
using System.Collections.Generic;
using SAE_Dubai.Leonardo.CameraSys.Client_System;

namespace SAE_Dubai.Leonardo.CameraSys.Client_System
{
    public class PhotoSessionManager : MonoBehaviour
    {
        public static PhotoSessionManager Instance { get; private set; }
        
        [Header("- Session Settings")]
        public int maxActiveSessions = 3;
        public float travelCost = 25f;
        
        [Header("- Locations")]
        public List<Transform> photoLocations;
        
        private List<PhotoSession> activeSessions = new List<PhotoSession>();
        private ComputerUI computerUI;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            computerUI = FindAnyObjectByType<ComputerUI>();
        }

        public bool CanAddNewSession()
        {
            return activeSessions.Count < maxActiveSessions;
        }

        public void AddNewSession(PhotoSession session)
        {
            if (CanAddNewSession())
            {
                activeSessions.Add(session);
            }
        }

        public bool TravelToLocation(int locationIndex)
        {
            if (locationIndex < 0 || locationIndex >= photoLocations.Count)
                return false;

            if (computerUI.AttemptBuy(travelCost))
            {
                // Teleport player to location.
                computerUI.PlayerObject.transform.position = photoLocations[locationIndex].position;
                computerUI.PlayerObject.transform.rotation = photoLocations[locationIndex].rotation;
                return true;
            }
            return false;
        }

        private void SpawnClientAtLocation(int locationIndex)
        {
            // Leo: Get the ClientSpawner from my system.
            var clientSpawner = FindAnyObjectByType<ClientSpawner>();
            if (clientSpawner != null)
            {
                // Spawn client at the location.
                clientSpawner.SpawnInitialClients();
            }
        }
    }

    [System.Serializable]
    public class PhotoSession
    {
        public string clientName;
        public PortraitShotType requiredShotType;
        public int locationIndex;
        public float reward;
    }
} 