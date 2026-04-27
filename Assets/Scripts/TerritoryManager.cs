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
        Debug.Log("Territory Center:" + territoryCenter);
    }

    void CreateStartTerritory()
    {
        List<Vector2> points = new List<Vector2>();

        for (int i = 0; i < pointCount; i++)
        {
            float angle = i * Mathf.PI * 2 / pointCount;

            float randomRadius = baseRadius + Random.Range(-variance, variance);

            Vector2 point = new Vector2(
                Mathf.Cos(angle) * randomRadius,
                Mathf.Sin(angle) * randomRadius
            );

            points.Add(point);
           
        }

        polygonCollider.SetPath(0, points.ToArray());

        transform.position = player.position;
    }

    // Plugin Clipper2 사용 영역 복귀 시 새 영역 생성
    public void CreateCapturedArea(List<Vector3> trailPoints, Vector2 exitPoint, Vector2 enterPoint)
    {
        if (trailPoints == null || trailPoints.Count < 3)
            return;

        // 기존 Territory 경로 가져오기
        Vector2[] currentPath = polygonCollider.GetPath(0);

        int exitIndex = FindClosestPointIndex(currentPath, exitPoint);

        int enterIndex = FindClosestPointIndex(currentPath, enterPoint);

        List<Vector2> boundaryCW = GetBoundarySegmentCW(currentPath, exitIndex, enterIndex);

        List<Vector2> boundaryCCW = GetBoundarySegmentCCW(currentPath, exitIndex, enterIndex);

        List<Vector2> selectedBoundary = boundaryCW.Count < boundaryCCW.Count ? boundaryCW : boundaryCCW;

        Debug.Log("CW Count: " + boundaryCW.Count);
        Debug.Log("CCW Count: " + boundaryCCW.Count);
        Debug.Log("Selected Count: " + selectedBoundary.Count);


        Debug.Log("ExitPoint:" + exitPoint);
        Debug.Log("EnterPoint:" + enterPoint);

        List<Vector2> boundarySegment = GetBoundarySegment(currentPath, exitIndex, enterIndex);

        Debug.Log("Boundary Count: " + boundarySegment.Count);

        PathD territoryPath = new PathD();

        foreach (Vector2 p in currentPath)
        {
            // local → world 좌표 변환
            Vector2 worldPoint = (Vector2)transform.position + p;

            territoryPath.Add(new PointD(worldPoint.x, worldPoint.y));
        }

        // Trail 경로 만들기
        PathD trailPath = new PathD();

        foreach (Vector3 p in trailPoints)
        {
            trailPath.Add(new PointD(p.x, p.y));
        }

        //안쪽판별 점포인트
        Vector2 insideDir = GetInsideDirection(trailPoints);

        // 마지막 점
        Vector2 lastPoint =
            trailPoints[trailPoints.Count - 1];

        // 안쪽으로 살짝 들어간 보조점
        Vector2 helperPoint =
            lastPoint + insideDir * 2f;
        trailPath.Add(new PointD(helperPoint.x, helperPoint.y));

        Debug.Log("Helper Point: " + helperPoint);




        // PathsD 생성
        PathsD subject = new PathsD();
        subject.AddRange(new[] { territoryPath });

        PathsD clip = new PathsD();
        clip.AddRange(new[] { trailPath });

        // Polygon Union 실행
        PathsD solution = Clipper.Union(subject, clip, FillRule.NonZero);

        if (solution.Count > 0)
        {
            List<Vector2> finalPoints = new List<Vector2>();

            foreach (PointD p in solution[0])
            {
                // world → local 좌표 변환
                finalPoints.Add(
                    new Vector2(
                        (float)p.x - transform.position.x,
                        (float)p.y - transform.position.y
                    )
                );
            }

            // 기존 Territory 자체를 확장
            polygonCollider.SetPath(0, finalPoints.ToArray());

            UpdateVisual();

            territoryCenter = polygonCollider.bounds.center;
            Debug.Log("영역 합치기 완료");
        }
    }

    //Mesh 영역머테리얼
    public void UpdateVisual() 
    {
        Vector2[] points = polygonCollider.GetPath(0);

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[points.Length];
        int[] triangles = new int[(points.Length - 2) * 3];

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

    //영역 중앙 방향
    private Vector2 GetInsideDirection(List<Vector3> trailPoints) 
    {
        if (trailPoints == null || trailPoints.Count == 0)
            return Vector2.zero;

        Vector2 lastTrailPoint = trailPoints[trailPoints.Count - 1];

        Vector2 direction = (territoryCenter - lastTrailPoint).normalized;

        Debug.Log("Last Trail Point:" + lastTrailPoint);
        Debug.Log("Inside Direction:" + direction);

        return direction;
    }

    //영역 시계방향 체크
    private List<Vector2> GetBoundarySegmentCW(Vector2[] polygonPoints, int startIndex, int endIndex)
    {
        List<Vector2> segment = new List<Vector2>();

        int index = startIndex;

        while (index != endIndex)
        {
            Vector2 worldPoint =
                (Vector2)transform.position + polygonPoints[index];

            segment.Add(worldPoint);

            index = (index + 1) % polygonPoints.Length;
        }

        Vector2 finalPoint =
            (Vector2)transform.position + polygonPoints[endIndex];

        segment.Add(finalPoint);

        return segment;
    }

    //영역 반시계방향 체크
    private List<Vector2> GetBoundarySegmentCCW(Vector2[] polygonPoints, int startIndex, int endIndex)
    {
        List<Vector2> segment = new List<Vector2>();

        int index = startIndex;

        while (index != endIndex)
        {
            Vector2 worldPoint =
                (Vector2)transform.position + polygonPoints[index];

            segment.Add(worldPoint);

            index--;

            if (index < 0)
                index = polygonPoints.Length - 1;
        }

        Vector2 finalPoint =
            (Vector2)transform.position + polygonPoints[endIndex];

        segment.Add(finalPoint);

        return segment;
    }

    //영역 닫을때 가까운 점 포인트 찾는 함수
    private int FindClosestPointIndex(Vector2[] points, Vector2 target)
    {
        int closestIndex = 0;
        float minDistance = Mathf.Infinity;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 worldPoint =
                (Vector2)transform.position + points[i];

            float distance =
                Vector2.Distance(worldPoint, target);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    //
    private List<Vector2> GetBoundarySegment( Vector2[] polygonPoints,int startIndex,int endIndex)
    {
        List<Vector2> segment = new List<Vector2>();

        int index = startIndex;

        while (index != endIndex)
        {
            Vector2 worldPoint =
                (Vector2)transform.position + polygonPoints[index];

            segment.Add(worldPoint);

            index = (index + 1) % polygonPoints.Length;
        }

        // 마지막 점도 추가
        Vector2 finalPoint =
            (Vector2)transform.position + polygonPoints[endIndex];

        segment.Add(finalPoint);

        return segment;
    }
}

