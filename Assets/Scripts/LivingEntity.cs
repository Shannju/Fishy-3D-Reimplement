using UnityEngine;

public abstract class LivingEntity : MonoBehaviour
{
    [Header("Identity")]
    public SpeciesId Species;

    public bool IsAlive { get; private set; } = true;

    /// <summary>
    /// Semantic hook:
    /// Called when this entity is eaten by another entity.
    /// Caller must not care about concrete type.
    /// </summary>
    public virtual void OnEaten(LivingEntity eater)
    {
        Die();
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
