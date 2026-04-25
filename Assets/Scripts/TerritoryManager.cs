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

    void Start()
    {

        polygonCollider = GetComponent<PolygonCollider2D>();
        meshFilter = GetComponent<MeshFilter>();

        CreateStartTerritory();
        UpdateVisual();
        
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
    // 영역 복귀 시 새 영역 생성
    public void CreateCapturedArea(List<Vector3> trailPoints)
    {
        if (trailPoints == null || trailPoints.Count < 3)
            return;

        // 기존 Territory 경로 가져오기
        Vector2[] currentPath = polygonCollider.GetPath(0);

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

            Debug.Log("영역 합치기 완료");
        }
    }

    //영역머테리얼
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
}

