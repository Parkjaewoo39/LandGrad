using System.Collections.Generic;
using UnityEngine;
using Clipper2Lib;
using LibTessDotNet;

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerritoryManager : MonoBehaviour
{
    public Transform player;

    [Header("Start Territory")]
    public float radius = 2.5f;

    public int pointCount = 240;

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
                Mathf.PI * 2f * i / pointCount;

            Vector2 point =
                new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
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

    public void BuildNewTerritory(
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

        int exitIndex = FindClosestEdgeIndex(currentPath, exitPoint);

        int enterIndex = FindClosestEdgeIndex(currentPath, enterPoint);

        List<Vector2> clockwise =
            GetClockwisePoints(
                currentPath,
                exitIndex,
                enterIndex
            );

        List<Vector2> counterClockwise =
            GetCounterClockwisePoints(
                currentPath,
                exitIndex,
                enterIndex
            );

        List<Vector2> polygonA =
            BuildPolygon(
                exitPoint,
                enterPoint,
                trailPoints,
                clockwise
            );

        List<Vector2> polygonB =
            BuildPolygon(
                exitPoint,
                enterPoint,
                trailPoints,
                counterClockwise
            );

        float areaA =
            Mathf.Abs(
                CalculateArea(polygonA)
            );

        float areaB =
            Mathf.Abs(
                CalculateArea(polygonB)
            );

        List<Vector2> capturePolygon =
            areaA > areaB
            ? polygonA
            : polygonB;

        PathD territoryPath =
            new PathD();

        foreach (Vector2 p in currentPath)
        {
            Vector2 world =
                (Vector2)transform.position + p;

            territoryPath.Add(
                new PointD(world.x, world.y)
            );
        }

        PathD capturePath =
            new PathD();

        foreach (Vector2 p in capturePolygon)
        {
            capturePath.Add(
                new PointD(p.x, p.y)
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
                0.03
            );

        if (solution.Count == 0)
            return;

        PathD largest =
            solution[0];

        double largestArea =
            Mathf.Abs(
                (float)Clipper.Area(largest)
            );

        foreach (PathD p in solution)
        {
            double area =
                Mathf.Abs(
                    (float)Clipper.Area(p)
                );

            if (area > largestArea)
            {
                largestArea = area;
                largest = p;
            }
        }

        List<Vector2> finalPoints =
            new List<Vector2>();

        foreach (PointD p in largest)
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
    }

    List<Vector2> BuildPolygon(
        Vector2 exitPoint,
        Vector2 enterPoint,
        List<Vector3> trailPoints,
        List<Vector2> boundary
    )
    {
        List<Vector2> polygon =
            new List<Vector2>();

        AddPointIfFar(
            polygon,
            exitPoint
        );

        foreach (Vector3 p in trailPoints)
        {
            AddPointIfFar(
                polygon,
                new Vector2(p.x, p.y)
            );
        }

        AddPointIfFar(
            polygon,
            enterPoint
        );

        foreach (Vector2 p in boundary)
        {
            AddPointIfFar(
                polygon,
                p
            );
        }

        RemoveCollinearPoints(
            polygon
        );

        return polygon;
    }

    void AddPointIfFar(
        List<Vector2> points,
        Vector2 point
    )
    {
        if (points.Count == 0)
        {
            points.Add(point);
            return;
        }

        float dist =
            Vector2.Distance(
                points[points.Count - 1],
                point
            );

        if (dist > 0.02f)
        {
            points.Add(point);
        }
    }

    void RemoveCollinearPoints(
        List<Vector2> points
    )
    {
        if (points.Count < 3)
            return;

        for (
            int i = points.Count - 1;
            i >= 0;
            i--
        )
        {
            Vector2 prev =
                points[
                    (i - 1 + points.Count)
                    % points.Count
                ];

            Vector2 current =
                points[i];

            Vector2 next =
                points[
                    (i + 1)
                    % points.Count
                ];

            Vector2 dir1 =
                (current - prev).normalized;

            Vector2 dir2 =
                (next - current).normalized;

            float dot =
                Vector2.Dot(dir1, dir2);

            if (dot > 0.999f)
            {
                points.RemoveAt(i);
            }
        }
    }

    int FindClosestEdgeIndex(Vector2[] polygon, Vector2 target)
    {
        int closestIndex = 0;

        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = (Vector2)transform.position + polygon[i];

            Vector2 b = (Vector2)transform.position + polygon[(i + 1) % polygon.Length];

            Vector2 projected = GetClosestPointOnSegment(a, b, target);

            float dist = (target - projected).sqrMagnitude;

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    List<Vector2> GetClockwisePoints(
        Vector2[] polygon,
        int start,
        int end
    )
    {
        List<Vector2> result =
            new List<Vector2>();

        int index = start;

        while (index != end)
        {
            result.Add(
                (Vector2)transform.position
                + polygon[index]
            );

            index =
                (index + 1)
                % polygon.Length;
        }

        result.Add(
            (Vector2)transform.position
            + polygon[end]
        );

        return result;
    }

    List<Vector2> GetCounterClockwisePoints(
        Vector2[] polygon,
        int start,
        int end
    )
    {
        List<Vector2> result =
            new List<Vector2>();

        int index = start;

        while (index != end)
        {
            result.Add(
                (Vector2)transform.position
                + polygon[index]
            );

            index--;

            if (index < 0)
            {
                index =
                    polygon.Length - 1;
            }
        }

        result.Add(
            (Vector2)transform.position
            + polygon[end]
        );

        return result;
    }

    float CalculateArea(
        List<Vector2> polygon
    )
    {
        float area = 0f;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 current =
                polygon[i];

            Vector2 next =
                polygon[
                    (i + 1)
                    % polygon.Count
                ];

            area +=
                (
                    current.x * next.y
                    - next.x * current.y
                );
        }

        return area * 0.5f;
    }

    public Vector2 GetClosestBoundaryPoint(
        Vector2 worldPosition
    )
    {
        Vector2[] points =
            polygonCollider.GetPath(0);

        Vector2 closestPoint =
            Vector2.zero;

        float closestDistance =
            Mathf.Infinity;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a =
                (Vector2)transform.position
                + points[i];

            Vector2 b =
                (Vector2)transform.position
                + points[
                    (i + 1)
                    % points.Length
                ];

            Vector2 projected =
                GetClosestPointOnSegment(
                    a,
                    b,
                    worldPosition
                );

            float dist =
                (
                    worldPosition
                    - projected
                ).sqrMagnitude;

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestPoint = projected;
            }
        }

        return closestPoint;
    }

    Vector2 GetClosestPointOnSegment(
        Vector2 a,
        Vector2 b,
        Vector2 point
    )
    {
        Vector2 ab = b - a;

        float t =
            Vector2.Dot(
                point - a,
                ab
            ) / ab.sqrMagnitude;

        t = Mathf.Clamp01(t);

        return a + ab * t;
    }

    void UpdateVisual()
    {
        Vector2[] points =
            polygonCollider.GetPath(0);

        if (points.Length < 3)
            return;

        Tess tess =
            new Tess();

        ContourVertex[] contour =
            new ContourVertex[
                points.Length
            ];

        for (int i = 0; i < points.Length; i++)
        {
            contour[i].Position =
                new Vec3(
                    points[i].x,
                    points[i].y,
                    0
                );
        }

        tess.AddContour(
            contour,
            ContourOrientation.Clockwise
        );

        tess.Tessellate(
            WindingRule.NonZero,
            ElementType.Polygons,
            3
        );

        Mesh mesh =
            new Mesh();

        Vector3[] vertices =
            new Vector3[
                tess.Vertices.Length
            ];

        for (int i = 0; i < tess.Vertices.Length; i++)
        {
            vertices[i] =
                new Vector3(
                    tess.Vertices[i].Position.X,
                    tess.Vertices[i].Position.Y,
                    0
                );
        }

        int[] triangles =
            new int[
                tess.ElementCount * 3
            ];

        for (int i = 0; i < tess.ElementCount; i++)
        {
            triangles[i * 3] =
                tess.Elements[i * 3];

            triangles[i * 3 + 1] =
                tess.Elements[i * 3 + 1];

            triangles[i * 3 + 2] =
                tess.Elements[i * 3 + 2];
        }

        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;

        mesh.RecalculateBounds();

        mesh.RecalculateNormals();

        meshFilter.mesh =
            mesh;
    }
}