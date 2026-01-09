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
