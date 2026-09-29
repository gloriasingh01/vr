using UnityEngine;

public class FinalBossController : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 20f;

    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Distance")]
    public float attackRange = 3f;
    public float minimumDistance = 2.8f;

    [Header("Attack")]
    public float attackDamage = 20f;
    public float attackCooldown = 1.5f;
    public float attackHitTime = 0.45f;

    [Header("Animator")]
    public Animator animator;

    public string speedParameter = "Speed";
    public string attackTrigger = "Attack";
    public string deadBool = "Dead";
    public string victoryBool = "Victory";

    private Transform player;
    private PlayerHealth playerHealth;

    private float nextAttackTime;
    private float attackTimer;

    private bool attacking;
    private bool damageApplied;
    private bool dead;
    private bool victory;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        FindPlayer();

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        nextAttackTime = Time.time;
    }

    private void Update()
    {
        if (dead || victory)
            return;

        FindPlayer();

        if (player == null)
            return;

        Vector3 targetPosition = player.position;

        // Keep Astria on her current vertical level.
        targetPosition.y = transform.position.y;

        Vector3 toPlayer = targetPosition - transform.position;
        float distance = toPlayer.magnitude;

        // Outside detection range.
        if (distance > detectionRange)
        {
            SetSpeed(0f);
            return;
        }

        // =====================================================
        // EMERGENCY MINIMUM DISTANCE
        // =====================================================

        if (distance < minimumDistance)
        {
            Vector3 awayFromPlayer =
                transform.position - targetPosition;

            awayFromPlayer.y = 0f;

            if (awayFromPlayer.sqrMagnitude < 0.001f)
            {
                awayFromPlayer = -player.forward;
                awayFromPlayer.y = 0f;
            }

            awayFromPlayer.Normalize();

            Vector3 safePosition =
                targetPosition +
                awayFromPlayer * minimumDistance;

            safePosition.y = transform.position.y;

            transform.position = safePosition;

            distance = minimumDistance;
        }

        // =====================================================
        // CHASE
        // =====================================================

        if (distance > attackRange)
        {
            attacking = false;
            attackTimer = 0f;
            damageApplied = false;

            Vector3 moveDirection = toPlayer.normalized;

            // Only move until attack range.
            float distanceToStop =
                distance - attackRange;

            float movement =
                Mathf.Min(
                    moveSpeed * Time.deltaTime,
                    distanceToStop
                );

            if (movement > 0f)
            {
                transform.position +=
                    moveDirection * movement;
            }

            SetSpeed(moveSpeed);

            FacePlayer(targetPosition);
        }
        else
        {
            // =================================================
            // ATTACK RANGE
            // =================================================

            SetSpeed(0f);

            FacePlayer(targetPosition);

            TryAttack();
        }

        UpdateAttack();
    }

    // =========================================================
    // ATTACK
    // =========================================================

    private void TryAttack()
    {
        if (attacking)
            return;

        if (Time.time < nextAttackTime)
            return;

        attacking = true;
        damageApplied = false;
        attackTimer = 0f;

        nextAttackTime =
            Time.time + attackCooldown;

        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }

        Debug.Log("ASTRIA ATTACK START");
    }

    private void UpdateAttack()
    {
        if (!attacking)
            return;

        attackTimer += Time.deltaTime;

        // Exactly ONE damage event per attack.
        if (!damageApplied &&
            attackTimer >= attackHitTime)
        {
            damageApplied = true;

            if (playerHealth == null)
            {
                playerHealth =
                    FindFirstObjectByType<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);

                Debug.Log(
                    "ASTRIA HIT PLAYER | DAMAGE = " +
                    attackDamage
                );
            }
        }

        // Finish attack.
        if (attackTimer >= 0.8f)
        {
            attacking = false;
            attackTimer = 0f;
        }
    }

    // =========================================================
    // FACE PLAYER
    // =========================================================

    private void FacePlayer(Vector3 target)
    {
        Vector3 direction =
            target - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
    }

    // =========================================================
    // FIND PLAYER
    // =========================================================

    private void FindPlayer()
    {
        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<PlayerHealth>();
        }

        if (player == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                player = mainCamera.transform;
            }
        }

        if (player == null && playerHealth != null)
        {
            player = playerHealth.transform;
        }
    }

    // =========================================================
    // ANIMATION SPEED
    // =========================================================

    private void SetSpeed(float speed)
    {
        if (animator == null)
            return;

        foreach (
            AnimatorControllerParameter parameter
            in animator.parameters
        )
        {
            if (
                parameter.name == speedParameter &&
                parameter.type ==
                AnimatorControllerParameterType.Float
            )
            {
                animator.SetFloat(
                    speedParameter,
                    speed
                );

                return;
            }
        }
    }

    // =========================================================
    // BOSS DEATH
    // =========================================================

    public void SetDead()
    {
        if (dead)
            return;

        dead = true;
        attacking = false;

        SetSpeed(0f);

        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetBool(deadBool, true);
        }

        Debug.Log("ASTRIA DEFEATED");
    }

    // =========================================================
    // VICTORY
    // =========================================================

    public void SetVictory()
    {
        if (victory)
            return;

        victory = true;
        attacking = false;

        SetSpeed(0f);

        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetBool(victoryBool, true);
        }

        Debug.Log("ASTRIA VICTORY");
    }
}