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

        [Required]
        [Tooltip("Reference to the Animator component on this GameObject.")]
        [ReadOnly]
        [SerializeField]
        private Animator animator;

        [Button("Test Random Pose Trigger")]
        [ShowIf("randomizePoseOnStart")]
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
        }

        void Start()
        {
            if (randomizePoseOnStart)
            {
                TriggerRandomPose();
            }
            else
            {
                ResetAllPoseParameters();
                Debug.Log($"StartRandomPose: Random pose on start is disabled for {gameObject.name}");
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
                Debug.LogWarning("StartRandomPose: No pose parameter names provided in the inspector. Cannot trigger random pose.", this);
                return;
            }

            int randomIndex = Random.Range(0, poseParameterNames.Count);
            string chosenParameter = poseParameterNames[randomIndex];

            ResetAllPoseParameters();

            if (!string.IsNullOrEmpty(chosenParameter) && HasParameter(chosenParameter, animator))
            {
                animator.SetBool(chosenParameter, true);
                Debug.Log($"StartRandomPose: Triggered pose parameter '{chosenParameter}' on {gameObject.name}");
            }
            else if (!string.IsNullOrEmpty(chosenParameter))
            {
                Debug.LogError($"StartRandomPose: Chosen parameter '{chosenParameter}' exists in list but NOT found in Animator Controller! Check spelling.", animator);
            }
        }

        private void ResetAllPoseParameters()
        {
            if (animator == null || poseParameterNames == null) return;

            foreach (string paramName in poseParameterNames)
            {
                if (!string.IsNullOrEmpty(paramName) && HasParameter(paramName, animator))
                {
                    animator.SetBool(paramName, false);
                }
            }
        }

        private bool HasParameter(string paramName, Animator animatorToCheck)
        {
            if (animatorToCheck == null || string.IsNullOrEmpty(paramName)) return false;
            foreach (AnimatorControllerParameter param in animatorToCheck.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
        }
    }
}