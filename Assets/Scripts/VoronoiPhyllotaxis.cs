using System;
using UnityEngine;
using Delaunay;
using Delaunay.Geo;
using System.Collections.Generic;

public class VoronoiPhyllotaxis : MonoBehaviour
{
    public int width = 500;
    public int height = 300;
    public float initialVal = 4;
    public Color color1 = new Color(248f / 255f, 158f / 255f, 79f / 255f);
    public Color color2 = new Color(252f / 255f, 238f / 255f, 33f / 255f);
    public float lerpDuration = 60f;

    private Camera cam;
    private List<Vector2> seedPoints;
    private List<Color> seedColors;
    private List<LineSegment> edges;
    private float val;
    private float frameCount;

    private void Awake()
    {
        cam = Camera.main;
        cam.backgroundColor = new Color(0.847f, 0.506f, 0f);
        cam.orthographicSize = height / 2f;
        cam.transform.position = new Vector3(244, height / 2f, -10f);
    }

    void Start()
    {
        seedPoints = new List<Vector2>();
        seedColors = new List<Color>();
        val = initialVal;
        frameCount = 0;

        // Initial point generation
        for (int i = 0; i < 200; i++)
        {
            GenerateNextPoint();
        }

        CalculateVoronoi();
    }

    void Update()
    {
        frameCount++;
        GenerateNextPoint();
        CalculateVoronoi();
    }

    void GenerateNextPoint()
    {
        float a = frameCount * 137.5f;
        float r = val * Mathf.Sqrt(frameCount);
        float x = r * Mathf.Cos(a * Mathf.Deg2Rad) + width / 2f;
        float y = r * Mathf.Sin(a * Mathf.Deg2Rad) + height / 2f;

        // Only add points within the bounds
        if (x >= 0 && x <= width && y >= 0 && y <= height)
        {
            Vector2 newPoint = new Vector2(x, y);
            Color interpolatedColor = Color.Lerp(color1, color2, (frameCount % lerpDuration) / lerpDuration);

            seedPoints.Add(newPoint);
            seedColors.Add(interpolatedColor);

            val += 0.05f;
        }
    }

    void CalculateVoronoi()
    {
        if (seedPoints.Count < 3) return;

        Voronoi voronoi = new Voronoi(seedPoints, new Rect(0, 0, width, height));
        edges = voronoi.VoronoiDiagram();
    }

    private void OnDrawGizmos()
    {
        if (seedPoints == null || edges == null) return;

        // Draw the points
        for (int i = 0; i < seedPoints.Count; i++)
        {
            Gizmos.color = seedColors[i];
            Gizmos.DrawSphere(seedPoints[i], 1f);
        }

        // Draw the Voronoi edges
        Gizmos.color = Color.black;
        foreach (var edge in edges)
        {
            Vector2 left = edge.p0.Value;
            Vector2 right = edge.p1.Value;

            // Only draw edges within the bounds
            if (IsWithinBounds(left) && IsWithinBounds(right))
            {
                Gizmos.DrawLine(left, right);
            }
        }

        // Draw the bounding box
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(0, height));
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(width, 0));
        Gizmos.DrawLine(new Vector2(width, 0), new Vector2(width, height));
        Gizmos.DrawLine(new Vector2(0, height), new Vector2(width, height));
    }

    private bool IsWithinBounds(Vector2 point)
    {
        return point.x >= 0 && point.x <= width && point.y >= 0 && point.y <= height;
    }
}
