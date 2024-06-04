using System.Collections.Generic;
using Delaunay.Utils;
using UnityEngine;

namespace Delaunay
{
	
	public sealed class Triangle: IDisposable
	{
		private List<Site> sites;
		public List<Site> Sites => sites;

		public Triangle (Site a, Site b, Site c)
		{
			sites = new List<Site> () { a, b, c };
		}
		
		
		public void Dispose ()
		{
			sites.Clear ();
			sites = null;
		}
		
		
		
		
		
	}
	
}