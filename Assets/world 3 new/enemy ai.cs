using UnityEngine;

public class MosquitoEnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 100f;

    [Header("Movement")]
    public float moveSpeed = 300f;
    public float stopDistance = 4f;
    public float absoluteMinimumDistance = 3.5f;

    [Header("Combat position")]
    public float eyeHeightOffset = 0f;
    public float sideOffset = 0f;

    [Header("Hover")]
    [Min(0f)] public float hoverAmount = 0.12f;
    [Min(0f)] public float hoverSpeed = 3f;

    [Header("Attack")]
    public float attackDistance = 4.5f;
    public float attackCooldown = 1.5f;
    public float damage = 10f;

    private Transform player;
    private Transform playerCamera;
    private PlayerHealth playerHealth;
    private float nextAttackTime;
    private float hoverOffset;
    private bool pairConfigured;

    private void Awake()
    {
        if (!pairConfigured)
            moveSpeed = 300f;
        detectionRange = Mathf.Max(100f, detectionRange);
        stopDistance = 4f;
        absoluteMinimumDistance = 3.5f;
        hoverAmount = 0.12f;
        attackCooldown = 1.5f;
        damage = 10f;
    }

    private void Start()
    {
        ResolvePlayerReferences();
        hoverOffset = Random.Range(0f, 10f);
        nextAttackTime = Time.time;

        if (playerHealth == null)
            Debug.LogError(gameObject.name + ": PLAYER HEALTH NOT FOUND; this mech cannot attack.", this);
        else
            Debug.Log(gameObject.name + " READY | SPEED = " + moveSpeed + " | HOVER = " + hoverAmount, this);
    }

    /// <summary>Sets the speed and left/right formation for a member of a wave pair.</summary>
    public void ConfigureForPair(int pairPosition)
    {
        pairConfigured = true;
        moveSpeed = pairPosition == 0 ? 300f : 275f;
        sideOffset = pairPosition == 0 ? -1.25f : 1.25f;
        detectionRange = 100f;
        stopDistance = 4f;
        absoluteMinimumDistance = 3.5f;
        hoverAmount = 0.12f;
        hoverSpeed = 3f;
        attackDistance = 4.5f;
        attackCooldown = 1.5f;
        damage = 10f;
    }

    private void Update()
    {
        ResolvePlayerReferences();
        if (player == null)
            return;

        Vector3 headPosition = playerCamera != null ? playerCamera.position : player.position;
        Vector3 toHead = headPosition - transform.position;
        float distance = toHead.magnitude;
        if (distance > detectionRange)
            return;

        Vector3 forward = playerCamera != null ? playerCamera.forward : player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = player.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 desiredPosition = headPosition + forward * stopDistance + right * sideOffset;
        desiredPosition.y = headPosition.y + eyeHeightOffset + hoverAmount * Mathf.Sin((Time.time + hoverOffset) * hoverSpeed);

        // Keep the full 3D separation outside the safety radius, including during overshoot.
        Vector3 fromPlayer = desiredPosition - headPosition;
        if (fromPlayer.magnitude < absoluteMinimumDistance)
            desiredPosition = headPosition + (fromPlayer.sqrMagnitude > 0.001f ? fromPlayer.normalized : forward) * absoluteMinimumDistance;

        transform.position = Vector3.MoveTowards(transform.position, desiredPosition, moveSpeed * Time.deltaTime);

        Vector3 actualSeparation = transform.position - headPosition;
        if (actualSeparation.magnitude < absoluteMinimumDistance)
            transform.position = headPosition + (actualSeparation.sqrMagnitude > 0.001f ? actualSeparation.normalized : -forward) * absoluteMinimumDistance;

        FacePlayer(headPosition);

        if (Vector3.Distance(transform.position, headPosition) <= attackDistance)
            AttackPlayer();
    }

    private void FacePlayer(Vector3 playerPosition)
    {
        Vector3 direction = playerPosition - transform.position;
        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);
    }

    private void AttackPlayer()
    {
        if (playerHealth == null || Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        playerHealth.TakeDamage(damage);
        Debug.Log(gameObject.name + " ATTACKED PLAYER | DAMAGE = " + damage, this);
    }

    private void ResolvePlayerReferences()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (player == null)
        {
            for (Transform current = playerCamera; current != null; current = current.parent)
            {
                if (current.name.Contains("XR Origin") || current.name.Contains("XR Rig"))
                {
                    player = current;
                    break;
                }
            }

            if (player == null && playerHealth != null)
                player = playerHealth.transform;
        }
    }
}
