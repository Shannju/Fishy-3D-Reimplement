using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody rb;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 5f;     // 最大速度
    [SerializeField] private float accel = 8f;        // 加速度，越大越快启动
    [SerializeField] private float decel = 10f;       // 减速度，越大越容易停下来
    [SerializeField] private float lateralDamping = 10f; // 侧向阻尼（越大越容易抓地，越小越容易甩尾）

    [Header("Turning")]
    [SerializeField] private float turnSpeed = 720f;  // 度/秒（基础转向速度）
    [SerializeField] private float turnAcceleration = 1800f; // 度/秒²（转向加速度，越小转向越平滑）
    [SerializeField] private float turnDamping = 15f; // 转向阻尼（越大越容易停下）

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    [Header("Shader 属性匹配")]
    [SerializeField] private Material fishMaterial;  // 拖拽鱼的材质到这里
    [SerializeField] private string lerpPropertyName = "_LerpFactor";

    [Header("速度感应设置")]
    [SerializeField] private float movementMaxSpeed = 2.0f;      // 鱼达到多快时摆动幅度达到最大（调小让摆动更明显）
    [SerializeField] private float movementSmoothSpeed = 5.0f;  // 摆动开始/停止的平滑度

    // 输入：双重保险（InputAction + Keyboard fallback）
    private InputAction moveAction;

    // 运动跟踪
    private float currentLerpVal;
    private Vector2 move; // x,y

    // 转向控制
    private float currentAngularVelocity; // 当前角速度

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        // 确保Rigidbody物理设置正确
        rb.useGravity = false;
        rb.angularDamping = turnDamping;   // 使用可调节的转向阻尼

        // 确保碰撞检测模式为Continuous，避免高速穿透
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        currentLerpVal = 0f; // 确保初始值为0

        // 确保Shader初始状态为静止
        if (fishMaterial != null)
        {
            fishMaterial.SetFloat(lerpPropertyName, 0f);
            if (debugLog)
                Debug.Log("[FishShader] Material assigned and initialized");
        }
        else if (debugLog)
        {
            Debug.LogWarning("[FishShader] No material assigned in Inspector");
        }
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

        // 4) 更新材质摆动效果
        UpdateMovementShader();
    }

    private void FixedUpdate()
    {
        // 如果有输入方向，转向对应方向
        if (move != Vector2.zero)
        {
            // 将2D输入转换为3D世界方向
            Vector3 inputDirection = new Vector3(move.x, 0f, move.y).normalized;

            // 计算目标角度
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + 45f;

            // 转向目标方向
            TurnTowardsAngle(targetAngle);

            // 往前移动（长按时加速）
            float speedMultiplier = move.magnitude; // 长按时magnitude接近1.0，短按时也接近1.0
            rb.AddForce(transform.forward * maxSpeed * speedMultiplier, ForceMode.Force);
        }
        else
        {
            // 没有输入时，逐渐减速
            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                Vector3.zero,
                decel * Time.fixedDeltaTime);
        }

        // 限制最大速度
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
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

    /// <summary>
    /// 转向指定角度（带阻尼控制）
    /// </summary>
    /// <param name="targetAngle">目标角度（度）</param>
    private void TurnTowardsAngle(float targetAngle)
    {
        // 获取当前角度
        float currentAngle = transform.eulerAngles.y;

        // 计算角度差（-180到180度之间）
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

        // 计算目标角速度（基于角度差）
        float targetAngularVelocity = angleDifference * turnAcceleration * Time.fixedDeltaTime;

        // 限制最大角速度
        targetAngularVelocity = Mathf.Clamp(targetAngularVelocity, -turnSpeed, turnSpeed);

        // 应用阻尼（模拟物理阻尼）
        currentAngularVelocity = Mathf.Lerp(currentAngularVelocity, targetAngularVelocity,
            turnDamping * Time.fixedDeltaTime);

        // 计算新的角度
        float newAngle = currentAngle + currentAngularVelocity * Time.fixedDeltaTime;

        // 设置新的旋转
        transform.rotation = Quaternion.Euler(0f, newAngle, 0f);
    }

    /// <summary>
    /// 根据移动速度更新材质的摆动效果
    /// </summary>
    private void UpdateMovementShader()
    {
        if (fishMaterial == null) return;

        // 1. 使用 Rigidbody 的速度（更准确，不依赖帧率）
        float actualVelocity = rb.linearVelocity.magnitude;

        // 2. 将速度转换为 0 到 1 的目标权重
        // 如果速度超过 movementMaxSpeed，权重也是 1
        float targetLerp = Mathf.Clamp01(actualVelocity / movementMaxSpeed);

        // 3. 使用 Lerp 平滑当前权重值，避免摆动突然开启或停止
        currentLerpVal = Mathf.Lerp(
            currentLerpVal,
            targetLerp,
            Time.deltaTime * movementSmoothSpeed
        );

        // 4. 将计算后的权重传给 Shader
        fishMaterial.SetFloat(lerpPropertyName, currentLerpVal);

        // 调试：输出当前值（仅在debugLog启用时）
        if (debugLog && currentLerpVal > 0.001f) // 只在有明显摆动时输出，避免刷屏
        {
            Debug.Log($"[FishShader] velocity={actualVelocity:F3}, target={targetLerp:F3}, current={currentLerpVal:F3}");
        }
    }
}

// 如果需要等距旋转功能，取消注释下面这段来启用
// public static class Helpers
// {
//     private static Matrix4x4 _isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
//     public static Vector3 ToIso(this Vector3 input) => _isoMatrix.MultiplyPoint3x4(input);
// }
