using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 玩家控制的鱼，继承 LivingEntity，使用 X 键控制张嘴闭嘴
/// </summary>
public class FishPlayer : LivingEntity
{
    [Header("Bite -> Grow")]
    [Tooltip("吃了多少口以后长大一次")]
    [SerializeField] protected int bitesToGrow = 5;

    [Tooltip("当前累计吃了多少口（到阈值会清零）")]
    protected int bitesAccumulated = 0;

    [Header("Refs")]
    [Tooltip("嘴巴感知")]
    [SerializeField] protected MouthSensor mouth;

    [Header("Audio")]
    [Tooltip("是否启用音效（玩家鱼启用，AI鱼禁用）")]
    [SerializeField] protected bool enableAudio = false;
    [SerializeField] protected AudioSource audioSource;
    [Tooltip("张嘴音效")]
    [SerializeField] protected AudioClip mouthOpenSfx;
    [Tooltip("闭嘴音效")]
    [SerializeField] protected AudioClip mouthCloseSfx;

    [Header("Bite Settings")]
    [Tooltip("咬一口的冷却时间")]
    [SerializeField] protected float biteCooldown = 0.15f;
    protected float _nextBiteTime;

    protected override void Awake()
    {
        base.Awake();

        if (mouth == null)
            mouth = GetComponent<MouthSensor>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // 玩家鱼启用音效
        enableAudio = true;
    }

    /// <summary>
    /// 张嘴（供子类或外部调用）
    /// </summary>
    public void OpenMouth()
    {
        if (Time.time < _nextBiteTime)
        {
            Debug.Log($"[FishPlayer] 冷却中，还需等待 {_nextBiteTime - Time.time:F2} 秒");
            return;
        }

        // 播放张嘴音效（如果启用）
        if (enableAudio)
            PlayMouthOpenSound();

        // 开始咬的动作
        TryBiteOnce();
        _nextBiteTime = Time.time + biteCooldown;
    }

    /// <summary>
    /// 闭嘴（供子类或外部调用）
    /// </summary>
    public void CloseMouth()
    {
        // 播放闭嘴音效（如果启用）
        if (enableAudio)
            PlayMouthCloseSound();
    }

    /// <summary>
    /// 尝试咬一次（供AI调用，不播放音效）
    /// </summary>
    public bool TryBite()
    {
        if (Time.time < _nextBiteTime)
            return false;

        bool success = TryBiteOnce();
        if (success)
            _nextBiteTime = Time.time + biteCooldown;

        return success;
    }

    protected virtual bool TryBiteOnce()
    {
        if (mouth == null)
        {
            Debug.LogWarning("[FishPlayer] MouthSensor 未找到");
            return false;
        }

        var target = mouth.CurrentTarget;
        if (target == null)
        {
            Debug.Log("[FishPlayer] 没有检测到可吃的目标");
            return false;
        }

        Debug.Log($"[FishPlayer] 尝试吃目标: {target.GetType().Name}");
        bool eaten = BiteOnce(target);

        if (eaten)
        {
            Debug.Log($"[FishPlayer] 成功吃到！累计: {bitesAccumulated}/{bitesToGrow}");
        }
        else
        {
            Debug.Log("[FishPlayer] 吃失败（目标可能已被吃完）");
        }

        return eaten;
    }

    /// <summary>
    /// 吃一口：成功吃到 1 个单位则返回 true。
    /// 具体谁触发（玩家按键 / AI张嘴）由外部系统调用此函数。
    /// </summary>
    public bool BiteOnce(IEatable target)
    {
        if (target == null || target.IsDepleted) return false;

        bool eaten = target.ConsumeOneUnit();
        if (!eaten) return false;

        bitesAccumulated += 1;

        if (bitesAccumulated >= bitesToGrow)
        {
            GrowOnce();
            bitesAccumulated = 0; // 清零
        }

        return true;
    }

    private void GrowOnce()
    {
        // 使用 LivingEntity 的档位系统来长大
        int newTier = CurrentSizeTier + 1;
        ApplySizeTier(newTier);
        Debug.Log($"[FishPlayer] 长大到第 {newTier} 档！");
    }

    protected void PlayMouthOpenSound()
    {
        if (audioSource != null && mouthOpenSfx != null)
            audioSource.PlayOneShot(mouthOpenSfx);
    }

    protected void PlayMouthCloseSound()
    {
        if (audioSource != null && mouthCloseSfx != null)
            audioSource.PlayOneShot(mouthCloseSfx);
    }

    private void Update()
    {
        // 检测按下X键
        if (IsBitePressedThisFrame())
        {
            Debug.Log("[FishPlayer] X键被按下");
            OpenMouth();
        }
        
        // 检测抬起X键
        if (IsBiteReleasedThisFrame())
        {
            Debug.Log("[FishPlayer] X键被抬起");
            CloseMouth();
        }
    }

    private bool IsBitePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.xKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.X);
#endif
    }

    private bool IsBiteReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.xKey.wasReleasedThisFrame;
#else
        return Input.GetKeyUp(KeyCode.X);
#endif
    }
}

