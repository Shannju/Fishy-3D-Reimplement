using UnityEngine;

public class MouthSensor : MonoBehaviour
{
    private IEatable currentTarget;
    public IEatable CurrentTarget => currentTarget;

    private void OnTriggerEnter(Collider other)
    {
        var e = other.GetComponentInParent<IEatable>();
        if (e != null && !e.IsDepleted)
        {
            currentTarget = e;
            // Debug.Log($"MouthSensor: 嘴碰到 {e.GetType().Name} (可食用)");
        }
        else if (e != null && e.IsDepleted)
        {
            // Debug.Log($"MouthSensor: 嘴碰到 {e.GetType().Name} (已被吃完)");
        }
        else
        {
            // Debug.Log($"MouthSensor: 嘴碰到 {other.gameObject.name} (不可食用)");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var e = other.GetComponentInParent<IEatable>();
        if (e != null && currentTarget == e) currentTarget = null;
    }
}
