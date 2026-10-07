using UnityEngine;

public class Delete : MonoBehaviour
{
    public GameObject Enemy;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))//
        {
            Destroy(Enemy);
        }

    }
}
