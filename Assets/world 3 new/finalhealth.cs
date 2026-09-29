using UnityEngine;

public class MosquitoEnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 70f;
    public float currentHealth;

    private EnemyWaveManager waveManager;
    private bool defeated;
    private GameObject enemyRoot;

    private void Awake()
    {
        // This enemy type always starts at 70 HP, even if an old scene override exists.
        maxHealth = 70f;
        currentHealth = maxHealth;
        MosquitoEnemyAI ai = GetComponentInParent<MosquitoEnemyAI>();
        enemyRoot = ai != null ? ai.gameObject : gameObject;
        waveManager = FindFirstObjectByType<EnemyWaveManager>();
    }

    public void TakeDamage(float damage)
    {
        if (defeated || damage <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log(enemyRoot.name + " HP = " + currentHealth + "/" + maxHealth, this);
        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (defeated)
            return;

        defeated = true;
        Debug.Log(enemyRoot.name + " DEFEATED", this);
        if (waveManager == null)
            waveManager = FindFirstObjectByType<EnemyWaveManager>();
        if (waveManager != null)
            waveManager.EnemyDefeated();

        // Hide immediately, then destroy the complete enemy root at the end of this frame.
        enemyRoot.SetActive(false);
        Destroy(enemyRoot);
    }
}
