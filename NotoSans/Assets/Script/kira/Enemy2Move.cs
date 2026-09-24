using JetBrains.Annotations;
using UnityEngine;

public class Enemy2Move: MonoBehaviour
{
    public float EnemyMoveSpeed = 100f;
    public float EnemyDistance = 3f;
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



        if (transform.position.x >= 3 || transform.position.x <= -3)
        {
            EnemyMoveSpeed = -EnemyMoveSpeed;
        }
       
    }
}
