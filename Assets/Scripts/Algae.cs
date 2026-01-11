using UnityEngine;
using System.Collections;

public class Algae : SizeableEntity, IEatable
{
    [Header("Food Units")]
    [Tooltip("这坨藻一共有几口（单位口数）")]
    [SerializeField] private int totalUnits = 3;

    [Tooltip("缩放与剩余口数关联（根据剩余口数设置档位）")]
    [SerializeField] private bool scaleWithUnits = true;

    [Header("Visual")]
    [Tooltip("视觉子物体（用于隐藏显示）")]
    [SerializeField] private GameObject visualObject;

    [Header("Regeneration")]    
    [Tooltip("恢复到 totalUnits 所需的时间（秒）")]
    [SerializeField] private float regenerationTime = 5f;

    [Tooltip("恢复更新的频率（每秒更新次数）")]
    [SerializeField] private float regenerationUpdateRate = 10f;

    private int _unitsRemaining;
    private Coroutine _regenerationCoroutine;

    public int UnitsRemaining => _unitsRemaining;
    public bool IsDepleted => false; // 藻可以被无限吃，不会被耗尽

    protected override void Awake()
    {
        base.Awake();
        _unitsRemaining = Mathf.Max(0, totalUnits);

        // 如果没有手动指定visualObject，尝试自动查找
        if (visualObject == null)
        {
            // 查找名为"Visual"的子物体
            Transform visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                visualObject = visualTransform.gameObject;
            }
            else
            {
                // 如果找不到Visual子物体，使用自身作为视觉对象
                visualObject = gameObject;
            }
        }
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
        base.Start(); // 先让 SizeableEntity 应用初始档位缩放
        UpdateVisual();
        
        // 如果初始状态不是满的，开始恢复
        if (_unitsRemaining < totalUnits)
        {
            StartRegeneration();
        }
    }

    public bool ConsumeOneUnit()
    {
        if (_unitsRemaining <= 0) return false;

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

            // 检查是否需要更新（目标单位数增加，或者从<=0恢复到>0）
            bool shouldUpdate = targetUnits > _unitsRemaining ||
                               (_unitsRemaining <= 0 && targetUnits > 0);

            if (shouldUpdate)
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
        // 处理视觉子物体的显示/隐藏
        if (visualObject != null)
        {
            visualObject.SetActive(_unitsRemaining > 0);
        }

        if (!scaleWithUnits || totalUnits <= 0 || _unitsRemaining <= 0)
        {
            // 如果不缩放、没有单位或已被吃光，保持当前档位或不应用缩放
            return;
        }

        // 根据剩余口数计算档位（剩余口数就是档位）
        int targetTier = _unitsRemaining;
        ApplySizeTier(targetTier);
    }
}
