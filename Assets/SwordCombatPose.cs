using UnityEngine;
using UnityEngine.InputSystem;

public class SwordCombatPose : MonoBehaviour
{
    public float moveSpeed = 8f;

    public Vector3 idlePosition;
    public Vector3 movingPosition;

    public Vector3 idleRotation;
    public Vector3 movingRotation;

    void Update()
    {
        bool moving = false;

        if (Keyboard.current != null)
        {
            moving =
                Keyboard.current.wKey.isPressed ||
                Keyboard.current.aKey.isPressed ||
                Keyboard.current.sKey.isPressed ||
                Keyboard.current.dKey.isPressed;
        }

        Vector3 targetPos = moving ? movingPosition : idlePosition;
        Vector3 targetRot = moving ? movingRotation : idleRotation;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            Time.deltaTime * moveSpeed
        );

        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            Quaternion.Euler(targetRot),
            Time.deltaTime * moveSpeed
        );
    }
}