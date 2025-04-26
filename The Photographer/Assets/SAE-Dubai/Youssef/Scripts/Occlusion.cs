using UnityEngine;

/// <summary>
/// Handles sound occlusion effects using Unity's AudioSource, adjusting volume and low-pass filter based on obstacles between audio source and listener.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class OcclusionAudioSource : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private GameObject m_AudioListenerObject;

    [Header("Occlusion Settings")]
    [SerializeField] private bool m_UseOcclusion = false;
    [SerializeField] private LayerMask m_OccludeLayer;
    [SerializeField, Range(0.1f, 1f)] private float m_CheckInterval = 0.2f;
    [SerializeField] private bool m_UseDebug = false;

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f)] private float m_MaxVolume = 1f;
    [SerializeField, Range(500f, 22000f)] private float m_LowPassWhenOccluded = 500f;
    [SerializeField, Range(500f, 22000f)] private float m_LowPassWhenClear = 22000f;
    #endregion

    #region Private Fields
    private AudioSource m_AudioSource;
    private AudioLowPassFilter m_LowPassFilter;
    private float m_Timer;
    private Vector3 m_LastHitPoint;
    private bool m_IsOccluded;
    private Transform m_CachedTransform;
    private Transform m_ListenerTransform;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        m_CachedTransform = transform;
        m_AudioSource = GetComponent<AudioSource>();

        m_LowPassFilter = GetComponent<AudioLowPassFilter>();
        if (m_LowPassFilter == null)
        {
            m_LowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        }

        InitializeOcclusion();
    }

    private void OnEnable()
    {
        PerformOcclusionCheck();
    }

    private void Update()
    {
        if (!ShouldCheckOcclusion()) return;

        m_Timer += Time.deltaTime;
        if (m_Timer >= m_CheckInterval)
        {
            m_Timer = 0f;
            PerformOcclusionCheck();
        }

        if (m_UseDebug)
        {
            DrawDebugLines();
        }
    }

    private void OnValidate()
    {
        m_CheckInterval = Mathf.Max(0.1f, m_CheckInterval);
    }
    #endregion

    #region Private Methods
    private void InitializeOcclusion()
    {
        if (m_OccludeLayer == 0)
        {
            m_OccludeLayer = LayerMask.GetMask("Occlude Sound");
        }

        if (m_AudioListenerObject != null)
        {
            m_ListenerTransform = m_AudioListenerObject.transform;
        }
        else
        {
            Debug.LogWarning($"[OcclusionAudioSource] No AudioListener assigned to {gameObject.name}", this);
            enabled = false;
            return;
        }

        SetAudioSettings(false);
    }

    private bool ShouldCheckOcclusion()
    {
        return m_UseOcclusion && m_AudioListenerObject != null;
    }

    private void PerformOcclusionCheck()
    {
        Vector3 listenerPosition = m_ListenerTransform.position;
        Vector3 emitterPosition = m_CachedTransform.position;

        m_IsOccluded = Physics.Linecast(listenerPosition, emitterPosition, out RaycastHit hit, m_OccludeLayer)
                       && hit.collider.gameObject != gameObject;

        if (m_IsOccluded)
        {
            m_LastHitPoint = hit.point;
        }

        SetAudioSettings(m_IsOccluded);
    }

    private void SetAudioSettings(bool _isOccluded)
    {
        if (m_AudioSource == null) return;

        if (_isOccluded)
        {
            m_AudioSource.volume = m_MaxVolume * 0.5f; // Reduce volume if occluded
            m_LowPassFilter.cutoffFrequency = m_LowPassWhenOccluded;
        }
        else
        {
            m_AudioSource.volume = m_MaxVolume;
            m_LowPassFilter.cutoffFrequency = m_LowPassWhenClear;
        }
    }

    private void DrawDebugLines()
    {
        Vector3 listenerPosition = m_ListenerTransform.position;
        Vector3 emitterPosition = m_CachedTransform.position;

        if (m_IsOccluded)
        {
            Debug.DrawLine(listenerPosition, m_LastHitPoint, Color.red);
            Debug.DrawLine(m_LastHitPoint, emitterPosition, Color.yellow);
        }
        else
        {
            Debug.DrawLine(listenerPosition, emitterPosition, Color.green);
        }
    }
    #endregion
}
