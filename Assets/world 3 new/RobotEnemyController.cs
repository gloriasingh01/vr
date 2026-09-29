using UnityEngine;

public class RobotEnemyController : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 100f;

    [Header("Movement")]
    public float moveSpeed = 300f;
    public float stopDistance = 4f;
    public float minimumDistance = 3.5f;

    [Header("Hover")]
    public float eyeHeight = 0f;
    public float hoverAmount = 0.3f;
    public float hoverSpeed = 3f;

    [Header("Attack")]
    public float attackDistance = 3.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    private Transform player;
    private PlayerHealth playerHealth;
    private float nextAttackTime;
    private float hoverOffset;
    private Vector3 startingPosition;

    private void Start()
    {
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();

        if (health == null)
        {
            Debug.LogError("ROBOT: PlayerHealth not found!");
            enabled = false;
            return;
        }

        playerHealth = health;
        player = health.transform;

        startingPosition = transform.position;
        hoverOffset = Random.Range(0f, 10f);

        Debug.Log("ROBOT READY: " + gameObject.name);
    }

    private void Update()
    {
        if (player == null || playerHealth == null)
            return;

        Vector3 target = player.position;

        // Keep robot around player's eye level.
        target.y = player.position.y + eyeHeight;

        float distance = Vector3.Distance(transform.position, target);

        // Do nothing until player enters detection range.
        if (distance > detectionRange)
            return;

        // Face player.
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(lookDirection.normalized);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                8f * Time.deltaTime
            );
        }

        // Attack when close enough.
        if (distance <= attackDistance)
        {
            AttackPlayer();
        }

        // Move toward player until stop distance.
        if (distance > stopDistance)
        {
            Vector3 direction = target - transform.position;
            direction.Normalize();

            transform.position +=
                direction * moveSpeed * Time.deltaTime;
        }

        // Prevent robot from getting closer than minimum distance.
        float horizontalDistance =
            Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(player.position.x, 0f, player.position.z)
            );

        if (horizontalDistance < minimumDistance)
        {
            Vector3 away =
                transform.position - player.position;

            away.y = 0f;

            if (away.sqrMagnitude > 0.001f)
            {
                away.Normalize();

                Vector3 correctedPosition =
                    player.position + away * minimumDistance;

                correctedPosition.y = transform.position.y;

                transform.position = correctedPosition;
            }
        }

        // Small floating/hovering motion.
        float hover =
            Mathf.Sin((Time.time + hoverOffset) * hoverSpeed)
            * hoverAmount;

        Vector3 pos = transform.position;
        pos.y = target.y + hover;
        transform.position = pos;
    }

    private void AttackPlayer()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        playerHealth.TakeDamage(attackDamage);

        Debug.Log(
            gameObject.name +
            " ATTACKED PLAYER | DAMAGE = " +
            attackDamage
        );
    }
}