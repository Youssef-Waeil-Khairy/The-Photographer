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

        public void AddBalance(int amount) {
            Balance += amount;
        }

        public void DeductBalance(int amount) {
            if (Balance >= amount) {
                Balance -= amount;
            }
        }

        public bool HasSufficientBalance(int amount) {
            return Balance >= amount;
        }
    }
}