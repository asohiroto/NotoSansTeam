using UnityEngine;

public class Player : MonoBehaviour
{
    static private readonly Vector3 k_firstPosition = new Vector3(0.0f, 10.0f, 0.0f);
    static private readonly Vector3 k_PlayerMinSpeed = new Vector3(0.0f, -0.3f, 0.0f);
    private GameObject player_;

    public Vector3 speed_ = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_ = gameObject;
        player_.transform.position = k_firstPosition;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (speed_.y <= k_PlayerMinSpeed.y)
        {
            speed_.y = k_PlayerMinSpeed.y;
        }
        player_.transform.position += speed_;
    }
}
