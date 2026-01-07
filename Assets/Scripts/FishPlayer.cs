using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 玩家控制的鱼：
/// X：张嘴/咬
/// </summary>
public class FishPlayer : LivingEntity
{
    [Header("Bite -> Grow")]
    [Tooltip("吃了多少口以后长大一次")]
    [SerializeField] protected int bitesToGrow = 5;
    [Tooltip("当前累计吃了多少口（到阈值会清零）")]
    protected int bitesAccumulated = 0;

    [Header("Refs")]
    [SerializeField] protected MouthSensor mouth;
    [SerializeField] protected Rigidbody rb;

    [Header("Audio")]
    [SerializeField] protected bool enableAudio = true;
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioClip mouthOpenSfx;
    [SerializeField] protected AudioClip mouthCloseSfx;

    [Header("Bite Settings")]
    [SerializeField] protected float biteCooldown = 0.15f;
    protected float _nextBiteTime;

    // -----------------------------
    // Awake
    // -----------------------------
    protected override void Awake()
    {
        base.Awake();

        if (mouth == null) mouth = GetComponent<MouthSensor>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        enableAudio = true;
    }

    // -----------------------------
    // Bite/Grow (保留)
    // -----------------------------
    public void OpenMouth()
    {
        if (Time.time < _nextBiteTime)
        {
            Debug.Log($"[FishPlayer] 冷却中，还需等待 {_nextBiteTime - Time.time:F2} 秒");
            return;
        }

        if (enableAudio) PlayMouthOpenSound();

        TryBiteOnce();
        _nextBiteTime = Time.time + biteCooldown;
    }

    public void CloseMouth()
    {
        if (enableAudio) PlayMouthCloseSound();
    }

    public bool TryBite()
    {
        if (Time.time < _nextBiteTime) return false;

        bool success = TryBiteOnce();
        if (success) _nextBiteTime = Time.time + biteCooldown;
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

        if (eaten) Debug.Log($"[FishPlayer] 成功吃到！累计: {bitesAccumulated}/{bitesToGrow}");
        else Debug.Log("[FishPlayer] 吃失败（目标可能已被吃完）");

        return eaten;
    }

    public bool BiteOnce(IEatable target)
    {
        if (target == null || target.IsDepleted) return false;

        bool eaten = target.ConsumeOneUnit();
        if (!eaten) return false;

        bitesAccumulated += 1;

        if (bitesAccumulated >= bitesToGrow)
        {
            GrowOnce();
            bitesAccumulated = 0;
        }

        return true;
    }

    private void GrowOnce()
    {
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





    // -----------------------------
    // Update (Input)
    // -----------------------------
    private void Update()
    {
        // X: bite
        if (IsBitePressedThisFrame())
        {
            Debug.Log("[FishPlayer] X键被按下");
            OpenMouth();
        }
        if (IsBiteReleasedThisFrame())
        {
            Debug.Log("[FishPlayer] X键被抬起");
            CloseMouth();
        }
    }

    private bool IsBitePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.X);
#endif
    }

    private bool IsBiteReleasedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.xKey.wasReleasedThisFrame;
#else
        return Input.GetKeyUp(KeyCode.X);
#endif
    }

}
