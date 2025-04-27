using UnityEngine;

// ! Leo: I added random sound effects.

namespace SAE_Dubai.Youssef.Scripts
{
    public class MovementSystem : MonoBehaviour
    {
        //Adjustments
        [Header("Movemnt Settings")]
        public float walkSpeed = 5f;

        public float runningSpeed = 8f;
        public float jumpForce = 7f;
        public float gravity = 10f;

        [Header("Moused Setings")]
        public float mouseSens = 2f;

        public float maxAngleRotation = 80f;
        public float minAngleRotation = -80f;

        [Header("Footstep Audio")]
        [SerializeField] private AudioSource footstepAudioSource;

        [SerializeField] private AudioClip[] walkingFootstepSounds;
        [SerializeField] private AudioClip[] runningFootstepSounds;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip landSound;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.5f;
        [SerializeField] private float walkingStepInterval = 0.5f;
        [SerializeField] private float runningStepInterval = 0.3f;

        [Header("Sound Randomization")]
        [SerializeField, Range(0f, 0.5f)] private float volumeVariation = 0.1f;

        [SerializeField, Range(0f, 0.5f)] private float pitchVariation = 0.2f;
        [SerializeField, Range(0.5f, 1.5f)] private float basePitch = 1.0f;

        private Rigidbody rb;
        private Transform playerCamera;
        private float rotationX = 0f;

        //refrecne crouching script , to acces the same camera
        private CrouchingSystem crouchingSystem;

        // Footstep timer
        private float footstepTimer = 0f;
        private bool wasGrounded = true;

        private void Start() {
            rb = GetComponent<Rigidbody>();
            playerCamera = Camera.main?.transform;

            //For mouse pointer to disapper
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            //to prevent roatations
            rb.freezeRotation = true;

            //refrence
            crouchingSystem = GetComponent<CrouchingSystem>();

            // Initialize audio source if needed
            if (footstepAudioSource == null) {
                footstepAudioSource = GetComponent<AudioSource>();
                if (footstepAudioSource == null) {
                    footstepAudioSource = gameObject.AddComponent<AudioSource>();
                    footstepAudioSource.playOnAwake = false;
                    footstepAudioSource.spatialBlend = 1.0f; // Make sound 3D
                    footstepAudioSource.volume = footstepVolume;
                }
            }
        }

        void Update() {
            Move();
            MouseLook();

            if (playerCamera != null) {
                //Debug.Log("Camera Position: " + playerCamera.position);
            }
        }

        void Move() {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            //Calculate movement direction
            Vector3 move = transform.right * moveX + transform.forward * moveZ;

            // Check magnitude of movement for footstep sounds
            float moveMagnitude = new Vector2(moveX, moveZ).magnitude;

            //Detrmine speed
            float speed = Input.GetKey(KeyCode.LeftShift) ? runningSpeed : walkSpeed;
            bool isRunning = Input.GetKey(KeyCode.LeftShift);

            //if crouching then reduce the speed by 50
            if (crouchingSystem != null && crouchingSystem.IsCrouching) {
                speed *= 0.5f;
            }

            // Handle footstep sounds
            bool isGrounded = IsGrounded();
            if (isGrounded && moveMagnitude > 0.1f) {
                HandleFootsteps(moveMagnitude, isRunning);
            }

            // Handle jump and landing sounds
            if (isGrounded && !wasGrounded) {
                // Just landed
                PlayLandSound();
            }

            wasGrounded = isGrounded;

            rb.linearVelocity = new Vector3(move.x * speed, rb.linearVelocity.y, move.z * speed);

            if (Input.GetKey(KeyCode.Space) && IsGrounded()) {
                // Play jump sound
                PlayJumpSound();

                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            }
        }

        void MouseLook() {
            float mouseX = Input.GetAxis("Mouse X") * mouseSens;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSens;

            //Adjust vericla roattion 
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, minAngleRotation, maxAngleRotation);

            playerCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
        }

        //checks ground
        bool IsGrounded() {
            return Physics.Raycast(transform.position, Vector3.down, 1.1f);
        }

        private void HandleFootsteps(float moveMagnitude, bool isRunning) {
            float stepInterval = isRunning ? runningStepInterval : walkingStepInterval;
            stepInterval = stepInterval / Mathf.Lerp(0.5f, 1.0f, moveMagnitude);

            footstepTimer += Time.deltaTime;

            if (footstepTimer >= stepInterval) {
                PlayFootstepSound(isRunning);
                footstepTimer = 0f;
            }
        }

        private void PlayFootstepSound(bool isRunning) {
            if (footstepAudioSource == null) return;

            AudioClip[] soundArray = isRunning ? runningFootstepSounds : walkingFootstepSounds;
            if (soundArray == null || soundArray.Length == 0) return;
            AudioClip clip = soundArray[Random.Range(0, soundArray.Length)];

            if (clip != null) {
                float randomVolume = footstepVolume * Random.Range(1f - volumeVariation, 1f + volumeVariation);
                randomVolume = Mathf.Clamp(randomVolume, 0.05f, 1.0f); // Ensure volume stays in reasonable range

                float randomPitch = basePitch * Random.Range(1f - pitchVariation, 1f + pitchVariation);
                float originalPitch = footstepAudioSource.pitch;
                footstepAudioSource.pitch = randomPitch;
                footstepAudioSource.PlayOneShot(clip, randomVolume);
                footstepAudioSource.pitch = originalPitch;
            }
        }

        private void PlayJumpSound() {
            if (footstepAudioSource != null && jumpSound != null) {
                float originalPitch = footstepAudioSource.pitch;
                float randomPitch = basePitch * Random.Range(1f - pitchVariation * 0.5f, 1f + pitchVariation * 0.5f);
                footstepAudioSource.pitch = randomPitch;

                float randomVolume = footstepVolume * Random.Range(1f - volumeVariation, 1f + volumeVariation);
                footstepAudioSource.PlayOneShot(jumpSound, randomVolume);

                footstepAudioSource.pitch = originalPitch;
            }
        }

        private void PlayLandSound() {
            if (footstepAudioSource != null && landSound != null) {
                float originalPitch = footstepAudioSource.pitch;
                float randomPitch = basePitch * Random.Range(0.8f, 1f);
                footstepAudioSource.pitch = randomPitch;

                float randomVolume = footstepVolume * Random.Range(1.1f, 1.3f);
                randomVolume = Mathf.Clamp(randomVolume, 0.05f, 1.0f);
                footstepAudioSource.PlayOneShot(landSound, randomVolume);

                footstepAudioSource.pitch = originalPitch;
            }
        }
    }
}