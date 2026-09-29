using UnityEngine;

public class FinalBossHealth : MonoBehaviour
{
    [Header("Boss Health")]
    public float maxHealth = 500f;

    private float currentHealth;
    private bool isDead;

    private FinalBossController bossController;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        bossController = GetComponent<FinalBossController>();
    }

    private void Start()
    {
        Debug.Log(gameObject.name + " BOSS HP = " + currentHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log(
            gameObject.name +
            " BOSS HIT | DAMAGE = " +
            damage +
            " | HP = " +
            currentHealth
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log(gameObject.name + " BOSS DEFEATED!");

        if (bossController != null)
        {
            bossController.SetDead();
        }
    }
}