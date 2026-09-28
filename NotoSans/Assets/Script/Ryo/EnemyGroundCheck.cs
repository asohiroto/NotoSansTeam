using UnityEngine;

public class EnemyGroundCheck : MonoBehaviour
{
    [SerializeField] private Transform checkPoint;

    [SerializeField] private float checkDistance = 0.3f;

    [SerializeField] private LayerMask groundLayer;

    //実行中に結果を確認するため
    [SerializeField] private bool hasGround;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()    {}

    // Update is called once per frame
    void FixedUpdate()
    {
        hasGround = Physics.Raycast(
            checkPoint.position,
            Vector2.down,
            checkDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
            );

        //判定用の線を表示
        Debug.DrawRay(
            checkPoint.position,
            Vector2.down * checkDistance,
            hasGround ? Color.green : Color.red
            );
    }
}
