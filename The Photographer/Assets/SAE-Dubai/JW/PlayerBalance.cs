using System;
using UnityEngine;
using UnityEngine;

namespace SAE_Dubai.JW
{
    public class PlayerBalance : MonoBehaviour
    {
        public static PlayerBalance Instance { get; private set; }

        [SerializeField] private int startingBalance = 200;

        public int Balance { get; private set; }

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Balance = startingBalance;
            }
            else {
                Destroy(gameObject);
            }
        }

        public bool AddBalance(int amount) {
            Balance += amount;
            Debug.Log($"Added {amount} to balance. New balance: {Balance}");
            return true;
        }

        public bool DeductBalance(int amount) {
            if (Balance >= amount) {
                Balance -= amount;
                Debug.Log($"Deducted {amount} from balance. New balance: {Balance}");
                return true;
            }

            Debug.Log($"Cannot deduct {amount} from balance. Current balance: {Balance}");
            return false;
        }

        public bool HasSufficientBalance(int amount) {
            return Balance >= amount;
        }
    }
}