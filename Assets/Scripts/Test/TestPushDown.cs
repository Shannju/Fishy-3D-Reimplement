using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TestPushDown : MonoBehaviour
{
    public float downForce = 1.5f;        // 向下持续力的大小（Acceleration模式）
    public float upImpulse = 5.0f;         // 向上冲量的大小（Impulse模式）

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Z))
        {
            rb.AddForce(Vector3.down * downForce, ForceMode.Acceleration);
        }
        if (Input.GetKeyUp(KeyCode.Z))
        {
            // 松开时给冲量
            rb.AddForce(Vector3.up * upImpulse, ForceMode.Impulse);
        }
    }
}
