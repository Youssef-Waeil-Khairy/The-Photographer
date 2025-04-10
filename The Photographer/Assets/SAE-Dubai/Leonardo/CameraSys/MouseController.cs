using UnityEngine;

namespace SAE_Dubai.Leonardo
{
    public class MouseController : MonoBehaviour
    {
        [Header("- Mouse Control Settings")]
        public KeyCode freeMouseKey = KeyCode.LeftAlt;
        
        [Header("- Player Controller")]
        [Tooltip("Reference to your movement controller script - assign in inspector")]
        public MonoBehaviour playerController;
        
        private CursorLockMode _originalLockState;
        private bool _originalCursorVisible;
        private bool _freeMouseActive;
        private Vector2 _lastCameraRotation;
        
        private void Start()
        {
            _originalLockState = Cursor.lockState;
            _originalCursorVisible = Cursor.visible;
            
            if (playerController == null)
            {
                playerController = GetComponent<MonoBehaviour>();
                
                if (playerController == null)
                {
                    Debug.LogWarning("MouseController.cs: Player controller not found. Please assign it in the inspector.");
                }
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(freeMouseKey))
            {
                _freeMouseActive = true;
                EnableFreeMouse();
            }
            else if (Input.GetKeyUp(freeMouseKey))
            {
                _freeMouseActive = false;
                DisableFreeMouse();
            }
        }
        
        private void EnableFreeMouse()
        {
            if (Camera.main != null)
            {
                _lastCameraRotation = new Vector2(Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.x);
            }
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            if (playerController != null)
            {
                playerController.enabled = false;
                Debug.Log("MpuseController.cs: Disabled player controller: " + playerController.GetType().Name);
            }
            
            Debug.Log("MouseController.cs: Free mouse mode enabled");
        }
        
        private void DisableFreeMouse()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (playerController != null)
            {
                playerController.enabled = true;
                Debug.Log("MouseController.cs: Enabled player controller: " + playerController.GetType().Name);
            }
            
            Debug.Log("MouseController.cs: Free mouse mode disabled");
        }
        
        private void OnDisable()
        {
            if (_freeMouseActive)
            {
                DisableFreeMouse();
                _freeMouseActive = false;
            }
        }
    }
}