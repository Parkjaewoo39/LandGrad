using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private float moveSpeed = 1.0f;
    private Vector2 moveInput;
    public void Awake()
    {
        moveInput = Vector2.zero;
    }


    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    //}public void OnMove(InputAction.CallbackContext value)
    //{
    //    moveInput = value.ReadValue<Vector2>();
       
    //}

    void Update()
    {
        Vector3 move = new Vector3(moveInput.x, moveInput.y, 0);
        transform.position += move * moveSpeed * Time.deltaTime;
        Debug.Log("되고있니" + moveInput);
    }

}
