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
            // 弾の補充はPlayerの地面状態に入った瞬間に行う
            player_.isGround_ = true;
            Debug.Log("yuka");
            return;
        }
        Debug.Log("yukajanai");
    }
}
