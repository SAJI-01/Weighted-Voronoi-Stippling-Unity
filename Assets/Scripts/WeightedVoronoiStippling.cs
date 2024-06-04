using UnityEngine;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;

public class WeightedVoronoiStippling : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Texture2D stipplingImage;
    
    [Space]
    [SerializeField] private int initialPointsCount = 6000;
    
    [Space]
    [SerializeField] private bool showRegularPoints;
    [SerializeField] private bool showVoronoiDiagram;
    
    [Space]
    [SerializeField] private float lerpFactor = 0.1f;
    [SerializeField] private float pointSizeGizmos = 0.2f;
    
    
    private Voronoi           voronoi;
    private List<Vector2>     points;
    private List<LineSegment> edges;
    private const int         UpdateInterval = 10;
    private int               frameCounter;
    

    private void Start()
    {
        GenerateRandomPoints(initialPointsCount);
        CalculateVoronoiDiagram();
    }

    private void Update()
    {
        frameCounter++;
        if (frameCounter >= UpdateInterval)
        {
            CalculateCentroidsAndUpdatePoints();
            CalculateVoronoiDiagram();
            frameCounter = 0;
        }
    }

    private void GenerateRandomPoints(int count)
    {
        points = new List<Vector2>();
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(0, stipplingImage.width);
            float y = Random.Range(0, stipplingImage.height);
            Color pixelColor = stipplingImage.GetPixel((int)x, (int)y);
            float brightness = pixelColor.grayscale;
            if (Random.Range(0f, 1f) > brightness)
            {
                points.Add(new Vector2(x, y));
            }
            else
            {
                i--;
            }
        }
    }

    private void CalculateVoronoiDiagram()
    {
        voronoi = new Voronoi(points, new Rect(0, 0, stipplingImage.width, stipplingImage.height));
        edges = voronoi.VoronoiDiagram();
    }

    private void CalculateCentroidsAndUpdatePoints()
    {
        Dictionary<Vector2, Vector2> centroids = new Dictionary<Vector2, Vector2>();
        Dictionary<Vector2, float> weights = new Dictionary<Vector2, float>();

        foreach (var point in points)
        {
            centroids[point] = Vector2.zero;
            weights[point] = 0f;
        }

        for (int i = 0; i < stipplingImage.width; i += 2)
        {
            for (int j = 0; j < stipplingImage.height; j += 2)
            {
                Color pixelColor = stipplingImage.GetPixel(i, j);
                float brightness = pixelColor.grayscale;
                float weight = 1 - brightness;

                Vector2 nearestPoint = GetNearestPoint(new Vector2(i, j));
                centroids[nearestPoint] += new Vector2(i, j) * weight;
                weights[nearestPoint] += weight;
            }
        }

        List<Vector2> newPoints = new List<Vector2>();
        foreach (var point in points)
        {
            if (weights[point] > 0)
            {
                Vector2 centroid = centroids[point] / weights[point];
                newPoints.Add(Vector2.Lerp(point, centroid, lerpFactor));
            }
            else
            {
                newPoints.Add(point);
            }
        }
        points = newPoints;
    }

    private Vector2 GetNearestPoint(Vector2 pos)
    {
        Vector2 nearestPoint = points[0];
        float minDistance = Vector2.Distance(pos, points[0]);
        foreach (var point in points)
        {
            float distance = Vector2.Distance(pos, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = point;
            }
        }
        return nearestPoint;
    }

    private void OnDrawGizmos()
    {
        if (points != null && showRegularPoints)
        {
            Gizmos.color = Color.black;
            foreach (var point in points)
                Gizmos.DrawSphere(point, pointSizeGizmos);
        }

        if (showVoronoiDiagram && edges != null)
        {
            Gizmos.color = Color.black;
            foreach (var edge in edges)
            {
                Vector2 left = edge.p0.Value;
                Vector2 right = edge.p1.Value;
                Gizmos.DrawLine(left, right);
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(0, stipplingImage.height));
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(stipplingImage.width, 0));
        Gizmos.DrawLine(new Vector2(stipplingImage.width, 0), new Vector2(stipplingImage.width, stipplingImage.height));
        Gizmos.DrawLine(new Vector2(0, stipplingImage.height), new Vector2(stipplingImage.width, stipplingImage.height));
    }
}
