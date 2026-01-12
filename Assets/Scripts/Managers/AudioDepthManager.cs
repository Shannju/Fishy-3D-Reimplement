using UnityEngine;

/// <summary>
/// 根据玩家Y轴位置切换音频的深度管理器
/// 使用已有的AudioSource，根据深度切换不同的音频剪辑
/// </summary>
public class AudioDepthManager : MonoBehaviour
{
    [Header("音频源")]
    [Tooltip("使用的AudioSource（会自动从FishPlayer查找）")]
    [SerializeField]
    private AudioSource audioSource;

    [Header("音频文件")]
    [Tooltip("浅水层背景音乐")]
    [SerializeField]
    private AudioClip shallowWaterBgm;

    [Tooltip("深水层背景音乐")]
    [SerializeField]
    private AudioClip deepWaterBgm;

    [Header("切换设置")]
    [Tooltip("切换到深水音乐的Y轴高度")]
    [SerializeField]
    private float deepWaterThresholdY = -5f;

    [Tooltip("是否启用音频")]
    [SerializeField]
    private bool enableAudio = true;

    [Header("玩家引用")]
    [Tooltip("玩家对象（如果为空会自动查找FishPlayer）")]
    [SerializeField]
    private FishPlayer player;

    private bool _isPlayingDeepWaterMusic = false;

    private void Awake()
    {
        // 如果没有手动指定玩家，自动查找
        if (player == null)
        {
            player = FindObjectOfType<FishPlayer>();
        }

        // 如果没有手动指定AudioSource，从玩家获取
        if (audioSource == null && player != null)
        {
            // 通过反射获取FishPlayer基类的audioSource字段
            var field = typeof(FishPlayer).BaseType.GetField(
                "audioSource",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (field != null)
            {
                audioSource = field.GetValue(player) as AudioSource;
            }
        }
    }

    private void Start()
    {
        if (enableAudio && audioSource != null)
        {
            StartInitialAudio();
        }
    }

    private void StartInitialAudio()
    {
        if (shallowWaterBgm != null)
        {
            audioSource.clip = shallowWaterBgm;
            audioSource.loop = true;
            audioSource.Play();
            _isPlayingDeepWaterMusic = false;
        }
    }

    private void Update()
    {
        if (!enableAudio || player == null || audioSource == null)
        {
            return;
        }

        CheckAndSwitchAudio();
    }

    private void CheckAndSwitchAudio()
    {
        float playerY = player.transform.position.y;
        bool shouldPlayDeepWater = playerY <= deepWaterThresholdY;

        if (shouldPlayDeepWater != _isPlayingDeepWaterMusic)
        {
            SwitchAudio(shouldPlayDeepWater);
            _isPlayingDeepWaterMusic = shouldPlayDeepWater;
        }
    }

    private void SwitchAudio(bool toDeepWater)
    {
        AudioClip targetClip = toDeepWater ? deepWaterBgm : shallowWaterBgm;

        if (targetClip != null)
        {
            // 记录当前播放时间
            float currentTime = audioSource.time;

            // 切换音频剪辑
            audioSource.clip = targetClip;

            // 尝试在相同的时间位置继续播放
            if (currentTime < targetClip.length)
            {
                audioSource.time = currentTime;
            }

            audioSource.Play();

            Debug.Log($"[AudioDepthManager] 切换到 {(toDeepWater ? "深水" : "浅水")} 音乐");
        }
    }

    /// <summary>
    /// 启用或禁用音频
    /// </summary>
    public void SetAudioEnabled(bool enabled)
    {
        enableAudio = enabled;

        if (enabled)
        {
            StartInitialAudio();
        }
        else
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }

    /// <summary>
    /// 设置深水切换阈值
    /// </summary>
    public void SetDeepWaterThreshold(float thresholdY)
    {
        deepWaterThresholdY = thresholdY;
    }

    /// <summary>
    /// 设置玩家对象
    /// </summary>
    public void SetPlayer(FishPlayer fishPlayer)
    {
        player = fishPlayer;

        // 重新获取AudioSource
        if (audioSource == null && player != null)
        {
            var field = typeof(FishPlayer).BaseType.GetField(
                "audioSource",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (field != null)
            {
                audioSource = field.GetValue(player) as AudioSource;
            }
        }
    }
}
