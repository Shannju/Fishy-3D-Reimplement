using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class AiFish : FishBase
{
    [Header("Movement Settings")]
    [SerializeField]
    protected float _moveSpeed = 3f; // 稍微加快移动速度

    [SerializeField]
    protected float _turnSpeed = 10f   ;//微加快转向速度

    [SerializeField]
    protected float _straightMoveTime = 3f;

    [SerializeField]
    protected float _randomTurnAngle = 45f;

    [Header("Sensor Settings")]
    [SerializeField]
    protected SphereCollider _senseCollider;

    [Header("Debug")]
    [SerializeField]
    protected bool _debugAlwaysActive = false;

    protected Rigidbody _rb;
    protected float _nextTurnTime;

    // 调试相关变量
    protected Vector3 _currentTargetPosition = Vector3.zero;
    protected bool _hasTarget = false;

    protected override void Awake()
    {
        base.Awake();

        _rb = GetComponent<Rigidbody>();

        if (_senseCollider != null)
        {
            _senseCollider.isTrigger = true;
        }
    }

    protected virtual void Start()
    {
        base.Start();

        // 设置物理阻尼，AI鱼需要更高的角阻尼来减少抖动
        _rb.linearDamping = 0.5f; // 很小的线性阻尼，让运动更平滑
        _rb.angularDamping = 200f;// 更高的角阻尼，减少AI鱼的转向抖动

        _nextTurnTime = Time.time + Random.Range(1f, _straightMoveTime);
    }

    protected virtual void Update()
    {
        Wander();
        ApplyMovement();
    }

    protected void Wander()
    {
        // 障碍物避障
        Vector3 avoidanceVelocity = CalculateObstacleAvoidance();
        if (avoidanceVelocity != Vector3.zero)
        {
            // 检测到障碍物时，优先避障
            // 添加一些随机性，避免所有鱼都往同一个方向避障
            Vector3 randomizedDirection = avoidanceVelocity.normalized;
            float randomAngle = Random.Range(-15f, 15f); // 添加±15度的随机偏移
            randomizedDirection = Quaternion.Euler(0, randomAngle, 0) * randomizedDirection;

            TurnTowardsDirection(randomizedDirection);
            _rb.AddForce(transform.forward * _moveSpeed, ForceMode.Force);
            return;
        }

        if (Time.time >= _nextTurnTime)
        {
            // 随机转向：添加随机角度偏移到当前角度
            float currentAngle = transform.eulerAngles.y;
            float randomAngleOffset = Random.Range(-_randomTurnAngle, _randomTurnAngle);
            float targetAngle = currentAngle + randomAngleOffset;

            // 使用统一的转向方法，避免抖动
            TurnTowardsAngle(targetAngle);

            _nextTurnTime = Time.time + Random.Range(0.5f, _straightMoveTime * 0.6f); // 更频繁地转向
        }

        _rb.AddForce(transform.forward * _moveSpeed, ForceMode.Force);
    }

    protected virtual void ApplyMovement()
    {
        // 子类可以重写此方法来添加自定义的移动逻辑
        // 现在不再通过代码调节Rigidbody属性
    }

    /// <summary>
    /// 统一的物理转向方法，直接设置rotation避免抖动
    /// </summary>
    /// <param name="targetAngle">目标角度（度）</param>
    /// <param name="turnSpeedMultiplier">转向速度倍数，默认1.0f</param>
    protected void TurnTowardsAngle(float targetAngle, float turnSpeedMultiplier = 1.0f)
    {
        float currentAngle = _rb.rotation.eulerAngles.y;
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

        // 直接计算新角度，避免AddTorque导致的抖动
        float newAngle = currentAngle + Mathf.Clamp(
            angleDifference,
            -_turnSpeed * Time.fixedDeltaTime * turnSpeedMultiplier,
            _turnSpeed * Time.fixedDeltaTime * turnSpeedMultiplier);

        // 直接设置rotation，更加稳定
        transform.rotation = Quaternion.Euler(0f, newAngle, 0f);
    }

    /// <summary>
    /// 向指定方向转向
    /// </summary>
    /// <param name="direction">目标方向（XZ平面）</param>
    /// <param name="turnSpeedMultiplier">转向速度倍数，默认1.0f</param>
    protected void TurnTowardsDirection(Vector3 direction, float turnSpeedMultiplier = 1.0f)
    {
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            TurnTowardsAngle(targetAngle, turnSpeedMultiplier);
        }
    }

    protected void MoveTowardsTarget(Transform target)
    {
        if (target == null)
        {
            _hasTarget = false;
            return;
        }

        _currentTargetPosition = target.position;
        _hasTarget = true;

        Vector3 directionToTarget = (target.position - transform.position);
        float distance = directionToTarget.magnitude;

        if (distance > 0.1f)
        {
            // 使用统一的转向方法
            TurnTowardsDirection(directionToTarget, 2.0f);
            _rb.AddForce(transform.forward * _moveSpeed * 1.5f, ForceMode.Force);

            // 限制速度，防止过度加速
            _rb.linearVelocity = Vector3.ClampMagnitude(_rb.linearVelocity, _moveSpeed * 2f);
        }
        else
        {
            _rb.linearVelocity *= 0.5f; // 减速
            _hasTarget = false;
        }
    }

    /// <summary>
    /// 增强感知系统：检测物体进入感知区域
    /// </summary>
    protected virtual void OnTriggerEnter(Collider other)
    {
        // 子类可以重写此方法来实现具体的感知行为
        // 例如：检测食物、捕食者或其他感兴趣的目标

        // 默认情况下，AI鱼类不主动追逐其他IEatable对象
        // 由子类（如VegFish、MeatFish）决定追逐什么
    }

    /// <summary>
    /// 持续检测物体在感知区域内
    /// </summary>
    protected virtual void OnTriggerStay(Collider other)
    {
        // 子类可以重写此方法来实现持续的感知行为

        // 默认情况下，AI鱼类不主动追逐其他IEatable对象
        // 由子类决定追逐什么
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.green;
        if (_senseCollider != null)
        {
            Vector3 senseCenter = transform.TransformPoint(_senseCollider.center);
            Gizmos.DrawWireSphere(senseCenter, _senseCollider.radius);
        }

        Gizmos.color = Color.red;
        if (mouth != null)
        {
            Gizmos.DrawWireSphere(mouth.transform.position, 1f);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // 绘制当前速度向量
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, _rb.linearVelocity.normalized * 2f);

        // 绘制当前目标位置
        if (_hasTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_currentTargetPosition, 0.5f);
            Gizmos.DrawLine(transform.position, _currentTargetPosition);
        }

        // 绘制转向角度指示器
        Gizmos.color = Color.cyan;
        Vector3 turnIndicator =
            Quaternion.Euler(0, _rb.rotation.eulerAngles.y, 0) * Vector3.forward;
        Gizmos.DrawRay(transform.position, turnIndicator * 1.5f);
    }
}
