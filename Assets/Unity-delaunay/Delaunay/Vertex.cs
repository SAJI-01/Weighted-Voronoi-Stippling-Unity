using UnityEngine;
using System.Collections.Generic;

namespace Delaunay
{
	
	public sealed class Vertex: ICoord
	{
		public static readonly Vertex VERTEX_AT_INFINITY = new Vertex (float.NaN, float.NaN);
		
		private static Stack<Vertex> pool = new Stack<Vertex> (); //pool of available vertices how to Find particular vertex location in pool?
		
		private static int nVertices = 0;
		
		private static Vertex Create (float x, float y)
		{
			if (float.IsNaN (x) || float.IsNaN (y)) {
				return VERTEX_AT_INFINITY; 
			}
			if (pool.Count > 0) {
				return pool.Pop ().Init (x, y);
			} else {
				return new Vertex (x, y);
			}
		}


		
		private Vector2 coord;
		public Vector2 Coord
		{
			get => coord;
			set => coord = value;
		}
		
		public float x => coord.x;
		public float y => coord.y;

		private int vertexIndex; 
		public int VertexIndex => vertexIndex;

		public Vertex (float x, float y)
		{
			Init (x, y);
		}
		
		private Vertex Init (float x, float y)
		{
			coord = new Vector2 (x, y);
			return this;
		}
		
		public void Dispose ()
		{
			pool.Push (this);
		}
		
		public void SetIndex ()
		{
			vertexIndex = nVertices++; 
		}
		
		public override string ToString ()
		{
			return "Vertex (" + vertexIndex + ")"; 
		}

		/**
		 * This is the only way to make a Vertex
		 * 
		 * @param halfedge0
		 * @param halfedge1
		 * @return 
		 * 
		 */
		public static Vertex Intersect (Halfedge halfedge0, Halfedge halfedge1) 
		{
			Edge edge0, edge1, edge;
			Halfedge halfedge;
			float determinant, intersectionX, intersectionY;
			bool rightOfSite;
		
			edge0 = halfedge0.edge;
			edge1 = halfedge1.edge;
			if (edge0 == null || edge1 == null) {
				return null;
			}
			if (edge0.rightSite == edge1.rightSite) {
				return null;
			}
		
			determinant = edge0.a * edge1.b - edge0.b * edge1.a;
			if (-1.0e-10 < determinant && determinant < 1.0e-10) {
				// the edges are parallel
				return null;
			}
		
			intersectionX = (edge0.c * edge1.b - edge1.c * edge0.b) / determinant;
			intersectionY = (edge1.c * edge0.a - edge0.c * edge1.a) / determinant;
		
			if (Voronoi.CompareByYThenX (edge0.rightSite, edge1.rightSite) < 0) {
				halfedge = halfedge0;
				edge = edge0;
			} else {
				halfedge = halfedge1;
				edge = edge1;
			}
			rightOfSite = intersectionX >= edge.rightSite.x;
			if ((rightOfSite && halfedge.leftRight == LR.LEFT)
				|| (!rightOfSite && halfedge.leftRight == LR.RIGHT)) {
				return null;
			}
		
			return Create (intersectionX, intersectionY);
		}
		

	}
}