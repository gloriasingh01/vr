using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerLook : MonoBehaviour
{
    public float lookSpeed = 60f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float pitch;

    void Update()
    {
        if (Gamepad.current == null)
            return;

        float vertical = Gamepad.current.rightStick.ReadValue().y;

        pitch -= vertical * lookSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}