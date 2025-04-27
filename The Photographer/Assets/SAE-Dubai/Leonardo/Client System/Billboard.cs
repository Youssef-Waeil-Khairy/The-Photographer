using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    /// <summary>
    /// Makes an object always face the main camera.
    /// Useful for UI elements that should always face the player.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera _mainCamera;
        
        [Tooltip("Lock rotation around the forward axis")]
        public bool lockX = true;
        
        [Tooltip("Lock rotation around the right axis")]
        public bool lockY = false;
        
        [Tooltip("Lock rotation around the up axis")]
        public bool lockZ = true;
        
        private void Start()
        {
            _mainCamera = Camera.main;
            
            if (_mainCamera == null)
            {
                Debug.LogWarning("Billboard.cs: Main camera not found. Billboard effect will not work until a camera is assigned.", this);
            }
        }
        
        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }
            
            Vector3 directionToCamera = _mainCamera.transform.position - transform.position;
            transform.rotation = Quaternion.LookRotation(-directionToCamera);
            Vector3 eulerAngles = transform.eulerAngles;
            
            if (lockX) eulerAngles.x = 0;
            if (lockY) eulerAngles.y = 0;
            if (lockZ) eulerAngles.z = 0;
            
            transform.eulerAngles = eulerAngles;
        }
    }
}