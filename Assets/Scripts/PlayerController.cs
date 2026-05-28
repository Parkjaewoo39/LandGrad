using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Move")]
    public float moveSpeed = 10f;

    public float turnSpeed = 200f;

    private float turnInput;

    [Header("Prefab")]
    public TerritoryManager territoryPrefab;

    public TrailManager trailPrefab;

    [HideInInspector]
    public TerritoryManager territoryManager;

    [HideInInspector]
    public TrailManager trailManager;

    private Rigidbody2D rb;

    private bool isOutside = false;

    private bool canHitOwnTrail = false;

    private bool waitingTrailStart = false;

    private Vector2 territoryExitPosition;

    private Vector2 exitPoint;

    private Vector2 enterPoint;

    void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        rb.interpolation =
            RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        SpawnManagers();
    }

    void SpawnManagers()
    {
        territoryManager =
            Instantiate(
                territoryPrefab,
                transform.position,
                Quaternion.identity
            );

        trailManager =
            Instantiate(
                trailPrefab
            );

        territoryManager.player =
            transform;

        trailManager.player =
            transform;

        trailManager.owner =
            this;
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

        Collider2D territoryCollider = other.GetComponent<Collider2D>();

        exitPoint =
            territoryCollider.ClosestPoint(
                transform.position
            );
    }

    void OnTriggerStay2D(
        Collider2D other
    )
    {
        if (
            other.CompareTag("Territory")
        )
        {
            if (!isOutside)
                return;

            isOutside = false;

            waitingTrailStart = false;

            canHitOwnTrail = false;

            Collider2D territoryCollider =
    other.GetComponent<Collider2D>();

            enterPoint =
                territoryCollider.ClosestPoint(
                    transform.position
                );

            territoryManager.BuildNewTerritory(
                trailManager.points,
                exitPoint,
                enterPoint
            );

            trailManager.ClearTrail();

            return;
        }

        TrailManager trail =
            other.GetComponent<TrailManager>();

        if (trail == null)
            return;

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

        if (
            trail.owner != this
        )
        {
            Die();
        }
    }

    void EnableOwnTrailHit()
    {
        canHitOwnTrail = true;
    }

    void Die()
    {
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