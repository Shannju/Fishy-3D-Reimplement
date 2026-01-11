using UnityEngine;

/// <summary>
/// 可缩放的生命体基类：提供变大变小功能
/// 继承此类可以获得大小档位功能
/// </summary>
public abstract class SizeableEntity : LivingEntity
{
    [Header("Size Tiers")]
    [Tooltip("总档位数（例如：3表示有3个档位）")]
    [SerializeField] private int sizeTiers = 3;

    [Tooltip("当前档位（例如：3表示放大3倍，在Start时应用）")]
    [SerializeField] private int currentSizeTier = 1;

    public int CurrentSizeTier => currentSizeTier;

    private Vector3 _baseScale;

    protected override void Awake()
    {
        base.Awake();
        // 记录基础缩放（在应用档位缩放之前）
        _baseScale = transform.localScale;
    }

    protected override void Start()
    {
        base.Start();
        // 根据当前档位缩放
        ApplySizeTier(currentSizeTier);
    }

    /// <summary>
    /// 根据档位缩放物体（倍数缩放）
    /// </summary>
    public void ApplySizeTier(int tier)
    {
        if (tier < 1) tier = 1;
        if (tier > sizeTiers) tier = sizeTiers;

        currentSizeTier = tier;
        transform.localScale = _baseScale * tier;
    }

    public int GetSizeTiers() => sizeTiers;
}

