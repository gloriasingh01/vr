using UnityEngine;
using UnityEngine.InputSystem;

public class SwordControllerTest : MonoBehaviour
{
    public float rotationSpeed = 120f;

    [Header("Controller")]
    public bool isLeftSword = true;

    void Update()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
            return;

        float horizontal = 0f;

        if (isLeftSword)
        {
            // Left sword: D-Pad Left / Right
            if (gamepad.dpad.left.isPressed)
                horizontal = -1f;

            if (gamepad.dpad.right.isPressed)
                horizontal = 1f;
        }
        else
        {
            // Right sword: D-Pad Left / Right
            if (gamepad.dpad.left.isPressed)
                horizontal = -1f;

            if (gamepad.dpad.right.isPressed)
                horizontal = 1f;
        }

        transform.Rotate(
            0f,
            horizontal * rotationSpeed * Time.deltaTime,
            0f,
            Space.Self
        );
    }
}