using System.Collections;
using UnityEngine;

/// <summary>Waits for the tracked headset pose, then faces the player toward the first gate once.</summary>
public class InitialFacing : MonoBehaviour
{
    [SerializeField] private Transform focusTarget;
    [SerializeField, Min(0f)] private float alignmentDelay = 1.5f;

    public void Configure(Transform target)
    {
        focusTarget = target;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(alignmentDelay);

        if (focusTarget == null || Camera.main == null)
            yield break;

        Transform head = Camera.main.transform;
        Vector3 desiredForward = Vector3.ProjectOnPlane(focusTarget.position - head.position, Vector3.up);
        Vector3 currentForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (desiredForward.sqrMagnitude < 0.001f || currentForward.sqrMagnitude < 0.001f)
            yield break;

        transform.RotateAround(
            head.position,
            Vector3.up,
            Vector3.SignedAngle(currentForward, desiredForward, Vector3.up));
    }
}