using System.Collections.Generic;
using UnityEngine;
using Clipper2Lib; //폴리곤 콜라이더 합치는 플러그인

public class TrailManager : MonoBehaviour
{
    public Transform player;
    public float minDistance = 0.2f;

    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;

    public PlayerController playerController;
    public List<Vector3> points = new List<Vector3>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();

        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer 없음");
        }

        if (edgeCollider == null)
        {
            Debug.LogError("EdgeCollider2D 없음");
        }
    }

    void Update()
    {
        // Trail 그리는 중이 아니면 종료
        if (!playerController.isDrawingTrail)
        {
            return;
        }
        // lineRenderer가 이미 제거되었거나 없는 경우 방지
        if (lineRenderer == null)
            return;
        // 첫 시작점이 없으면 최초 1회 추가
        if (points.Count == 0) 
        {
            AddPoint();
            return;
        }

        if (Vector3.Distance(player.position, points[points.Count - 1]) > minDistance)
        {
            AddPoint();
        }
    }

    //LineRenderer.positionCount에 더하는 함수
    public void AddPoint()
    {
        // destroyed object 접근 방지
        if (lineRenderer == null)
            return;

        Vector3 newPoint = player.position;

        // 같은 위치 중복 추가 방지
        if (points.Count > 0)
        {
            if (Vector3.Distance(newPoint, points[points.Count - 1]) < 0.01f)
            {
                return;
            }
        }

        points.Add(newPoint);

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());

        UpdateCollider();
    }

    //지나간 position의 좌표를 배열형태로 저장.EdgeCollider 갱신
    void UpdateCollider()
    {
        if (edgeCollider == null)
            return;

        if (points.Count < 2)
        {
            edgeCollider.points = new Vector2[0];
            return;
        }

        Vector2[] colliderPoints = new Vector2[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            colliderPoints[i] = new Vector2(points[i].x, points[i].y);
        }

        edgeCollider.points = colliderPoints;
    }

    //영역 합치기 함수
    /// <summary>
    /// (예전 테스트용)
    /// 지금은 TerritoryManager 사용하므로 사실상 사용 안 함
    /// </summary>
    public void CreateCapturedArea(List<Vector3> trailPoints)
    {
        if (trailPoints == null || trailPoints.Count < 3)
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

        // destroyed object 방지
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        if (edgeCollider != null)
        {
            edgeCollider.points =
                new Vector2[0];
        }

        Debug.Log("Trail 초기화 완료");

    }
}

