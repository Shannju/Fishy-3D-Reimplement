using UnityEngine;

public abstract class LivingEntity : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private SpeciesId Species;

    [Header("Size Tiers")]
    [Tooltip("总档位数（例如：3表示有3个档位）")]
    [SerializeField] private int sizeTiers = 3;

    [Tooltip("当前档位（例如：3表示放大3倍，在Start时应用）")]
    [SerializeField] private int currentSizeTier = 1;

    public int CurrentSizeTier => currentSizeTier;

    public bool IsAlive { get; private set; } = true;

    private Vector3 _baseScale;

    protected virtual void Awake()
    {
        // 记录基础缩放（在应用档位缩放之前）
        _baseScale = transform.localScale;
    }

    protected virtual void Start()
    {
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

    /// <summary>
    /// Semantic hook:
    /// Called when this entity is eaten by another entity.
    /// Caller must not care about concrete type.
    /// </summary>
    public virtual void OnEaten(LivingEntity eater)
    {
    }

    /// <summary>
    /// Semantic hook:
    /// Default death behaviour.
    /// Subclasses may override for VFX / animation / state change.
    /// </summary>
    public virtual void Die()
    {
        if (!IsAlive) return;

        IsAlive = false;
        Destroy(gameObject);
    }
}
