using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class GrassFish : FishBase
{    
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _turnSpeed = 90f;
    [SerializeField] private float _straightMoveTime = 3f;
    [SerializeField] private float _randomTurnAngle = 45f;

    [Header("Feeding Settings")]
    [SerializeField] private SphereCollider _senseCollider;
    [SerializeField] private LayerMask _algaeLayer = -1;

    [Header("Debug")]
    [SerializeField] private bool _debugAlwaysFeeding = false; // 调试：强制保持feeding状态

    [Header("Rigidbody Settings")]
    [SerializeField] private float _linearDamping = 1f;
    [SerializeField] private float _angularDamping = 2f;

    private Rigidbody _rb;
    private float _nextTurnTime;
    private float _nextFeedTime;
    private Transform _targetAlgae;
    private bool _isFeeding = true;
    private bool _isEating = false;

    protected override void Awake()
    {
        base.Awake();

        SetSpecies(SpeciesId.VegFish);

        _rb = GetComponent<Rigidbody>();

        _rb.useGravity = false;
        _rb.linearDamping = _linearDamping;
        _rb.angularDamping = _angularDamping;

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
        CheckAndEatAlgae();

        DetectAlgae();

        if (_isFeeding && _targetAlgae != null)
        {
            MoveTowardsAlgae();
        }
        else
        {
            Wander();
        }

        ApplyMovement();
    }

    private void CheckAndEatAlgae()
    {
        if (_isEating) return;

        if (mouth != null && mouth.CurrentTarget != null)
        {
            var target = mouth.CurrentTarget;
            if (target is Algae)
            {
                _isEating = true;
                OpenMouth();
                StartCoroutine(CloseMouthAfterDelay(0.3f));
            }
        }
    }

    private System.Collections.IEnumerator CloseMouthAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseMouth();
        _isEating = false;
    }

    private void DetectAlgae()
    {
        if (Time.time < _nextFeedTime) return;

        Transform closestAlgae = null;
        float closestDistance = float.MaxValue;

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
            _isFeeding = _debugAlwaysFeeding;
        }
    }


    private void MoveTowardsAlgae()
    {
        if (_targetAlgae == null) return;

        Vector3 directionToAlgae = (_targetAlgae.position - transform.position);
        float distance = directionToAlgae.magnitude;

        if (distance > 0.1f)
        {
            directionToAlgae.y = 0;
            directionToAlgae = directionToAlgae.normalized;

            float targetAngle = Mathf.Atan2(directionToAlgae.x, directionToAlgae.z) * Mathf.Rad2Deg;
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

    private void Wander()
    {
        if (Time.time >= _nextTurnTime)
        {
            float randomAngle = Random.Range(-_randomTurnAngle, _randomTurnAngle);
            _rb.AddTorque(Vector3.up * randomAngle, ForceMode.Impulse);

            _nextTurnTime = Time.time + Random.Range(1f, _straightMoveTime);
        }

        _rb.AddForce(transform.forward * _moveSpeed, ForceMode.Force);
    }

    private void ApplyMovement()
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

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = UnityEngine.Color.green;
        if (_senseCollider != null)
        {
            Vector3 senseCenter = transform.TransformPoint(_senseCollider.center);
            Gizmos.DrawWireSphere(senseCenter, _senseCollider.radius);
        }

        Gizmos.color = UnityEngine.Color.red;
        if (mouth != null)
        {
            Gizmos.DrawWireSphere(mouth.transform.position, 1f);
        }

        Gizmos.color = UnityEngine.Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}