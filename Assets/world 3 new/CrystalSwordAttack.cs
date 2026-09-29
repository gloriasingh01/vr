using System;
using UnityEngine;

public class SwordSwing : MonoBehaviour
{
    [Header("Slash Detection")]
    [Tooltip("Minimum angular speed required to confirm a slash.")]
    public float slashAngularSpeed = 150f;

    [Tooltip("Minimum rotation angle required during one slash.")]
    public float minimumSlashAngle = 12f;

    // CrystalSwordAttack listens to this event.
    public event Action SlashDetected;

    private bool slashWindowActive;
    private bool slashAlreadyDetected;

    private Quaternion lastRotation;
    private float totalSlashAngle;

    private void Awake()
    {
        lastRotation = transform.localRotation;
    }

    private void Update()
    {
        Quaternion currentRotation = transform.localRotation;

        float angleDelta = Quaternion.Angle(
            lastRotation,
            currentRotation
        );

        float angularSpeed = 0f;

        if (Time.deltaTime > 0f)
        {
            angularSpeed = angleDelta / Time.deltaTime;
        }

        if (slashWindowActive && !slashAlreadyDetected)
        {
            totalSlashAngle += angleDelta;

            if (angularSpeed >= slashAngularSpeed &&
                totalSlashAngle >= minimumSlashAngle)
            {
                slashAlreadyDetected = true;

                Debug.Log(
                    "SWORD SLASH DETECTED! Angular Speed = " +
                    angularSpeed.ToString("0.##")
                );

                SlashDetected?.Invoke();
            }
        }

        lastRotation = currentRotation;
    }

    // Called by CrystalSwordAttack when RT starts the swing.
    public void BeginSlashWindow()
    {
        slashWindowActive = true;
        slashAlreadyDetected = false;
        totalSlashAngle = 0f;

        lastRotation = transform.localRotation;

        Debug.Log("SWORD SLASH WINDOW START");
    }

    // Called by CrystalSwordAttack when the swing ends.
    public void EndSlashWindow()
    {
        slashWindowActive = false;
        slashAlreadyDetected = false;
        totalSlashAngle = 0f;

        lastRotation = transform.localRotation;

        Debug.Log("SWORD SLASH WINDOW END");
    }
}
