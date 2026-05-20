// ========================================
// TerritoryManager.cs
// 최종 안정화 버전
// boundary segment 방식 + 큰 polygon 선택
// ========================================

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
    public float radius = 2f;

    public int pointCount = 10;

    public float noise = 0.15f;

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

    // ========================================
    // 시작 영역
    // ========================================

    void CreateStartTerritory()
    {
        List<Vector2> points =
            new List<Vector2>();

        for (int i = 0; i < pointCount; i++)
        {
            float angle =
                Mathf.PI * 2f
                * i
                / pointCount;

            float r =
                radius
                + Random.Range(
                    -noise,
                    noise
                );

            Vector2 point =
                new Vector2(
                    Mathf.Cos(angle) * r,
                    Mathf.Sin(angle) * r
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

    // ========================================
    // 영역 생성
    // ========================================

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

        int exitIndex =
            FindClosestIndex(
                currentPath,
                exitPoint
            );

        int enterIndex =
            FindClosestIndex(
                currentPath,
                enterPoint
            );

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

        polygonA =
            RemoveClosePoints(
                polygonA,
                0.03f
            );

        polygonB =
            RemoveClosePoints(
                polygonB,
                0.03f
            );

        float areaA =
            Mathf.Abs(
                CalculateArea(
                    polygonA
                )
            );

        float areaB =
            Mathf.Abs(
                CalculateArea(
                    polygonB
                )
            );

        List<Vector2> finalPolygon =
            areaA > areaB
            ? polygonA
            : polygonB;

        // ========================================
        // 더 큰 polygon 선택
        // ========================================

        List<Vector2> capturePolygon =
            areaA > areaB
            ? polygonA
            : polygonB;

        // ========================================
        // 현재 territory path
        // ========================================

        PathD territoryPath =
            new PathD();

        foreach (Vector2 p in currentPath)
        {
            Vector2 world =
                (Vector2)transform.position
                + p;

            territoryPath.Add(
                new PointD(
                    world.x,
                    world.y
                )
            );
        }

        // ========================================
        // capture path
        // ========================================

        PathD capturePath =
            new PathD();

        foreach (Vector2 p in capturePolygon)
        {
            capturePath.Add(
                new PointD(
                    p.x,
                    p.y
                )
            );
        }

        // ========================================
        // union
        // ========================================

        PathsD subject =
            new PathsD();

        subject.Add(
            territoryPath
        );

        PathsD clip =
            new PathsD();

        clip.Add(
            capturePath
        );

        PathsD solution =
            Clipper.Union(
                subject,
                clip,
                FillRule.NonZero
            );

        // ========================================
        // simplify
        // ========================================

        solution =
            Clipper.SimplifyPaths(
                solution,
                0.02
            );

        if (solution.Count == 0)
        {
            Debug.LogWarning(
                "Union 실패"
            );

            return;
        }

        // ========================================
        // 가장 큰 polygon 선택
        // ========================================

        PathD largest =
            solution[0];

        double largestArea =
            Mathf.Abs(
                (float)Clipper.Area(
                    largest
                )
            );

        foreach (PathD p in solution)
        {
            double area =
                Mathf.Abs(
                    (float)Clipper.Area(p)
                );

            if (area > largestArea)
            {
                largestArea =
                    area;

                largest = p;
            }
        }

        // ========================================
        // collider 적용
        // ========================================

        List<Vector2> finalPoints =
            new List<Vector2>();

        foreach (PointD p in largest)
        {
            finalPoints.Add(
                new Vector2(
                    (float)p.x
                    - transform.position.x,

                    (float)p.y
                    - transform.position.y
                )
            );
        }

        polygonCollider.SetPath(
            0,
            finalPoints.ToArray()
        );

        UpdateVisual();

        Debug.Log(
            "영역 합치기 완료"
        );
    }

    // ========================================
    // polygon 생성
    // ========================================

    List<Vector2> BuildPolygon(
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
                new Vector2(
                    p.x,
                    p.y
                )
            );
        }

        polygon.Add(enterPoint);

        foreach (Vector2 p in boundary)
        {
            polygon.Add(p);
        }

        return polygon;
    }

    // ========================================
    // 가까운 index
    // ========================================

    int FindClosestIndex(
        Vector2[] polygon,
        Vector2 target
    )
    {
        int closest = 0;

        float minDistance =
            Mathf.Infinity;

        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 worldPoint =
                (Vector2)transform.position
                + polygon[i];

            float dist =
                Vector2.Distance(
                    worldPoint,
                    target
                );

            if (dist < minDistance)
            {
                minDistance = dist;
                closest = i;
            }
        }

        return closest;
    }

    // ========================================
    // 시계방향
    // ========================================

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

    // ========================================
    // 반시계방향
    // ========================================

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

    // ========================================
    // polygon 면적 계산
    // ========================================

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

    // ========================================
    // 가까운 점 제거
    // ========================================

    List<Vector2> RemoveClosePoints(
        List<Vector2> points,
        float minDistance
    )
    {
        List<Vector2> result =
            new List<Vector2>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 current =
                points[i];

            Vector2 prev =
                points[
                    (i - 1 + points.Count)
                    % points.Count
                ];

            if (
                Vector2.Distance(
                    current,
                    prev
                ) > minDistance
            )
            {
                result.Add(current);
            }
        }

        return result;
    }

    // ========================================
    // mesh 생성
    // ========================================
    public Vector2 GetClosestBoundaryPoint(Vector2 worldPoint)
    {
        Vector2[] points =
            polygonCollider.GetPath(0);

        Vector2 closest =
            Vector2.zero;

        float minDistance =
            Mathf.Infinity;

        foreach (Vector2 p in points)
        {
            Vector2 world =
                (Vector2)transform.position + p;

            float dist =
                Vector2.Distance(
                    world,
                    worldPoint
                );

            if (dist < minDistance)
            {
                minDistance = dist;
                closest = world;
            }
        }

        return closest;
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

