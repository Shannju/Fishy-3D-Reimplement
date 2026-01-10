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
    private bool _isFeeding = true;
    private bool _isEating = false;

    protected override void Awake()
    {
        base.Awake();
        SetSpecies(SpeciesId.VegFish);
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




    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
}