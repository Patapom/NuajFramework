using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj
{
	/// <summary>
	/// This is a class that describes a viewing frustum
	/// A frustum is a collection of planes that define a convex hull.
	/// These planes can then be used to compute the vertices of that convex hull
	///  or to test if objects are inside or outside of the frustum.
	///  
	/// All planes are oriented with their normals pointing OUT of the convex hull,
	///  so a point is inside the frustum if all the dot products with the frustum
	///  planes are negative.
	/// </summary>
	public class Frustum
	{
		#region NESTED TYPES

		public class	Face
		{
			#region FIELDS

			protected int			m_Index = 0;
			protected int[]			m_Vertices = null;
			protected WingedEdge[]	m_Edges = null;

			#endregion

			#region PROPERTIES

			/// <summary>
			/// Gets the list of vertex indices defining that face
			/// </summary>
			public int[]		Vertices	{ get { return m_Vertices; } }

			/// <summary>
			/// Gets the list of edges defining that face
			/// </summary>
			/// <remarks>Each edge corresponds to a vertex, the vertex in question can either be the FORWARD or BACKWARD vertex index in the edge, depending on the edge's orientation...s</remarks>
			public WingedEdge[]	Edges		{ get { return m_Edges; } }

			#endregion

			#region METHODS

			public	Face( int _Index, int[] _Vertices, WingedEdge[] _Edges )
			{
				m_Index = _Index;
				m_Vertices = _Vertices;
				m_Edges = _Edges;
			}

			public override string ToString()
			{
				return "ID " + m_Index.ToString() + " VertexCount=" + m_Vertices.Length;
			}

			#endregion
		}

		public class	WingedEdge
		{
			#region FIELDS

			protected int			m_Index = 0;
			protected Vector3		m_Position = Vector3.Zero;		// Some position along the edge
			protected Vector3		m_Direction = Vector3.Zero;		// The edge's FORWARD direction
			protected int			m_LeftPlaneIndex = -1;			// The index of the left plane the edge belongs to
			protected int			m_RightPlaneIndex = -1;			// The index of the right plane the edge belongs to
			protected int			m_ForwardPlaneIndex = -1;		// The index of the plane we intersect by following that edge in the FORWARD direction
			protected int			m_BackardPlaneIndex = -1;		// The index of the plane we intersect by following that edge in the BACKWARD direction
			protected int			m_ForwardVertex = -1;			// The index of the vertex formed by the intersection of LEFT+RIGHT+FORWARD planes
			protected int			m_BackwardVertex = -1;			// The index of the vertex formed by the intersection of LEFT+RIGHT+BACWARD planes

			#endregion

			#region PROPERTIES

			public int			Index				{ get { return m_Index; } set { m_Index = value; } }
			public Vector3		Position			{ get { return m_Position; } }
			public Vector3		Direction			{ get { return m_Direction; } }
			public int			LeftPlaneIndex		{ get { return m_LeftPlaneIndex; } }
			public int			RightPlaneIndex		{ get { return m_RightPlaneIndex; } }
			public int			ForwardPlaneIndex	{ get { return m_ForwardPlaneIndex; } set { m_ForwardPlaneIndex = value; } }
			public int			BackwardPlaneIndex	{ get { return m_BackardPlaneIndex; } set { m_BackardPlaneIndex = value; } }
			public int			ForwardVertexIndex	{ get { return m_ForwardVertex; } set { m_ForwardVertex = value; } }
			public int			BackwardVertexIndex	{ get { return m_BackwardVertex; } set { m_BackwardVertex = value; } }

			#endregion

			#region METHODS

			public WingedEdge( Vector3 _Position, Vector3 _Direction, int _LeftPlaneIndex, int _RightPlaneIndex )
			{
				m_Position = _Position;
				m_Direction = _Direction;
				m_LeftPlaneIndex = _LeftPlaneIndex;
				m_RightPlaneIndex = _RightPlaneIndex;
			}

			public override string ToString()
			{
				return "ID " + m_Index + " L=" + m_LeftPlaneIndex + " R=" + m_RightPlaneIndex + " F=" + m_ForwardPlaneIndex + " B=" + m_BackardPlaneIndex;
			}

			#endregion
		}

		#endregion

		#region FIELDS

		protected bool			m_bUpdating = false;
		protected List<Plane>	m_Planes = new List<Plane>();
		protected Vector3[]		m_Vertices = new Vector3[0];
		protected WingedEdge[]	m_Edges = new WingedEdge[0];
		protected Face[]		m_Faces = new Face[0];

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the list of planes defining the frustum's boundaries
		/// </summary>
		public Plane[]		Planes		{ get { return m_Planes.ToArray(); } }

		/// <summary>
		/// Gets the list of edges defining the frustum's convex hull
		/// </summary>
		public WingedEdge[]	Edges		{ get { return m_Edges; } }

		/// <summary>
		/// Gets the list of vertices defining the frustum's convex hull
		/// </summary>
		public Vector3[]	Vertices	{ get { return m_Vertices; } }

		/// <summary>
		/// Gets the list of faces defining the frustum's convex hull
		/// </summary>
		public Face[]		Faces		{ get { return m_Faces; } }

		#endregion

		#region METHODS

		public	Frustum()
		{
		}

		public void		ClearPlanes()
		{
			m_Planes.Clear();
			m_Vertices = new Vector3[0];
			m_Faces = new Face[0];
		}

		/// <summary>
		/// Use this to bulk update the frustum
		/// </summary>
		public void		BeginUpdate()
		{
			m_bUpdating = true;
		}

		/// <summary>
		/// Use this to finish bulk updating the frustum
		/// </summary>
		public void		EndUpdate()
		{
			m_bUpdating = false;
			RebuildConvexHull();
		}

		/// <summary>
		/// Adds a new plane to the frustum
		/// </summary>
		/// <param name="_Position"></param>
		/// <param name="_Normal"></param>
		public void		AddPlane( Vector3 _Position, Vector3 _Normal )
		{
			AddPlane( new Plane( _Position, _Normal ) );
		}

		/// <summary>
		/// Adds a new plane to the frustum
		/// </summary>
		/// <param name="_Plane"></param>
		public void		AddPlane( Plane _Plane )
		{
			m_Planes.Add( _Plane );
			RebuildConvexHull();
		}

		/// <summary>
		/// Constructs a frustum from perspective projection data
		/// </summary>
		/// <param name="_FOV"></param>
		/// <param name="_AspectRatio"></param>
		/// <param name="_Near"></param>
		/// <param name="_Far"></param>
		/// <returns></returns>
		public static Frustum	FromPerspective( float _FOV, float _AspectRatio, float _Near, float _Far )
		{
			Frustum	Result = new Frustum();
			Result.BeginUpdate();

			// Near and far planes
			Result.AddPlane( new Plane( new Vector3( 0.0f, 0.0f, _Near ), new Vector3( 0.0f, 0.0f, -1.0f ) ) );
			Result.AddPlane( new Plane( new Vector3( 0.0f, 0.0f, _Far ),  new Vector3( 0.0f, 0.0f, 1.0f ) ) );

			// Top and bottom planes
			Result.AddPlane( new Plane( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( 0.0f, (float) Math.Cos( 0.5 * _FOV ), -(float) Math.Sin( 0.5 * _FOV ) ) ) );
			Result.AddPlane( new Plane( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( 0.0f, -(float) Math.Cos( 0.5 * _FOV ), -(float) Math.Sin( 0.5 * _FOV ) ) ) );

			// Left and right planes
			float	fHorizontalFOV = 2.0f * (float) Math.Atan( _AspectRatio * Math.Tan( 0.5 * _FOV ) );
			Result.AddPlane( new Plane( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( (float) Math.Cos( 0.5 * fHorizontalFOV ), 0.0f, -(float) Math.Sin( 0.5 * fHorizontalFOV ) ) ) );
			Result.AddPlane( new Plane( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( -(float) Math.Cos( 0.5 * fHorizontalFOV ), 0.0f, -(float) Math.Sin( 0.5 * fHorizontalFOV ) ) ) );

			Result.EndUpdate();

			return Result;
		}

		/// <summary>
		/// Constructs a frustum from orthographic projection data
		/// </summary>
		/// <param name="_FOV"></param>
		/// <param name="_AspectRatio"></param>
		/// <param name="_Near"></param>
		/// <param name="_Far"></param>
		/// <returns></returns>
		public static Frustum	FromOrtho( float _Height, float _AspectRatio, float _Near, float _Far )
		{
			Frustum	Result = new Frustum();
			Result.BeginUpdate();

			// Near and far planes
			Result.AddPlane( new Plane( new Vector3( 0.0f, 0.0f, _Near ), new Vector3( 0.0f, 0.0f, -1.0f ) ) );
			Result.AddPlane( new Plane( new Vector3( 0.0f, 0.0f, _Far ),  new Vector3( 0.0f, 0.0f, 1.0f ) ) );

			// Top and bottom planes
			Result.AddPlane( new Plane( new Vector3( 0.0f, 0.5f*_Height, 0.0f ),  new Vector3( 0.0f, 1.0f, 0.0f ) ) );
			Result.AddPlane( new Plane( new Vector3( 0.0f, -0.5f*_Height, 0.0f ), new Vector3( 0.0f, -1.0f, 0.0f ) ) );

			// Left and right planes
			float	fWidth = _Height * _AspectRatio;
			Result.AddPlane( new Plane( new Vector3( 0.5f*fWidth, 0.0f, 0.0f ),  new Vector3( 1.0f, 0.0f, 0.0f ) ) );
			Result.AddPlane( new Plane( new Vector3( -0.5f*fWidth, 0.0f, 0.0f ), new Vector3( -1.0f, 0.0f, 0.0f ) ) );

			Result.EndUpdate();

			return Result;
		}

		/// <summary>
		/// Rebuilds the convex hull's vertices, edges and faces from the current list of planes
		/// </summary>
		protected void	RebuildConvexHull()
		{
			if ( m_bUpdating )
				return;	// Wait for update end...

			// 1] Build unconnected winged edges by computing all the plane intersections 2 by 2
			List<WingedEdge>	Edges = new List<WingedEdge>();
			for ( int PlaneIndex0=0; PlaneIndex0 < m_Planes.Count-1; PlaneIndex0++ )
			{
				Plane	P0 = m_Planes[PlaneIndex0];
				for ( int PlaneIndex1=PlaneIndex0+1; PlaneIndex1 < m_Planes.Count; PlaneIndex1++ )
				{
					Plane	P1 = m_Planes[PlaneIndex1];
					if ( 1.0f - Math.Abs( Vector3.Dot( P0.Normal, P1.Normal ) ) < 1e-3f )
						continue;	// Those 2 are assumed parallel...

					// The edge direction must be tangent to both planes so it's obviously the cross product of their normals
					Vector3	EdgeDirection = Vector3.Cross( P0.Normal, P1.Normal );
							EdgeDirection.Normalize();

					// Now we compute the tangent to plane #0 and the position of a point in plane #0
					Vector3	PlaneTangent = Vector3.Cross( P0.Normal, EdgeDirection );
					Vector3	PlanePosition = -P0.D * P0.Normal;
					
					// ... and compute the intersection of the ray (Position,Tangent) with plane #1
					// The plane equation is : N.P + D = 0
					// The ray equation is : P = Start + V.t
					// so  t = -(Start.N + D) / (N.V)
					//
					float	t = -(Vector3.Dot( PlanePosition, P1.Normal ) + P1.D) / Vector3.Dot( PlaneTangent, P1.Normal );
					Vector3	EdgePosition = PlanePosition + t * PlaneTangent;

					WingedEdge	E = new WingedEdge( EdgePosition, EdgeDirection, PlaneIndex1, PlaneIndex0 );
					Edges.Add( E );
				}
			}

			// 2] Compute edge intersections with planes to obtain the smallest bounded segments
			List<WingedEdge>					UsedEdges = new List<WingedEdge>();
			Dictionary<WingedEdge,Vector3[]>	Edge2Segment = new Dictionary<WingedEdge,Vector3[]>();
			foreach ( WingedEdge E in Edges )
			{
				// Initialize edge bounds to infinity
				Vector3[]	Segment = new Vector3[2];

				float		fBoundDistanceForward = float.PositiveInfinity;		// Far away in the edge direction
				float		fBoundDistanceBackward = float.NegativeInfinity;	// Far away in the opposite edge direction

				for ( int PlaneIndex=0; PlaneIndex < m_Planes.Count; PlaneIndex++ )
				{
					Plane	P = m_Planes[PlaneIndex];
					if ( Math.Abs( Vector3.Dot( P.Normal, E.Direction ) ) < 1e-3f )
						continue;	// The edge seems to belong to that plane so there won't be any intersection. Skip !

					// Compute edge intersection with that plane
					// Same equation as before...
					float	fConcurrence = Vector3.Dot( E.Direction, P.Normal );
					float	t = -(Vector3.Dot( E.Position, P.Normal ) + P.D) / fConcurrence;
					if ( fConcurrence > 0.0 )
					{	// Edge and plane normal go in the same direction, that means t gives a FORWARD intersection
						if ( t < fBoundDistanceForward )
						{	// Found a better bounding plane !
							fBoundDistanceForward = t;
							E.ForwardPlaneIndex = PlaneIndex;
						}
					}
					else
					{	// Edge and plane normal go in opposite directions, that means t gives a BACKWARD intersection
						if ( t > fBoundDistanceBackward )
						{	// Found a better bounding plane !
							fBoundDistanceBackward = t;
							E.BackwardPlaneIndex = PlaneIndex;
						}
					}
				}

				if ( Math.Abs( fBoundDistanceForward - fBoundDistanceBackward ) < 1e-5f )
					continue;	// This edge is degenerate, don't use !

				// At this point, we should have the 2 best bounding planes and the shortest segment distance
				// We thus can compute the segment vertices...
				if ( E.ForwardPlaneIndex != -1 )
					Segment[0] = E.Position + fBoundDistanceForward * E.Direction;
				if ( E.BackwardPlaneIndex != -1 )
					Segment[1] = E.Position + fBoundDistanceBackward * E.Direction;

				// Add another used edge...
				E.Index = UsedEdges.Count;
				UsedEdges.Add( E );
				Edge2Segment[E] = Segment;
			}

			m_Edges = UsedEdges.ToArray();

			// 3] Merge identical vertices to obtain the final list of vertices
			float			fTolerance = 1e-3f;	// Below that tolerance threshold, 2 vertices are considered identical
			List<Vector3>	Vertices = new List<Vector3>();
			Dictionary<int,List<WingedEdge>>	VertexIndex2Edges = new Dictionary<int,List<WingedEdge>>();	// The map of shared edges for every vertex

			foreach ( WingedEdge E in Edge2Segment.Keys )
			{
				Vector3[]	Segment = Edge2Segment[E];
				if ( E.ForwardPlaneIndex != -1 )
				{	// Check if forward segment vertex is a new vertex or can be merged with existing vertices...
					for ( int ExistingVertexIndex=0; ExistingVertexIndex < Vertices.Count; ExistingVertexIndex++ )
					{
						float	fDistance = (Segment[0]-Vertices[ExistingVertexIndex]).Length();
						if ( fDistance > fTolerance )
							continue;

						E.ForwardVertexIndex = ExistingVertexIndex;
						break;
					}

					if ( E.ForwardVertexIndex == -1 )
					{	// Found a new vertex !
						E.ForwardVertexIndex = Vertices.Count;
						Vertices.Add( Segment[0] );
					}

					// Add that edge as sharing the vertex
					if ( !VertexIndex2Edges.ContainsKey( E.ForwardVertexIndex ) )
						VertexIndex2Edges.Add( E.ForwardVertexIndex, new List<WingedEdge>() );
					VertexIndex2Edges[E.ForwardVertexIndex].Add( E );
				}

				if ( E.BackwardPlaneIndex != -1 )
				{	// Check if backward segment vertex is a new vertex or can be merged with existing vertices...
					for ( int ExistingVertexIndex=0; ExistingVertexIndex < Vertices.Count; ExistingVertexIndex++ )
					{
						float	fDistance = (Segment[1]-Vertices[ExistingVertexIndex]).Length();
						if ( fDistance > fTolerance )
							continue;

						E.BackwardVertexIndex = ExistingVertexIndex;
						break;
					}

					if ( E.BackwardVertexIndex == -1 )
					{	// Found a new vertex !
						E.BackwardVertexIndex = Vertices.Count;
						Vertices.Add( Segment[1] );
					}

					// Add that edge as sharing the vertex
					if ( !VertexIndex2Edges.ContainsKey( E.BackwardVertexIndex ) )
						VertexIndex2Edges.Add( E.BackwardVertexIndex, new List<WingedEdge>() );
					VertexIndex2Edges[E.BackwardVertexIndex].Add( E );
				}
			}

			m_Vertices = Vertices.ToArray();

			// 4] Finally, for each plane, we find the first edge that is part of it and follow it until it loops back so we form a face
			List<Face>	Faces = new List<Face>();
			for ( int PlaneIndex=0; PlaneIndex < m_Planes.Count; PlaneIndex++ )
			{
				// Find an edge that uses the plane as left or right
				WingedEdge	StartEdge = null;
				for ( int EdgeIndex=0; EdgeIndex < m_Edges.Length; EdgeIndex++ )
				{
					WingedEdge	E = m_Edges[EdgeIndex];
					if ( E.LeftPlaneIndex == PlaneIndex || E.RightPlaneIndex == PlaneIndex )
					{	// Found it !
						StartEdge = E;
						break;
					}
				}
				if ( StartEdge == null )
					continue;	// This plane does not generate any edge ?

				List<int>			FaceVertices = new List<int>();
				List<WingedEdge>	FaceEdges = new List<WingedEdge>();

				bool		bLoops = false;
				WingedEdge	LastEdge = null;
				WingedEdge	CurrentEdge = StartEdge;
				while ( !bLoops )
				{
					// Retrieve the vertex index we need for that face
					int	FaceVertexIndex = -1;
					if ( PlaneIndex == CurrentEdge.RightPlaneIndex )
						FaceVertexIndex = CurrentEdge.BackwardVertexIndex;
					else if ( PlaneIndex == CurrentEdge.LeftPlaneIndex )
						FaceVertexIndex = CurrentEdge.ForwardVertexIndex;

					if ( FaceVertexIndex == -1 )
						break;	// Not a valid vertex index, that face does not loop and therefore is not a valid face !

					// Add both edge & vertex
					FaceEdges.Add( CurrentEdge );
					FaceVertices.Add( FaceVertexIndex );

					// Find the next edge...
					WingedEdge	NextEdge = null;
					foreach ( WingedEdge SharedEdge in VertexIndex2Edges[FaceVertexIndex] )
						if ( SharedEdge != CurrentEdge && SharedEdge != LastEdge )	// Make sure we're not going backward or stalling...
						{
							if ( SharedEdge.LeftPlaneIndex == PlaneIndex || SharedEdge.RightPlaneIndex == PlaneIndex )
							{	// Found it !
								NextEdge = SharedEdge;
								break;
							}
						}

					if ( NextEdge == null )
						break;			// The trail ends here ! This face doesn't loop !
					if ( NextEdge == StartEdge )
						bLoops = true;	// We made a loop ! The face is complete !

					// Scroll edges
					LastEdge = CurrentEdge;
					CurrentEdge = NextEdge;
				}

				if ( !bLoops )
					continue;	// Not a valid face that one !

				// Create one more face. The plane deserves it !
				Face	F = new Face( Faces.Count, FaceVertices.ToArray(), FaceEdges.ToArray() );
				Faces.Add( F );
			}

			m_Faces = Faces.ToArray();
		}

		/// <summary>
		/// Checks if a point is inside the frustum
		/// </summary>
		/// <param name="_Point"></param>
		/// <returns></returns>
		public bool		IsInside( Vector3 _Point )
		{
			foreach ( Plane P in m_Planes )
				if ( Vector3.Dot( P.Normal, _Point ) + P.D > 0.0f )
					return false;	// Outside...

			return true;
		}

		/// <summary>
		/// Checks if a bounding sphere is fully inside the frustum (i.e. returns false as soon as the sphere hits a frustum plane)
		/// </summary>/
		/// <param name="_Center"></param>
		/// <param name="_Radius"></param>
		/// <returns></returns>
		public bool		IsInsideExclusive( Vector3 _Center, float _Radius )
		{
			foreach ( Plane P in m_Planes )
			{
				float	fDistanceToPlane = Vector3.Dot( _Center, P.Normal ) + P.D;
				if ( fDistanceToPlane >= -_Radius )
					return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if a bounding sphere is partially inside the frustum (i.e. returns false only if the entire sphere is outside of a frustum plane)
		/// </summary>/
		/// <param name="_Center"></param>
		/// <param name="_Radius"></param>
		/// <returns></returns>
		public bool		IsInsideInclusive( Vector3 _Center, float _Radius )
		{
			foreach ( Plane P in m_Planes )
			{
				float	fDistanceToPlane = Vector3.Dot( _Center, P.Normal ) + P.D;
				if ( fDistanceToPlane >= _Radius )
					return false;
			}

			return true;
		}

		#endregion
	}
}
