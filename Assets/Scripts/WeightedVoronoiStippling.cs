using UnityEngine;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;

public class WeightedVoronoiStippling : MonoBehaviour
{
	[SerializeField] private int pointsCount = 100;
	[SerializeField] private bool ShowVoronoiDiagram;
	[SerializeField] private bool ShowLloydRelaxation;

	private List<Vector2> points;
	private List<Vector2> newPoints;
	private List<uint> colors;
	private float width = 64;
	private float height = 64;
	private List<LineSegment> edges = null;

	private void Update()
	{
		// generate new voronoi diagram
		if (Input.anyKeyDown)
		{
			Setup();
		}

		// lloyd relaxation algorithm to improve the voronoi diagram
		if (ShowLloydRelaxation)
		{
			newPoints = new Voronoi(points, colors, new Rect(0, 0, width, height)).RelaxPoints();
			points = newPoints;
			edges = new Voronoi(points, colors, new Rect(0, 0, width, height)).VoronoiDiagram(); // update edges
		}
	}

	private void Start()
	{
		Setup();
	}

	private void Setup()
	{
		colors = new List<uint>();
		points = new List<Vector2>();
		
		for (int i = 0; i < pointsCount; i++)
		{
			colors.Add(0);
			points.Add(new Vector2(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height)));
		}

		var v = new Voronoi(points, colors, new Rect(0, 0, width, height));
		edges = v.VoronoiDiagram();

		// lloyd relaxation
		if (ShowLloydRelaxation)
		{
			newPoints = v.RelaxPoints();
			points = newPoints;
		}
	}
	


	void OnDrawGizmos ()
	{
		// draw voronoi diagram
		if (ShowVoronoiDiagram)
		{
			Gizmos.color = Color.black;
			if (points != null)
			{
				for (int i = 0; i < points.Count; i++)
				{
					Gizmos.DrawSphere(points[i], 0.2f);
				}
			}

			if (edges != null)
			{
				foreach (var edge in edges)
				{
					Vector2 left = new Vector2(edge.p0.Value.x, edge.p0.Value.y);
					Vector2 right = new Vector2(edge.p1.Value.x, edge.p1.Value.y);
					Gizmos.DrawLine(left, right);
				}
			}
		}
		
		// draw lloyd relaxation
		if (ShowLloydRelaxation)
		{
			Gizmos.color = Color.black;
			if (newPoints != null)
			{
				for (int i = 0; i < newPoints.Count; i++)
				{
					Gizmos.DrawSphere(newPoints[i], 0.2f);
				}
			}
		}
		
		//Bounding Box
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine (new Vector2 (0, 0), new Vector2 (0, height));
		Gizmos.DrawLine (new Vector2 (0, 0), new Vector2 (width, 0));
		Gizmos.DrawLine (new Vector2 (width, 0), new Vector2 (width, height));
		Gizmos.DrawLine (new Vector2 (0, height), new Vector2 (width, height));
	}
}