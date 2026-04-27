using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private float moveSpeed = 10.0f;
    private float turnSpeed = 200.0f;
    private float turnInput;

    private Vector2 exitPoint;
    private Vector2 enterPoint;

    private Rigidbody2D rb;

    public TerritoryManager territoryManager;
    public TrailManager trailManager;

    public bool isInsideTerritory = true;
    public bool isDrawingTrail = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();

        // 좌우 입력만 사용
        turnInput = moveInput.x;
    }

    private void FixedUpdate()
    {
        // 자동 전진
        rb.linearVelocity = transform.up * moveSpeed;

        // 좌우 회전
        rb.MoveRotation(
            rb.rotation - turnInput * turnSpeed * Time.deltaTime
        );
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Territory"))
        {
            isInsideTerritory = false;
            isDrawingTrail = true;

            exitPoint = transform.position;

            Debug.Log("영역 밖으로 나감");
            Debug.Log("Exit Point:" + exitPoint);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Territory"))
        {
            // 밖에 있다가 다시 들어온 경우
            if (isDrawingTrail)
            {
                enterPoint = transform.position;

                Debug.Log("Enter Point:" + enterPoint);

                territoryManager.CreateCapturedArea(trailManager.points, exitPoint, enterPoint);

                trailManager.ClearTrail();

                Debug.Log("영역 복귀 → 새 영역 생성");
            }

            isInsideTerritory = true;
            isDrawingTrail = false;

            
            Debug.Log("영역 안으로 들어옴");
        }
    }
}