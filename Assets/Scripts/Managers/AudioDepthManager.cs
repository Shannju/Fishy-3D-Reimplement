using UnityEngine;

/// <summary>
/// 根据玩家Y轴位置切换音频的深度管理器
/// 两个音频源同时播放，通过调整音量实现无缝切换
/// </summary>
public class AudioDepthManager : MonoBehaviour
{
    [Header("音频源设置")]
    [Tooltip("浅水层音频源")]
    [SerializeField] private AudioSource shallowWaterAudioSource;

    [Tooltip("深水层音频源")]
    [SerializeField] private AudioSource deepWaterAudioSource;

    [Header("音频文件")]
    [Tooltip("浅水层背景音乐")]
    [SerializeField] private AudioClip shallowWaterBgm;

    [Tooltip("深水层背景音乐")]
    [SerializeField] private AudioClip deepWaterBgm;

    [Header("切换设置")]
    [Tooltip("Y轴切换开始的高度")]
    [SerializeField] private float transitionStartY = 0f;

    [Tooltip("Y轴切换结束的高度")]
    [SerializeField] private float transitionEndY = -10f;

    [Tooltip("是否启用音频")]
    [SerializeField] private bool enableAudio = true;

    [Header("玩家引用")]
    [Tooltip("玩家对象（如果为空会自动查找FishPlayer）")]
    [SerializeField] private Transform playerTransform;

    private void Awake()
    {
        // 如果没有手动指定玩家，自动查找
        if (playerTransform == null)
        {
            FishPlayer player = FindObjectOfType<FishPlayer>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        // 设置音频源
        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        if (shallowWaterAudioSource == null)
        {
            shallowWaterAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (deepWaterAudioSource == null)
        {
            deepWaterAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // 配置浅水音频源
        ConfigureAudioSource(shallowWaterAudioSource, shallowWaterBgm);

        // 配置深水音频源
        ConfigureAudioSource(deepWaterAudioSource, deepWaterBgm);
    }

    private void ConfigureAudioSource(AudioSource audioSource, AudioClip clip)
    {
        if (audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 1f;
        }
    }

    private void Start()
    {
        if (enableAudio)
        {
            StartAudioPlayback();
        }
    }

    private void StartAudioPlayback()
    {
        if (shallowWaterAudioSource != null && shallowWaterAudioSource.clip != null)
        {
            shallowWaterAudioSource.Play();
        }

        if (deepWaterAudioSource != null && deepWaterAudioSource.clip != null)
        {
            deepWaterAudioSource.Play();
        }
    }

    private void Update()
    {
        if (!enableAudio || playerTransform == null)
        {
            return;
        }

        UpdateAudioVolumes();
    }

    private void UpdateAudioVolumes()
    {
        float playerY = playerTransform.position.y;

        // 计算过渡比例
        float transitionRange = transitionStartY - transitionEndY;
        float normalizedY = Mathf.Clamp01((transitionStartY - playerY) / transitionRange);

        // 浅水音量：从1到0
        float shallowVolume = 1f - normalizedY;

        // 深水音量：从0到1
        float deepVolume = normalizedY;

        // 应用音量
        if (shallowWaterAudioSource != null)
        {
            shallowWaterAudioSource.volume = shallowVolume;
        }

        if (deepWaterAudioSource != null)
        {
            deepWaterAudioSource.volume = deepVolume;
        }

        // 调试输出（可移除）
        // Debug.Log($"Player Y: {playerY:F2}, Shallow Volume: {shallowVolume:F2}, Deep Volume: {deepVolume:F2}");
    }

    /// <summary>
    /// 启用或禁用音频
    /// </summary>
    public void SetAudioEnabled(bool enabled)
    {
        enableAudio = enabled;

        if (enabled)
        {
            StartAudioPlayback();
        }
        else
        {
            if (shallowWaterAudioSource != null)
                shallowWaterAudioSource.Stop();
            if (deepWaterAudioSource != null)
                deepWaterAudioSource.Stop();
        }
    }

    /// <summary>
    /// 设置过渡范围
    /// </summary>
    public void SetTransitionRange(float startY, float endY)
    {
        transitionStartY = startY;
        transitionEndY = endY;
    }

    /// <summary>
    /// 设置玩家对象
    /// </summary>
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }
}