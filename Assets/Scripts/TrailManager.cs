using System.Collections.Generic;
using UnityEngine;
using Clipper2Lib; //폴리곤 콜라이더 합치는 플러그인

public class TrailManager : MonoBehaviour
{
    public Transform player;
    public float minDistance = 0.2f;

    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;

    public List<Vector3> points = new List<Vector3>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();

        AddPoint();
    }

    void Update()
    {
        if (Vector3.Distance(player.position, points[points.Count - 1]) > minDistance)
        {
            AddPoint();
        }
    }

    //LineRenderer.positionCount에 더하는 함수
    void AddPoint()
    {
        Vector3 newPoint = player.position;

        points.Add(newPoint);

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());

        UpdateCollider();
    }

    //지나간 position의 좌표를 배열형태로 저장.
    void UpdateCollider()
    {
        Vector2[] colliderPoints = new Vector2[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            colliderPoints[i] = new Vector2(points[i].x, points[i].y);
        }

        edgeCollider.points = colliderPoints;
    }

    //영역 합치기 함수
    public void CreateCapturedArea(List<Vector3> trailPoints)
    {
        if (trailPoints.Count < 3)
            return;
       
        GameObject newArea = new GameObject("CapturedArea");

        PolygonCollider2D poly = newArea.AddComponent<PolygonCollider2D>();

        List<Vector2> polygonPoints = new List<Vector2>();

        foreach (Vector3 point in trailPoints)
        {
            polygonPoints.Add(new Vector2(point.x, point.y));
        }

        poly.SetPath(0, polygonPoints.ToArray());

        Debug.Log("새 영역 생성 완료");
    }

    //영역만들면 초기화
    public void ClearTrail() 
    {
        points.Clear();

        lineRenderer.positionCount = 0;

        edgeCollider.points = new Vector2[0];

        AddPoint();
    }
}

