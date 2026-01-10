using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 玩家控制的鱼：
/// X：张嘴/咬
/// </summary>
public class FishPlayer : FishBase
{
    [Header("Shrink Settings")]
    [Tooltip("在脏水中泡多长时间开始缩小（秒）")]
    [SerializeField] private float shrinkTimeThreshold = 3f;
    [Tooltip("缩小音效")]
    [SerializeField] private AudioClip shrinkSfx;
    [Tooltip("缩小冷却时间")]
    [SerializeField] private float shrinkCooldown = 1f;

    private float _dirtyWaterEnterTime = -1f;
    private float _lastShrinkTime = -1f;
    private bool _isInDirtyWater = false;
    protected override bool TryBiteOnce()
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

    protected override void GrowOnce()
    {
        int newTier = CurrentSizeTier + 1;
        ApplySizeTier(newTier);
        Debug.Log($"[FishPlayer] 长大到第 {newTier} 档！");
    }

    protected void ShrinkOnce()
    {
        if (CurrentSizeTier <= 1)
        {
            Debug.Log("[FishPlayer] 已是最小尺寸，无法继续缩小");
            return;
        }

        int newTier = CurrentSizeTier - 1;
        ApplySizeTier(newTier);

        // 播放缩小音效
        if (enableAudio && audioSource != null && shrinkSfx != null)
        {
            audioSource.PlayOneShot(shrinkSfx);
        }

        Debug.Log($"[FishPlayer] 缩小到第 {newTier} 档！");
    }

    // -----------------------------
    // Update (Input & Shrink)
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

        // 脏水缩小逻辑
        UpdateShrinkLogic();
    }

    private void UpdateShrinkLogic()
    {
        // 检测是否在脏水中（通过标签检测）
        bool currentlyInDirtyWater = IsInDirtyWater();

        if (currentlyInDirtyWater && !_isInDirtyWater)
        {
            // 刚进入脏水
            _isInDirtyWater = true;
            _dirtyWaterEnterTime = Time.time;
            Debug.Log("[FishPlayer] 进入脏水区域");
        }
        else if (!currentlyInDirtyWater && _isInDirtyWater)
        {
            // 刚离开脏水
            _isInDirtyWater = false;
            _dirtyWaterEnterTime = -1f;
            Debug.Log("[FishPlayer] 离开脏水区域");
        }

        // 如果在脏水中且时间足够，尝试缩小
        if (_isInDirtyWater && _dirtyWaterEnterTime > 0)
        {
            float timeInDirtyWater = Time.time - _dirtyWaterEnterTime;
            if (timeInDirtyWater >= shrinkTimeThreshold &&
                (Time.time - _lastShrinkTime) >= shrinkCooldown)
            {
                ShrinkOnce();
                _lastShrinkTime = Time.time;
                // 重置计时器，继续下一个缩小周期
                _dirtyWaterEnterTime = Time.time;
            }
        }
    }

    private bool IsInDirtyWater()
    {
        // 通过碰撞检测或重叠检测来判断是否在脏水中
        // 这里使用简单的标签检测，假设脏水物体有"DirtyWater"标签
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("DirtyWater"))
            {
                return true;
            }
        }
        return false;
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
