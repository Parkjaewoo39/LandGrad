using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 0, -10);
    public Transform targetPlayer;
    
   
    // Update is called once per frame
    void Update()
    {
        if (targetPlayer.transform.position.z > -11) 
        {
           this.gameObject.transform.position = targetPlayer.transform.position + offset;
        }
        else           
        {
           
        }
       
    }
}
