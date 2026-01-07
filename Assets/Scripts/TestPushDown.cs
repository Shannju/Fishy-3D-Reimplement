using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TestPushDown : MonoBehaviour
{
    public float downForce = 1.5f;   // 力的大小，越小越“轻”
    public ForceMode forceMode = ForceMode.Impulse;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            rb.AddForce(Vector3.down * downForce, forceMode);
        }
    }
}
