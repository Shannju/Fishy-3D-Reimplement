using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    // Public camera reference for size-based positioning
    public Camera Camera => GetComponent<Camera>();

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0f, 0f, 0f);
    public float smoothTime = 0.25f;

    [Header("Axis Control")]
    public bool followX = true;
    public bool followY = false;
    public bool followZ = true;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        Vector3 currentPos = transform.position;

        if (!followX) desiredPos.x = currentPos.x;
        if (!followY) desiredPos.y = currentPos.y;
        if (!followZ) desiredPos.z = currentPos.z;

        transform.position = Vector3.SmoothDamp(
            currentPos,
            desiredPos,
            ref velocity,
            smoothTime
        );
    }
}
