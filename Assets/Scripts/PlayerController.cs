using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private float moveSpeed = 10.0f;
    private float turnSpeed = 200.0f;
    private float turnInput;
    private Rigidbody2D rb;

    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();       
    }


    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();

        turnInput = moveInput.x;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = transform.up * moveSpeed;

        rb.MoveRotation(rb.rotation - turnInput * turnSpeed * Time.deltaTime);
    }

   
}
