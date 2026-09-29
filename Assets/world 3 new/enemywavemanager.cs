using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveManager : MonoBehaviour
{
    [Tooltip("Enemies are activated in this order, two at a time.")]
    public GameObject[] enemies;
    public float activationRange = 100f;

    private readonly List<GameObject> currentPair = new List<GameObject>(2);
    private Transform player;
    private Transform playerCamera;
    private int nextEnemyIndex;
    private int defeatedInPair;
    private bool pairActivated;

    private void Awake()
    {
        activationRange = Mathf.Max(100f, activationRange);
    }

    private void Start()
    {
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health != null)
            player = health.transform;
        if (Camera.main != null)
            playerCamera = Camera.main.transform;

        ResolveEnemyList();
        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogError("WAVE MANAGER: NO SK_ISO_MECH ENEMIES FOUND.", this);
            return;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                enemies[i].SetActive(false);
        }

        BuildNextPair();
        Debug.Log("ENEMY WAVE MANAGER READY | PAIR SIZE = 2 | RANGE = " + activationRange, this);
    }

    private void Update()
    {
        if (player == null && playerCamera == null)
            return;

        if (currentPair.Count == 0 || pairActivated)
            return;

        Vector3 playerPosition = GetPlayerPosition();
        for (int i = 0; i < currentPair.Count; i++)
        {
            GameObject enemy = currentPair[i];
            if (enemy == null)
                continue;

            if (Vector3.Distance(playerPosition, enemy.transform.position) <= activationRange)
            {
                ActivateCurrentPair();
                return;
            }
        }
    }

    private void ResolveEnemyList()
    {
        if (enemies != null && enemies.Length > 0)
            return;

        MosquitoEnemyAI[] found = FindObjectsByType<MosquitoEnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> discovered = new List<GameObject>();
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && !discovered.Contains(found[i].gameObject))
                discovered.Add(found[i].gameObject);
        }

        discovered.Sort(CompareEnemyNames);
        enemies = discovered.ToArray();
    }

    private static int CompareEnemyNames(GameObject a, GameObject b)
    {
        int aNumber = GetSuffixNumber(a != null ? a.name : string.Empty);
        int bNumber = GetSuffixNumber(b != null ? b.name : string.Empty);
        int numberOrder = aNumber.CompareTo(bNumber);
        return numberOrder != 0 ? numberOrder : string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty);
    }

    private static int GetSuffixNumber(string objectName)
    {
        int close = objectName.LastIndexOf(')');
        int open = objectName.LastIndexOf('(');
        if (open >= 0 && close == objectName.Length - 1 && close > open &&
            int.TryParse(objectName.Substring(open + 1, close - open - 1), out int value))
            return value;
        return 0;
    }

    private void BuildNextPair()
    {
        currentPair.Clear();
        defeatedInPair = 0;
        pairActivated = false;

        while (nextEnemyIndex < enemies.Length && currentPair.Count < 2)
        {
            GameObject enemy = enemies[nextEnemyIndex++];
            if (enemy == null)
                continue;

            enemy.SetActive(false);
            currentPair.Add(enemy);
        }

        for (int i = 0; i < currentPair.Count; i++)
        {
            MosquitoEnemyAI ai = currentPair[i].GetComponentInChildren<MosquitoEnemyAI>(true);
            if (ai != null)
                ai.ConfigureForPair(i);
        }

        if (currentPair.Count == 0)
            Debug.Log("ALL ENEMY WAVES DEFEATED.", this);
    }

    private void ActivateCurrentPair()
    {
        pairActivated = true;
        for (int i = 0; i < currentPair.Count; i++)
        {
            GameObject enemy = currentPair[i];
            if (enemy != null)
            {
                enemy.SetActive(true);
                Debug.Log("ENEMY " + (nextEnemyIndex - currentPair.Count + i + 1) + " ENTERED THE FIGHT!", enemy);
            }
        }
    }

    /// <summary>Called by MosquitoEnemyHealth once for each defeated enemy.</summary>
    public void EnemyDefeated()
    {
        if (!pairActivated || currentPair.Count == 0)
            return;

        defeatedInPair++;
        if (defeatedInPair < currentPair.Count)
            return;

        Debug.Log("CURRENT ENEMY PAIR DEFEATED. ACTIVATING NEXT PAIR WHEN IN RANGE.", this);
        BuildNextPair();
    }

    private Vector3 GetPlayerPosition()
    {
        if (playerCamera != null)
            return playerCamera.position;
        return player != null ? player.position : transform.position;
    }
}
