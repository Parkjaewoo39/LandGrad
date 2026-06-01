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

    public override void Spawned()
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

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Territory"))
        {
            if (!isOutside) return;

            isOutside = false;
            waitingTrailStart = false;
            canHitOwnTrail = false;

            Collider2D territoryCollider = other.GetComponent<Collider2D>();

            // 1. 정확한 진입점 계산
            enterPoint = territoryCollider.ClosestPoint(transform.position);

            // 2. [핵심] 빈 공간 제거를 위한 보정 작업 (Padding)
            if (trailManager.points.Count >= 2)
            {
                // 나갈 때: 첫 번째 트레일 점 방향을 구해서 exitPoint를 영토 안쪽으로 살짝 밀어 넣음
                Vector2 exitDir = ((Vector2)trailManager.points[0] - exitPoint).normalized;
                exitPoint = exitPoint - exitDir * 0.1f; // 0.1m 만큼 안쪽으로 연장

                // 들어올 때: 마지막 트레일 점에서 enterPoint로 향하는 방향을 구함
                Vector2 enterDir = (enterPoint - (Vector2)trailManager.points[trailManager.points.Count - 1]).normalized;

                // 플레이어가 이동하던 방향 그대로 영토 안쪽으로 트레일 포인트를 강제로 하나 더 추가
                Vector3 extraEnterPoint = (Vector3)(enterPoint + enterDir * 0.2f);
                trailManager.points.Add(extraEnterPoint);

                // enterPoint 자체도 영토 안쪽으로 살짝 밀어 넣음
                enterPoint = enterPoint + enterDir * 0.1f;
            }

            // 3. 보정된 포인트들로 영토 빌드 요청
            territoryManager.BuildNewTerritory(
                trailManager.points,
                exitPoint,
                enterPoint
            );

            trailManager.ClearTrail();
            return;
        }

        // ... 기존 trail 충돌(Die) 로직은 그대로 유지 ...
        TrailManager trail = other.GetComponent<TrailManager>();
        if (trail == null)
            return;
        if (trail.owner == this && isOutside && canHitOwnTrail) 
        { 
            Die(); return;
        }
        if (trail.owner != this)
        { 
            trail.owner.KillPlayer();
            return;
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
        Vector2 center =
    territoryManager
    .GetComponent<PolygonCollider2D>()
    .bounds.center;

        Vector2 inwardDir =
            (center - respawnPosition).normalized;

        respawnPosition += inwardDir * 0.3f;
        rb.linearVelocity =
            Vector2.zero;

        rb.angularVelocity = 0f;

        rb.position =
            respawnPosition;

        transform.position =
            respawnPosition;
    }

    void KillPlayer() 
    {
        Die();
    }
}