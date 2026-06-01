// ========================================
// TrailManager.cs
// 중심선 기반 안정화 버전
// ========================================

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class TrailManager : MonoBehaviour
{
    public Transform player;
    public PlayerController owner;
    [Header("Trail")]
    public float minDistance = 0.05f;

    private LineRenderer lineRenderer;

    private EdgeCollider2D edgeCollider;

    public List<Vector3> points =
        new List<Vector3>();

    void Awake()
    {
        lineRenderer =
            GetComponent<LineRenderer>();

        edgeCollider =
            GetComponent<EdgeCollider2D>();

        edgeCollider.isTrigger = true;

        edgeCollider.edgeRadius = 0.05f;

        lineRenderer.positionCount = 0;

        lineRenderer.useWorldSpace = true;

        lineRenderer.loop = false;

        lineRenderer.numCornerVertices = 8;

        lineRenderer.numCapVertices = 8;
    }

    void FixedUpdate()
    {
        if (points.Count == 0)
            return;

        Vector3 current =
            player.position;

        float distance =
            Vector3.Distance(
                current,
                points[points.Count - 1]
            );

        if (distance >= minDistance)
        {
            AddPoint(current);
        }
    }

    // ========================================
    // Trail 시작
    // ========================================

    public void StartTrail(
        Vector2 startPoint
    )
    {
        points.Clear();

        lineRenderer.positionCount = 0;

        points.Add(startPoint);

        lineRenderer.positionCount =
            points.Count;

        lineRenderer.SetPositions(
            points.ToArray()
        );

        UpdateCollider();
    }

    // ========================================
    // Point 추가
    // ========================================

    void AddPoint(
        Vector3 point
    )
    {
        if (points.Count > 0)
        {
            float dist =
                Vector3.Distance(
                    point,
                    points[
                        points.Count - 1
                    ]
                );

            if (dist < 0.01f)
                return;
        }

        // 시작 꺾임 방지
        if (points.Count == 2)
        {
            Vector3 dir =
                (
                    point
                    - points[1]
                ).normalized;

            points[0] =
                points[1]
                - dir * 0.02f;
        }

        points.Add(point);

        lineRenderer.positionCount =
            points.Count;

        lineRenderer.SetPositions(
            points.ToArray()
        );

        UpdateCollider();
    }

    // ========================================
    // EdgeCollider 업데이트
    // ========================================

    void UpdateCollider()
    {
        if (points.Count < 2)
            return;

        Vector2[] colliderPoints =
            new Vector2[
                points.Count
            ];

        for (
            int i = 0;
            i < points.Count;
            i++
        )
        {
            colliderPoints[i] =
                points[i];
        }

        edgeCollider.points =
            colliderPoints;
    }

    // ========================================
    // Trail 제거
    // ========================================

    public void ClearTrail()
    {
        points.Clear();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        edgeCollider.points =
            new Vector2[0];
    }
}