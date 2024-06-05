using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;
using UnityEngine;

public class DelaunayVoronoiLloydRelaxation : MonoBehaviour
{
    [Header("Settings")] [SerializeField] private float width = 64;
    [SerializeField] private float height = 64;

    [Space] [SerializeField] private int pointsCount = 100;

    [Space] [SerializeField] private bool showRegularPoints = true;
    [SerializeField] private bool showDelaunayTriangulation;
    [SerializeField] private bool showVoronoiDiagram;
    [SerializeField] private bool showDelaunaySpanningTree;
    [SerializeField] private bool showLloydRelaxation;
    [SerializeField] private bool noLoop;

    [Space] private Voronoi voronoi;
    private List<Vector2> points;
    private List<Vector2> newPoints;
    private List<LineSegment> edges;
    private List<LineSegment> spanningTree;
    private List<LineSegment> delaunayTriangulation;


    private void Start()
    {
        Setup();
    }

    private void Update()
    {
        // generate new Voronoi diagram
        if (Input.GetKeyDown(KeyCode.Space)) Setup();

        // if mouse button is pressed, get the nearest point index.
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var nearestPointIndex = -1;
            var minDistance = float.MaxValue;
            for (var i = 0; i < points.Count; i++)
            {
                var distance = Vector2.Distance(points[i], mousePos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPointIndex = i;
                }
            }

            // debug.log the nearest point index and position
            Debug.Log("Nearest Point Index: " + nearestPointIndex + " Position: " + points[nearestPointIndex]);
        }

        // Move Lloyd relaxation algorithm to stipplingImage
        if (noLoop) return;
        if (showLloydRelaxation)
        {
            newPoints = new Voronoi(points, new Rect(0, 0, width, height)).LloydRelaxation();
            points = newPoints;
        }

        if (showVoronoiDiagram) edges = new Voronoi(points, new Rect(0, 0, width, height)).VoronoiDiagram();
        if (showDelaunayTriangulation)
            delaunayTriangulation = new Voronoi(points, new Rect(0, 0, width, height)).DelaunayTriangulation();
        spanningTree = DelaunayHelpers.Kruskal(delaunayTriangulation, KruskalType.MAXIMUM);
        noLoop = false;
    }

    private void Setup()
    {
        points = new List<Vector2>();
        for (var i = 0; i < pointsCount; i++) points.Add(new Vector2(Random.Range(0, width), Random.Range(0, height)));

        voronoi = new Voronoi(points, new Rect(0, 0, width, height));
        edges = voronoi.VoronoiDiagram();
        delaunayTriangulation = voronoi.DelaunayTriangulation();
    }


    private void OnDrawGizmos()
    {
        // draw points
        if (points != null && showRegularPoints)
        {
            Gizmos.color = Color.red;
            foreach (var point in points)
                Gizmos.DrawSphere(point, 0.2f);
        }

        // draw Voronoi diagram
        if (showVoronoiDiagram && edges != null)
        {
            Gizmos.color = Color.black;
            foreach (var edge in edges)
            {
                var left = new Vector2(edge.p0.Value.x, edge.p0.Value.y);
                var right = new Vector2(edge.p1.Value.x, edge.p1.Value.y);
                Gizmos.DrawLine(left, right);
            }
        }

        // draw Lloyd relaxation
        if (showLloydRelaxation)
        {
            Gizmos.color = Color.black;
            if (newPoints != null)
                for (var i = 0; i < newPoints.Count; i++)
                    Gizmos.DrawSphere(newPoints[i], 0.2f);
        }

        // draw Delaunay triangulation
        if (delaunayTriangulation != null && showDelaunayTriangulation)
        {
            Gizmos.color = Color.green;
            foreach (var edge in delaunayTriangulation)
            {
                var left = new Vector2(edge.p0.Value.x, edge.p0.Value.y);
                var right = new Vector2(edge.p1.Value.x, edge.p1.Value.y);
                Gizmos.DrawLine(left, right);
            }
        }

        // spanning tree
        if (spanningTree != null && showDelaunaySpanningTree)
        {
            Gizmos.color = Color.red;
            foreach (var edge in spanningTree)
            {
                var left = new Vector2(edge.p0.Value.x, edge.p0.Value.y);
                var right = new Vector2(edge.p1.Value.x, edge.p1.Value.y);
                Gizmos.DrawLine(left, right);
            }
        }

        // Bounding box
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(0, height));
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(width, 0));
        Gizmos.DrawLine(new Vector2(width, 0), new Vector2(width, height));
        Gizmos.DrawLine(new Vector2(0, height), new Vector2(width, height));
    }
}