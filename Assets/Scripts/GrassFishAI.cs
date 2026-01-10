using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class GrassFishAI : FishBase
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _turnSpeed = 90f;
    [SerializeField] private float _straightMoveTime = 3f; // 直线移动时间
    [SerializeField] private float _randomTurnAngle = 45f; // 随机转向角度范围

    [Header("Feeding Settings")]
    [SerializeField] private SphereCollider _senseCollider; // 探测藻类的碰撞体
    [SerializeField] private LayerMask _algaeLayer = -1; // 藻类所在层

    [Header("Rigidbody Settings")]
    [SerializeField] private float _linearDamping = 1f;
    [SerializeField] private float _angularDamping = 2f;

    // 内部状态
    private Rigidbody _rb;
    private float _nextTurnTime;
    private float _nextFeedTime;
    private Transform _targetAlgae;
    private bool _isFeeding = false;

    protected override void Awake()
    {
        base.Awake(); // 调用FishBase的Awake，初始化MouthSensor等

        _rb = GetComponent<Rigidbody>();

        // 配置Rigidbody
        _rb.useGravity = false;
        _rb.linearDamping = _linearDamping;
        _rb.angularDamping = _angularDamping;

        // 配置sense碰撞体为触发器
        if (_senseCollider != null)
        {
            _senseCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        _nextTurnTime = Time.time + Random.Range(1f, _straightMoveTime);
    }

    private void Update()
    {
        if (!enabled) return;

        // 探测藻类
        DetectAlgae();

        // 根据状态执行行为
        if (_isFeeding && _targetAlgae != null)
        {
            MoveTowardsAlgae();
        }
        else
        {
            Wander();
        }

        // 应用移动
        ApplyMovement();
    }

    // 探测附近的藻类（使用碰撞体范围）
    private void DetectAlgae()
    {
        if (Time.time < _nextFeedTime) return;

        Transform closestAlgae = null;
        float closestDistance = float.MaxValue;

        // 使用senseCollider的范围检测藻类
        if (_senseCollider != null)
        {
            Vector3 senseCenter = transform.TransformPoint(_senseCollider.center);
            float senseRadius = _senseCollider.radius;

            Collider[] nearby = Physics.OverlapSphere(senseCenter, senseRadius, _algaeLayer);
            foreach (var col in nearby)
            {
                Algae algae = col.GetComponent<Algae>();
                if (algae != null && algae.IsAlive)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestAlgae = col.transform;
                    }
                }
            }
        }

        if (closestAlgae != null)
        {
            _targetAlgae = closestAlgae;
            _isFeeding = true;
        }
        else
        {
            _targetAlgae = null;
            _isFeeding = false;
        }
    }


    // 向藻类移动
    private void MoveTowardsAlgae()
    {
        if (_targetAlgae == null) return;

        Vector3 directionToAlgae = (_targetAlgae.position - transform.position).normalized;
        directionToAlgae.y = 0; // 保持水平

        // 计算目标角度
        float targetAngle = Mathf.Atan2(directionToAlgae.x, directionToAlgae.z) * Mathf.Rad2Deg;
        float currentAngle = _rb.rotation.eulerAngles.y;

        // 转向藻类
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
        float torque = Mathf.Clamp(angleDifference * 2f, -_turnSpeed * 2f, _turnSpeed * 2f);
        _rb.AddTorque(Vector3.up * torque, ForceMode.Force);

        // 向前移动
        _rb.AddForce(transform.forward * _moveSpeed * 1.5f, ForceMode.Force);

        // 当靠近藻类时，尝试吃掉它（通过FishBase的MouthSensor）
        float distanceToAlgae = Vector3.Distance(transform.position, _targetAlgae.position);
        if (distanceToAlgae <= 2f) // 在一定距离内尝试吃
        {
            TryBite();
        }
    }

    // 自由游动
    private void Wander()
    {
        // 直线移动一段时间后随机转向
        if (Time.time >= _nextTurnTime)
        {
            // 随机转向
            float randomAngle = Random.Range(-_randomTurnAngle, _randomTurnAngle);
            _rb.AddTorque(Vector3.up * randomAngle, ForceMode.Impulse);

            // 设置下次转向时间
            _nextTurnTime = Time.time + Random.Range(1f, _straightMoveTime);
        }

        // 向前移动
        _rb.AddForce(transform.forward * _moveSpeed, ForceMode.Force);
    }


    // 重写FishBase的TryBiteOnce方法
    protected override bool TryBiteOnce()
    {
        // 对于AI控制的鱼，进食逻辑由MouthSensor处理
        // 这里只需要确保有目标就可以了
        if (mouth != null && mouth.CurrentTarget != null && !mouth.CurrentTarget.IsDepleted)
        {
            bool success = base.TryBiteOnce(); // 调用基类的进食逻辑

            if (success)
            {
                // 进食成功后的处理
                _targetAlgae = null;
                _isFeeding = false;

                // 短暂停止移动，模拟进食行为
                if (_rb != null)
                {
                    _rb.linearVelocity *= 0.3f;
                }
            }

            return success;
        }

        return false;
    }

    // 应用移动力
    private void ApplyMovement()
    {
        // 限制最大速度
        if (_rb.linearVelocity.magnitude > _moveSpeed * 2f)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * _moveSpeed * 2f;
        }

        // 限制最大角速度
        if (_rb.angularVelocity.magnitude > _turnSpeed * 2f)
        {
            _rb.angularVelocity = _rb.angularVelocity.normalized * _turnSpeed * 2f;
        }
    }

    // 调试：绘制检测范围
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 绘制探测范围（使用senseCollider的半径）
        Gizmos.color = UnityEngine.Color.green;
        if (_senseCollider != null)
        {
            Vector3 senseCenter = transform.TransformPoint(_senseCollider.center);
            Gizmos.DrawWireSphere(senseCenter, _senseCollider.radius);
        }

        // 绘制吃藻类范围（使用MouthSensor的位置）
        Gizmos.color = UnityEngine.Color.red;
        if (mouth != null)
        {
            Gizmos.DrawWireSphere(mouth.transform.position, 1f); // MouthSensor大约1单位范围
        }

        // 绘制移动方向
        Gizmos.color = UnityEngine.Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}