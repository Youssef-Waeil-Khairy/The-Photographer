using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    /// <summary>
    /// Attaches to a game object to mark it as a portrait subject that can be photographed
    /// for portrait assignments. Ensures the object is on the correct layer!!! epic.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PortraitSubject : MonoBehaviour
    {
        public static readonly string SubjectLayerName = "PortraitSubject";

        [Header("- Subject Info")]
        [Tooltip("Name for easier identification in logs - UNIMPORTANT")]
        public string subjectName = "Portrait Subject";

        void Awake() {
            int portraitLayer = LayerMask.NameToLayer(SubjectLayerName);
            if (portraitLayer == -1) {
                Debug.LogError(
                    $"PortraitSubject.cs: Layer '{SubjectLayerName}' not found! Please create it in Unity's Tag Manager and assign it to portrait subjects.");
            }
            else {
                if (gameObject.layer != portraitLayer) {
                    Debug.LogWarning(
                        $"PortraitSubject.cs: Subject '{name}' was not on the '{SubjectLayerName}' layer. Assigning it automatically.",
                        this);
                    gameObject.layer = portraitLayer;
                }
            }
        }
    }
}