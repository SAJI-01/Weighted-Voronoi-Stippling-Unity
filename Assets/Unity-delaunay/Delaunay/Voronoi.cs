using UnityEngine;
using System;
using System.Collections.Generic;
using Delaunay.Geo;


namespace Delaunay
{
	public class Voronoi: Utils.IDisposable
	{
		/*const halfedges = this.halfedges = this._delaunator.halfedges;// Uint32Array.from(d.halfedges);
		const hull = this.hull = this._delaunator.hull;// Uint32Array.from(d.hull);
		const triangles = this.triangles = this._delaunator.triangles;// Uint32Array.from(d.triangles);
		const inedges = this.inedges.fill(-1); // incoming halfedges
		const hullIndex = this._hullIndex.fill(-1); // indices in hull*/ //Convert this to C#
		public SiteList sites;
		private Dictionary <Vector2,Site> sitesIndexedByLocation;
		private List<Triangle> triangles;
		private List<Edge> edges;

		
		// TODO generalize this so it doesn't have to be a rectangle;
		// then we can make the fractal voronois-within-voronois
		private Rect _plotBounds;
		public Rect plotBounds {
			get { return _plotBounds;}
		}
		
		public void Dispose ()
		{
			int i, n;
			if (sites != null) {
				sites.Dispose ();
				sites = null;
			}
			if (triangles != null) {
				n = triangles.Count;
				for (i = 0; i < n; ++i) {
					triangles [i].Dispose ();
				}
				triangles.Clear ();
				triangles = null;
			}
			if (edges != null) {
				n = edges.Count;
				for (i = 0; i < n; ++i) {
					edges [i].Dispose ();
				}
				edges.Clear ();
				edges = null;
			}
//			_plotBounds = null;
			sitesIndexedByLocation = null;
		}
		
		
		public Voronoi (List<Vector2> points, Rect plotBounds)
		{
			sites = new SiteList ();
			sitesIndexedByLocation = new Dictionary <Vector2,Site> (); // XXX: Used to be Dictionary(true) -- weak refs. 
			AddSites (points);
			_plotBounds = plotBounds;
			triangles = new List<Triangle> ();
			edges = new List<Edge> ();
			FortunesAlgorithm ();
		}
		
		private void AddSites (List<Vector2> points)
		{
			int length = points.Count;
			for (int index = 0; index < length; ++index) {
				AddSite (points [index], index);
			}
		}
		
		private void AddSite (Vector2 p, int index)
		{
			if (sitesIndexedByLocation.ContainsKey (p))
				return; // Prevent duplicate site! (Adapted from https://github.com/nodename/as3delaunay/issues/1)
			float weight = UnityEngine.Random.value * 100f;
			Site site = Site.Create (p, (uint)index, weight);
			sites.Add (site);
			sitesIndexedByLocation [p] = site;
		}

		public List<Edge> Edges ()
		{
			return edges;
		}
		
          
		public List<Vector2> Region (Vector2 p)
		{
			Site site = sitesIndexedByLocation [p];
			if (site == null) {
				return new List<Vector2> ();
			}
			return site.Region (_plotBounds);
		}

		// TODO: bug: if you call this before you call region(), something goes wrong :(
		public List<Vector2> NeighborSitesForSite (Vector2 coord)
		{
			List<Vector2> points = new List<Vector2> ();
			Site site = sitesIndexedByLocation [coord];
			if (site == null) {
				return points;
			}
			List<Site> sites = site.NeighborSites ();
			Site neighbor;
			for (int nIndex =0; nIndex<sites.Count; nIndex++) {
				neighbor = sites [nIndex];
				points.Add (neighbor.Coord);
			}
			return points;
		}
		

		public List<Circle> Circles () 
		{
			return sites.Circles ();
		}
		

		
		public List<LineSegment> VoronoiBoundaryForSite (Vector2 coord)
		{
			return DelaunayHelpers.VisibleLineSegments (DelaunayHelpers.SelectEdgesForSitePoint (coord, edges));
		}

		public List<LineSegment> DelaunayLinesForSite (Vector2 coord)
		{
			return DelaunayHelpers.DelaunayLinesForEdges (DelaunayHelpers.SelectEdgesForSitePoint (coord, edges));
		}
		
		public List<LineSegment> VoronoiDiagram ()
		{
			return DelaunayHelpers.VisibleLineSegments (edges);
		}
		
		public List<LineSegment> DelaunayTriangulation (/*BitmapData keepOutMask = null*/)
		{
			return DelaunayHelpers.DelaunayLinesForEdges (DelaunayHelpers.SelectNonIntersectingEdges (/*keepOutMask,*/edges));
		}
		
		public List<LineSegment> Hull () 
		{
			return DelaunayHelpers.DelaunayLinesForEdges (HullEdges ());
		}
		
		private List<Edge> HullEdges ()
		{
			return edges.FindAll (delegate (Edge edge) {
				return (edge.IsPartOfConvexHull ());
			});
		}

		public List<Vector2> HullPointsInOrder ()
		{
			List<Edge> hullEdges = HullEdges ();
			
			List<Vector2> points = new List<Vector2> ();
			if (hullEdges.Count == 0) {
				return points;
			}
			
			EdgeReorderer reorderer = new EdgeReorderer (hullEdges, VertexOrSite.SITE);
			hullEdges = reorderer.edges;
			List<LR> orientations = reorderer.EdgeOrientations;
			reorderer.Dispose ();
			
			LR orientation;

			int n = hullEdges.Count;
			for (int i = 0; i < n; ++i) {
				Edge edge = hullEdges [i];
				orientation = orientations [i];
				points.Add (edge.Site (orientation).Coord);
			}
			return points;
		}
		
		public List<LineSegment> SpanningTree (KruskalType type = KruskalType.MINIMUM/*, BitmapData keepOutMask = null*/)
		{
			List<Edge> edges = DelaunayHelpers.SelectNonIntersectingEdges (/*keepOutMask,*/this.edges);
			List<LineSegment> segments = DelaunayHelpers.DelaunayLinesForEdges (edges);
			return DelaunayHelpers.Kruskal (segments, type);
		}

		public List<List<Vector2>> Regions () //Regions = Polygons or Cells
		{
			return sites.Regions (_plotBounds);
		}
		
		public List<uint> SiteColors (/*BitmapData referenceImage = null*/)
		{
			return sites.SiteColors (/*referenceImage*/);
		}
		
		/**
		 * 
		 * @param proximityMap a BitmapData whose regions are filled with the site index values; see PlanePointsCanvas::fillRegions()
		 * @param x
		 * @param y
		 * @return coordinates of nearest Site to (x, y)
		 * 
		 */
		public Nullable<Vector2> NearestSitePoint (/*BitmapData proximityMap,*/float x, float y)
		{
			return sites.NearestSitePoint (/*proximityMap,*/x, y);
		}
		
		public List<Vector2> SiteCoords ()
		{
			return sites.SiteCoords ();
		}

		private Site fortunesAlgorithm_bottomMostSite;
				private void FortunesAlgorithm() {
			Site newSite, bottomSite, topSite, tempSite;
			Vertex v, vertex;
			Vector2 newIntStar = Vector2.zero;
			LR leftRight;
			Halfedge lbnd, rbnd, llbnd, rrbnd, bisector;
			Edge edge;

			Rect dataBounds = sites.GetSitesBounds();

			int sqrtSitesNb = (int)Math.Sqrt(sites.Count + 4);
			HalfedgePriorityQueue heap = new HalfedgePriorityQueue(dataBounds.y, dataBounds.height, sqrtSitesNb);
			EdgeList edgeList = new EdgeList(dataBounds.x, dataBounds.width, sqrtSitesNb);
			List<Halfedge> halfEdges = new List<Halfedge>();
			List<Vertex> vertices = new List<Vertex>();

			Site bottomMostSite = sites.Next();
			newSite = sites.Next();

			while (true) {
				if (!heap.Empty()) {
					newIntStar = heap.Min();
				}

				if (newSite != null &&
				    (heap.Empty() || CompareByYThenX(newSite, newIntStar) < 0)) {
					// New site is smallest
					//Debug.Log("smallest: new site " + newSite);

					// Step 8:
					lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coord);	// The halfedge just to the left of newSite
					//UnityEngine.Debug.Log("lbnd: " + lbnd);
					rbnd = lbnd.edgeListRightNeighbor;		// The halfedge just to the right
					//UnityEngine.Debug.Log("rbnd: " + rbnd);
					bottomSite = RightRegion(lbnd, bottomMostSite);			// This is the same as leftRegion(rbnd)
					// This Site determines the region containing the new site
					//UnityEngine.Debug.Log("new Site is in region of existing site: " + bottomSite);

					// Step 9
					edge = Edge.CreateBisectingEdge(bottomSite, newSite);
					//UnityEngine.Debug.Log("new edge: " + edge);
					edges.Add(edge);

					bisector = Halfedge.Create(edge, LR.LEFT);
					halfEdges.Add(bisector);
					// Inserting two halfedges into edgelist constitutes Step 10:
					// Insert bisector to the right of lbnd:
					edgeList.Insert(lbnd, bisector);

					// First half of Step 11:
					if ((vertex = Vertex.Intersect(lbnd, bisector)) != null) {
						vertices.Add(vertex);
						heap.Remove(lbnd);
						lbnd.vertex = vertex;
						lbnd.ystar = vertex.y + newSite.Dist(vertex);
						heap.Insert(lbnd);
					}

					lbnd = bisector;
					bisector = Halfedge.Create(edge, LR.RIGHT);
					halfEdges.Add(bisector);
					// Second halfedge for Step 10::
					// Insert bisector to the right of lbnd:
					edgeList.Insert(lbnd, bisector);

					// Second half of Step 11:
					if ((vertex = Vertex.Intersect(bisector, rbnd)) != null) {
						vertices.Add(vertex);
						bisector.vertex = vertex;
						bisector.ystar = vertex.y + newSite.Dist(vertex);
						heap.Insert(bisector);
					}

					newSite = sites.Next();
				} else if (!heap.Empty()) {
					// Intersection is smallest
					lbnd = heap.ExtractMin();
					llbnd = lbnd.edgeListLeftNeighbor;
					rbnd = lbnd.edgeListRightNeighbor;
					rrbnd = rbnd.edgeListRightNeighbor;
					bottomSite = LeftRegion(lbnd, bottomMostSite);
					topSite = RightRegion(rbnd, bottomMostSite);
					// These three sites define a Delaunay triangle
					// (not actually using these for anything...)
					// triangles.Add(new Triangle(bottomSite, topSite, RightRegion(lbnd, bottomMostSite)));

					v = lbnd.vertex;
					v.SetIndex();
					lbnd.edge.SetVertex(lbnd.leftRight, v);
					rbnd.edge.SetVertex(rbnd.leftRight, v);
					edgeList.Remove(lbnd);
					heap.Remove(rbnd);
					edgeList.Remove(rbnd);
					leftRight = LR.LEFT;
					if (bottomSite.y > topSite.y) {
						tempSite = bottomSite;
						bottomSite = topSite;
						topSite = tempSite;
						leftRight = LR.RIGHT;
					}
					edge = Edge.CreateBisectingEdge(bottomSite, topSite);
					edges.Add(edge);
					bisector = Halfedge.Create(edge, leftRight);
					halfEdges.Add(bisector);
					edgeList.Insert(llbnd, bisector);
					edge.SetVertex(LR.Other(leftRight), v);
					if ((vertex = Vertex.Intersect(llbnd, bisector)) != null) {
						vertices.Add(vertex);
						heap.Remove(llbnd);
						llbnd.vertex = vertex;
						llbnd.ystar = vertex.y + bottomSite.Dist(vertex);
						heap.Insert(llbnd);
					}
					if ((vertex = Vertex.Intersect(bisector, rrbnd)) != null) {
						vertices.Add(vertex);
						bisector.vertex = vertex;
						bisector.ystar = vertex.y + bottomSite.Dist(vertex);
						heap.Insert(bisector);
					}
				} else {
					break;
				}
			}

			// Heap should be empty now
			heap.Dispose();
			edgeList.Dispose();

			foreach (Halfedge halfedge in halfEdges) {
				halfedge.ReallyDispose();
			}
			halfEdges.Clear();

			// we need the vertices to clip the edges
			foreach (Edge e in edges) {
				e.ClipVertices(plotBounds);
			}
			// But we don't actually ever use them again!
			foreach (Vertex ve in vertices) {
				ve.Dispose();
			}
			vertices.Clear();
		}
		


		private Site LeftRegion(Halfedge he, Site bottomMostSite) {
			Edge edge = he.edge;
			if (edge == null) {
				return bottomMostSite;
			}
			return edge.Site(he.leftRight);
		}
		
		private Site RightRegion(Halfedge he, Site bottomMostSite) {
			Edge edge = he.edge;
			if (edge == null) {
				return bottomMostSite;
			}
			return edge.Site(LR.Other(he.leftRight));
		}

		public static int CompareByYThenX (Site s1, Site s2) 
		{
			if (s1.y < s2.y)
				return -1;
			if (s1.y > s2.y)
				return 1;
			if (s1.x < s2.x)
				return -1;
			if (s1.x > s2.x)
				return 1;
			return 0;
		}

		public static int CompareByYThenX (Site s1, Vector2 s2)
		{
			if (s1.y < s2.y)
				return -1;
			if (s1.y > s2.y)
				return 1;
			if (s1.x < s2.x)
				return -1;
			if (s1.x > s2.x)
				return 1;
			return 0;
		}

		public List<Vector2> LloydRelaxation() //Lloyd's Relaxation Algorithm
		{
			var polygons = Regions();
			var cells = polygons;
			
			var centroids = new List<Vector2>();
			foreach (var poly in cells)
			{
				var area = 0f;
				var centroid = Vector2.zero;
				for (int i = 0; i < poly.Count; i++)
				{
					var v0 = poly[i];
					var v1 = poly[(i + 1) % poly.Count];
					var crossValue = v0.x * v1.y - v1.x * v0.y;
					area += crossValue;
					centroid.x += (v0.x + v1.x) * crossValue;
					centroid.y += (v0.y + v1.y) * crossValue;
				}
				area /= 2;
				centroid /= 6 * area;
				centroids.Add(centroid);
			}
			
			return centroids;
		}
		
	}
/// <summary>
/// This File Contains:
/// 
/// Voronoi Diagram
/// Delaunay Triangulation
/// Fortunes Algorithm
/// Lloyd's Relaxation
/// Spanning Tree
/// 
/// Lloyd's Relaxation Explanation:
/// {
/// Area = 1/2 * Summation of (X0*Y1 - X1*Y0)
/// CrossProduct = X0*Y1 - X1*Y0
/// First, Area is calculated for each cell By iterating through all of the Vertices
/// And then calculate the Cross Product of One Vertex(X0,Y0) and the Next Vertex(X1 + 1, Y1 + 1)
/// last area/=2;
///
/// x Component of the Centroid (Cx)= 1/6*Area * Summation of (X0 + X1) * CrossProduct
/// y Component of the Centroid (Cy)= 1/6*Area * Summation of (Y0 + Y1) * CrossProduct
///
/// Centroid = (Cx, Cy)
/// AtLast - Centroid = Centroid/6*Area
/// <returns> A list of the new points</returns>
/// }
/// 
/// Sources Used:
/// https://en.wikipedia.org/wiki/Lloyd%27s_algorithm
/// https://www.cs.ubc.ca/labs/imager/tr/2002/secord2002b/secord.2002b.pdf
/// https://en.wikipedia.org/wiki/Delaunay_triangulation
/// https://paulbourke.net/geometry/polygonmesh/
/// https://www.youtube.com/watch?v=Bxdt6T_1qgc
///
/// </summary> 
}

