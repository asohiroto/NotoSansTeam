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
            player_.isGround_ = true;
            player_.remainAmmunition_ = player_.k_MaxAmmunition;
            Debug.Log("yuka");
            return;
        }
        Debug.Log("yukajanai");
    }
}
