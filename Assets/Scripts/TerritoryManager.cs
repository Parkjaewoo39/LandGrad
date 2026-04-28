using Clipper2Lib;
using System.Collections.Generic;
using UnityEngine;

public class TerritoryManager : MonoBehaviour
{
    public Transform player;

    public float baseRadius = 5f;
    public float variance = 0.5f;
    public int pointCount = 30;

    private PolygonCollider2D polygonCollider;
    private MeshFilter meshFilter;
    private Vector2 territoryCenter;

    void Start()
    {
        polygonCollider = GetComponent<PolygonCollider2D>();
        meshFilter = GetComponent<MeshFilter>();

        CreateStartTerritory();
        UpdateVisual();

        territoryCenter = polygonCollider.bounds.center;
        Debug.Log("Start Territory Created");
    }

    /// <summary>
    /// 시작 영역 생성
    /// </summary>
    void CreateStartTerritory()
    {
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < pointCount; i++)
        {
            float angle = i * Mathf.PI * 2f / pointCount;
            float radius = baseRadius + Random.Range(-variance, variance);

            Vector2 point = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );

            points.Add(point);
        }

        polygonCollider.SetPath(0, points.ToArray());

        // 시작 위치를 플레이어 기준으로
        transform.position = player.position;
    }

    /// <summary>
    /// 핵심 방식 변경:
    /// Trail 자체를 하나의 닫힌 Polygon으로 만들고
    /// 기존 Territory와 Union
    /// </summary>
    public void CreateCapturedArea(
        List<Vector3> trailPoints,
        Vector2 exitPoint,
        Vector2 enterPoint)
    {
        if (trailPoints == null || trailPoints.Count < 2)
        {
            Debug.LogWarning("Trail 부족");
            return;
        }

        Vector2[] currentPath = polygonCollider.GetPath(0);

        if (currentPath == null || currentPath.Length < 3)
        {
            Debug.LogWarning("기존 Territory 이상");
            return;
        }

        //--------------------------------------------------
        // 1. 기존 Territory → World 좌표
        //--------------------------------------------------

        PathD territoryPath = new PathD();

        foreach (Vector2 p in currentPath)
        {
            Vector2 worldPoint =
                (Vector2)transform.position + p;

            territoryPath.Add(
                new PointD(worldPoint.x, worldPoint.y)
            );
        }

        //--------------------------------------------------
        // 2. 새로 그린 영역 (Trail Polygon)
        //
        // Exit
        // → Trail
        // → Enter
        // → Exit 로 닫기
        //--------------------------------------------------

        PathD newCapturedPath = new PathD();

        // 시작점
        newCapturedPath.Add(
            new PointD(exitPoint.x, exitPoint.y)
        );

        // Trail 전체
        foreach (Vector3 p in trailPoints)
        {
            newCapturedPath.Add(
                new PointD(p.x, p.y)
            );
        }

        // 끝점
        newCapturedPath.Add(
            new PointD(enterPoint.x, enterPoint.y)
        );

        // 닫기
        newCapturedPath.Add(
            new PointD(exitPoint.x, exitPoint.y)
        );

        //--------------------------------------------------
        // 3. Union
        //--------------------------------------------------

        PathsD subject = new PathsD();
        subject.Add(territoryPath);

        PathsD clip = new PathsD();
        clip.Add(newCapturedPath);

        PathsD solution =
            Clipper.Union(subject, clip, FillRule.NonZero);

        solution =
            Clipper.SimplifyPaths(solution, 0.01);

        if (solution == null || solution.Count == 0)
        {
            Debug.LogWarning("Union 실패");
            return;
        }

        //--------------------------------------------------
        // 4. 가장 큰 Polygon만 선택
        //
        // 분리된 조각 제거
        //--------------------------------------------------

        PathD bestPath = solution[0];
        double bestArea =
            System.Math.Abs(Clipper.Area(bestPath));

        for (int i = 1; i < solution.Count; i++)
        {
            double area =
                System.Math.Abs(Clipper.Area(solution[i]));

            if (area > bestArea)
            {
                bestArea = area;
                bestPath = solution[i];
            }
        }

        if (bestPath.Count < 3)
        {
            Debug.LogWarning("최종 polygon 부족");
            return;
        }

        //--------------------------------------------------
        // 5. World → Local 변환 후 적용
        //--------------------------------------------------

        List<Vector2> finalPoints =
            new List<Vector2>();

        foreach (PointD p in bestPath)
        {
            finalPoints.Add(
                new Vector2(
                    (float)p.x - transform.position.x,
                    (float)p.y - transform.position.y
                )
            );
        }

        polygonCollider.SetPath(
            0,
            finalPoints.ToArray()
        );

        UpdateVisual();

        territoryCenter =
            polygonCollider.bounds.center;

        Debug.Log("영역 합치기 완료");
        Debug.Log("Trail Count : " + trailPoints.Count);
        Debug.Log("Final Count : " + finalPoints.Count);
    }

    /// <summary>
    /// Mesh 갱신
    /// </summary>
    public void UpdateVisual()
    {
        Vector2[] points = polygonCollider.GetPath(0);

        if (points == null || points.Length < 3)
            return;

        Mesh mesh = new Mesh();

        Vector3[] vertices =
            new Vector3[points.Length];

        int[] triangles =
            new int[(points.Length - 2) * 3];

        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] = points[i];
        }

        for (int i = 0; i < points.Length - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }
}