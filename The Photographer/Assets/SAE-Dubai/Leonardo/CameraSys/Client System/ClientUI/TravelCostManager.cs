using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SAE_Dubai.JW.UI
{
    public class TravelCostManager : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text feedbackText;
        
        [SerializeField] private float feedbackDisplayTime = 2f;
        
        private float _feedbackTimer;
        
        private void Start()
        {
            if (button == null)
                button = GetComponent<Button>();
                
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }
        
        private void Update()
        {
            if (feedbackText != null && feedbackText.gameObject.activeSelf)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0)
                {
                    feedbackText.gameObject.SetActive(false);
                }
            }
        }
        
        public void SetCost(int cost)
        {
            if (costText != null)
            {
                costText.text = $"${cost}";
            }
        }
        
        public bool AttemptTravelPayment(out float playerBalance, int cost)
        {
            // Check if player has enough money.
            if (PlayerBalance.Instance != null)
            {
                playerBalance = PlayerBalance.Instance.Balance;
                
                if (PlayerBalance.Instance.HasSufficientBalance(cost))
                {
                    // Deduct the cost.
                    PlayerBalance.Instance.DeductBalance(cost);
                    
                    // Show success feedback.
                    ShowFeedback($"Travel successful! Paid ${cost}", Color.green);
                    
                    return true;
                }
                else
                {
                    // Show insufficient funds feedback.
                    ShowFeedback("Insufficient funds for travel!", Color.red);
                    return false;
                }
            }
            
            playerBalance = 0;
            return false;
        }
        
        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                feedbackText.gameObject.SetActive(true);
                _feedbackTimer = feedbackDisplayTime;
            }
        }
    }
}