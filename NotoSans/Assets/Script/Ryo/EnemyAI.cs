using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float speed = 1.0f;    //移動速度
    [SerializeField] private int direction = 1;     //移動方向 (1:右 / -1:左)

    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.x = speed * direction;
        rb.linearVelocity = velocity;
    }
}
