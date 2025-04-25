using System.Collections.Generic;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    [CreateAssetMenu(fileName = "New Client Data", menuName = "Photography/Client Data")]
    public class ClientData : ScriptableObject
    {
        [Header("Client Identity")]
        public List<string> possibleNames = new() { "Bab", "Dud", "Jan", "Bob" };
        // public List<string> possibleDescriptions = new List<string> { "Wants some nice photos.", "Needs portraits for their portfolio." };
        public List<Sprite> possibleAvatars;

        [Header("Job Requirements")]
        [Tooltip("Which shot types this client archetype MIGHT ask for.")]
        public List<PortraitShotType> potentialShotTypes = new();
        [Tooltip("Minimum number of different shots client will require.")]
        [Range(1, 5)] public int minRequiredShots = 1;
        [Tooltip("Maximum number of different shots client will require.")]
        [Range(1, 5)] public int maxRequiredShots = 2;

        [Header("Rewards")]
        public int minReward = 50;
        public int maxReward = 150;

        public string GetRandomName()
        {
            if (possibleNames == null || possibleNames.Count == 0) return "Client";
            return possibleNames[Random.Range(0, possibleNames.Count)];
        }

        public Sprite GetRandomAvatar()
        {
            if (possibleAvatars == null || possibleAvatars.Count == 0) return null;
            return possibleAvatars[Random.Range(0, possibleAvatars.Count)];
        }
    }
}