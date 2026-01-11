using UnityEngine;

public abstract class LivingEntity : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private SpeciesId Species;

    [Header("Body Collider")]
    [Tooltip("用于被吃检测的碰撞体")]
    [SerializeField] public Collider bodyCollider;

    [Header("Obstacle Avoidance")]
    [SerializeField] protected bool enableObstacleAvoidance = false;
    [SerializeField] protected LayerMask obstacleLayer = -1; // 检测所有层
    [SerializeField] protected float lookAheadDistance = 1.5f; // 前方检测距离（稍微缩短）
    [SerializeField] protected int rayCount = 5; // 检测射线数量
    [SerializeField] protected float rayAngle = 45f; // 射线角度范围（稍微缩小）
    [SerializeField] protected float avoidanceForce = 6f; // 避障力强度（降低，避免过度转向）

    protected SpeciesId SpeciesValue => Species; // 受保护的getter
    protected void SetSpecies(SpeciesId species) => Species = species; // 受保护的setter

    public bool IsAlive { get; private set; } = true;

    protected virtual void Awake()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider>();
        }
    }

    protected virtual void Start()
    {
    }

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

    /// <summary>
    /// 计算障碍物避障方向
    /// 返回避障力向量，如果没有检测到障碍物则返回Vector3.zero
    /// </summary>
    protected Vector3 CalculateObstacleAvoidance()
    {
        if (!enableObstacleAvoidance) return Vector3.zero;

        float minDistance = float.MaxValue;
        Vector3 bestDirection = transform.forward;

        // 发射多条射线检测前方障碍物
        for (int i = 0; i < rayCount; i++)
        {
            // 计算射线角度（从-角度范围到+角度范围）
            float angle = rayCount > 1 ? (i / (float)(rayCount - 1) - 0.5f) * rayAngle : 0f;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, lookAheadDistance, obstacleLayer))
            {
                float distance = hit.distance;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    // 计算避开方向：反射方向，只保留Y轴旋转
                    Vector3 reflected = Vector3.Reflect(direction, hit.normal);
                    reflected.y = 0; // 强制XZ平面，只转Y轴
                    bestDirection = reflected.normalized;
                }
            }
        }

        if (minDistance < lookAheadDistance)
        {
            // 返回避障方向，距离越近力越大
            float forceMultiplier = 1f - (minDistance / lookAheadDistance);
            return bestDirection * avoidanceForce * forceMultiplier;
        }

        return Vector3.zero;
    }
}
