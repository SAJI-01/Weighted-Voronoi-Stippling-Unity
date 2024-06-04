using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Delaunay.Geo;


namespace Delaunay
{	

	public class Node
	{
		public static Stack<Node> pool = new Stack<Node> ();
		
		public Node parent;
		public int treeSize;
	}

	public enum KruskalType
	{
		MINIMUM,
		MAXIMUM
	}

	public static class DelaunayHelpers
	{
		public static List<LineSegment> VisibleLineSegments(IEnumerable<Edge> edges)
		{
			return (from edge in edges where edge.visible 
				let p1 = edge.clippedEnds[LR.LEFT] 
				let p2 = edge.clippedEnds[LR.RIGHT] 
				select new LineSegment(p1, p2)).ToList();
		}

		public static List<Edge> SelectEdgesForSitePoint (Vector2 coord, List<Edge> edgesToTest)
		{
			return edgesToTest.FindAll (edge => (edge.leftSite != null && edge.leftSite.Coord == coord) || (edge.rightSite != null && edge.rightSite.Coord == coord));
		}

		public static List<Edge> SelectNonIntersectingEdges (/*keepOutMask:BitmapData,*/List<Edge> edgesToTest)
		{
			return edgesToTest;
		}

		public static List<LineSegment> DelaunayLinesForEdges (List<Edge> edges)
		{
			return edges.Select(edge => edge.DelaunayLine()).ToList();
		}
		
		/**
		*  Kruskal's spanning tree algorithm with union-find
		 * Skiena: The Algorithm Design Manual, p. 196ff
		 * Note: the sites are implied: they consist of the end points of the line segments
		*/
		
		public static List<LineSegment> Kruskal(List<LineSegment> lineSegments, KruskalType type = KruskalType.MINIMUM)
		{
			var nodes = new Dictionary<Vector2?, Node>();
			var mst = new List<LineSegment>(); // mst = minimum spanning tree
			var nodePool = Node.pool;

			if (type ==
			    // note that the compare functions are the reverse of what you'd expect
			    // because (see below) we traverse the lineSegments in reverse order for speed
			    KruskalType.MAXIMUM)
				lineSegments.Sort(LineSegment.CompareLengths);
			else
				lineSegments.Sort(LineSegment.CompareLengths_MAX);

			for (var i = lineSegments.Count; --i > -1;)
			{
				var lineSegment = lineSegments[i];

				Node node0 = null;
				Node rootOfSet0;
				if (!nodes.ContainsKey(lineSegment.p0))
				{
					node0 = nodePool.Count > 0 ? nodePool.Pop() : new Node();
					// initialize the node:
					rootOfSet0 = node0.parent = node0;
					node0.treeSize = 1;

					nodes[lineSegment.p0] = node0;
				}
				else
				{
					node0 = nodes[lineSegment.p0];
					rootOfSet0 = Find(node0);
				}

				Node node1 = null;
				Node rootOfSet1;
				if (!nodes.ContainsKey(lineSegment.p1))
				{
					node1 = nodePool.Count > 0 ? nodePool.Pop() : new Node();
					// initialize the node:
					rootOfSet1 = node1.parent = node1;
					node1.treeSize = 1;

					nodes[lineSegment.p1] = node1;
				}
				else
				{
					node1 = nodes[lineSegment.p1];
					rootOfSet1 = Find(node1);
				}

				if (rootOfSet0 != rootOfSet1)
				{
					// nodes not in same set
					mst.Add(lineSegment);

					// merge the two sets:
					var treeSize0 = rootOfSet0.treeSize;
					var treeSize1 = rootOfSet1.treeSize;
					if (treeSize0 >= treeSize1)
					{
						// set0 absorbs set1:
						rootOfSet1.parent = rootOfSet0;
						rootOfSet0.treeSize += treeSize1;
					}
					else
					{
						// set1 absorbs set0:
						rootOfSet0.parent = rootOfSet1;
						rootOfSet1.treeSize += treeSize0;
					}
				}
			}

			foreach (var node in nodes.Values) nodePool.Push(node);

			return mst;
		}
		
		// find 

		private static Node Find (Node node) // how to implement in weighted voronoi stippling.cs script
		{
			if (node.parent == node) {
				return node;
			} else {
				Node root = Find (node.parent);
				// this line is just to speed up subsequent finds by keeping the tree depth low:
				node.parent = root;
				return root;
			}
		}
		
		
		
		
		
		
	}


}