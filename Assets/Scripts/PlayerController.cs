using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;

    public float moveSpeed = 10f;
    public float turnSpeed = 200f;

    private float turnInput;

    public bool isDrawingTrail = false;

    private Vector2 exitPoint;
    private Vector2 enterPoint;

    public TrailManager trailManager;
    public TerritoryManager territoryManager;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input =
            context.ReadValue<Vector2>();

        turnInput = input.x;
    }

    void FixedUpdate()
    {
        rb.linearVelocity =
            transform.up * moveSpeed;

        rb.MoveRotation(
            rb.rotation -
            turnInput * turnSpeed * Time.fixedDeltaTime
        );
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Territory"))
            return;

        Debug.Log("영역 밖 나감");

        isDrawingTrail = true;

        exitPoint = transform.position;

        trailManager.StartTrail();

        Debug.Log("Trail 시작");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Territory"))
            return;

        Debug.Log("영역 안 들어옴");

        if (isDrawingTrail)
        {
            Debug.Log("영역 생성 시작");

            enterPoint = transform.position;

            territoryManager.CreateCapturedArea(
                trailManager.points,
                exitPoint,
                enterPoint
            );

            trailManager.ClearTrail();

            isDrawingTrail = false;

            Debug.Log("Trail 종료");
        }
    }
}