using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class AiFish : FishBase
{
    [Header("Movement Settings")]
    [SerializeField] protected float _moveSpeed = 2f;
    [SerializeField] protected float _turnSpeed = 90f;
    [SerializeField] protected float _straightMoveTime = 3f;
    [SerializeField] protected float _randomTurnAngle = 45f;

    [Header("Sensor Settings")]
    [SerializeField] protected SphereCollider _senseCollider;

    [Header("Debug")]
    [SerializeField] protected bool _debugAlwaysActive = false;

    [Header("Rigidbody Settings")]
    [SerializeField] protected float _linearDamping = 1f;
    [SerializeField] protected float _angularDamping = 2f;

    protected Rigidbody _rb;
    protected float _nextTurnTime;

    protected override void Awake()
    {
        base.Awake();

        _rb = GetComponent<Rigidbody>();

        _rb.useGravity = false;
        _rb.linearDamping = _linearDamping;
        _rb.angularDamping = _angularDamping;

        if (_senseCollider != null)
        {
            _senseCollider.isTrigger = true;
        }
    }

    protected virtual void Start()
    {
        _nextTurnTime = Time.time + Random.Range(1f, _straightMoveTime);
    }

    protected virtual void Update()
    {
        Wander();
        ApplyMovement();
    }

    protected void Wander()
    {
        if (Time.time >= _nextTurnTime)
        {
            float randomAngle = Random.Range(-_randomTurnAngle, _randomTurnAngle);
            _rb.AddTorque(Vector3.up * randomAngle, ForceMode.Impulse);

            _nextTurnTime = Time.time + Random.Range(1f, _straightMoveTime);
        }

        _rb.AddForce(transform.forward * _moveSpeed, ForceMode.Force);
    }

    protected void ApplyMovement()
    {
        if (_rb.linearVelocity.magnitude > _moveSpeed * 2f)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * _moveSpeed * 2f;
        }

        if (_rb.angularVelocity.magnitude > _turnSpeed * 2f)
        {
            _rb.angularVelocity = _rb.angularVelocity.normalized * _turnSpeed * 2f;
        }
    }

    protected void MoveTowardsTarget(Transform target)
    {
        if (target == null) return;

        Vector3 directionToTarget = (target.position - transform.position);
        float distance = directionToTarget.magnitude;

        if (distance > 0.1f)
        {
            directionToTarget.y = 0;
            directionToTarget = directionToTarget.normalized;

            float targetAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            float currentAngle = _rb.rotation.eulerAngles.y;

            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            float torque = Mathf.Clamp(angleDifference * 2f, -_turnSpeed * 2f, _turnSpeed * 2f);
            _rb.AddTorque(Vector3.up * torque, ForceMode.Force);

            _rb.AddForce(transform.forward * _moveSpeed * 1.5f, ForceMode.Force);
        }
        else
        {
            _rb.linearVelocity *= 0.5f;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

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
    }
}