using UnityEngine;

public abstract class LivingEntity : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private SpeciesId Species;

    [Header("Body Collider")]
    [Tooltip("用于被吃检测的碰撞体")]
    [SerializeField] protected Collider bodyCollider;

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
}
