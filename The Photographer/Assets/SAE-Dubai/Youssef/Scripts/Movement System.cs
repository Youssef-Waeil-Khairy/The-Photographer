using UnityEngine;

namespace SAE_Dubai.Youssef.Scripts
{
    public class MovementSystem : MonoBehaviour
    {
        //Adjustments
        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float runningSpeed = 8f;
        public float jumpForce = 7f;
        public float gravity = 10f;

        [Header("Mouse Settings")]
        public float mouseSens = 2f;
        public float maxAngleRotation = 80f;
        public float minAngleRotation = -80f;
        [Tooltip("Enable mouse smoothing to reduce jitter")]
        public bool enableMouseSmoothing = true;
        [Range(0.01f, 0.3f)]
        public float mouseSmoothing = 0.1f;

        private Rigidbody rb;
        private Transform playerCamera;
        private float rotationX = 0f;
        private Vector2 currentMouseDelta = Vector2.zero;
        private Vector2 currentMouseDeltaVelocity = Vector2.zero;

        //reference crouching script, to access the same camera
        private CrouchingSystem crouchingSystem;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            playerCamera = Camera.main?.transform;

            //For mouse pointer to disappear
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            //to prevent rotations
            rb.freezeRotation = true;

            //reference
            crouchingSystem = GetComponent<CrouchingSystem>();
        }

        void Update()
        {
            MouseLook();
        }
    
        void FixedUpdate()
        {
            Move();
        }

        void Move()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            //Calculate movement direction
            Vector3 move = transform.right * moveX + transform.forward * moveZ;
        
            // Normalize movement vector to prevent diagonal speed boost
            if (move.magnitude > 1f)
            {
                move.Normalize();
            }

            //Determine speed
            float speed = Input.GetKey(KeyCode.LeftShift) ? runningSpeed : walkSpeed;

            //if crouching then reduce the speed by 50%
            if (crouchingSystem != null && crouchingSystem.IsCrouching) 
            {
                speed *= 0.5f;
            }

            rb.linearVelocity = new Vector3(move.x * speed, rb.linearVelocity.y, move.z * speed);

            if(Input.GetKey(KeyCode.Space) && IsGrounded())
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            }
        
            if (!IsGrounded())
            {
                rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
            }
        }

        void MouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSens;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSens;
        
            Vector2 targetMouseDelta = new Vector2(mouseX, mouseY);
        
            // ? Apply smoothing if enabled
            if (enableMouseSmoothing)
            {
                currentMouseDelta = Vector2.SmoothDamp(
                    currentMouseDelta, 
                    targetMouseDelta, 
                    ref currentMouseDeltaVelocity, 
                    mouseSmoothing);
            }
            else
            {
                currentMouseDelta = targetMouseDelta;
            }
        
            rotationX -= currentMouseDelta.y;
            rotationX = Mathf.Clamp(rotationX, minAngleRotation, maxAngleRotation);
        
            if (playerCamera != null)
            {
                playerCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
            }
        
            transform.Rotate(Vector3.up * currentMouseDelta.x);
        }

        //checks ground
        bool IsGrounded()
        {
            return Physics.SphereCast(
                transform.position + Vector3.up * 0.1f, 
                0.4f, 
                Vector3.down, 
                out RaycastHit hit, 
                0.6f);
        }
    }
}