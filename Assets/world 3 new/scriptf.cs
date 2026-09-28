using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadLook : MonoBehaviour
{
    public Transform cameraTransform;
    public float lookSpeed = 60f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float pitch;

    void Start()
    {
        if (cameraTransform != null)
            pitch = cameraTransform.localEulerAngles.x;
    }

    void Update()
    {
        if (Gamepad.current == null || cameraTransform == null)
            return;

        float vertical = Gamepad.current.rightStick.ReadValue().y;

        if (Mathf.Abs(vertical) < 0.1f)
            return;

        pitch -= vertical * lookSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Vector3 current = cameraTransform.localEulerAngles;
        cameraTransform.localRotation =
            Quaternion.Euler(pitch, current.y, current.z);
    }
}