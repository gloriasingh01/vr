using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class StoneMonsterEnemy : MonoBehaviour
{
    private ShootingGameManager gameManager;
    private Transform target;
    private Rigidbody body;
    private BoxCollider hitbox;
    private Animation_Test animationTest;
    private float moveSpeed;
    private bool defeated;
    private bool stopped;
    private bool launched;

    public void Initialize(ShootingGameManager manager, float moveSpeed)
    {
        gameManager = manager;
        target = manager != null ? manager.PlayerTarget : null;
        this.moveSpeed = moveSpeed;
        defeated = false;
        stopped = false;
        launched = false;
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<BoxCollider>();

        body.mass = 1f;
        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        hitbox.isTrigger = true;
        FitHitboxToModel();

        animationTest = GetComponent<Animation_Test>();
    }

    public void Launch()
    {
        if (defeated || gameManager == null)
            return;

        launched = true;
    }

    private void FixedUpdate()
    {
        if (!launched || defeated || stopped || gameManager == null || gameManager.IsGameOver || target == null)
            return;

        Vector3 direction = target.position - body.position;
        if (direction.sqrMagnitude < 0.01f)
            return;

        Vector3 nextPosition = Vector3.MoveTowards(body.position, target.position, moveSpeed * Time.fixedDeltaTime);
        body.MovePosition(nextPosition);
        Quaternion faceTarget = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, faceTarget, 120f * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || defeated || stopped || gameManager == null || other == null)
            return;

        Transform playerRoot = gameManager.PlayerRoot;
        if (playerRoot != null && (other.transform == playerRoot || other.transform.IsChildOf(playerRoot)))
            gameManager.RegisterPlayerContact(this);
    }

    public void StopMoving()
    {
        stopped = true;
        if (hitbox != null)
            hitbox.enabled = false;
    }

    public void Die()
    {
        if (defeated)
            return;

        defeated = true;
        if (hitbox != null)
            hitbox.enabled = false;

        if (animationTest != null)
            animationTest.DeathAni();

        if (gameManager != null)
            gameManager.RegisterMonsterDefeated(this);

        Destroy(gameObject, 1.25f);
    }

    private void FitHitboxToModel()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            hitbox.center = Vector3.zero;
            hitbox.size = new Vector3(1.3f, 1.3f, 1f);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; ++i)
            bounds.Encapsulate(renderers[i].bounds);

        hitbox.center = transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = transform.InverseTransformVector(bounds.size);
        hitbox.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z)) + Vector3.one * 0.08f;
    }
}
