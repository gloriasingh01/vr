using UnityEngine;

/// <summary>Applies one melee hit when a sword hitbox enters an enemy.</summary>
public class SwordEnemyDamage : MonoBehaviour
{
    [Min(1f)] public float enemyDamage = 50f;

    private void OnTriggerEnter(Collider other)
    {
        MosquitoEnemyHealth mechHealth = other.GetComponentInParent<MosquitoEnemyHealth>();
        if (mechHealth != null)
        {
            mechHealth.TakeDamage(enemyDamage);
            return;
        }

        EnemyHealth health = other.GetComponentInParent<EnemyHealth>();
        if (health != null)
            health.TakeDamage(enemyDamage);
    }
}
