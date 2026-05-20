// ========================================
// PlayerController.cs
// ========================================

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 10f;
    public float turnSpeed = 220f;

    [Header("Refs")]
    public TrailManager trailManager;
    public TerritoryManager territoryManager;

    private Rigidbody2D rb;

    private float turnInput;

    private bool isOutside = false;

    private Vector2 exitPoint;
    private Vector2 enterPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.interpolation =
            RigidbodyInterpolation2D.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;
    }

    public void OnMove(
        InputAction.CallbackContext context
    )
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
            rb.rotation
            - turnInput
            * turnSpeed
            * Time.fixedDeltaTime
        );
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Territory"))
            return;

        if (isOutside)
            return;

        isOutside = true;

        exitPoint =
            territoryManager
            .GetClosestBoundaryPoint(
                transform.position
            );

        trailManager.StartTrail(exitPoint);

        Debug.Log("영역 밖");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Territory"))
            return;

        if (!isOutside)
            return;

        isOutside = false;

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
    }
}