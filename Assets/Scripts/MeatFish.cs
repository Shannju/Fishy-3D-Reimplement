using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeatFish : AiFish
{
    [Header("Eating Settings")]
    [SerializeField] private float _eatDecisionChance = 0.3f; // 遇到能吃的鱼时决定吃的概率 (0-1)

    private bool _isEating = false;

    protected override void Awake()
    {
        base.Awake();
        SetSpecies(SpeciesId.MeatFish);
    }

    protected override void Update()
    {
        CheckAndEatFish();
        base.Update(); // Always wander, no hunting behavior
    }

    private void CheckAndEatFish()
    {
        if (_isEating) return;

        if (mouth != null && mouth.CurrentTarget != null)
        {
            var target = mouth.CurrentTarget;
            // 检查目标是否是鱼类且体型比自己小
            if (target is FishBase fish && CanEatFish(fish))
            {
                // 遇到能吃的鱼时，有一定概率吃掉
                if (Random.value < _eatDecisionChance)
                {
                    Debug.Log($"[MeatFish] 正在吃 {fish.GetType().Name} (体型:{fish.CurrentSizeTier} vs 自己:{this.CurrentSizeTier})");
                    _isEating = true;
                    OpenMouth();
                    StartCoroutine(CloseMouthAfterDelay(0.3f));
                }
                else
                {
                    Debug.Log($"[MeatFish] 遇到 {fish.GetType().Name} 但选择不吃 (体型:{fish.CurrentSizeTier} vs 自己:{this.CurrentSizeTier})");
                }
            }
            else if (target is FishBase fish2)
            {
                Debug.Log($"[MeatFish] 发现 {fish2.GetType().Name} 但无法吃 (体型:{fish2.CurrentSizeTier} vs 自己:{this.CurrentSizeTier})");
            }
        }
    }

    private bool CanEatFish(FishBase fish)
    {
        // 只能吃比自己体型小的鱼
        return fish.CurrentSizeTier < this.CurrentSizeTier;
    }

    private System.Collections.IEnumerator CloseMouthAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseMouth();
        _isEating = false;
    }


    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
}