using UnityEngine;

namespace SAE_Dubai.Youssef.Scripts
{
    public class CrouchingSystem : MonoBehaviour
    {
        [Header("Crouching settings")]
        //when the player is standing aka normal position
        public Transform standingPoint;
        //when the player crouches to any positions the players wants
        public Transform crouchPoint;
        public float crouchSpeed = 5f;
    
        // * Leo: I added this to stop clipping.
        [Tooltip("Maximum crouch level (0-1) to prevent ground clipping")]
        [Range(0f, 1f)]
        public float maxCrouchLevel = 0.8f;
    
        private Transform playerCam;
        //0 being standing and 1 is fully crouched, so like (0.1 ,0.2 ,0.9 ,1.0)
        private float crouchLevel = 0.2f;
        private float targetCrouchLevel = 0.0f;
        private bool isCrouching = false;

        //for movement script to access it
        public bool IsCrouching => isCrouching;
    
        private Vector3 currentCameraPosition;

        private void Start()
        {
            playerCam = Camera.main?.transform;
            if (playerCam != null)
            {
                currentCameraPosition = standingPoint.position;
                playerCam.localPosition = currentCameraPosition;
            }
        }

        private void LateUpdate()
        {
            HandleCrouch();
        }

        void HandleCrouch()
        {
            isCrouching = Input.GetKey(KeyCode.LeftControl);

            if (isCrouching) 
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                targetCrouchLevel = Mathf.Clamp(targetCrouchLevel + scroll, 0f, maxCrouchLevel);
            }
            else
            {
                targetCrouchLevel = 0f;
            }

            crouchLevel = Mathf.Lerp(crouchLevel, targetCrouchLevel, Time.deltaTime * crouchSpeed);

            if (playerCam != null && standingPoint != null && crouchPoint != null)
            {
                Vector3 newPosition = Vector3.Lerp(standingPoint.position, crouchPoint.position, crouchLevel);
            
                currentCameraPosition = Vector3.Lerp(currentCameraPosition, newPosition, Time.deltaTime * crouchSpeed * 2f);
            
                playerCam.localPosition = currentCameraPosition;
            }
        }
    }
}