using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SAE_Dubai.Leonardo.CameraSys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SAE_Dubai.Leonardo.Client_System
{
    /// <summary>
    /// Displays feedback text as a billboard above clients when photos are taken.
    /// Uses DOTween animations and emotion-based sound effects.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ClientFeedbackText : MonoBehaviour
    {
        [Header("- Text Settings")]
        [SerializeField] private GameObject feedbackTextPrefab;

        [SerializeField] private Transform textSpawnPosition;
        [SerializeField] private float textOffsetY = 2.0f;
        [SerializeField] private float textDisplayDuration = 3.0f;

        [Header("- Animation Settings")]
        [SerializeField] private float popInDuration = 0.3f;

        [SerializeField] private float holdDuration = 2.0f;
        [SerializeField] private float fadeOutDuration = 0.7f;
        [SerializeField] private float floatDistance = 0.5f;
        [SerializeField] private Ease popInEase = Ease.OutBack;
        [SerializeField] private Ease floatEase = Ease.InOutSine;
        [SerializeField] private Ease fadeEase = Ease.InCubic;

        [Header("- Color Settings")]
        [Tooltip("Color for positive feedback text")] [SerializeField]
        private Color positiveColor = Color.green;

        [Tooltip("Color for negative feedback text")] [SerializeField]
        private Color negativeColor = Color.red;

        [Tooltip("Color for confused feedback text")] [SerializeField]
        private Color confusedColor = Color.yellow;

        [Header("- Audio Settings")]
        [SerializeField] private AudioClip[] positiveEmotionSounds;

        [SerializeField] private AudioClip[] negativeEmotionSounds;
        [SerializeField] private AudioClip[] confusedEmotionSounds;
        [SerializeField] private float volumeMin = 0.8f;
        [SerializeField] private float volumeMax = 1.0f;
        [SerializeField] private float pitchMin = 0.9f;
        [SerializeField] private float pitchMax = 1.1f;

        private ClientJobController _clientController;
        private AudioSource _audioSource;
        private TextMeshProUGUI _currentFeedbackText;
        private GameObject _currentTextObject;
        private Canvas _worldSpaceCanvas;

        #region Feedback Messages

        private readonly List<string> _positiveExtremeCloseUpResponses = new() {
            "Perfect detail on my face! Exactly what I wanted!",
            "Wow! You captured every detail! I love it!",
            "The extreme close-up is perfect! Thank you!",
            "This is exactly the intimate portrait I needed!",
            "You've really captured my essence with this close shot!"
        };

        private readonly List<string> _positiveBigCloseUpResponses = new() {
            "Great big close-up! My face looks perfect!",
            "I love this framing - forehead to chin - perfect!",
            "This big close-up is exactly what I wanted!",
            "Perfect facial portrait! Just right!",
            "You've captured my face beautifully in this shot!"
        };

        private readonly List<string> _positiveCloseUpResponses = new() {
            "That's a beautiful close-up! Just what I needed!",
            "Perfect head and shoulders shot! I love this portrait!",
            "That's exactly the close-up I was looking for!",
            "Fantastic portrait! My face and neck look amazing!",
            "You nailed the close-up! Thank you!"
        };

        private readonly List<string> _positiveMediumCloseUpResponses = new() {
            "Great shot of my upper body! Just perfect!",
            "I love how you captured my shoulders and chest together!",
            "That's exactly the medium close-up I wanted!",
            "Excellent framing of my upper body! Thank you!",
            "This medium close-up is just right! Great job!"
        };

        private readonly List<string> _positiveMidShotResponses = new() {
            "Perfect waist-up shot! Exactly what I needed!",
            "That's a great mid shot! I look so good!",
            "Just the framing I wanted! From waist to head, perfect!",
            "This mid shot is exactly right! Thanks!",
            "You've captured my upper half perfectly!"
        };

        private readonly List<string> _positiveMediumLongShotResponses = new() {
            "Perfect shot from knees up! Just what I wanted!",
            "Great American shot! This is exactly right!",
            "I love this medium-long framing! Excellent work!",
            "That's exactly the knee-to-head shot I needed!",
            "Perfect medium-long frame! You're a pro!"
        };

        private readonly List<string> _positiveLongShotResponses = new() {
            "Perfect full-body shot! I look amazing!",
            "That's exactly the long shot I wanted! My whole look is captured!",
            "Great job getting my entire figure in frame!",
            "This full-body composition is exactly what I needed!",
            "Perfect long shot! Every detail from head to toe!"
        };

        private readonly List<string> _positiveVeryLongShotResponses = new() {
            "I love how this shows me in my environment! Perfect!",
            "This very long shot is exactly what I wanted!",
            "You've captured me with just the right amount of surroundings!",
            "Perfect very long shot! I love how you framed this!",
            "This is exactly the sense of space I was looking for!"
        };

        private readonly List<string> _positiveExtremeLongShotResponses = new() {
            "I love how small I look in this vast space! Perfect!",
            "This extreme long shot is exactly what I wanted!",
            "You've captured the entire environment beautifully!",
            "Perfect extreme long shot! I look great in this landscape!",
            "This is exactly the dramatic wide view I was looking for!"
        };

        private readonly List<string> _genericPositiveResponses = new() {
            "Perfect! This is exactly what I wanted!",
            "I love this shot! You're amazing!",
            "Wow! This looks fantastic! Thank you!",
            "This is perfect! Couldn't ask for better!",
            "Absolutely beautiful! Great work!"
        };

        private readonly List<string> _genericNegativeResponses = new() {
            "No, that's not quite right. Let's try again.",
            "Hmm, not what I was looking for. Could we retry?",
            "This isn't working for me. Let's take another shot.",
            "Not quite there yet. Let's keep trying.",
            "I don't think this is what I asked for."
        };

        #endregion

        private void Awake() {
            _clientController = GetComponent<ClientJobController>();
            _audioSource = GetComponent<AudioSource>();

            if (_audioSource == null) {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1.0f;
            }

            if (feedbackTextPrefab == null) {
                Debug.LogWarning(
                    $"ClientFeedbackText on {gameObject.name}: feedbackTextPrefab is not assigned. Dynamic canvas creation will be used, which might have scaling/positioning issues. Using a prefab is recommended.",
                    this);
            }
        }

        private void Start() {
            if (_clientController != null) {
                // _clientController.OnPhotoChecked += HandlePhotoResult;
                _clientController.OnJobCompleted += HandleJobCompleted;
            }
            else {
                Debug.LogError("ClientFeedbackText: No ClientJobController component found!", this);
            }
        }

        private void OnDestroy() {
            if (_clientController != null) {
                // _clientController.OnPhotoChecked -= HandlePhotoResult;
                _clientController.OnJobCompleted -= HandleJobCompleted;
            }

            DOTween.Kill(transform);
            if (_currentTextObject != null) {
                DOTween.Kill(_currentTextObject.transform);
                DOTween.Kill(_currentFeedbackText);
                Destroy(_currentTextObject);
            }
        }

        public void HandlePhotoResult(CapturedPhoto photo, bool isCorrectComposition) {
            if (photo == null || !photo.portraitShotType.HasValue) {
                ShowFeedback("Hmm... I'm not sure what kind of photo that was...", EmotionType.Confused);
                return;
            }

            if (isCorrectComposition) {
                ShowPositiveFeedback(photo.portraitShotType.Value);
            }
            else {
                ShowNegativeFeedback(photo.portraitShotType.Value);
            }
        }

        private void ShowPositiveFeedback(PortraitShotType shotType) {
            string feedback = GetRandomPositiveFeedback(shotType);
            ShowFeedback(feedback, EmotionType.Positive);
        }

        private void ShowNegativeFeedback(PortraitShotType capturedType) {
            List<PortraitShotType> requiredTypes = _clientController?.requiredShotTypes;
            if (requiredTypes == null || requiredTypes.Count == 0) {
                ShowFeedback("That's not quite what I'm looking for...", EmotionType.Negative);
                return;
            }

            List<PortraitShotType> remainingTypes = requiredTypes.Except(_clientController.completedShotTypes).ToList();
            if (remainingTypes.Count == 0) {
                ShowFeedback("I think we have that shot already. Let's move on!", EmotionType.Confused);
                return;
            }

            PortraitShotType wantedType = remainingTypes[Random.Range(0, remainingTypes.Count)];
            string feedbackStart = GetRandomGenericNegativeFeedback();
            string feedbackEnd = $"I need a {PhotoCompositionEvaluator.GetShotTypeDisplayName(wantedType)} please!";
            ShowFeedback($"{feedbackStart} {feedbackEnd}", EmotionType.Negative);
        }

        private void HandleJobCompleted(ClientJobController client)
        {
            string[] completionMessages = {
                "Perfect! Thank you so much!",
                "These photos are exactly what I needed! Fantastic work!",
                "Wow! You're an amazing photographer! Thanks!",
                "I'm thrilled with these photos! Thank you!",
                "Fantastic work! I'll be sure to recommend you!"
            };

            if (completionMessages.Length > 0)
            {
                string message = completionMessages[Random.Range(0, completionMessages.Length)];
                ShowFeedback(message, EmotionType.Positive);
            }
            else
            {
                Debug.LogWarning("completionMessages array is empty in ClientFeedbackText.HandleJobCompleted!", this);
                ShowFeedback("Thank you!", EmotionType.Positive);
            }
        }


        // ! --- REVISED ShowFeedback ---
        private void ShowFeedback(string message, EmotionType emotion) {
            // 1. PREPARE.
            if (_currentTextObject != null) {
                DOTween.Kill(_currentTextObject.transform, true);
                DOTween.Kill(_currentFeedbackText, true);
                Destroy(_currentTextObject);
                _currentTextObject = null;
                _currentFeedbackText = null;
            }

            PlayEmotionSound(emotion);

            if (feedbackTextPrefab == null) {
                Debug.LogError($"ClientFeedbackText on {gameObject.name}: feedbackTextPrefab is NOT assigned!", this);
                return;
            }

            // 2. DETERMINE SPAWN POSITION/PARENT.
            Transform spawnReference = (textSpawnPosition != null) ? textSpawnPosition : transform;
            Vector3 spawnWorldPosition = spawnReference.position + new Vector3(0, textOffsetY, 0);

            // 3. INSTANTIATE.
            _currentTextObject = Instantiate(feedbackTextPrefab, spawnWorldPosition, Quaternion.identity);

            // 4. SET PARENT
            // _currentTextObject.transform.SetParent(transform, true);

            // 5. ENSURE CORRECT SCALE.
            _currentTextObject.transform.localScale = feedbackTextPrefab.transform.localScale;

            // 6. GET TEXT COMPONENT.
            _currentFeedbackText = _currentTextObject.GetComponentInChildren<TextMeshProUGUI>();
            if (_currentFeedbackText == null) {
                Debug.LogError("TextMeshProUGUI not found in children of feedbackTextPrefab!", _currentTextObject);
                Destroy(_currentTextObject);
                return;
            }

            // Add Billboard if missing (dummy proofing!!!!!!).
            if (_currentTextObject.GetComponent<Billboard>() == null) {
                _currentTextObject.AddComponent<Billboard>();
            }

            // 7. SET TEXT & COLOR (Using Inspector variables).
            _currentFeedbackText.text = message;
            switch (emotion) {
                case EmotionType.Positive:
                    _currentFeedbackText.color = positiveColor;
                    break;
                case EmotionType.Negative:
                    _currentFeedbackText.color = negativeColor;
                    break;
                case EmotionType.Confused:
                    _currentFeedbackText.color = confusedColor;
                    break;
                default:
                    _currentFeedbackText.color = Color.white;
                    break;
            }

            // 8. ANIMATE.
            AnimateFeedbackText();
        }

        private void AnimateFeedbackText() {
            if (_currentTextObject == null || _currentFeedbackText == null) return;

            Vector3 finalScale = _currentTextObject.transform.localScale;

            _currentTextObject.transform.localScale = Vector3.zero; 
            _currentFeedbackText.alpha = 1.0f; // Ensure text is visible initially
            Vector3 startLocalPos = _currentTextObject.transform.localPosition; // Capture starting local position

            Sequence seq = DOTween.Sequence();
            seq.SetTarget(_currentTextObject);

            seq.Append(_currentTextObject.transform.DOScale(finalScale, popInDuration).SetEase(popInEase));

            Vector3 endLocalPos =
                startLocalPos +
                _currentTextObject.transform.up * floatDistance;
            seq.Append(_currentTextObject.transform.DOLocalMove(endLocalPos, holdDuration).SetEase(floatEase));

            float fadeStartTime = holdDuration - fadeOutDuration;
            if (fadeStartTime < popInDuration) fadeStartTime = popInDuration;
            seq.Join(_currentFeedbackText.DOFade(0, fadeOutDuration).SetEase(fadeEase).SetDelay(fadeStartTime));

            seq.OnComplete(() => {
                if (_currentTextObject != null) {
                    Destroy(_currentTextObject);
                    _currentTextObject = null;
                    _currentFeedbackText = null;
                }
            });

            seq.Play();
        }


        private void PlayEmotionSound(EmotionType emotion) {
            if (_audioSource == null) return;
            AudioClip[] clips;
            switch (emotion) {
                case EmotionType.Positive: clips = positiveEmotionSounds; break;
                case EmotionType.Negative: clips = negativeEmotionSounds; break;
                case EmotionType.Confused: clips = confusedEmotionSounds; break;
                default: return;
            }

            if (clips == null || clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;
            _audioSource.pitch = Random.Range(pitchMin, pitchMax);
            _audioSource.volume = Random.Range(volumeMin, volumeMax);
            _audioSource.PlayOneShot(clip);
        }

        private string GetRandomPositiveFeedback(PortraitShotType shotType) {
            List<string> responseList;
            switch (shotType) 
            {
                case PortraitShotType.ExtremeCloseUp: responseList = _positiveExtremeCloseUpResponses; break;
                case PortraitShotType.BigCloseUp: responseList = _positiveBigCloseUpResponses; break;
                case PortraitShotType.CloseUp: responseList = _positiveCloseUpResponses; break;
                case PortraitShotType.MediumCloseUp: responseList = _positiveMediumCloseUpResponses; break;
                case PortraitShotType.MidShot: responseList = _positiveMidShotResponses; break;
                case PortraitShotType.MediumLongShot: responseList = _positiveMediumLongShotResponses; break;
                case PortraitShotType.LongShot: responseList = _positiveLongShotResponses; break;
                case PortraitShotType.VeryLongShot: responseList = _positiveVeryLongShotResponses; break;
                case PortraitShotType.ExtremeLongShot: responseList = _positiveExtremeLongShotResponses; break;
                default: responseList = _genericPositiveResponses; break;
            }

            if (responseList == null || responseList.Count == 0) return "Great shot!";
            return responseList[Random.Range(0, responseList.Count)];
        }

        private string GetRandomGenericNegativeFeedback() {
            if (_genericNegativeResponses == null || _genericNegativeResponses.Count == 0)
                return "Not quite...";
            return _genericNegativeResponses[Random.Range(0, _genericNegativeResponses.Count)];
        }

        private enum EmotionType
        {
            Positive,
            Negative,
            Confused
        }
    }
}