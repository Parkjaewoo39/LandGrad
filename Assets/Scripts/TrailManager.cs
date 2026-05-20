// ========================================
// TrailManager.cs
// 안정화 버전
// ========================================

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrailManager : MonoBehaviour
{
    public Transform player;

    [Header("Trail")]
    public float minDistance = 0.05f;

    public float startOffset = 0.08f;

    private LineRenderer lineRenderer;

    public List<Vector3> points =
        new List<Vector3>();

    void Awake()
    {
        lineRenderer =
            GetComponent<LineRenderer>();

        lineRenderer.positionCount = 0;

        // 곡선 부드럽게
        lineRenderer.numCornerVertices = 8;
        lineRenderer.numCapVertices = 8;

        lineRenderer.useWorldSpace = true;

        lineRenderer.loop = false;
    }

    void Update()
    {
        if (lineRenderer == null)
            return;

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

        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = 0;

        // =========================
        // 시작 꺾임 방지 핵심
        // 플레이어 진행방향 반대로
        // 아주 조금 뒤로 뺌
        // =========================

        Vector2 direction =
            -player.up;

        Vector2 fixedStart =
            startPoint
            + direction * startOffset;

        // 첫 점 2개 동일하게
        points.Add(fixedStart);
        points.Add(fixedStart);

        lineRenderer.positionCount =
            points.Count;

        lineRenderer.SetPositions(
            points.ToArray()
        );
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
                    points[points.Count - 1]
                );

            // 너무 가까우면 무시
            if (dist < 0.01f)
            {
                return;
            }
        }

        // =========================
        // 시작 직후 꺾임 제거
        // =========================

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
    }
}