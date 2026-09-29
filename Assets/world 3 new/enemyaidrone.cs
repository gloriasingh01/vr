using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneAI : MonoBehaviour
{
    [Header("Detection")]
    [Min(0f)] public float detectionRange = 12f;

    [Header("Flying")]
    [Min(0f)] public float moveSpeed = 2.5f;
    [Min(0f)] public float flyingHeight = 3f;
    [Min(0f)] public float attackDistance = 2.5f;
    [Min(0f)] public float orbitRadius = 0.75f;

    [Header("One at a time")]
    [Min(0.1f)] public float turnDuration = 4.5f;
    [Min(0f)] public float pauseBetweenTurns = 0.8f;

    [Header("Attack")]
    [Min(0f)] public float attackCooldown = 2.5f;
    [Min(0f)] public float damage = 4f;

    private static readonly List<DroneAI> activeDrones = new List<DroneAI>();
    private static float groupEngagementStartTime;
    private static float nextGroupAttackTime;

    private Transform player;
    private PlayerHealth playerHealth;
    private Rigidbody droneBody;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private static bool groupEngaged;

    private void Awake()
    {
        droneBody = GetComponent<Rigidbody>();
        droneBody.useGravity = false;
        homePosition = droneBody.position;
        homeRotation = droneBody.rotation;

        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health == null)
            health = gameObject.AddComponent<EnemyHealth>();
        health.maxHealth = 100f;
    }

    private void OnEnable()
    {
        if (!activeDrones.Contains(this))
            activeDrones.Add(this);
    }

    private void OnDisable()
    {
        activeDrones.Remove(this);
    }

    private void Start()
    {
        RefreshPlayerTarget();
    }

    private void FixedUpdate()
    {
        if (player == null)
            RefreshPlayerTarget();
        if (player == null)
            return;

        RemoveDestroyedDrones();
        if (activeDrones.Count == 0)
            return;

        float nearestDistance = GetNearestDroneDistance();
        if (!groupEngaged && nearestDistance <= detectionRange)
        {
            groupEngaged = true;
            groupEngagementStartTime = Time.time;
        }
        else if (groupEngaged && nearestDistance > detectionRange * 1.15f)
        {
            groupEngaged = false;
        }

        float cycleLength = turnDuration + pauseBetweenTurns;
        float elapsed = groupEngaged ? Time.time - groupEngagementStartTime : 0f;
        int turnNumber = groupEngaged ? Mathf.FloorToInt(elapsed / cycleLength) : -1;
        bool attackWindow = groupEngaged && elapsed % cycleLength < turnDuration;
        int activeIndex = attackWindow ? turnNumber % activeDrones.Count : -1;
        int myIndex = activeDrones.IndexOf(this);

        if (myIndex == activeIndex)
            ApproachAndAttack(elapsed);
        else
            ReturnToHover(elapsed, myIndex);
    }

    private void RefreshPlayerTarget()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            player = mainCamera.transform;
    }

    private static void RemoveDestroyedDrones()
    {
        for (int i = activeDrones.Count - 1; i >= 0; i--)
        {
            if (activeDrones[i] == null)
                activeDrones.RemoveAt(i);
        }
    }

    private float GetNearestDroneDistance()
    {
        float nearest = float.MaxValue;
        foreach (DroneAI drone in activeDrones)
        {
            if (drone != null && drone.player != null)
                nearest = Mathf.Min(nearest, Vector3.Distance(drone.droneBody.position, player.position));
        }
        return nearest;
    }

    private void ApproachAndAttack(float elapsed)
    {
        Vector3 towardPlayer = player.position - droneBody.position;
        towardPlayer.y = 0f;
        if (towardPlayer.sqrMagnitude < 0.01f)
            towardPlayer = Vector3.forward;
        towardPlayer.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, towardPlayer).normalized;
        float buzzPhase = elapsed * 3.2f;
        Vector3 orbit = side * Mathf.Sin(buzzPhase) * orbitRadius;
        orbit += towardPlayer * Mathf.Cos(buzzPhase * 0.7f) * (orbitRadius * 0.35f);
        Vector3 targetPosition = player.position + Vector3.up * flyingHeight + orbit;
        droneBody.MovePosition(Vector3.MoveTowards(
            droneBody.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime));

        FacePlayer();

        if (Vector3.Distance(droneBody.position, targetPosition) <= attackDistance && Time.time >= nextGroupAttackTime)
        {
            nextGroupAttackTime = Time.time + attackCooldown;
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }
    }

    private void ReturnToHover(float elapsed, int index)
    {
        float bob = Mathf.Sin(Time.time * 2.2f + Mathf.Max(0, index) * 1.7f) * 0.12f;
        droneBody.MovePosition(Vector3.MoveTowards(
            droneBody.position,
            homePosition + Vector3.up * bob,
            moveSpeed * Time.fixedDeltaTime));
        droneBody.MoveRotation(Quaternion.RotateTowards(
            droneBody.rotation,
            homeRotation,
            90f * Time.fixedDeltaTime));
    }

    private void FacePlayer()
    {
        Vector3 lookDirection = player.position - droneBody.position;
        if (lookDirection.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        droneBody.MoveRotation(Quaternion.RotateTowards(
            droneBody.rotation,
            targetRotation,
            360f * Time.fixedDeltaTime));
    }
}
