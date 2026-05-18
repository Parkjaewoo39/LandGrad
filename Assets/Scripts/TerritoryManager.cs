using Clipper2Lib;
using System.Collections.Generic;
using UnityEngine;

public class TerritoryManager : MonoBehaviour
{
    public Transform player;

    public float baseRadius = 5f;

    public int pointCount = 30;

    private PolygonCollider2D polygonCollider;

    private MeshFilter meshFilter;

    void Awake()
    {
        polygonCollider =
            GetComponent<PolygonCollider2D>();

        meshFilter =
            GetComponent<MeshFilter>();
    }

    void Start()
    {
        CreateStartTerritory();

        UpdateVisual();
    }

    void CreateStartTerritory()
    {
        List<Vector2> points =
            new List<Vector2>();

        for (int i = 0; i < pointCount; i++)
        {
            float angle =
                i * Mathf.PI * 2 / pointCount;

            float randomRadius =
                baseRadius +
                Random.Range(-0.7f, 0.7f);

            Vector2 point =
                new Vector2(
                    Mathf.Cos(angle) * randomRadius,
                    Mathf.Sin(angle) * randomRadius
                );

            points.Add(point);
        }

        polygonCollider.SetPath(
            0,
            points.ToArray()
        );

        transform.position =
            player.position;
    }

    public void CreateCapturedArea(
        List<Vector3> trailPoints,
        Vector2 exitPoint,
        Vector2 enterPoint
    )
    {
        if (trailPoints == null)
            return;

        if (trailPoints.Count < 3)
            return;

        Vector2[] currentPath =
            polygonCollider.GetPath(0);

        int exitIndex =
            FindClosestPointIndex(
                currentPath,
                exitPoint
            );

        int enterIndex =
            FindClosestPointIndex(
                currentPath,
                enterPoint
            );

        List<Vector2> boundaryCW =
            GetBoundarySegmentCW(
                currentPath,
                exitIndex,
                enterIndex
            );

        List<Vector2> boundaryCCW =
            GetBoundarySegmentCCW(
                currentPath,
                exitIndex,
                enterIndex
            );

        List<Vector2> polygonCW =
            BuildCapturePolygon(
                exitPoint,
                enterPoint,
                trailPoints,
                boundaryCW
            );

        List<Vector2> polygonCCW =
            BuildCapturePolygon(
                exitPoint,
                enterPoint,
                trailPoints,
                boundaryCCW
            );

        Vector2 territoryCenter =
            polygonCollider.bounds.center;

        bool cwContainsCenter =
            IsPointInsidePolygon(
                territoryCenter,
                polygonCW
            );

        bool ccwContainsCenter =
            IsPointInsidePolygon(
                territoryCenter,
                polygonCCW
            );

        List<Vector2> selectedPolygon;

        if (cwContainsCenter)
        {
            selectedPolygon = polygonCCW;
        }
        else
        {
            selectedPolygon = polygonCW;
        }

        PathD territoryPath =
            new PathD();

        foreach (Vector2 p in currentPath)
        {
            Vector2 worldPoint =
                (Vector2)transform.position + p;

            territoryPath.Add(
                new PointD(
                    worldPoint.x,
                    worldPoint.y
                )
            );
        }

        PathD capturePath =
            new PathD();

        foreach (Vector2 p in selectedPolygon)
        {
            capturePath.Add(
                new PointD(
                    p.x,
                    p.y
                )
            );
        }

        // polygon 닫기
        if (capturePath.Count > 0)
        {
            capturePath.Add(
                capturePath[0]
            );
        }

        PathsD subject =
            new PathsD();

        subject.Add(territoryPath);

        PathsD clip =
            new PathsD();

        clip.Add(capturePath);

        PathsD solution =
            Clipper.Union(
                subject,
                clip,
                FillRule.NonZero
            );

        solution =
            Clipper.SimplifyPaths(
                solution,
                0.05
            );

        if (solution.Count > 0)
        {
            List<Vector2> finalPoints =
                new List<Vector2>();

            foreach (PointD p in solution[0])
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

            Debug.Log("영역 합치기 완료");
        }
    }

    void UpdateVisual()
    {
        Vector2[] points =
            polygonCollider.GetPath(0);

        Mesh mesh =
            new Mesh();

        Vector3[] vertices =
            new Vector3[points.Length];

        int[] triangles =
            new int[(points.Length - 2) * 3];

        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] =
                points[i];
        }

        for (int i = 0; i < points.Length - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;

        mesh.triangles = triangles;

        mesh.RecalculateBounds();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    private bool IsPointInsidePolygon(
        Vector2 point,
        List<Vector2> polygon
    )
    {
        bool inside = false;

        for (
            int i = 0, j = polygon.Count - 1;
            i < polygon.Count;
            j = i++
        )
        {
            if (
                ((polygon[i].y > point.y) !=
                (polygon[j].y > point.y))
                &&
                (
                    point.x <
                    (polygon[j].x - polygon[i].x)
                    * (point.y - polygon[i].y)
                    / (polygon[j].y - polygon[i].y)
                    + polygon[i].x
                )
            )
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private List<Vector2> BuildCapturePolygon(
        Vector2 exitPoint,
        Vector2 enterPoint,
        List<Vector3> trailPoints,
        List<Vector2> boundary
    )
    {
        List<Vector2> polygon =
            new List<Vector2>();

        polygon.Add(exitPoint);

        foreach (Vector3 p in trailPoints)
        {
            polygon.Add(
                new Vector2(p.x, p.y)
            );
        }

        polygon.Add(enterPoint);

        foreach (Vector2 p in boundary)
        {
            polygon.Add(p);
        }

        return polygon;
    }

    private int FindClosestPointIndex(
        Vector2[] points,
        Vector2 target
    )
    {
        int closestIndex = 0;

        float minDistance =
            Mathf.Infinity;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 worldPoint =
                (Vector2)transform.position
                + points[i];

            float distance =
                Vector2.Distance(
                    worldPoint,
                    target
                );

            if (distance < minDistance)
            {
                minDistance = distance;

                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private List<Vector2> GetBoundarySegmentCW(
        Vector2[] polygonPoints,
        int startIndex,
        int endIndex
    )
    {
        List<Vector2> segment =
            new List<Vector2>();

        int index = startIndex;

        while (index != endIndex)
        {
            Vector2 worldPoint =
                (Vector2)transform.position
                + polygonPoints[index];

            segment.Add(worldPoint);

            index =
                (index + 1)
                % polygonPoints.Length;
        }

        Vector2 finalPoint =
            (Vector2)transform.position
            + polygonPoints[endIndex];

        segment.Add(finalPoint);

        return segment;
    }

    private List<Vector2> GetBoundarySegmentCCW(
        Vector2[] polygonPoints,
        int startIndex,
        int endIndex
    )
    {
        List<Vector2> segment =
            new List<Vector2>();

        int index = startIndex;

        while (index != endIndex)
        {
            Vector2 worldPoint =
                (Vector2)transform.position
                + polygonPoints[index];

            segment.Add(worldPoint);

            index--;

            if (index < 0)
            {
                index =
                    polygonPoints.Length - 1;
            }
        }

        Vector2 finalPoint =
            (Vector2)transform.position
            + polygonPoints[endIndex];

        segment.Add(finalPoint);

        return segment;
    }
}