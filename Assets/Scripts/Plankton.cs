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
    [SerializeField] private float _separationDistance = 0.3f;
    [SerializeField] private float _separationWeight = 2.0f;
    [SerializeField] private float _alignmentWeight = 1.0f;
    [SerializeField] private float _cohesionWeight = 0.8f;
    [SerializeField] private float _fleeDistance = 6f;

    // --- 内部变量 ---
    private Vector3 _currentVelocity;
    private Vector3 _smoothedVelocity;
    private Vector3 _velocityRef;
    private Transform _threatTarget;
    private PlanktonState _currentState = PlanktonState.Gathering;

    // 关键优化：使用静态列表代替每帧的 OverlapSphere
    private static List<Plankton> _allPlanktons = new List<Plankton>();

    protected override void Awake()
    {
        base.Awake();
        // 如果有Rigidbody，设置为Kinematic以防干扰手动位移
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 初始化材质颜色
        Renderer meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
            meshRenderer.material.color = (color == PlanktonColor.Blue) ? UnityEngine.Color.blue : new UnityEngine.Color(0.5f, 0f, 0.5f);
    }

    protected override void Start()
    {
        base.Start();
        _allPlanktons.Add(this);
        _currentVelocity = transform.forward * _normalSpeed;
        _smoothedVelocity = _currentVelocity;
    }

    private void OnDestroy()
    {
        _allPlanktons.Remove(this);
    }

    private void Update()
    {
        if (!IsAlive) return;

        CheckForThreats();

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
        _currentVelocity = targetVelocity * _normalSpeed;
    }

    private void UpdateFleeing()
    {
        if (_threatTarget == null) return;
        Vector3 fleeDir = (transform.position - _threatTarget.position).normalized;
        fleeDir.y = 0;
        _currentVelocity = fleeDir * _fleeSpeed;
    }

    // --- 核心逻辑 3：丝滑移动 (抄袭重点) ---
    private void ApplyMovement()
    {
        // 1. 速度平滑 (解决抖动问题的关键)
        _smoothedVelocity = Vector3.SmoothDamp(
            _smoothedVelocity, 
            _currentVelocity, 
            ref _velocityRef, 
            _smoothTime
        );

        // 2. 坐标位移
        transform.position += _smoothedVelocity * Time.deltaTime;

        // 3. 根据状态调整转向速度
        if (_smoothedVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_smoothedVelocity);
            // 根据状态调整转向速度：gathering时很慢，fleeing时快
            float turnSpeed = (_currentState == PlanktonState.Fleeing) ? _turnSpeed * 2f : _turnSpeed * 0.2f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                turnSpeed * Time.deltaTime
            );
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
                Debug.Log($"[{gameObject.name}] 检测到FishBase威胁: {predator.GetType().Name}, 距离: {Vector3.Distance(transform.position, potentialThreat.position):F2}");
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
            Debug.Log($"[{gameObject.name}] 找到最近威胁: {nearest.name}, 距离: {nearestDistance:F2}");
        }

        return nearest;
    }
}
