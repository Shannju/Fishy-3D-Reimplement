using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody rb;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6f;     // 最大速度
    [SerializeField] private float accel = 8f;        // 加速度，越大越快启动
    [SerializeField] private float decel = 10f;       // 减速度，越大越容易停下来
    [SerializeField] private float lateralDamping = 4f; // 侧向阻尼（越大越容易抓地，越小越容易甩尾）

    [Header("Turning")]
    [SerializeField] private float turnSpeed = 220f;  // 度/秒（越小转弯半径越大）

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    // 输入：双重保险（InputAction + Keyboard fallback）
    private InputAction moveAction;
    private Vector2 move; // x,y

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // InputAction优先使用，如果没有会自动回退到 Keyboard
        moveAction = new InputAction("Move", InputActionType.Value);

        // WASD
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Arrow keys
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        // Gamepad
        moveAction.AddBinding("<Gamepad>/leftStick");

        moveAction.Enable();

        if (debugLog)
            Debug.Log($"[FishInput] Enabled={moveAction.enabled}, Keyboard exists={(Keyboard.current != null)}");
    }

    private void OnDisable()
    {
        moveAction?.Disable();
    }

    private void Update()
    {
        // 1) 先读 InputAction
        Vector2 actionMove = moveAction.ReadValue<Vector2>();

        // 2) 再读 Keyboard（双重保险）
        Vector2 keyMove = ReadKeyboardMove();

        // 3) 如果 actionMove 有值就使用，否则使用 keyMove
        move = (actionMove != Vector2.zero) ? actionMove : keyMove;

        if (debugLog && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            Debug.Log($"[FishInput] action={actionMove}  key={keyMove}  chosen={move}");
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 input = new Vector3(move.x, 0f, move.y);
        float inputMag = Mathf.Clamp01(input.magnitude);

        // 转向：平滑转向到目标方向，转弯半径由 turnSpeed 控制
        if (input != Vector3.zero)
        {
            var relative = (transform.position + input.ToIso()) - transform.position;
            var rot = Quaternion.LookRotation(relative, Vector3.up);

            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, rot, turnSpeed * Time.fixedDeltaTime));
        }

        // 移动：速度追踪目标速度 + 侧向阻尼
        Vector3 forward = transform.forward;
        Vector3 velocity = rb.linearVelocity;

        Vector3 forwardVel = Vector3.Project(velocity, forward);
        Vector3 lateralVel = velocity - forwardVel;

        // 侧向阻尼（甩尾效果）
        lateralVel = Vector3.Lerp(lateralVel, Vector3.zero, lateralDamping * Time.fixedDeltaTime);

        float targetSpeed = maxSpeed * inputMag;
        float currentForwardSpeed = Vector3.Dot(forwardVel, forward);

        float rate = (inputMag > 0.001f) ? accel : decel;
        float newForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        rb.linearVelocity = forward * newForwardSpeed + lateralVel;
    }

    private Vector2 ReadKeyboardMove()
    {
        if (Keyboard.current == null) return Vector2.zero;

        float x = 0f, y = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed) y += 1f;

        // 方向键也支持
        if (Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.downArrowKey.isPressed) y -= 1f;
        if (Keyboard.current.upArrowKey.isPressed) y += 1f;

        Vector2 v = new Vector2(x, y);
        return (v.sqrMagnitude > 1f) ? v.normalized : v;
    }
}

// 如果需要等距旋转功能，取消注释下面这段来启用
// public static class Helpers
// {
//     private static Matrix4x4 _isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
//     public static Vector3 ToIso(this Vector3 input) => _isoMatrix.MultiplyPoint3x4(input);
// }
