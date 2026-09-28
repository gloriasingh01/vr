using UnityEngine;

public class TestEnemyMove : MonoBehaviour
{
    public float speed = 2f;

    void Update()
    {
        transform.position += Vector3.forward * speed * Time.deltaTime;
    }
}