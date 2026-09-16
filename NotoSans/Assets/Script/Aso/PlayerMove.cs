using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // プレイヤーの移動速度
    public static readonly Vector3 k_MoveSpeed = new Vector3(0.0f, 0.01f, 0.0f);

    [SerializeField] GameObject _playerObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        _playerObj.transform.position += k_MoveSpeed;
    }
}
