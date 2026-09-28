using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    private bool spent;

    public void Initialize()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.mass = 0.05f;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (spent || other == null)
            return;

        StoneMonsterEnemy monster = other.GetComponentInParent<StoneMonsterEnemy>();
        if (monster == null)
            return;

        spent = true;
        monster.Die();
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (spent)
            return;

        StoneMonsterEnemy monster = collision.collider.GetComponentInParent<StoneMonsterEnemy>();
        if (monster != null)
            monster.Die();

        spent = true;
        Destroy(gameObject);
    }
}
