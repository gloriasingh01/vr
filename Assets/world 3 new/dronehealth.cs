using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;

        Debug.Log(
            gameObject.name + " HP = " + currentHealth
        );
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log(
            gameObject.name +
            " TOOK DAMAGE: " + damage +
            " | HP = " + currentHealth
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(
            gameObject.name + " DEFEATED!"
        );

        Destroy(gameObject);
    }
}