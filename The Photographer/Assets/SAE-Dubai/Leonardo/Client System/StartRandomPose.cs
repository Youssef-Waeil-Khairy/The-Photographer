using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    [RequireComponent(typeof(Animator))]
    public class StartRandomPose : MonoBehaviour
    {
        [Header("- Behavior")]
        [Tooltip("If true, a random pose from the list below will be triggered when the object starts.")]
        [SerializeField]
        private bool randomizePoseOnStart = true;

        [Title("- Animator Parameters")]
        [Tooltip("List of BOOLEAN parameter names in the Animator Controller that trigger poses.")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowFoldout = true)]
        [ShowIf("randomizePoseOnStart")]
        [SerializeField]
        private List<string> poseParameterNames = new() {
            "Cross Arms",
            "Check Watch",
            "Normal Hand On Hips",
            "Grumpy Hands on Hips"
        };

        [Tooltip("The name of the TRIGGER parameter in the Animator Controller for job completion.")]
        [SerializeField]
        private string completionTriggerName = "Completed";
        
        [Required]
        [Tooltip("Reference to the Animator component on this GameObject.")]
        [ReadOnly]
        [SerializeField]
        private Animator animator;

        [Button("Test Random Pose Trigger")]
        [ShowIf("randomizePoseOnStart")]
        
        private ClientJobController clientJobController;

        private void TestTriggerInEditor()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Test button pressed during Play mode. Re-running pose logic.");
                TriggerRandomPose();
            }
            else
            {
                Debug.LogWarning("Test button works reliably in Edit Mode if Animator is available, but won't trigger runtime animations.");
            }
        }


        void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError("StartRandomPose: Animator component not found!", this);
            }
            
            clientJobController = GetComponentInParent<ClientJobController>();
            if (clientJobController == null)
            {
                Debug.LogError("StartRandomPose: ClientJobController component not found!", this);
            }
        }

        void Start()
        {
            if (clientJobController != null)
            {
                clientJobController.OnJobCompleted += HandleJobCompleted;
            }
            
            if (randomizePoseOnStart)
            {
                TriggerRandomPose();
            }
            else
            {
                ResetAllPoseParameters();
                //Debug.Log($"StartRandomPose: Random pose on start is disabled for {gameObject.name}");
            }
        }
        
        void OnDestroy()
        {
            if (clientJobController != null)
            {
                clientJobController.OnJobCompleted -= HandleJobCompleted;
            }
        }
        
        private void HandleJobCompleted(ClientJobController completedClient)
        {
            if (animator != null && !string.IsNullOrEmpty(completionTriggerName) && HasParameter(completionTriggerName, animator, AnimatorControllerParameterType.Trigger))
            {
                Debug.Log($"StartRandomPose: Triggering '{completionTriggerName}' on {gameObject.name} because job completed.");
                animator.SetTrigger(completionTriggerName);
            }
            else if (animator != null && !string.IsNullOrEmpty(completionTriggerName))
            {
                Debug.LogError($"StartRandomPose: Completion trigger parameter '{completionTriggerName}' not found or not a Trigger in Animator!", animator);
            }
        }

        private void TriggerRandomPose()
        {
            if (animator == null)
            {
                return;
            }

            if (poseParameterNames == null || poseParameterNames.Count == 0)
            {
                //Debug.LogWarning("StartRandomPose: No pose parameter names provided in the inspector. Cannot trigger random pose.", this);
                return;
            }

            int randomIndex = Random.Range(0, poseParameterNames.Count);
            string chosenParameter = poseParameterNames[randomIndex];

            ResetAllPoseParameters();

            if (!string.IsNullOrEmpty(chosenParameter) && HasParameter(chosenParameter, animator, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(chosenParameter, true);
            }
            else if (!string.IsNullOrEmpty(chosenParameter))
            {
                //Debug.LogError($"StartRandomPose: Pose parameter '{chosenParameter}' not found or not a Bool in Animator!", animator);
            }
        }

        private void ResetAllPoseParameters()
        {
            if (animator == null || poseParameterNames == null) return;

            foreach (string paramName in poseParameterNames)
            {
                if (!string.IsNullOrEmpty(paramName) && HasParameter(paramName, animator, AnimatorControllerParameterType.Bool)) // Check if it's a Bool
                {
                    animator.SetBool(paramName, false);
                }
            }
        }

        private bool HasParameter(string paramName, Animator animatorToCheck, AnimatorControllerParameterType expectedType)
        {
            if (animatorToCheck == null || string.IsNullOrEmpty(paramName)) return false;
            foreach (AnimatorControllerParameter param in animatorToCheck.parameters)
            {
                if (param.type == expectedType && param.name == paramName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}