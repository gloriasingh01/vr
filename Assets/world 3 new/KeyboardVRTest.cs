using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardVRTest : MonoBehaviour
{
    public float speed = 8f;

    void Update()
    {
        Vector3 movement = Vector3.zero;

        if (Keyboard.current != null)
        {
            // Forward / Backward
            if (Keyboard.current.wKey.isPressed)
                movement += transform.forward;

            if (Keyboard.current.sKey.isPressed)
                movement -= transform.forward;

            // Left / Right
            if (Keyboard.current.aKey.isPressed)
                movement -= transform.right;

            if (Keyboard.current.dKey.isPressed)
                movement += transform.right;

            // Up / Down
            if (Keyboard.current.spaceKey.isPressed)
                movement += Vector3.up;

            if (Keyboard.current.leftCtrlKey.isPressed)
                movement -= Vector3.up;
        }

        if (movement != Vector3.zero)
        {
            transform.position += movement.normalized * speed * Time.deltaTime;
        }
    }
}