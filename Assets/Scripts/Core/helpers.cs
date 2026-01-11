using UnityEngine;

public static class Helpers
{
    // 45° 等距旋转矩阵：把输入方向旋转到等距坐标系
    private static readonly Matrix4x4 IsoMatrix =
        Matrix4x4.Rotate(Quaternion.Euler(0f, 45f, 0f));

    public static Vector3 ToIso(this Vector3 input)
    {
        return IsoMatrix.MultiplyPoint3x4(input);
    }
}
