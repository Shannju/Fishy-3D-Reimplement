using UnityEngine;
using System.Collections.Generic;

public enum PlanktonColor
{
    Blue,
    Purple,
}

public enum PlanktonState
{
    Gathering,  // 聚集状态
    Fleeing     // 逃窜状态
}

public class Plankton : LivingEntity, IEatable
{
    // --- IEatable 接口实现 ---
    private const int totalUnits = 1;
    private int _unitsRemaining = 1;

    public int UnitsRemaining => _unitsRemaining;
    public bool IsDepleted => _unitsRemaining <= 0;

    // --- 原有属性保留 ---
    [Header("Color")]
    [SerializeField] private PlanktonColor color = PlanktonColor.Blue;
    public PlanktonColor Color => color;

    // --- 抄自 PreyFish 的核心参数 ---
    [Header("Movement (Copied from PreyFish)")]
    [SerializeField] private float _normalSpeed = 1.5f;
    [SerializeField] private float _fleeSpeed = 3f;
    [SerializeField] private float _turnSpeed = 120f;
    [SerializeField] private float _smoothTime = 0.5f; // 速度平滑时间

    [Header("Flocking (Copied from PreyFish)")]
    [SerializeField] private float _neighborRadius = 10f;
    [SerializeField] private float _separationDistance = 0.2f;
    [SerializeField] private float _separationWeight = 1.0f;
    [SerializeField] private float _alignmentWeight = 1.0f;
    [SerializeField] private float _cohesionWeight = 1.5f;
    [SerializeField] private float _fleeDistance = 6f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask _obstacleLayer = -1; // 检测所有层
    [SerializeField] private float _lookAheadDistance = 2f; // 前方检测距离
    [SerializeField] private int _rayCount = 5; // 检测射线数量
    [SerializeField] private float _rayAngle = 60f; // 射线角度范围
    [SerializeField] private float _avoidanceForce = 10f; // 避障力强度

    [Header("Stuck Detection")]
    [SerializeField] private float _stuckCheckInterval = 3f; // 卡住检测间隔
    [SerializeField] private float _stuckThreshold = 0.5f; // 卡住判断的最小移动距离
    [SerializeField] private float _stuckTurnAngle = 120f; // 卡住时转向角度

    // --- 内部变量 ---
    private Vector3 _currentVelocity;
    private Vector3 _smoothedVelocity;
    private Vector3 _velocityRef;
    private Transform _threatTarget;
    private PlanktonState _currentState = PlanktonState.Gathering;

    // 卡住检测
    private Vector3 _lastPosition;
    private float _lastStuckCheckTime;

    // 关键优化：使用静态列表代替每帧的 OverlapSphere
    private static List<Plankton> _allPlanktons = new List<Plankton>();

    protected override void Awake()
    {
        base.Awake();
        // 配置Rigidbody用于完全的物理移动和转向
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false; // 禁用重力
            rb.linearDamping = 0.1f; // 很小的线性阻尼，让运动更平滑
            rb.angularDamping = 3f; // 降低角度阻尼，允许更快转向
            // 保持非Kinematic状态以支持物理力
        }

        // 初始化材质颜色和 shader
        Renderer meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
        {
            meshRenderer.material.color = (color == PlanktonColor.Blue) ? UnityEngine.Color.blue : new UnityEngine.Color(0.5f, 0f, 0.5f);
            meshRenderer.material.shader = Shader.Find("Custom/PlanktonSwim");
        }
    }

    protected override void Start()
    {
        base.Start();
        _allPlanktons.Add(this);
        _currentVelocity = transform.forward * _normalSpeed;
        _smoothedVelocity = _currentVelocity;

        // 初始化卡住检测
        _lastPosition = transform.position;
        _lastStuckCheckTime = Time.time;
    }

    private void OnDestroy()
    {
        _allPlanktons.Remove(this);
    }

    private void Update()
    {
        if (!IsAlive) return;

        CheckForThreats();
        CheckIfStuck();

        if (_currentState == PlanktonState.Fleeing)
            UpdateFleeing();
        else
            UpdateSchooling();

        ApplyMovement();
        }

    // --- 核心逻辑 1：威胁检测 (改为类似 PreyFish 的逻辑) ---
    private void CheckForThreats()
    {
        // 简单处理：你可以从外部传递威胁，或者这里保留你的 FindNearestPredator
        // 这里的逻辑参考 PreyFish，根据距离切换状态
        _threatTarget = FindNearestPredatorTransform(); // 沿用你原来的检测逻辑

        if (_threatTarget != null && Vector3.Distance(transform.position, _threatTarget.position) < _fleeDistance)
            _currentState = PlanktonState.Fleeing;
        else
            _currentState = PlanktonState.Gathering;
        }

    // --- 核心逻辑 2：三大定律 (使用静态列表，大幅提升性能) ---
    private void UpdateSchooling()
        {
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 avgPos = Vector3.zero;
        int count = 0;

        foreach (Plankton other in _allPlanktons)
        {
            if (other == this || other.Color != this.color || !other.IsAlive) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < _neighborRadius)
            {
                // 分离 - 在整个邻域范围内都起作用，避免突然推拉
                Vector3 diff = transform.position - other.transform.position;
                // 距离越近，分离力越强；距离越远，分离力越弱
                float separationStrength = Mathf.Max(0, 1f - (dist / _neighborRadius));
                separation += diff.normalized * separationStrength;

                // 对齐
                alignment += other._smoothedVelocity.normalized;
                // 内聚
                avgPos += other.transform.position;
                count++;
    }
        }

        Vector3 targetVelocity = transform.forward; // 默认向前
        if (count > 0)
        {
            alignment /= count;
            avgPos /= count;
            cohesion = (avgPos - transform.position).normalized;

            targetVelocity = (separation * _separationWeight + 
                             alignment * _alignmentWeight + 
                             cohesion * _cohesionWeight).normalized;
        }

        targetVelocity.y = 0; // 强制 XZ 平面

        // 障碍物避障
        Vector3 avoidanceVelocity = CalculateObstacleAvoidance();
        if (avoidanceVelocity != Vector3.zero)
    {
            // 检测到障碍物时，大角度转向避开
            targetVelocity = avoidanceVelocity.normalized;
        }

        _currentVelocity = targetVelocity * _normalSpeed;
    }

    private void UpdateFleeing()
    {
        if (_threatTarget == null) return;
        Vector3 fleeDir = (transform.position - _threatTarget.position).normalized;
        fleeDir.y = 0;
        _currentVelocity = fleeDir * _fleeSpeed;
                }

    // 障碍物避障
    private Vector3 CalculateObstacleAvoidance()
    {
        float minDistance = float.MaxValue;
        Vector3 bestDirection = transform.forward;

        // 发射多条射线检测前方障碍物
        for (int i = 0; i < _rayCount; i++)
        {
            // 计算射线角度（从-角度范围到+角度范围）
            float angle = _rayCount > 1 ? (i / (float)(_rayCount - 1) - 0.5f) * _rayAngle : 0f;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, _lookAheadDistance, _obstacleLayer))
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

        if (minDistance < _lookAheadDistance)
        {
            // 返回避障方向，距离越近力越大
            float forceMultiplier = 1f - (minDistance / _lookAheadDistance);
            return bestDirection * _avoidanceForce * forceMultiplier;
        }

        return Vector3.zero;
    }

    // --- 核心逻辑 3：完全基于Rigidbody的物理移动 ---
    private void ApplyMovement()
    {
        // 1. 速度平滑 (解决抖动问题的关键)
        _smoothedVelocity = Vector3.SmoothDamp(
            _smoothedVelocity,
            _currentVelocity,
            ref _velocityRef,
            _smoothTime
        );

        // 2. 使用Rigidbody进行完全的物理移动和转向
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 2.1 移动力：计算需要的力来达到目标速度
            Vector3 velocityDifference = _smoothedVelocity - rb.linearVelocity;
            Vector3 force = velocityDifference * 8f; // 增加力乘数以获得更好的响应

            // 限制最大力，防止突然加速
            if (force.magnitude > 15f)
            {
                force = force.normalized * 15f;
            }

            rb.AddForce(force, ForceMode.Force);

            // 2.2 转向力：使用扭矩进行物理转向（只转Y轴）
            if (_smoothedVelocity.sqrMagnitude > 0.01f)
            {
                // 计算目标Y轴角度
                float targetAngle = Mathf.Atan2(_smoothedVelocity.x, _smoothedVelocity.z) * Mathf.Rad2Deg;
                float currentAngle = rb.rotation.eulerAngles.y;

                // 计算角度差（考虑角度的周期性）
                float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

                // 根据状态调整转向速度：gathering时很慢，fleeing时快
                float turnSpeed = (_currentState == PlanktonState.Fleeing) ? _turnSpeed * 3f : _turnSpeed * 0.3f;

                // 限制最大角度差对应的扭矩
                float maxTorque = turnSpeed;
                float torque = Mathf.Clamp(angleDifference, -maxTorque, maxTorque);
                
                // 应用Y轴扭矩
                rb.AddTorque(Vector3.up * torque, ForceMode.Force);
            }
        }
        else
        {
            // 备用方案：如果没有Rigidbody，使用传统方式
            transform.position += _smoothedVelocity * Time.deltaTime;

            // 备用转向
            if (_smoothedVelocity.sqrMagnitude > 0.01f)
            {
                float targetAngle = Mathf.Atan2(_smoothedVelocity.x, _smoothedVelocity.z) * Mathf.Rad2Deg;
                float currentAngle = transform.eulerAngles.y;
                float turnSpeed = (_currentState == PlanktonState.Fleeing) ? _turnSpeed * 2f : _turnSpeed * 0.2f;
                float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
                transform.eulerAngles = new Vector3(0, newAngle, 0);
            }
        }
    }

    // --- 捕食扩散逻辑 (抄自 PreyFish) ---
    public bool ConsumeOneUnit()
    {
        if (IsDepleted) return false;

        _unitsRemaining = 0;
        NotifyNearbyPlanktons(this.transform); // 被吃时惊动队友
        Die();
        return true;
    }

    private void NotifyNearbyPlanktons(Transform threat)
    {
        foreach (Plankton other in _allPlanktons)
        {
            if (other == this || !other.IsAlive) continue;
            if (Vector3.Distance(transform.position, other.transform.position) < _neighborRadius * 1.5f)
            {
                other._threatTarget = threat;
                other._currentState = PlanktonState.Fleeing;
            }
        }
    }

    // 辅助：检测附近的捕食者
    private Transform FindNearestPredatorTransform()
    {
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        // 检测附近的威胁（鱼类或玩家）
        Collider[] nearby = Physics.OverlapSphere(transform.position, _fleeDistance);
        foreach (var col in nearby)
        {
            Transform potentialThreat = null;

            // 检测FishBase类型的威胁（包括父对象）
            FishBase predator = col.GetComponent<FishBase>();
            if (predator == null)
            {
                // 如果当前对象没有，检查父对象
                predator = col.GetComponentInParent<FishBase>();
            }
            if (predator != null && predator.IsAlive)
            {
                potentialThreat = predator.transform;
                // 调试：检测到FishBase威胁
                // Debug.Log($"[{gameObject.name}] 检测到FishBase威胁: {predator.GetType().Name}, 距离: {Vector3.Distance(transform.position, potentialThreat.position):F2}");
            }
            if (potentialThreat != null)
            {
                float distance = Vector3.Distance(transform.position, potentialThreat.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = potentialThreat;
                }
            }
        }

        // 调试：最终检测结果
        if (nearest != null)
        {
            // Debug.Log($"[{gameObject.name}] 找到最近威胁: {nearest.name}, 距离: {nearestDistance:F2}");
        }

        return nearest;
    }

    // 调试：绘制障碍物检测射线
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = UnityEngine.Color.yellow;
        for (int i = 0; i < _rayCount; i++)
        {
            float angle = _rayCount > 1 ? (i / (float)(_rayCount - 1) - 0.5f) * _rayAngle : 0f;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * transform.forward;
            Gizmos.DrawRay(transform.position, direction * _lookAheadDistance);
        }
    }

    // 卡住检测和处理
    private void CheckIfStuck()
    {
        if (Time.time - _lastStuckCheckTime >= _stuckCheckInterval)
        {
            float distanceMoved = Vector3.Distance(transform.position, _lastPosition);

            if (distanceMoved < _stuckThreshold)
            {
                // 卡住了，使用物理方式强制转向
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 应用大的Y轴扭矩进行随机转向
                    float randomTorque = Random.Range(-_stuckTurnAngle * 2f, _stuckTurnAngle * 2f);
                    rb.AddTorque(Vector3.up * randomTorque, ForceMode.Impulse);

                    // 给一个向前的冲量，帮助摆脱卡住状态
                    rb.AddForce(transform.forward * _normalSpeed * 3f, ForceMode.Impulse);
                }

                // Debug.Log($"[{gameObject.name}] 检测到卡住，应用物理冲量转向");
            }

            _lastPosition = transform.position;
            _lastStuckCheckTime = Time.time;
        }
    }
}
