using UnityEngine;

public class CameraMove : MonoBehaviour
{
    private static readonly float k_distance = 3.0f;

    [SerializeField] private GameObject cameraObj_;
    [SerializeField] private GameObject playerObj_;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log(playerObj_.transform.position);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        cameraObj_.transform.position = playerObj_.transform.position + playerObj_.transform.forward * k_distance;
        cameraObj_.transform.LookAt(playerObj_.transform.position);
    }
}
