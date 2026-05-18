using System.Collections.Generic;
using UnityEngine;

public class TrailManager : MonoBehaviour
{
    public Transform player;

    public float minDistance = 0.2f;

    private LineRenderer lineRenderer;

    public List<Vector3> points =
        new List<Vector3>();

    void Awake()
    {
        lineRenderer =
            GetComponent<LineRenderer>();

        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        if (points.Count == 0)
            return;

        if (
            Vector3.Distance(
                player.position,
                points[points.Count - 1]
            ) > minDistance
        )
        {
            AddPoint();
        }
    }

    public void StartTrail()
    {
        points.Clear();

        lineRenderer.positionCount = 0;

        AddPoint();
    }

    void AddPoint()
    {
        Vector3 point =
            player.position;

        points.Add(point);

        lineRenderer.positionCount =
            points.Count;

        lineRenderer.SetPositions(
            points.ToArray()
        );
    }

    public void ClearTrail()
    {
        points.Clear();

        lineRenderer.positionCount = 0;
    }
}