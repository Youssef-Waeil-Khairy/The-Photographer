using SAE_Dubai.JW;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    /// <summary>
    /// Manages the cost and payment processing for player travel.
    /// Follows singleton pattern to be accessible throughout the application.
    /// </summary>
    public class TravelCostManager : MonoBehaviour
    {
        public static TravelCostManager Instance { get; private set; }
        
        [Header("Travel Settings")]
        [SerializeField] private float defaultTravelCost = 25f;
        
        // Delegate for notifying results of travel payment attempts
        public delegate void TravelPaymentResult(bool success, int cost, string message);
        public event TravelPaymentResult OnTravelPaymentProcessed;
        
        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Attempts to process a travel payment from the player's balance.
        /// </summary>
        /// <param name="cost">The cost to travel</param>
        /// <returns>True if payment was successful, false otherwise</returns>
        public bool AttemptTravelPayment(int cost)
        {
            // Check if player has enough money
            if (PlayerBalance.Instance != null)
            {
                if (PlayerBalance.Instance.HasSufficientBalance(cost))
                {
                    // Deduct the cost
                    PlayerBalance.Instance.DeductBalance(cost);
                    
                    // Notify listeners of success
                    OnTravelPaymentProcessed?.Invoke(true, cost, $"Travel successful! Paid ${cost}");
                    return true;
                }
                else
                {
                    // Notify listeners of failure
                    OnTravelPaymentProcessed?.Invoke(false, cost, "Insufficient funds for travel!");
                    return false;
                }
            }
            
            // If player balance system isn't available
            OnTravelPaymentProcessed?.Invoke(false, cost, "Payment system unavailable");
            return false;
        }
        
        /// <summary>
        /// Gets the default travel cost.
        /// </summary>
        public float GetDefaultTravelCost()
        {
            return defaultTravelCost;
        }
    }
}