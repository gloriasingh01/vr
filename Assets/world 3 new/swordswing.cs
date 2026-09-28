using UnityEngine;

public class SwordSwing : MonoBehaviour
{
    public float swingThreshold = 0.5f;

    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;

        Debug.Log(gameObject.name + " SwordSwing STARTED");
    }

    void Update()
    {
        float speed = Vector3.Distance(
            transform.position,
            lastPosition
        ) / Time.deltaTime;

        lastPosition = transform.position;

        if (speed > swingThreshold)
        {
            Debug.Log(
                gameObject.name +
                " SLASH! Speed: " +
                speed.ToString("F2")
            );
        }
    }
}