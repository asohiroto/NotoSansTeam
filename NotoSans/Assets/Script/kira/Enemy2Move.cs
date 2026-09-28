using JetBrains.Annotations;
using UnityEngine;

public class Enemy2Move: MonoBehaviour
{
    public float EnemyMoveSpeed = 14f;
    public float EnemyDistance = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = transform.position + EnemyMoveSpeed * Time.deltaTime * Vector3.right;
        //bool Raycastcolision = Physics.Raycast(1, 1, 1);
        float checkX = transform.position.x + Mathf.Sign(EnemyMoveSpeed) * EnemyDistance;


        Vector3 checkPos = new Vector3(checkX, transform.position.y, transform.position.z);


        bool isGround = Physics.Raycast(checkPos, Vector3.down, 1);



        if (!isGround)
        {
            EnemyMoveSpeed = -EnemyMoveSpeed;
        }

      
     


    }
}
