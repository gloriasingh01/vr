using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    private float currentHealth;

    public bool IsDead => currentHealth <= 0f;

    [Header("UI")]
    public Image healthBarFill;

    void Start()
    {
        currentHealth = maxHealth;

        UpdateHealthBar();

        Debug.Log("PLAYER HP = " + currentHealth);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        currentHealth =
            Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log(
            "PLAYER TOOK DAMAGE: " +
            damage +
            " | HP = " +
            currentHealth
        );

        UpdateHealthBar();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount =
                currentHealth / maxHealth;
        }
    }

    void Die()
    {
        Debug.Log("PLAYER DEFEATED");
    }
}
