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
    [Header("Camera Settings")]
    [SerializeField] private CameraFollow cameraFollow;

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
        UpdateSizeBasedCamera();
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
        UpdateSizeBasedCamera();

        // 播放缩小音效
        if (enableAudio && audioSource != null && shrinkSfx != null)
        {
            audioSource.PlayOneShot(shrinkSfx);
        }

        Debug.Log($"[FishPlayer] 缩小到第 {newTier} 档！");
    }

    // -----------------------------
    // Awake
    // -----------------------------
    protected override void Awake()
    {
        base.Awake();
        // 初始化摄像机位置
        UpdateSizeBasedCamera();
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

    // -----------------------------
    // Wall Collision Detection
    // -----------------------------
    private void OnCollisionEnter(Collision collision)
    {
        // 使用 bodyCollider 检测墙面碰撞
        if (bodyCollider != null && collision.collider == bodyCollider)
        {
            return; // 忽略自身碰撞
        }

        // 检查是否是墙面（可以通过标签、层或名称识别）
        if (IsWall(collision.gameObject))
        {
            StopMovement();
            Debug.Log($"[FishPlayer] 碰到墙面，停止运动: {collision.gameObject.name}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 也可以使用触发器检测墙面
        if (bodyCollider != null && other == bodyCollider)
        {
            return; // 忽略自身碰撞
        }

        if (IsWall(other.gameObject))
        {
            StopMovement();
            Debug.Log($"[FishPlayer] 碰到墙面（触发器），停止运动: {other.gameObject.name}");
        }
    }

    private bool IsWall(GameObject obj)
    {
        // 可以根据标签、层或名称来识别墙面

        // 1. 标签检测：支持多种墙面标签
        if (obj.CompareTag("Wall") || obj.CompareTag("obstacles"))
        {
            return true;
        }

        // 2. 或者根据名称检测
        if (obj.name.Contains("Wall") || obj.name.Contains("wall") ||
            obj.name.Contains("Obstacle") || obj.name.Contains("obstacle"))
        {
            return true;
        }

        // 3. 或者根据层检测（检查是否是障碍物层，默认检测所有层）
        // 这里简单地认为所有不是"Water"或"Default"的层都是墙面
        int layer = obj.layer;
        string layerName = LayerMask.LayerToName(layer);
        if (!string.IsNullOrEmpty(layerName) &&
            !layerName.Contains("Water") &&
            !layerName.Contains("Default") &&
            layer != 0) // 不是默认层
        {
            return true;
        }

        return false;
    }

    private void StopMovement()
    {
        if (rb != null)
        {
            // 停止所有运动
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 可选：也可以设置位置到碰撞点前一点距离
            // 但这里简单地停止运动即可
        }
    }

    /// <summary>
    /// 根据鱼的大小更新摄像机Z轴位置
    /// 鱼大小从1到maxSize映射到相机Z轴从-5到-10
    /// </summary>
    private void UpdateSizeBasedCamera()
    {
        if (cameraFollow == null) return;

        // 获取最大大小档位
        int maxSize = GetSizeTiers();

        // 计算标准化大小 (0到1之间)
        float normalizedSize = (float)(CurrentSizeTier - 1) / (maxSize - 1);

        // 使用Lerp映射到Z轴位置 (-5到-10)
        float targetZ = Mathf.Lerp(-5f, -10f, normalizedSize);

        // 更新摄像机位置
        Vector3 currentPos = cameraFollow.Camera.transform.position;
        cameraFollow.Camera.transform.position = new Vector3(currentPos.x, currentPos.y, targetZ);
    }
}
