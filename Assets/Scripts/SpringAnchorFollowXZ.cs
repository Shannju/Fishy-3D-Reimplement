using UnityEngine;

[DefaultExecutionOrder(-50)]
public class SpringAnchorFollowXZ : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;          // 拖你的鱼进来

    [Header("Anchor Settings")]
    public float targetY = 0f;        // 你希望最终稳定的高度：0 或 -0.3 等
    public bool useFixedUpdate = true;

    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb) _rb.isKinematic = true;
    }

    void FixedUpdate()
    {
        if (!useFixedUpdate) return;
        Follow();
    }

    void LateUpdate()
    {
        if (useFixedUpdate) return;
        Follow();
    }

    void Follow()
    {
        if (!player) return;

        Vector3 p = player.position;
        Vector3 targetPos = new Vector3(p.x, targetY, p.z);

        // 由于是 kinematic rb，用 MovePosition 最稳（对物理友好）
        if (_rb)
            _rb.MovePosition(targetPos);
        else
            transform.position = targetPos;
    }
}
