using SAE_Dubai.Youssef.Scripts;
using TMPro;
using UnityEngine;

namespace SAE_Dubai.Leonardo
{
    [RequireComponent(typeof(Collider))]
    public class Teleporter : MonoBehaviour
    {
        [Header("- Teleport Settings")]
        [Tooltip("The destination transform where the player will be teleported.")]
        public Transform destination;

        [Tooltip("Text displayed when player looks at the teleporter.")]
        public string interactionPromptMessage = "Press E to go back to your apartment";

        [Header("- Interaction")]
        public LayerMask interactableLayer;
        [Tooltip("How close the player needs to be to interact.")]
        public float interactionRange = 3f;
        [Tooltip("The key to press to activate the teleporter.")]
        public KeyCode interactKey = KeyCode.E;
        [Tooltip("Assign the TextMeshProUGUI component within the panel.")]
        public TextMeshProUGUI interactionPromptText;

        [Header("- Tutorial Integration")]
        [Tooltip("Check this box ONLY for the teleporter that returns the player to the apartment.")]
        public bool isReturnToApartmentTeleporter = false;
        
        private Transform _playerTransform;
        private Camera _playerCamera;
        private CharacterController _characterController;
        private MovementSystem _movementSystem;
        private MouseController _mouseLook;

        private bool _playerIsLooking;
        private bool _isTeleporting;

        void Start()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
                _characterController = playerObject.GetComponent<CharacterController>();
                _movementSystem = playerObject.GetComponent<MovementSystem>();
                _mouseLook = playerObject.GetComponentInChildren<MouseController>();
            }
            else
            {
                Debug.LogError("Teleporter: Could not find GameObject with tag 'Player'. Teleporter will not function.", this);
                enabled = false;
                return;
            }

            _playerCamera = Camera.main;

            if (destination == null) Debug.LogError($"Teleporter '{gameObject.name}': Destination is not assigned!", this);
            if (interactionPromptText == null) Debug.LogWarning($"Teleporter '{gameObject.name}': Interaction Prompt Text not assigned.", this);
            if (_playerCamera == null) Debug.LogError("Teleporter: Main Camera not found!", this);
            if (_characterController == null) Debug.LogWarning("Teleporter: Player CharacterController not found.", this);
            if (_movementSystem == null) Debug.LogWarning("Teleporter: Player MovementSystem script not found.", this);
            if (_mouseLook == null) Debug.LogWarning("Teleporter: Player MouseController script not found.", this);

            HideInteractionPrompt();
        }

        void Update()
        {
            if (_isTeleporting || _playerCamera == null || _playerTransform == null) return;

            CheckIfPlayerIsLooking();

            if (_playerIsLooking && Input.GetKeyDown(interactKey))
            {
                StartTeleport();
            }
        }

        void CheckIfPlayerIsLooking()
        {
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange) && hit.collider == GetComponent<Collider>())
            {
                if (!_playerIsLooking)
                {
                    _playerIsLooking = true;
                    ShowInteractionPrompt();
                }
            }
            else
            {
                if (_playerIsLooking)
                {
                    _playerIsLooking = false;
                    HideInteractionPrompt();
                }
            }
        }

        void ShowInteractionPrompt()
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.text = interactionPromptMessage;
                interactionPromptText.gameObject.SetActive(true);
            }
        }

        void HideInteractionPrompt()
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
            }
        }

        void StartTeleport() {
            if (_isTeleporting || destination == null) return;

            Debug.Log($"Teleporter '{gameObject.name}': Initiating teleport to '{destination.name}'.");
            _isTeleporting = true;
            HideInteractionPrompt();

            ScreenFader fader = ScreenFader.Instance;

            SetPlayerControlsActive(false);

            if (fader != null)
            {
                // Fade out, teleport when black, then fade in.
                fader.StartFadeOut(onComplete: () => {
                    ExecuteTeleport();
                    // Re-enable controls *after* fading back in.
                    fader.StartFadeIn(onComplete: () => {
                        SetPlayerControlsActive(true);
                        _isTeleporting = false;
                    });
                });
            }
            else
            {
                // No fader, teleport instantly.
                ExecuteTeleport();
                SetPlayerControlsActive(true); // Re-enable controls immediately.
                _isTeleporting = false; // Allow teleporting again.
            }
        }

        void ExecuteTeleport()
        {
            if (_characterController != null)
            {
                _characterController.enabled = false; // MUST disable before changing position.
                _playerTransform.position = destination.position;
                _playerTransform.rotation = destination.rotation;
                _characterController.enabled = true;
                isReturnToApartmentTeleporter = true;
                //Debug.Log($"Teleporter: Player moved to {destination.position}");
            }
            else if (_playerTransform != null) // ? Fallback if no CharacterController.
            {
                _playerTransform.position = destination.position;
                _playerTransform.rotation = destination.rotation;
                isReturnToApartmentTeleporter = true;
                //Debug.Log($"Teleporter: Player moved to {destination.position} (using Transform directly)");
            }
            if (isReturnToApartmentTeleporter)
            {
                TutorialManager.Instance?.NotifyReturnedToApartment();
            }
        }

        void SetPlayerControlsActive(bool isActive)
        {
            if (_movementSystem != null) _movementSystem.enabled = isActive;
            if (_mouseLook != null) _mouseLook.enabled = isActive;

            // Manage cursor lock state based on controls being active.
            if (isActive)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Debug.Log($"Teleporter: Player controls set to {isActive}");
        }
    }
}