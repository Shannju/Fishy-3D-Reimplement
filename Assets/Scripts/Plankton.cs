using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    [Header("Food")]
    private const int totalUnits = 1;
    private int _unitsRemaining = 1;

    public int UnitsRemaining => _unitsRemaining;
    public bool IsDepleted => _unitsRemaining <= 0;

    [Header("Color")]
    [Tooltip("浮游生物的颜色，相同颜色的会聚在一起")]
    [SerializeField] private PlanktonColor color = PlanktonColor.Blue;
    
    public PlanktonColor Color => color;

    [Header("Movement")]
    [Tooltip("移动速度")]
    [SerializeField] private float moveSpeed = 1.5f;
    [Tooltip("初始速度（刚开始时较慢）")]
    [SerializeField] private float initialSpeed = 0.5f;
    [Tooltip("速度加速时间（秒）")]
    [SerializeField] private float speedUpTime = 3f;
    [Tooltip("转向速度（度/秒）")]
    [SerializeField] private float rotationSpeed = 120f;
    [Tooltip("随机游动速度")]
    [SerializeField] private float wanderSpeed = 0.5f;
    [Tooltip("推动力强度")]
    [SerializeField] private float forceMultiplier = 5f;

    [Header("Behavior")]
    [Tooltip("聚集检测半径")]
    [SerializeField] private float gatheringRadius = 8f;
    [Tooltip("聚集力的强度")]
    [SerializeField] private float gatheringForce = 4f;
    [Tooltip("分离距离（避免太挤）")]
    [SerializeField] private float separationDistance = 1f;
    [Tooltip("分离力的强度")]
    [SerializeField] private float separationForce = 3f;
    [Tooltip("逃窜检测半径")]
    [SerializeField] private float fleeRadius = 8f;
    [Tooltip("逃窜力的强度")]
    [SerializeField] private float fleeForce = 5f;
    [Tooltip("人少区域的检测半径")]
    [SerializeField] private float emptySpaceRadius = 3f;

    [Header("Obstacle Avoidance")]
    [Tooltip("前方检测距离")]
    [SerializeField] private float lookAheadDistance = 2f;
    [Tooltip("检测射线数量")]
    [SerializeField] private int rayCount = 5;
    [Tooltip("射线角度范围（度）")]
    [SerializeField] private float rayAngle = 60f;
    [Tooltip("障碍物检测层")]
    [SerializeField] private LayerMask obstacleLayer = ~0; // 检测所有层

    [Header("Stuck Detection")]
    [Tooltip("卡住检测时间（秒）")]
    [SerializeField] private float stuckCheckInterval = 2f;
    [Tooltip("卡住判断的最小移动距离")]
    [SerializeField] private float stuckThreshold = 0.5f;
    [Tooltip("卡住时转向角度（度）")]
    [SerializeField] private float stuckTurnAngle = 150f;

    private Rigidbody rb;
    private Renderer meshRenderer;
    private PlanktonState currentState = PlanktonState.Gathering;
    private Vector3 lastPosition;
    private float lastStuckCheckTime;
    private Vector3 wanderDirection;
    private float wanderChangeTime;
    private float spawnTime;
    private float currentEffectiveSpeed;

    // 颜色对应的材质颜色
    private static readonly Dictionary<PlanktonColor, UnityEngine.Color> ColorMap = new Dictionary<PlanktonColor, UnityEngine.Color>
    {
        { PlanktonColor.Blue, UnityEngine.Color.blue },
        { PlanktonColor.Purple, new UnityEngine.Color(0.5f, 0f, 0.5f) }
    };

    protected override void Awake()
    {
        base.Awake();
        
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 2f;
            rb.angularDamping = 5f;
        }

        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = ColorMap[color];
        }

        lastPosition = transform.position;
        lastStuckCheckTime = Time.time;
        wanderChangeTime = Time.time;
        spawnTime = Time.time;
        currentEffectiveSpeed = initialSpeed;
        wanderDirection = Random.insideUnitSphere;
        wanderDirection.y = 0; // 保持水平
        wanderDirection.Normalize();
    }

    protected override void Start()
    {
        base.Start();
    }

    private void Update()
    {
        if (!IsAlive || IsDepleted) return;

        UpdateEffectiveSpeed();
        UpdateState();
        CheckIfStuck();
    }

    private void FixedUpdate()
    {
        if (!IsAlive || IsDepleted) return;

        UpdateMovement();
    }

    private void UpdateEffectiveSpeed()
    {
        // 从初始速度慢慢加速到正常速度
        float timeSinceSpawn = Time.time - spawnTime;
        if (timeSinceSpawn < speedUpTime)
        {
            float progress = timeSinceSpawn / speedUpTime;
            currentEffectiveSpeed = Mathf.Lerp(initialSpeed, moveSpeed, progress);
        }
        else
        {
            currentEffectiveSpeed = moveSpeed;
        }
    }

    private void UpdateState()
    {
        // 检查附近是否有捕食者
        FishBase nearestPredator = FindNearestPredator();
        if (nearestPredator != null && Vector3.Distance(transform.position, nearestPredator.transform.position) < fleeRadius)
        {
            currentState = PlanktonState.Fleeing;
        }
        else
        {
            currentState = PlanktonState.Gathering;
        }
    }

    private void UpdateMovement()
    {
        Vector3 steeringForce = Vector3.zero;

        if (currentState == PlanktonState.Fleeing)
        {
            steeringForce += CalculateFleeForce();
        }
        else // Gathering
        {
            steeringForce += CalculateGatheringForce();
            steeringForce += CalculateSeparationForce();
            steeringForce += CalculateEmptySpaceForce();
            steeringForce += CalculateWanderForce();
        }

        // 避障
        Vector3 avoidanceForce = CalculateObstacleAvoidance();
        if (avoidanceForce.magnitude > 0.1f)
        {
            steeringForce = avoidanceForce; // 避障优先
        }

        // 应用转向
        if (steeringForce.magnitude > 0.1f)
        {
            Vector3 targetDirection = steeringForce.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // 使用 Rigidbody 的 AddForce 来驱动运动
        Vector3 moveDirection = transform.forward;
        float speed = currentState == PlanktonState.Fleeing ? currentEffectiveSpeed * 1.5f : currentEffectiveSpeed;
        
        // 计算目标速度
        Vector3 targetVelocity = moveDirection * speed;
        
        // 计算需要的力来达到目标速度
        Vector3 velocityDifference = targetVelocity - rb.linearVelocity;
        Vector3 force = velocityDifference * forceMultiplier;
        
        // 限制力的最大强度，避免突然加速
        if (force.magnitude > forceMultiplier * speed)
        {
            force = force.normalized * forceMultiplier * speed;
        }
        
        rb.AddForce(force, ForceMode.Force);
    }

    private Vector3 CalculateGatheringForce()
    {
        Vector3 force = Vector3.zero;
        Collider[] nearby = Physics.OverlapSphere(transform.position, gatheringRadius);

        int sameColorCount = 0;
        Vector3 centerOfMass = Vector3.zero;

        foreach (var col in nearby)
        {
            Plankton other = col.GetComponent<Plankton>();
            if (other != null && other != this && other.Color == this.color && other.IsAlive)
            {
                centerOfMass += other.transform.position;
                sameColorCount++;
            }
        }

        if (sameColorCount > 0)
        {
            centerOfMass /= sameColorCount;
            Vector3 directionToCenter = (centerOfMass - transform.position);
            float distance = directionToCenter.magnitude;
            
            // 如果距离较远，增加聚集力，避免掉队
            float distanceFactor = Mathf.Clamp01(distance / gatheringRadius);
            float adjustedForce = gatheringForce * (1f + distanceFactor * 2f);
            
            directionToCenter.Normalize();
            force = directionToCenter * adjustedForce;
        }

        return force;
    }

    private Vector3 CalculateSeparationForce()
    {
        Vector3 force = Vector3.zero;
        Collider[] nearby = Physics.OverlapSphere(transform.position, separationDistance);

        foreach (var col in nearby)
        {
            Plankton other = col.GetComponent<Plankton>();
            if (other != null && other != this && other.Color == this.color && other.IsAlive)
            {
                Vector3 directionAway = (transform.position - other.transform.position);
                float distance = directionAway.magnitude;
                if (distance > 0)
                {
                    directionAway.Normalize();
                    force += directionAway / distance * separationForce;
                }
            }
        }

        return force;
    }

    private Vector3 CalculateEmptySpaceForce()
    {
        Vector3 force = Vector3.zero;
        Collider[] nearby = Physics.OverlapSphere(transform.position, emptySpaceRadius);

        int nearbyCount = nearby.Count(col =>
        {
            Plankton p = col.GetComponent<Plankton>();
            return p != null && p != this && p.IsAlive;
        });

        // 如果附近有很多其他浮游生物，往人少的地方游
        if (nearbyCount > 5)
        {
            // 随机选择一个方向，远离人群
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0;
            randomDirection.Normalize();
            force = randomDirection * 1f;
        }

        return force;
    }

    private Vector3 CalculateWanderForce()
    {
        // 定期改变漫游方向
        if (Time.time - wanderChangeTime > Random.Range(2f, 5f))
        {
            wanderDirection = Random.insideUnitSphere;
            wanderDirection.y = 0;
            wanderDirection.Normalize();
            wanderChangeTime = Time.time;
        }

        return wanderDirection * wanderSpeed;
    }

    private Vector3 CalculateFleeForce()
    {
        Vector3 force = Vector3.zero;
        FishBase predator = FindNearestPredator();

        if (predator != null)
        {
            Vector3 directionAway = (transform.position - predator.transform.position).normalized;
            force = directionAway * fleeForce;
        }

        return force;
    }

    private FishBase FindNearestPredator()
    {
        FishBase nearest = null;
        float nearestDistance = float.MaxValue;

        Collider[] nearby = Physics.OverlapSphere(transform.position, fleeRadius);
        foreach (var col in nearby)
        {
            FishBase predator = col.GetComponent<FishBase>();
            if (predator != null && predator.IsAlive)
            {
                float distance = Vector3.Distance(transform.position, predator.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = predator;
                }
            }
        }

        return nearest;
    }

    private Vector3 CalculateObstacleAvoidance()
    {
        Vector3 avoidanceForce = Vector3.zero;
        float minDistance = float.MaxValue;
        Vector3 bestDirection = transform.forward;

        // 发射多条射线检测前方障碍物
        for (int i = 0; i < rayCount; i++)
        {
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
                    // 计算避开方向
                    bestDirection = Vector3.Reflect(direction, hit.normal).normalized;
                }
            }
        }

        if (minDistance < lookAheadDistance)
        {
            avoidanceForce = bestDirection * (1f - minDistance / lookAheadDistance) * fleeForce;
        }

        return avoidanceForce;
    }

    private void CheckIfStuck()
    {
        if (Time.time - lastStuckCheckTime >= stuckCheckInterval)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            
            if (distanceMoved < stuckThreshold)
            {
                // 卡住了，随机转向
                float randomAngle = Random.Range(-stuckTurnAngle, stuckTurnAngle);
                Quaternion turnRotation = Quaternion.Euler(0, randomAngle, 0);
                transform.rotation = turnRotation * transform.rotation;
                
                // 重置漫游方向
                wanderDirection = transform.forward;
                wanderChangeTime = Time.time;
            }

            lastPosition = transform.position;
            lastStuckCheckTime = Time.time;
        }
    }

    // IEatable 接口实现
    public bool ConsumeOneUnit()
    {
        if (IsDepleted) return false;

        _unitsRemaining = 0;
        Die();
        return true;
    }

    public override void Die()
    {
        base.Die();
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制前方检测射线
        Gizmos.color = UnityEngine.Color.white;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = rayCount > 1 ? (i / (float)(rayCount - 1) - 0.5f) * rayAngle : 0f;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * transform.forward;
            Gizmos.DrawRay(transform.position, direction * lookAheadDistance);
        }
    }
}

