using UnityEngine;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using static Unity.Mathematics.math;

public class WeightedVoronoiStippling : MonoBehaviour
{
    [Header("Settings")]
    [Space]
    [SerializeField]
    private Texture2D stipplingImage;
    private Camera cam;

    [SerializeField] private int initialPointsCount = 6000;

    [Header("Enable/Disable")]
    [SerializeField]
    private bool showRegularPoints;

    [SerializeField] private bool showVoronoiDiagram;

    [Space]
    [SerializeField] private float lerpFactor = 0.1f;
    [SerializeField] private int UpdateInterval = 10;

    private float width;
    private float height;
    private Voronoi voronoi;
    private List<Vector2> points;
    private List<LineSegment> edges;
    private int frameCounter = 0;

    private void Awake()
    {
        cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = stipplingImage.height / 2;
        cam.transform.position = new Vector3(stipplingImage.width / 2, stipplingImage.height / 2, -10);
        width = stipplingImage.width;
        height = stipplingImage.height;
    }

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
        for (var i = 0; i < count; i++)
        {
            var x = Random.Range(0, width);
            var y = Random.Range(0, height);
            var pixelColor = stipplingImage.GetPixel((int)x, (int)y);
            var brightness = pixelColor.grayscale;
            if (Random.Range(0f, 1f) > brightness)
                points.Add(new Vector2(x, y));
            else
                i--;
        }
    }

    private void CalculateVoronoiDiagram()
    {
        voronoi = new Voronoi(points, new Rect(0, 0, width, height));
        edges = voronoi.VoronoiDiagram();
    }

    private void CalculateCentroidsAndUpdatePoints()
    {
        NativeArray<float2> nativePoints = new NativeArray<float2>(points.Count, Allocator.TempJob);
        for (int i = 0; i < points.Count; i++)
        {
            nativePoints[i] = new float2(points[i].x, points[i].y);
        }

        NativeArray<float2> centroids = new NativeArray<float2>(points.Count, Allocator.TempJob);
        NativeArray<float> weights = new NativeArray<float>(points.Count, Allocator.TempJob);
        NativeArray<float2> newPoints = new NativeArray<float2>(points.Count, Allocator.TempJob); // Separate array for new points

        CalculateCentroidsJob centroidsJob = new CalculateCentroidsJob
        {
            width = (int)width,
            height = (int)height,
            stipplingImage = stipplingImage.GetRawTextureData<Color32>(),
            points = nativePoints,
            centroids = centroids,
            weights = weights
        };

        JobHandle centroidsHandle = centroidsJob.Schedule();

        UpdatePointsJob updatePointsJob = new UpdatePointsJob
        {
            lerpFactor = lerpFactor,
            points = nativePoints,
            centroids = centroids,
            weights = weights,
            newPoints = newPoints
        };

        JobHandle updatePointsHandle = updatePointsJob.Schedule(centroidsHandle);
        updatePointsHandle.Complete();

        points.Clear();
        for (int i = 0; i < newPoints.Length; i++)
        {
            points.Add(new Vector2(newPoints[i].x, newPoints[i].y));
        }

        nativePoints.Dispose();
        centroids.Dispose();
        weights.Dispose();
        newPoints.Dispose();
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct CalculateCentroidsJob : IJob
    {
        public int width;
        public int height;
        [ReadOnly] public NativeArray<Color32> stipplingImage;
        [ReadOnly] public NativeArray<float2> points;
        public NativeArray<float2> centroids;
        public NativeArray<float> weights;

        public void Execute()
        {
            for (int i = 0; i < points.Length; i++)
            {
                centroids[i] = new float2(0, 0); // Corrected initialization
                weights[i] = 0f;
            }

            for (int i = 0; i < width; i += 2)
            {
                for (int j = 0; j < height; j += 2)
                {
                    var color = stipplingImage[i + j * width];
                    var brightness = color.r / 255f * 0.299f + color.g / 255f * 0.587f + color.b / 255f * 0.114f;
                    var weight = 1 - brightness;

                    float2 pos = new float2(i, j);
                    float minDistance = math.distancesq(points[0], pos);
                    int nearestIndex = 0;

                    for (int k = 1; k < points.Length; k++)
                    {
                        float dist = math.distancesq(points[k], pos);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearestIndex = k;
                        }
                    }

                    centroids[nearestIndex] += pos * weight;
                    weights[nearestIndex] += weight;
                }
            }
        }
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct UpdatePointsJob : IJob
    {
        public float lerpFactor;
        [ReadOnly] public NativeArray<float2> points;
        [ReadOnly] public NativeArray<float2> centroids;
        [ReadOnly] public NativeArray<float> weights;
        public NativeArray<float2> newPoints; // Separate array for new points

        public void Execute()
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (weights[i] > 0)
                {
                    float2 centroid = centroids[i] / weights[i];
                    newPoints[i] = lerp(points[i], centroid, lerpFactor);
                }
                else
                {
                    newPoints[i] = points[i];
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (points != null && showRegularPoints)
        {
            Gizmos.color = Color.black;
            foreach (var point in points)
                Gizmos.DrawSphere(point, 0.2f);
        }

        if (showVoronoiDiagram && edges != null)
        {
            Gizmos.color = Color.black;
            foreach (var edge in edges)
            {
                var left = edge.p0.Value;
                var right = edge.p1.Value;
                Gizmos.DrawLine(left, right);
            }
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(0, height));
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(width, 0));
        Gizmos.DrawLine(new Vector2(width, 0), new Vector2(width, height));
        Gizmos.DrawLine(new Vector2(0, height), new Vector2(width, height));
    }
}
