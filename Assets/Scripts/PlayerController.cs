using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private float moveSpeed = 10f;
    private float turnSpeed = 200f;
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
        turnInput = moveInput.x;
    }

    private void FixedUpdate()
    {
        // 자동 전진
        rb.linearVelocity = transform.up * moveSpeed;

        // 회전
        rb.MoveRotation(
            rb.rotation - turnInput * turnSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 실제 boundary 통과 지점 확보
    /// </summary>
    private Vector2 GetBoundaryPoint()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            -transform.up,   // 진행 반대 방향
            2f
        );

        if (hit.collider != null &&
            hit.collider.CompareTag("Territory"))
        {
            Debug.Log("Raycast Hit : " + hit.point);
            return hit.point;
        }

        Debug.LogWarning("Raycast 실패 → transform.position 사용");
        Debug.DrawRay(transform.position, -transform.up * 2f, Color.red, 2f);
        return transform.position;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Territory"))
            return;

        if (isDrawingTrail)
            return;

        isInsideTerritory = false;
        isDrawingTrail = true;

        // 핵심: 실제 경계점 사용
        exitPoint = GetBoundaryPoint();

        trailManager.ClearTrail();

        // 시작점을 정확히 exitPoint로 넣음
        trailManager.points.Add(exitPoint);
        trailManager.AddPoint();

        Debug.Log("영역 밖으로 나감");
        Debug.Log("Exit Point : " + exitPoint);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Territory"))
            return;

        if (!isDrawingTrail)
            return;

        // 핵심: 실제 경계점 사용
        enterPoint = GetBoundaryPoint();

        // 마지막 점도 정확히 enterPoint
        trailManager.points.Add(enterPoint);

        Debug.Log("영역 안으로 들어옴");
        Debug.Log("Enter Point : " + enterPoint);

        territoryManager.CreateCapturedArea(
            trailManager.points,
            exitPoint,
            enterPoint
        );

        trailManager.ClearTrail();

        isInsideTerritory = true;
        isDrawingTrail = false;

        Debug.Log("영역 확장 완료");
    }
}