using UnityEngine;

public class Algae : LivingEntity, IEatable
{
    [Header("Food Units")]
    [Tooltip("这坨藻一共有几口（单位口数）")]
    [SerializeField] private int totalUnits = 3;

    [Tooltip("缩放与剩余口数关联（根据剩余口数设置档位）")]
    [SerializeField] private bool scaleWithUnits = true;

    private int _unitsRemaining;

    public int UnitsRemaining => _unitsRemaining;
    public bool IsDepleted => _unitsRemaining <= 0;

    protected override void Awake()
    {
        base.Awake();
        _unitsRemaining = Mathf.Max(0, totalUnits);
    }

    protected override void Start()
    {
        base.Start(); // 先让 LivingEntity 应用初始档位缩放
        UpdateVisual();
    }

    public bool ConsumeOneUnit()
    {
        if (IsDepleted) return false;

        _unitsRemaining -= 1;
        UpdateVisual();

        if (IsDepleted)
        {
            Die();
        }

        return true;
    }

    private void UpdateVisual()
    {
        if (!scaleWithUnits || totalUnits <= 0)
        {
            // 如果不缩放或没有单位，保持当前档位
            return;
        }

        // 根据剩余口数计算档位（剩余口数就是档位）
        int targetTier = Mathf.Max(1, _unitsRemaining);
        ApplySizeTier(targetTier);
    }
}
