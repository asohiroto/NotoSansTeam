using UnityEngine;

public class PlayerFloorCollision : MonoBehaviour
{
    Player player_;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_ = gameObject.GetComponent<Player>();
        if (player_ != null)
        {
            Debug.Log("karajanai");
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Floor")
        {
            // 接地判定と弾の補充はPlayer側（SweepTest）で行う
            Debug.Log("yuka");
            return;
        }
        Debug.Log("yukajanai");
    }
}
