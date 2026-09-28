using UnityEngine;

public class DroneAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 100f;

    [Header("Flying")]
    public float moveSpeed = 4f;
    public float flyingHeight = 3f;
    public float attackDistance = 4f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;
    public float damage = 10f;

    private Transform player;
    private PlayerHealth playerHealth;
    private float nextAttackTime;

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogError("DRONE: PLAYER HEALTH NOT FOUND!");
            return;
        }

        player = playerHealth.transform;

        Debug.Log("DRONE AI STARTED");
        Debug.Log("DRONE FOUND PLAYER: " + player.name);
    }

    void Update()
    {
        if (player == null)
            return;

        // Target position: player's position + flying height
        Vector3 targetPosition = player.position;
        targetPosition.y += flyingHeight;

        // Distance to player
        float distance = Vector3.Distance(
            transform.position,
            targetPosition
        );

        // Player too far
        if (distance > detectionRange)
            return;

        // Move toward player
        if (distance > attackDistance)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }
        else
        {
            AttackPlayer();
        }

        // Face player
        Vector3 lookDirection = player.position - transform.position;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(
                lookDirection.normalized
            );
        }
    }

    void AttackPlayer()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);

            Debug.Log(
                "DRONE ATTACKED PLAYER! Damage = " + damage
            );
        }
    }
}