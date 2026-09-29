using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Gamepad locomotion and smooth look for the VR rig.</summary>
[RequireComponent(typeof(CharacterController))]
public class GamepadLook : MonoBehaviour
{
    [Header("Rig references")]
    public Transform cameraTransform;
    public Transform cameraOffset;

    [Header("Gamepad")]
    [Min(0f)] public float moveSpeed = 3f;
    [Min(0f)] public float lookSpeed = 100f;
    [Range(0f, 0.5f)] public float deadzone = 0.15f;
    [Range(-89f, 0f)] public float minPitch = -80f;
    [Range(0f, 89f)] public float maxPitch = 80f;

    private CharacterController characterController;
    private float pitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraOffset == null)
            cameraOffset = transform.Find("Camera Offset");

        if (cameraTransform == null && cameraOffset != null)
        {
            Camera rigCamera = cameraOffset.GetComponentInChildren<Camera>();
            if (rigCamera != null)
                cameraTransform = rigCamera.transform;
        }

        if (cameraOffset != null)
            pitch = NormalizeAngle(cameraOffset.localEulerAngles.x);
    }

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
        {
            characterController.SimpleMove(Vector3.zero);
            return;
        }

        float deltaTime = Time.deltaTime;
        Vector2 look = gamepad.rightStick.ReadValue();
        if (Mathf.Abs(look.x) > deadzone)
        {
            float yaw = look.x * lookSpeed * deltaTime;
            if (cameraTransform != null)
                transform.RotateAround(cameraTransform.position, Vector3.up, yaw);
            else
                transform.Rotate(0f, yaw, 0f, Space.World);
        }

        if (cameraOffset != null && Mathf.Abs(look.y) > deadzone)
        {
            pitch = Mathf.Clamp(pitch - look.y * lookSpeed * deltaTime, minPitch, maxPitch);
            cameraOffset.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Vector2 move = gamepad.leftStick.ReadValue();
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 movement = (forward * move.y + right * move.x) * moveSpeed;
        characterController.SimpleMove(movement);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
