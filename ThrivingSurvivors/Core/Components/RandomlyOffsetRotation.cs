using UnityEngine;

namespace ThrivingSurvivors.Core.Components;

public class RandomlyOffsetRotation : MonoBehaviour
{
    public float maxAngle;

    void OnEnable()
    {
        transform.Rotate(Random.insideUnitSphere * maxAngle, Space.Self);
    }
}
