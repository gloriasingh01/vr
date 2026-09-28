using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float detectionRange = 100f;
    public float moveSpeed = 4f;
    public float attackDistance = 2.5f;

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
            Debug.LogError("PLAYER HEALTH NOT FOUND!");
            return;
        }

        player = playerHealth.transform;

        Debug.Log("ENEMY AI STARTED");
        Debug.Log("PLAYER FOUND: " + player.name);
    }

    void Update()
    {
        if (player == null)
            return;

        Vector3 enemyPos = transform.position;
        Vector3 playerPos = player.position;

        enemyPos.y = 0f;
        playerPos.y = 0f;

        float distance = Vector3.Distance(
            enemyPos,
            playerPos
        );

        if (distance > detectionRange)
            return;

        if (distance > attackDistance)
        {
            Vector3 direction =
                playerPos - enemyPos;

            direction.Normalize();

            transform.position +=
                direction *
                moveSpeed *
                Time.deltaTime;
        }
        else
        {
            AttackPlayer();
        }
    }

    void AttackPlayer()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime =
            Time.time + attackCooldown;

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);

            Debug.Log(
                "ENEMY ATTACKED PLAYER! " +
                "Damage = " + damage
            );
        }
    }
}