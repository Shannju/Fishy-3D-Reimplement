using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VegFish : AiFish
{
    [Header("Feeding Settings")]
    [SerializeField] private LayerMask _algaeLayer = -1;

    [Header("Debug")]
    [SerializeField] private bool _debugAlwaysFeeding = false;

    private float _nextFeedTime;
    private Transform _targetAlgae;
    private bool _isFeeding = false; // 默认不处于觅食状态，让它主动寻找
    private bool _isEating = false;

    protected override void Awake()
    {
        base.Awake();
        SetSpecies(SpeciesId.VegFish);
        enableObstacleAvoidance = true; // 启用障碍物避障
    }

    private void Start()
    {
        _nextTurnTime = Time.time + Random.Range(1f, _straightMoveTime);
    }

    protected override void Update()
    {
        CheckAndEatAlgae();
        DetectAlgae();

        if (_isFeeding && _targetAlgae != null)
        {
            MoveTowardsTarget(_targetAlgae);
            // Don't call base.Update() to avoid wandering when feeding
            ApplyMovement();
        }
        else
        {
            base.Update(); // Call base Update for wandering behavior
        }
    }

    /// <summary>
    /// VegFish 只对 Algae 感兴趣
    /// </summary>
    protected override void OnTriggerStay(Collider other)
    {
        // 只对海藻做出反应
        Algae algae = other.GetComponentInParent<Algae>();
        if (algae != null && algae.IsAlive)
        {
            MoveTowardsTarget(other.transform);
        }
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

        // 更频繁地检查海藻，避免长时间随机游动
        _nextFeedTime = Time.time + Random.Range(0.3f, 1.0f); // 0.3-1.0秒间隔，更频繁检查

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




    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
}