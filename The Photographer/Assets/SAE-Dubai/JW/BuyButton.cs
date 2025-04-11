using UnityEngine;
using UnityEngine.Events;

namespace SAE_Dubai.JW
{
    public class BuyButton : MonoBehaviour
    {
        public float cost;
        public ComputerUI computerUI;

        public UnityEvent OnBuySuccess;

        public void BuyButtonClicked()
        {
            bool success = computerUI.AttemptBuy(cost);

            if (success)
            {
                OnBuySuccess?.Invoke();
                gameObject.SetActive(false);
            }
        }
    }
}