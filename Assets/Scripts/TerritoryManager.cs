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
    public float startRadius = 2.5f;
    public int pointCount = 60; // 시작 시 너무 많은 정점은 성능 저하를 유발 (60 정도면 충분)

    private PolygonCollider2D polygonCollider;
    private MeshFilter meshFilter;
    private Mesh territoryMesh;

    void Awake()
    {
        polygonCollider = GetComponent<PolygonCollider2D>();
        meshFilter = GetComponent<MeshFilter>();

        // 메시 재사용을 위해 한 번만 생성
        territoryMesh = new Mesh();
        territoryMesh.name = "TerritoryMesh";
        meshFilter.mesh = territoryMesh;

        // Territory 오브젝트는 월드 중심(0,0)에 있는 것이 계산에 유리합니다.
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    void Start()
    {
        CreateStartTerritory();
        UpdateVisual();
    }

    void CreateStartTerritory()
    {
        List<Vector2> points = new List<Vector2>();
        // 플레이어의 시작 위치를 기준으로 원형 영토 생성
        Vector2 startPos = (Vector2)player.position;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = Mathf.PI * 2f * i / pointCount;
            Vector2 point = startPos + new Vector2(Mathf.Cos(angle) * startRadius, Mathf.Sin(angle) * startRadius);
            points.Add(point);
        }

        polygonCollider.SetPath(0, points.ToArray());
    }

    public void BuildNewTerritory(List<Vector3> trailPoints, Vector2 exitPoint, Vector2 enterPoint)
    {
        if (trailPoints == null || trailPoints.Count < 2) return;

        // 1. 기존 영토 경로 (World Space)
        PathD currentPath = new PathD();
        foreach (Vector2 p in polygonCollider.GetPath(0))
        {
            currentPath.Add(new PointD(p.x, p.y));
        }

        // 2. 꼬리(Trail) 경로 생성
        // 꼬리는 선이므로 두께를 아주 미세하게 주어 다각형(Polygon)으로 인식시켜야 합집합이 잘 됩니다.
        PathD trailPath = new PathD();
        trailPath.Add(new PointD(exitPoint.x, exitPoint.y));
        foreach (Vector3 tp in trailPoints)
        {
            trailPath.Add(new PointD(tp.x, tp.y));
        }
        trailPath.Add(new PointD(enterPoint.x, enterPoint.y));

        // 3. Clipper2를 이용한 합집합 연산
        // 기존 영토와 플레이어가 방금 그린 꼬리 경로를 합칩니다.
        PathsD subject = new PathsD { currentPath };
        PathsD clip = new PathsD { trailPath };

        // Union 연산: 기존 땅 + 꼬리가 가두고 있는 영역
        PathsD solution = Clipper.Union(subject, clip, FillRule.NonZero);

        // 4. 경로 최적화 및 단순화 (성능 및 데이터 정밀도 조절)
        solution = Clipper.SimplifyPaths(solution, 0.02);

        if (solution.Count > 0)
        {
            // 가장 면적이 큰 다각형을 메인 영토로 선택 (섬 방지)
            PathD largestPath = GetLargestPath(solution);

            List<Vector2> newPoints = new List<Vector2>();
            foreach (PointD pd in largestPath)
            {
                newPoints.Add(new Vector2((float)pd.x, (float)pd.y));
            }

            polygonCollider.SetPath(0, newPoints.ToArray());
            UpdateVisual();
        }
    }

    private PathD GetLargestPath(PathsD paths)
    {
        PathD largest = paths[0];
        double largestArea = System.Math.Abs(Clipper.Area(largest));
        foreach (var path in paths)
        {
            double area = System.Math.Abs(Clipper.Area(path));
            if (area > largestArea)
            {
                largestArea = area;
                largest = path;
            }
        }
        return largest;
    }

    void UpdateVisual()
    {
        Vector2[] points = polygonCollider.GetPath(0);
        if (points.Length < 3) return;

        Tess tess = new Tess();
        ContourVertex[] contour = new ContourVertex[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            contour[i].Position = new Vec3(points[i].x, points[i].y, 0);
        }

        tess.AddContour(contour, ContourOrientation.Original);
        tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

        Vector3[] vertices = new Vector3[tess.Vertices.Length];
        for (int i = 0; i < tess.Vertices.Length; i++)
        {
            vertices[i] = new Vector3(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y, 0);
        }

        int[] triangles = new int[tess.ElementCount * 3];
        for (int i = 0; i < tess.ElementCount; i++)
        {
            triangles[i * 3] = tess.Elements[i * 3];
            triangles[i * 3 + 1] = tess.Elements[i * 3 + 1];
            triangles[i * 3 + 2] = tess.Elements[i * 3 + 2];
        }

        territoryMesh.Clear();
        territoryMesh.vertices = vertices;
        territoryMesh.triangles = triangles;
        territoryMesh.RecalculateBounds();
        territoryMesh.RecalculateNormals();
    }

    public Vector2 GetClosestBoundaryPoint(Vector2 worldPosition)
    {
        Vector2[] points = polygonCollider.GetPath(0);
        Vector2 closestPoint = worldPosition;
        float minDistance = float.PositiveInfinity;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Length];
            Vector2 projected = GetClosestPointOnSegment(a, b, worldPosition);
            float dist = Vector2.Distance(worldPosition, projected);

            if (dist < minDistance)
            {
                minDistance = dist;
                closestPoint = projected;
            }
        }
        return closestPoint;
    }

    private Vector2 GetClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
        return a + ab * Mathf.Clamp01(t);
    }
}