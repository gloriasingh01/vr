using UnityEngine;

public class RobotWaveSpawner : MonoBehaviour
{
    [Header("Robots - Put them in order")]
    public GameObject[] robots;

    [Header("Spawn Settings")]
    public float interval = 1f;
    public bool startAutomatically = true;

    private int nextRobotIndex = 0;
    private float timer;
    private bool started;

    private void Start()
    {
        if (robots == null || robots.Length == 0)
        {
            Debug.LogError("ROBOT WAVE: No robots assigned!");
            return;
        }

        // Start with all robots disabled.
        for (int i = 0; i < robots.Length; i++)
        {
            if (robots[i] != null)
                robots[i].SetActive(false);
        }

        timer = 0f;
        nextRobotIndex = 0;
        started = startAutomatically;

        if (started)
            ActivateNextRobot();
    }

    private void Update()
    {
        if (!started)
            return;

        if (nextRobotIndex >= robots.Length)
            return;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;
            ActivateNextRobot();
        }
    }

    private void ActivateNextRobot()
    {
        if (nextRobotIndex >= robots.Length)
            return;

        GameObject robot = robots[nextRobotIndex];

        if (robot != null)
        {
            robot.SetActive(true);

            Debug.Log(
                "ROBOT WAVE: Activated " +
                robot.name +
                " (" +
                (nextRobotIndex + 1) +
                "/" +
                robots.Length +
                ")"
            );
        }

        nextRobotIndex++;
    }
}