using UnityEngine;

public class MouthSensor : MonoBehaviour
{
    private IEatable currentTarget;
    public IEatable CurrentTarget => currentTarget;

    private void OnTriggerEnter(Collider other)
    {
        var e = other.GetComponentInParent<IEatable>();
        if (e != null && !e.IsDepleted) currentTarget = e;
    }

    private void OnTriggerExit(Collider other)
    {
        var e = other.GetComponentInParent<IEatable>();
        if (e != null && currentTarget == e) currentTarget = null;
    }
}
