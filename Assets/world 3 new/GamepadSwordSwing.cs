using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadSwordSwing : MonoBehaviour
{
    [Header("Swing")]
    [Min(0.05f)]
    public float swingDuration = 0.3f;

    [Range(30f, 180f)]
    public float swingAngle = 115f;

    [Range(0f, 1f)]
    public float triggerThreshold = 0.65f;

    [Header("Damage")]
    public float damage = 100f;

    [Tooltip("Damage is applied when the sword reaches this percentage of the swing.")]
    [Range(0.1f, 0.9f)]
    public float damagePoint = 0.5f;

    [Header("Hit Detection")]
    public BoxCollider swordCollider;

    private Quaternion restingRotation;

    private float swingProgress = 1f;
    private float swingDirection = 1f;

    private bool triggerWasPressed;
    private bool attacking;
    private bool damageApplied;

    private readonly HashSet<MosquitoEnemyHealth> hitEnemies =
        new HashSet<MosquitoEnemyHealth>();

    private void Awake()
    {
        restingRotation = transform.localRotation;

        if (swordCollider == null)
            swordCollider = GetComponent<BoxCollider>();

        if (swordCollider == null)
        {
            Debug.LogError(
                "GAMEPAD SWORD: BoxCollider not found on swordright!"
            );
        }
    }

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;

        bool triggerPressed =
            gamepad != null &&
            gamepad.rightTrigger.ReadValue() >= triggerThreshold;

        // RT rising edge = start ONE swing
        if (triggerPressed && !triggerWasPressed)
        {
            StartSwing();
        }

        triggerWasPressed = triggerPressed;

        if (!attacking)
            return;

        UpdateSwing();
    }

    private void StartSwing()
    {
        if (attacking)
            return;

        attacking = true;
        damageApplied = false;
        swingProgress = 0f;

        // Alternate slash direction
        swingDirection *= -1f;

        hitEnemies.Clear();

        Debug.Log("RT PRESSED - SWORD SWING START");
    }

    private void UpdateSwing()
    {
        swingProgress += Time.deltaTime / swingDuration;

        float progress =
            Mathf.Clamp01(swingProgress);

        // Smooth sword arc
        float arc =
            Mathf.Sin(progress * Mathf.PI);

        transform.localRotation =
            restingRotation *
            Quaternion.AngleAxis(
                swingDirection * swingAngle * arc,
                Vector3.right
            );

        // Actual slash damage moment
        if (!damageApplied &&
            progress >= damagePoint)
        {
            damageApplied = true;

            Debug.Log("SWORD SLASH DETECTED!");

            DealSlashDamage();
        }

        // Swing finished
        if (progress >= 1f)
        {
            transform.localRotation = restingRotation;

            attacking = false;

            Debug.Log("SWORD SWING END");
        }
    }

    private void DealSlashDamage()
    {
        if (swordCollider == null)
            return;

        Physics.SyncTransforms();

        Vector3 worldCenter =
            swordCollider.transform.TransformPoint(
                swordCollider.center
            );

        Vector3 scale =
            swordCollider.transform.lossyScale;

        scale = new Vector3(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z)
        );

        Vector3 halfExtents =
            Vector3.Scale(
                swordCollider.size * 0.5f,
                scale
            );

        Collider[] hits =
            Physics.OverlapBox(
                worldCenter,
                halfExtents,
                swordCollider.transform.rotation,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            MosquitoEnemyHealth enemy =
                hit.GetComponentInParent<MosquitoEnemyHealth>();

            if (enemy == null)
                continue;

            // Same enemy can only be damaged once per slash
            if (hitEnemies.Contains(enemy))
                continue;

            hitEnemies.Add(enemy);

            enemy.TakeDamage(damage);

            Debug.Log(
                "SWORD HIT " +
                enemy.gameObject.name +
                " | DAMAGE = " +
                damage
            );
        }
    }

    private void OnDisable()
    {
        attacking = false;
        damageApplied = false;
        swingProgress = 1f;
        triggerWasPressed = false;

        transform.localRotation = restingRotation;

        hitEnemies.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (swordCollider == null)
            return;

        Gizmos.matrix =
            Matrix4x4.TRS(
                swordCollider.transform.TransformPoint(
                    swordCollider.center
                ),
                swordCollider.transform.rotation,
                swordCollider.transform.lossyScale
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            swordCollider.size
        );
    }
}