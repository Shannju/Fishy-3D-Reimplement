using UnityEngine;
using System.Collections;

public class Algae : LivingEntity, IEatable
{
    [Header("Food Units")]
    [Tooltip("这坨藻一共有几口（单位口数）")]
    [SerializeField] private int totalUnits = 3;

    [Tooltip("缩放与剩余口数关联（根据剩余口数设置档位）")]
    [SerializeField] private bool scaleWithUnits = true;

    [Header("Regeneration")]
    [Tooltip("恢复到 totalUnits 所需的时间（秒）")]
    [SerializeField] private float regenerationTime = 5f;

    [Tooltip("恢复更新的频率（每秒更新次数）")]
    [SerializeField] private float regenerationUpdateRate = 10f;

    private int _unitsRemaining;
    private Coroutine _regenerationCoroutine;

    public int UnitsRemaining => _unitsRemaining;
    public bool IsDepleted => _unitsRemaining <= 0;

    protected override void Awake()
    {
        base.Awake();
        _unitsRemaining = Mathf.Max(0, totalUnits);
    }

    private void OnDestroy()
    {
        // 清理协程
        if (_regenerationCoroutine != null)
        {
            StopCoroutine(_regenerationCoroutine);
            _regenerationCoroutine = null;
        }
    }

    protected override void Start()
    {
        base.Start(); // 先让 LivingEntity 应用初始档位缩放
        UpdateVisual();
        
        // 如果初始状态不是满的，开始恢复
        if (_unitsRemaining < totalUnits)
        {
            StartRegeneration();
        }
    }

    public bool ConsumeOneUnit()
    {
        if (IsDepleted) return false;

        _unitsRemaining -= 1;
        UpdateVisual();

        // 启动或重启恢复协程
        StartRegeneration();

        return true;
    }

    private void StartRegeneration()
    {
        // 如果已经在恢复中，先停止之前的协程
        if (_regenerationCoroutine != null)
        {
            StopCoroutine(_regenerationCoroutine);
        }

        // 如果还没恢复到满值，开始恢复
        if (_unitsRemaining < totalUnits)
        {
            _regenerationCoroutine = StartCoroutine(RegenerationCoroutine());
        }
    }

    private IEnumerator RegenerationCoroutine()
    {
        float updateInterval = 1f / regenerationUpdateRate;
        int startUnits = _unitsRemaining;
        float elapsed = 0f;

        while (_unitsRemaining < totalUnits)
        {
            yield return new WaitForSeconds(updateInterval);
            
            elapsed += updateInterval;
            float progress = Mathf.Clamp01(elapsed / regenerationTime);
            
            // 根据进度计算应该恢复到的单位数
            int targetUnits = Mathf.RoundToInt(Mathf.Lerp(startUnits, totalUnits, progress));
            
            // 只有当目标单位数增加时才更新
            if (targetUnits > _unitsRemaining)
            {
                _unitsRemaining = targetUnits;
                UpdateVisual();
            }

            // 如果已经恢复到满值，退出循环
            if (_unitsRemaining >= totalUnits)
            {
                _unitsRemaining = totalUnits;
                UpdateVisual();
                break;
            }
        }

        _regenerationCoroutine = null;
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
