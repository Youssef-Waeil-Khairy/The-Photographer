using System;
using UnityEngine;

namespace SAE_Dubai.JW
{
    public class PlayerBalance : MonoBehaviour
    {
        public static PlayerBalance Instance;
        
        public float Balance;

        private void Start()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else if  (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}