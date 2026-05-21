// ========================================
// PlayerController.cs
// 안정화 버전
// ========================================

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 10f;

    public float turnSpeed = 200f;

    private float turnInput;

    [Header("References")]
    public TerritoryManager territoryManager;

    public TrailManager trailManager;

    private Rigidbody2D rb;

    private bool isOutside = false;

    private bool canHitOwnTrail = false;

    private bool waitingTrailStart = false;

    private Vector2 territoryExitPosition;

    private Vector2 exitPoint;

    private Vector2 enterPoint;

    // ========================================
    // Awake
    // ========================================

    void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        rb.interpolation =
            RigidbodyInterpolation2D.Interpolate;

        // 자기 owner 등록
        trailManager.owner = this;
    }

    // ========================================
    // Input
    // ========================================

    public void OnMove(
        InputAction.CallbackContext context
    )
    {
        Vector2 input =
            context.ReadValue<Vector2>();

        turnInput = input.x;
    }

    // ========================================
    // Movement
    // ========================================

    void FixedUpdate()
    {
        rb.linearVelocity =
            transform.up * moveSpeed;

        rb.MoveRotation(
            rb.rotation
            - turnInput
            * turnSpeed
            * Time.fixedDeltaTime
        );

        // ====================================
        // 영역 완전히 벗어난 후
        // trail 시작
        // ====================================

        if (waitingTrailStart)
        {
            float dist =
                Vector2.Distance(
                    transform.position,
                    territoryExitPosition
                );

            if (dist >= 0.2f)
            {
                waitingTrailStart = false;

                trailManager.StartTrail(
                    exitPoint
                );

                Invoke(
                    nameof(
                        EnableOwnTrailHit
                    ),
                    0.5f
                );
            }
        }
    }

    // ========================================
    // 영역 나감
    // ========================================

    void OnTriggerExit2D(
        Collider2D other
    )
    {
        if (!other.CompareTag("Territory"))
            return;

        if (isOutside)
            return;

        isOutside = true;

        canHitOwnTrail = false;

        waitingTrailStart = true;

        territoryExitPosition =
            transform.position;

        // 영역 경계 기준 시작점
        exitPoint =
            territoryManager
            .GetClosestBoundaryPoint(
                transform.position
            );

        Debug.Log("영역 밖");
    }

    // ========================================
    // Trigger Stay
    // ========================================

    void OnTriggerStay2D(
        Collider2D other
    )
    {
        // ====================================
        // Territory 복귀
        // ====================================

        if (
            other.CompareTag("Territory")
        )
        {
            if (!isOutside)
                return;

            isOutside = false;

            waitingTrailStart = false;

            canHitOwnTrail = false;

            enterPoint =
                territoryManager
                .GetClosestBoundaryPoint(
                    transform.position
                );

            territoryManager.BuildNewTerritory(
                trailManager.points,
                exitPoint,
                enterPoint
            );

            trailManager.ClearTrail();

            Debug.Log("영역 복귀");

            return;
        }

        // ====================================
        // Trail 충돌
        // ====================================

        TrailManager trail =
            other.GetComponent<TrailManager>();

        if (trail == null)
            return;

        // ====================================
        // 자기 trail 충돌
        // ====================================

        if (
            trail.owner == this
            &&
            isOutside
            &&
            canHitOwnTrail
        )
        {
            Die();

            return;
        }

        // ====================================
        // 상대 trail 충돌
        // ====================================

        if (
            trail.owner != this
        )
        {
            Die();
        }
    }

    // ========================================
    // 자기 선 충돌 활성화
    // ========================================

    void EnableOwnTrailHit()
    {
        canHitOwnTrail = true;
    }

    // ========================================
    // 죽음
    // ========================================

    void Die()
    {
        Debug.Log("죽음");

        CancelInvoke();

        trailManager.ClearTrail();

        isOutside = false;

        waitingTrailStart = false;

        canHitOwnTrail = false;

        Vector2 respawnPosition =
            territoryManager
            .GetClosestBoundaryPoint(
                transform.position
            );

        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity = 0f;

        rb.position =
            respawnPosition;

        transform.position =
            respawnPosition;
    }
}