using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SharpDX;

namespace ShadowMapTest
{
	/// <summary>
	/// This little app is used to visually debug the computation of a shadow map's bounding quadrilateral
	/// 
	/// The camera's frustum is defined by a pyramid whose apex is the camera's position.
	/// This pyramid is projected onto a plane (the shadow plane) and can thus take several configurations.
	/// Two of them are distinguished :
	///  1) The camera's position is projected inside the pyramid's base (viewing up case), in which case the base is used as the quadrilateral to map
	///  2) The camera's position is projected outside the pyramid's base (general case), in which case we need to find a triangle whose apex is the
	///		projected camera position and whose 2 other vertices need to be computed so the triangle encompasses the 4 vertices of the pyramid's base.
	///		(the triangle is thus a degenerated quadrilateral)
	///	
	/// Later, we show how to retrieve the (u,v) position parametrizing the quadrilateral from any point P projected onto the shadow plane.
	/// </summary>
	public partial class Form1 : Form
	{
		#region CONSTANTS

		protected const float	PLANET_RADIUS_KM = 6400.0f;
		protected const float	DEG2RAD = (float) Math.PI / 180.0f;
		protected const float	CAMERA_FOV = 60.0f * DEG2RAD;
		protected const float	CAMERA_ASPECT = 1.3333333f;//16.0f / 9.0f;

		protected const float	CLOUD_ALTITUDE_TOP_KM = 6.0f;


		#endregion

		#region METHODS

		public Form1()
		{
			InitializeComponent();
		}

		protected unsafe override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );

			ComputeShadowData();
		}

		// To fill the input with a bitmap (that was a test check FWD then BWD FFT to see if I got the image back again)
		protected unsafe void	FillWithBitmap( float[] _In )
		{
// 			System.Drawing.Imaging.BitmapData	LockedBitmap = Properties.Resources.TestPic.LockBits( new Rectangle( 0, 0, Properties.Resources.TestPic.Width, Properties.Resources.TestPic.Height ), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
// 			for ( int Y=0; Y < LockedBitmap.Height; Y++ )
// 			{
// 				byte*	pScanline = (byte*) LockedBitmap.Scan0.ToPointer() + LockedBitmap.Stride * Y;
// 				for ( int X=0; X < LockedBitmap.Width; X++ )
// 				{
// 					byte	B = *pScanline++;
// 					byte	G = *pScanline++;
// 					byte	R = *pScanline++;
// 					byte	A = *pScanline++;
// 					float	fLuminance = (0.3f * R + 0.5f * G + 0.2f * B) / 255.0f;
// 
// 					int CX = 256 * X / LockedBitmap.Width;
// 					int CY = 256 * Y / LockedBitmap.Height;
// 					_In[2*(256*CY+CX)+0] = fLuminance;
// 					_In[2*(256*CY+CX)+1] = 0.0f;
// 				}
// 			}
// 
			// Don't do that as it's a stock resource
//			Properties.Resources.TestPic.UnlockBits( LockedBitmap );
		}

		protected Vector2	m_CameraProjKm = Vector2.Zero;
		protected Vector2[]	m_FrustumProjKm = new Vector2[4];
		protected Vector2[]	m_ConvexHullKm = null;
		protected Vector2[]	m_ShadowQuadKm = new Vector2[4];

		protected Matrix	m_ShadowQuad2UV = new Matrix();
		protected Matrix	m_UV2ShadowQuad = new Matrix();

		protected void	ComputeShadowData()
		{
			float	PhiSun = DEG2RAD * floatTrackbarControlSunPhi.Value;
			float	ThetaSun = DEG2RAD * floatTrackbarControlSunTheta.Value;
			float	PhiCamera = DEG2RAD * floatTrackbarControlCameraPhi.Value;
			float	ThetaCamera = DEG2RAD * floatTrackbarControlCameraTheta.Value;

			//////////////////////////////////////////////////////////////////////////
			// Compute Sun's direction (also the normal to the shadow plane)
			float	CosPhi = (float) Math.Cos( PhiSun );
			float	SinPhi = (float) Math.Sin( PhiSun );
			float	CosTheta = (float) Math.Cos( ThetaSun );
			float	SinTheta = (float) Math.Sin( ThetaSun );
			Vector3	SunDirection = new Vector3( SinPhi * SinTheta, CosTheta, CosPhi * SinTheta );

			//////////////////////////////////////////////////////////////////////////
			// Compute camera's transform
			CosPhi = (float) Math.Cos( PhiCamera );
			SinPhi = (float) Math.Sin( PhiCamera );
			CosTheta = (float) Math.Cos( ThetaCamera );
			SinTheta = (float) Math.Sin( ThetaCamera );
			Vector3	CameraAt = new Vector3( SinPhi * SinTheta, CosTheta, CosPhi * SinTheta );
			Vector3	CameraUp = Vector3.UnitY;
			Vector3	CameraRight = Vector3.Normalize( Vector3.Cross( CameraAt, CameraUp ) );
			CameraUp = Vector3.Cross( CameraRight, CameraAt );


			//////////////////////////////////////////////////////////////////////////
			// Compute shadow plane tangent space
			Vector3	PlanetCenterKm = new Vector3( 0.0f, -PLANET_RADIUS_KM, 0.0f );
			Vector3	ShadowPlaneTangent = PlanetCenterKm + (PLANET_RADIUS_KM + CLOUD_ALTITUDE_TOP_KM) * SunDirection;
			Vector3	ShadowPlaneX = Vector3.Normalize( Vector3.Cross( CameraAt, SunDirection ) );
			Vector3	ShadowPlaneY = Vector3.Cross( SunDirection, ShadowPlaneX );


			//////////////////////////////////////////////////////////////////////////
			// Build camera frustum
			float	ShadowFarDistanceKm = 120.0f;
			float	TanFovV = (float) Math.Tan( 0.5f * CAMERA_FOV );
			float	TanFovH = CAMERA_ASPECT * (float) Math.Tan( 0.5f * CAMERA_FOV );

			Vector3		CameraPositionKm = new Vector3( 0.0f, 0.50f, 0.0f );	// Camera at 500m
			Vector3[]	CameraFrustumKm = new Vector3[4]
			{
				ShadowFarDistanceKm * new Vector3( -TanFovH, -TanFovV, 1.0f ),
				ShadowFarDistanceKm * new Vector3( +TanFovH, -TanFovV, 1.0f ),
				ShadowFarDistanceKm * new Vector3( +TanFovH, +TanFovV, 1.0f ),
				ShadowFarDistanceKm * new Vector3( -TanFovH, +TanFovV, 1.0f ),
			};

			// Transform into WORLD space
			for ( int i=0; i < 4; i++ )
			{
				Vector3	Dir = CameraFrustumKm[i];
				CameraFrustumKm[i] = CameraPositionKm + Dir.X * CameraRight + Dir.Y * CameraUp + Dir.Z * CameraAt;
			}

			// Clip with Earth
			CameraFrustumKm = ClipFrustum( CameraPositionKm, CameraFrustumKm );

			// Transform frustum vectors into points
			for ( int i=0; i < 4; i++ )
				CameraFrustumKm[i] += CameraPositionKm;


			//////////////////////////////////////////////////////////////////////////
			// Compute center offset.
			// The "center" of the shadow plane is the intersection of the ray starting from the camera and intersecting the shadow plane
// 			Vector2	ShadowPlaneOffset = World2Quad( CameraPositionKm, ShadowPlaneTangent, SunDirection, ShadowPlaneX, ShadowPlaneY, PositionVerticalClip );
// 			Vector3	ShadowPlaneCenter = ShadowPlaneTangent + ShadowPlaneOffset.X * ShadowPlaneX + ShadowPlaneOffset.Y * ShadowPlaneY;
 			Vector3	ShadowPlaneCenter = Project2ShadowPlane( CameraPositionKm, ShadowPlaneTangent, SunDirection );

			// Compute vertical clipping
			Vector3	ProjectionMin = Project2ShadowPlane( new Vector3( 0, 0, 0 ), ShadowPlaneCenter, SunDirection );
			Vector3	ProjectionMax = Project2ShadowPlane( new Vector3( 0, CLOUD_ALTITUDE_TOP_KM, 0 ), ShadowPlaneCenter, SunDirection );
			Vector2	ShadowClippingY = new Vector2( ProjectionMin.Y, ProjectionMax.Y );

			
			//////////////////////////////////////////////////////////////////////////
			// Project frustum to shadow plane
			m_CameraProjKm = World2Quad( CameraPositionKm, ShadowPlaneCenter, SunDirection, ShadowPlaneX, ShadowPlaneY, ShadowClippingY );
			for ( int i=0; i < 4; i++ )
				m_FrustumProjKm[i] = World2Quad( CameraFrustumKm[i], ShadowPlaneCenter, SunDirection, ShadowPlaneX, ShadowPlaneY, ShadowClippingY );


			//////////////////////////////////////////////////////////////////////////
			// Compute convex hull
			int[]	ConvexHullIndices = null;
			m_ConvexHullKm = ComputeConvexHull( new Vector2[] { m_CameraProjKm, m_FrustumProjKm[0], m_FrustumProjKm[1], m_FrustumProjKm[2], m_FrustumProjKm[3] }, out ConvexHullIndices );


			//////////////////////////////////////////////////////////////////////////
			// Compute bounding quadrilateral
			// (cf. ftp://ftp.cs.unc.edu/pub/techreports/02-024.pdf)
			//
			// At this point, our convex hull can either have 3, 4 or 5 vertices.
			// Since we always need a quadrilateral, the only case we need to deal with is the 5 vertices case
			//	that we need to reduce to 4 vertices.
			//
			switch ( m_ConvexHullKm.Length )
			{
				case 3:
					{	// The case of the triangle leads to numerical instabilities
						// We need to create an additional vertex
						Vector2	Direction = m_ConvexHullKm[2] - m_ConvexHullKm[0];
						Vector2	Offset = Direction * 0.01f;	// 1% of the opposite edge's length
						m_ShadowQuadKm = new Vector2[4]
						{
							m_ConvexHullKm[0],
							m_ConvexHullKm[1],
							m_ConvexHullKm[1] + Offset,
							m_ConvexHullKm[2],
						};
					}
					break;

				case 4:
					m_ShadowQuadKm = m_ConvexHullKm;	// Easy !
					break;

				case 5:
					m_ShadowQuadKm = ReduceConvexHull( m_ConvexHullKm, ConvexHullIndices, 0 );
					break;
			}

			//////////////////////////////////////////////////////////////////////////
			// Compute the transform matrices converting between UV <=> Shadow Quadrilateral Space
			//
			ComputeShadowTransforms();

			// Feed stuff to the panel & redraw
			panelOutput.m_Owner = this;
			panelOutput.m_CameraPosition = m_CameraProjKm;
			panelOutput.m_FrustumBase = m_FrustumProjKm;
			panelOutput.m_ConvexHull = m_ConvexHullKm;
			panelOutput.m_Quadrilateral = m_ShadowQuadKm;
			panelOutput.UpdateBitmap();


// DEBUG
Vector3		TestPosition = CameraPositionKm + 0.0f * CameraRight + 60.0f * CameraAt;
Vector2		ProjectedPosition = World2Quad( TestPosition, ShadowPlaneTangent, SunDirection, ShadowPlaneX, ShadowPlaneY, ShadowClippingY );
// DEBUG
		}

		// Calcul de l'ombre seulement tous les N steps !!!

		protected Vector3	Project2ShadowPlane( Vector3 _PositionKm, Vector3 _PlaneCenterKm, Vector3 _PlaneNormal )
		{
			Vector3	Position2CenterKm = _PlaneCenterKm - _PositionKm;
			Vector3	ProjectedPositionKm = _PositionKm + Vector3.Dot( Position2CenterKm, _PlaneNormal ) * _PlaneNormal;
			return ProjectedPositionKm;
		}

		protected Vector2	World2Quad( Vector3 _PositionKm, Vector3 _PlaneCenterKm, Vector3 _PlaneNormal, Vector3 _PlaneX, Vector3 _PlaneY, Vector2 _ClipY )
		{
			Vector3	ProjectedPositionKm = Project2ShadowPlane( _PositionKm, _PlaneCenterKm, _PlaneNormal );

			// Clamp the vertical position
			ProjectedPositionKm.Y = Math.Max( _ClipY.X, Math.Min( _ClipY.Y, ProjectedPositionKm.Y ) );

			return new Vector2( Vector3.Dot( ProjectedPositionKm, _PlaneX ), Vector3.Dot( ProjectedPositionKm, _PlaneY ) );
		}

		/// <summary>
		/// Computes the reduced camera frustum when intersecting the Earth's surface
		/// </summary>
		/// <param name="_CameraPositionKm"></param>
		/// <param name="_CameraFrustumKm"></param>
		protected Vector3[]	ClipFrustum( Vector3 _CameraPositionKm, Vector3[] _CameraFrustumKm )
		{
			// The idea here is that the camera's frustum is quite reduced by its intersection with the Earth's surface
			Vector3	PlanetCenterKm = new Vector3( 0.0f, -PLANET_RADIUS_KM, 0.0f );

			// 1] We first determine which camera rays intersect the Earth
			bool[]	Hits = new bool[4];
			float[]	HitDistancesKm = new float[4];
			int		HitsCount = 0;
			for ( int i=0; i < 4; i++ )
			{
				Hits[i] = ComputeSphereEnterIntersection( _CameraPositionKm, _CameraFrustumKm[i], PlanetCenterKm, PLANET_RADIUS_KM, out HitDistancesKm[i] );
				HitsCount += Hits[i] ? 1 : 0;
			}

			if ( HitsCount == 2 || HitsCount == 3 )
			{	// Browse the rays 2 by 2, the current ray and its successor (since the rays are circularly ordered)
				//	* If current & next rays don't hit then store only next ray
				//	* If current ray hits but next ray doesn't, store tangent ray and next ray
				//	* If next ray hits but current ray doesn't, store only tangent ray
				//	* If none of the rays hit, store nothing
				Vector3[]	NewFrustum = new Vector3[4];
				int			CornerIndex = 0;
				for ( int i=0; i < 4; i++ )
				{
					int	Ni = (i+1)&3;
					if ( Hits[i] && !Hits[Ni] )
					{
						NewFrustum[CornerIndex++] = ComputeTangentRay( _CameraPositionKm, _CameraFrustumKm[i], _CameraFrustumKm[Ni], PlanetCenterKm, PLANET_RADIUS_KM );
						NewFrustum[CornerIndex++] = _CameraFrustumKm[Ni];
					}
					else if ( !Hits[i] && Hits[Ni] )
						NewFrustum[CornerIndex++] = ComputeTangentRay( _CameraPositionKm, _CameraFrustumKm[Ni], _CameraFrustumKm[i], PlanetCenterKm, PLANET_RADIUS_KM );
					else if ( !Hits[i] && !Hits[Ni] )
						NewFrustum[CornerIndex++] = _CameraFrustumKm[Ni];
				}

				if ( CornerIndex == 3 )
					NewFrustum[CornerIndex++] = NewFrustum[CornerIndex-1];	// Double the last vertex...

				return NewFrustum;
			}
			else if ( HitsCount == 4 )
			{	// When all the rays hit the planet (like when viewing down), we need to use the frustum's projection onto the planet
				Vector3[]	NewFrustum = new Vector3[4];
				for ( int i=0; i < 4; i++ )
					NewFrustum[i] = _CameraPositionKm + HitDistancesKm[i] * _CameraFrustumKm[i];	// Go to hit position...

				return NewFrustum;
			}

			return _CameraFrustumKm;	// No change... Only 1 hit is unlikely, unless the camera is rolling
		}

		/// <summary>
		/// Computes the ray tangent to the specified sphere
		/// </summary>
		/// <param name="_CameraPositionKm"></param>
		/// <param name="_RayDown"></param>
		/// <param name="_RayUp"></param>
		/// <param name="_Center"></param>
		/// <param name="_RadiusKm"></param>
		/// <returns></returns>
		protected Vector3	ComputeTangentRay( Vector3 _CameraPositionKm, Vector3 _RayDown, Vector3 _RayUp, Vector3 _CenterKm, float _RadiusKm )
		{
			float	L0 = _RayDown.Length();
			_RayDown /= L0;
			float	L1 = _RayUp.Length();
			_RayUp /= L1;

			Vector3	D = _CameraPositionKm - _CenterKm;
			Vector3	W = _RayUp - _RayDown;

			float	k = Vector3.Dot( D, D ) - _RadiusKm*_RadiusKm;

			float	DV0 = Vector3.Dot( D, _RayDown );
			float	V0V0 = Vector3.Dot( _RayDown, _RayDown );
			float	DW = Vector3.Dot( D, W );
			float	V0W = Vector3.Dot( _RayDown, W );
			float	WW = Vector3.Dot( W, W );

			float	a = DW*DW - k*WW*WW;
			float	b = DV0*DW - k*V0W;
			float	c = DV0*DV0 - k*V0V0;

			float	Delta = b*b - a*c;
			if ( Delta < 0.0f )
				throw new Exception( "Can't find tangent ray ! This means both rays don't intersect the sphere. Check your rays before calling this function !" );

			Delta = (float) Math.Sqrt( Delta );
			a = 1.0f / a;

			float	t0 = (-b - Delta) * a;
			float	t1 = (-b + Delta) * a;

			if ( t0 < 0.0f || t0 > 1.0f )
				t0 = t1;	// This is the other solution...
			if ( t0 < 0.0f || t0 > 1.0f )
				throw new Exception( "No valid tangent was found !" );

			return (L0 + (L1-L0) * t0) * (_RayDown + t0 * W);
		}

		/// <summary>
		/// Computes the ENTRY intersection of a ray with a sphere
		/// </summary>
		/// <param name="_P"></param>
		/// <param name="_V"></param>
		/// <param name="_Center"></param>
		/// <param name="_RadiusKm"></param>
		/// <param name="_fDistanceKm"></param>
		/// <returns></returns>
		protected bool	ComputeSphereEnterIntersection( Vector3 _P, Vector3 _V, Vector3 _Center, float _RadiusKm, out float _fDistanceKm )
		{
			_fDistanceKm = -1.0f;

			Vector3	D = _P - _Center;
			float	a = Vector3.Dot( _V, _V );
			float	b = Vector3.Dot( _V, D );
			float	c = Vector3.Dot( D, D ) - _RadiusKm*_RadiusKm;
			float	Delta = b*b - a*c;
			if ( Delta < 0.0f )
				return false;

			Delta = (float) Math.Sqrt( Delta );

			_fDistanceKm = (-b - Delta) / a;
			return _fDistanceKm >= 0.0f && _fDistanceKm < 1.0f;
		}

		/// <summary>
		/// Computes the convex hull of a set of points using the gift wrapping algorithm
		/// (cf. http://en.wikipedia.org/wiki/Gift_wrapping_algorithm)
		/// </summary>
		/// <param name="_Points"></param>
		/// <param name="_ConvexHullIndices">The list of indices used in the convex hull</param>
		/// <returns>The 2D convex hull</returns>
		protected Vector2[]	ComputeConvexHull( Vector2[] _Points, out int[] _HullIndices )
		{
			int	Count = _Points.Length;

			// Find left most point
			int		LeftMostIndex = -1;
			float	LeftMostPosition = +float.MaxValue;
			for ( int i=0; i < Count; i++ )
				if ( _Points[i].X < LeftMostPosition )
				{
					LeftMostPosition = _Points[i].X;
					LeftMostIndex = i;
				}

			// Start building the convex hull
			_HullIndices = new int[Count];
			List<Vector2>	Hull = new List<Vector2>();
			int		HullIndex = 0, EndPoint;
			int		PointOnHull = LeftMostIndex;
			do 
			{
				_HullIndices[HullIndex++] = PointOnHull;
				Vector2	P0 = _Points[PointOnHull];
				Hull.Add( P0 );

				EndPoint = 0;	// Start from first point
				Vector2	P1 = _Points[EndPoint];
				for ( int i=1; i < Count; i++ )
					if ( EndPoint == PointOnHull || IsLeftOf( _Points[i], P0, P1 ) )
					{	// Found a new better outer point for the hull !
						EndPoint = i;
						P1 = _Points[i];
					}

				PointOnHull = EndPoint;	// Assign our new point

			} while ( EndPoint != _HullIndices[0] );

			return Hull.ToArray();
		}

		/// <summary>
		/// Returns true if P is "to the left" of segment [P0,P1]
		/// </summary>
		/// <param name="P"></param>
		/// <param name="P0"></param>
		/// <param name="P1"></param>
		/// <returns></returns>
		protected bool	IsLeftOf( Vector2 P, Vector2 P0, Vector2 P1 )
		{
			Vector2	D0 = P1 - P0;
			Vector2	D1 = P - P0;
			float	Cross = D0.X * D1.Y - D0.Y * D1.X;
			return Cross > 0.0f;
		}

		/// <summary>
		/// Reduces a 5-vertices convex hull into a 4-vertices quadrilateral by removing one specific vertex (the camera vertex)
		/// The routine is a simplification of the one found in ftp://ftp.cs.unc.edu/pub/techreports/02-024.pdf
		/// </summary>
		/// <param name="_ConvexHull"></param>
		/// <param name="_ConvexHullIndices"></param>
		/// <param name="_VertexToRemove"></param>
		/// <returns></returns>
		protected Vector2[]	ReduceConvexHull( Vector2[] _ConvexHull, int[] _ConvexHullIndices, int _VertexToRemove )
		{
			// We have a 5-vertices convex hull when the camera position stands outside of the convex hull to create a "letter shape":
			//
			//         x <== Camera standing out
			//       -- --
			//     --     --
			//  x--         --x
			//  |             |
			//  |             |
			//  |             |
			//  x-------------x
			//
			// We then use the method described in the paper quoted earlier to reduce one of the 2 edges sharing the camera vertex.
			//
			//  x <== New vertex
			//  |--
			//  |  --
			//  |    --
			//  |      o <== Camera vertex eliminated
			//  |    .  --
			//  |  .      --
			//  o.  \       --x
			//  |    removed  |
			//  |    edge     |
			//  |             |
			//  x-------------x
			//
			int	Count = _ConvexHull.Length;

			// 1] First, we find the occurence of the vertex to remove
			int	PivotIndex = -1;
			for ( int i=0; i < Count; i++ )
				if ( _ConvexHullIndices[i] == _VertexToRemove )
				{
					PivotIndex = i;
					break;
				}

			if ( PivotIndex == -1 )
				throw new Exception( "Vertex #" + _VertexToRemove + " was not found as part of the convex hull ! (how is that possible ?)" );

			// 2] Build the list of concerned vertices in an ordered list:
			// . V0 = 2 vertices BEFORE pivot
			// . V1 = 1 vertex BEFORE pivot
			// . V2 = pivot
			// . V3 = 1 vertex AFTER pivot
			// . V4 = 2 vertices AFTER pivot
			//
			int	EdgeIndex_m2 = (PivotIndex + Count-2) % Count;
			int	EdgeIndex_m1 = (PivotIndex + Count-1) % Count;
			int	EdgeIndex_p1 = (PivotIndex+1) % Count;
			int	EdgeIndex_p2 = (PivotIndex+2) % Count;

			Vector2[]	Vertices = new Vector2[]
			{
				_ConvexHull[EdgeIndex_m2],
				_ConvexHull[EdgeIndex_m1],
				_ConvexHull[PivotIndex],
				_ConvexHull[EdgeIndex_p1],
				_ConvexHull[EdgeIndex_p2],
			};

			// 3] Compute the intersection of the concerned edges and choose the edge removal that adds the minimal area
			float	Area0;
			Vector2	Intersection0 = ComputeIntersection( Vertices[1], Vertices[1] - Vertices[0], Vertices[2], Vertices[2] - Vertices[3], out Area0 );
			float	Area1;
			Vector2	Intersection1 = ComputeIntersection( Vertices[2], Vertices[2] - Vertices[1], Vertices[3], Vertices[3] - Vertices[4], out Area1 );

// 			if ( Area0 < 0.0f && Area1 < 0.0f )
// 				throw new Exception( "No valid edge to remove ! Can't reduce convex hull ! WTH ?" );

			bool	bRemoveEdge0 = false;
			if ( Area0 > 0.0f && Area1 > 0.0f )
				bRemoveEdge0 = Area0 < Area1;	// Choose the minimal area
			else
				bRemoveEdge0 = Area0 > 0.0f;	// Choose the only one with a valid intersection

			// 4] Build final list
			if ( bRemoveEdge0 )
				return new Vector2[]
				{
					Vertices[0],
					Intersection0,	// This intersection replaces vertices 1 and 2
					Vertices[3],
					Vertices[4]
				};
			else
				return new Vector2[]
				{
					Vertices[0],
					Vertices[1],
					Intersection1,	// This intersection replaces vertices 2 and 3
					Vertices[4]
				};
		}

		/// <summary>
		/// Computes the intersection of 2 lines (P0,V0) and (P1,V1) if it exists and computes the
		///  area added by the (P0,P1,Intersection) triangle.
		/// </summary>
		/// <param name="_P0"></param>
		/// <param name="_V0"></param>
		/// <param name="_P1"></param>
		/// <param name="_V1"></param>
		/// <param name="_Area">The additional area, which will be negative if there is no intersection</param>
		/// <returns></returns>
		protected Vector2	ComputeIntersection( Vector2 _P0, Vector2 _V0, Vector2 _P1, Vector2 _V1, out float _Area )
		{
			Vector2	D = _P0 - _P1;
			float	t = -(D.X * _V1.Y - D.Y * _V1.X) / (_V0.X * _V1.Y - _V0.Y * _V1.X);
			Vector2	I = _P0 + t * _V0;

			Vector2	E = I - _P1;
			_Area = 0.5f * (E.X * D.Y - E.Y * D.X);

			return I;
		}

		/// <summary>
		/// Computes the quadrilateral encompassing the camera position and the 4 base positions, assuming the camera position is OUTSIDE of the quadrilateral of base positions
		/// </summary>
		/// <param name="_CameraPosition"></param>
		/// <param name="_BasePositions"></param>
		/// <returns></returns>
		protected Vector2[]	ComputeEncompassingQuadrilateral( Vector2 _CameraPosition, Vector2[] _BasePositions )
		{
			// The configuration goes like this :
			//
			//    X <== Camera Position
			//
			//               x 3
			//             . .
			//           .   .
			//         .     .
			//     0 x       .     <= Any convex quadrilateral, the result of the projection of the base of the camera frustum
			//       |        .
			//     1 x        .
			//          -     .
			//             -  .
			//                x 2
			//
			// Two of the vertices of the quadrilateral we need to find are merged into only one vertex : the camera
			// The solution quadrilateral is then a triangle whose apex is the camera and that need to encompass the base quadrilateral [0,1,2,3]
			//
			// 1] First, we need to find the 2 angles defining the direction of the 2 lines of the triangle that will enclose the quadrilateral
			//
			Vector2	Center = 0.25f * (_BasePositions[0] + _BasePositions[1] + _BasePositions[2] + _BasePositions[3]);	// Approximate iso center of the quadrilateral
			Vector2	Camera2Center = Center - _CameraPosition;
			float	Camera2CenterLength = Camera2Center.Length();

			float	MaxAngle = -float.MaxValue;
			int		MaxAngleVertex = -1;
			float	MinAngle = +float.MaxValue;
			int		MinAngleVertex = -1;
			for ( int i=0; i < 4; i++ )
			{
				Vector2	Camera2Base = _BasePositions[i] - _CameraPosition;
				float	Camera2BaseLength = Camera2Base.Length();
				float	Cross = (float) Math.Asin( (Camera2Center.X * Camera2Base.Y - Camera2Center.Y * Camera2Base.X) / (Camera2CenterLength * Camera2BaseLength) );
				if ( Cross > MaxAngle )
				{
					MaxAngle = Cross;
					MaxAngleVertex = i;
				}
				if ( Cross < MinAngle )
				{
					MinAngle = Cross;
					MinAngleVertex = i;
				}
			}

			Vector2	Dir0 = _BasePositions[MinAngleVertex] - _CameraPosition;
			Vector2	Dir1 = _BasePositions[MaxAngleVertex] - _CameraPosition;

			// 2] Second, we need to find 2 points on the 2 free lines of the triangle so we minimize the area covered by the triangle.
			// This is a complex problem as we're facing an infinite number of solutions here

			// 2.1] We start by isolating the 2 vertices of the base which are the most far away from the camera
			float	MaxDistance0 = -float.MaxValue;
			int		MaxDistanceVertex0 = -1;
			float	MaxDistance1 = -float.MaxValue;
			int		MaxDistanceVertex1 = -1;
			for ( int i=0; i < 4; i++ )
			{
				Vector2	Camera2Base = _BasePositions[i] - _CameraPosition;
//				float	Distance = Camera2Base.LengthSquared();
				float	Distance = Vector2.Dot( Camera2Base, Camera2Center );	// We need depth, not distance here...
				if ( Distance > MaxDistance0 )
				{
					MaxDistanceVertex1 = MaxDistanceVertex0;
					MaxDistance1 = MaxDistance0;
					MaxDistanceVertex0 = i;
					MaxDistance0 = Distance;
				}
			}

			// From there, we know that we can't find a segment that is closer than the farthest vertex

			// 2.2] Let's simply assume the choice of taking the segment orthogonal to the Camera=>Center vector and passing through the farthest vertex of the base...
			Vector2	Ortho = new Vector2( -Camera2Center.Y, Camera2Center.X );
			Vector2	Start = _BasePositions[MaxDistanceVertex0];

			// 2.3] Then we compute the intersections of that line with the 2 lines we already have...
			Vector2	Camera2Start = Start - _CameraPosition;
			float	t0 = -(Camera2Start.X * Dir0.Y - Camera2Start.Y * Dir0.X) / (Dir0.Y * Ortho.X - Dir0.X * Ortho.Y);	// Intersection with (CameraPosition,Dir0)
			float	t1 = -(Camera2Start.X * Dir1.Y - Camera2Start.Y * Dir1.X) / (Dir1.Y * Ortho.X - Dir1.X * Ortho.Y);	// Intersection with (CameraPosition,Dir1)

			Vector2	QuadVertex0 = Start + t0 * Ortho;
			Vector2	QuadVertex1 = Start + t1 * Ortho;

			// 3] We now have our 4 points
			return new Vector2[4]
			{
				_CameraPosition,
				_CameraPosition,
				QuadVertex0,
				QuadVertex1
			};
		}

// 		/// <summary>
// 		/// Transforms a XY position in shadow plane space into a parametric UV coordinate within the shadow quadrilateral
// 		/// </summary>
// 		/// <param name="P"></param>
// 		/// <returns></returns>
// 		protected Vector2	XY2UV( Vector2 P )
// 		{
// //			Vector2	pu0 = m_ShadowQuadKm[0];
// // 			Vector2	pu1 = m_ShadowQuadKm[3];
// // 			Vector2	du0 = m_ShadowQuadKm[1] - pu0;
// // 			Vector2	du1 = m_ShadowQuadKm[2] - pu1;
// // 
// // 			float	u0 = Vector2.Dot( P - pu0, du0 ) / du0.LengthSquared();
// // 			float	u1 = Vector2.Dot( P - pu1, du1 ) / du1.LengthSquared();
// // 			float	u = 0.5f * (u0 + u1);
// // 
// // 			Vector2	pv0 = pu0 + u * du0;
// // 			Vector2	pv1 = pu1 + u * du1;
// // 			Vector2	dv = pv1 - pv0;
// // 			float	v = Vector2.Dot( P - pv0, dv ) / dv.LengthSquared();
// // //			float	v = Vector2.Dot( P - pv0, dv ) / (dv.Length() * (P-pv0).Length());
// // 
// // 			return new Vector2( u, v );
// 
// // 			// The shadow quadrilateral is defined by 4 vertices : P0, P1, P2 and P3
// // 			//
// // 			//               x P3
// // 			//             . .
// // 			//           .   .
// // 			//         .     .
// // 			//    P0 x       .
// // 			//       |        .
// // 			//    P1 x        .
// // 			//          -     .
// // 			//             -  .
// // 			//                x P2
// // 			//
// // 			// Positions within the quadrilateral are parametrized by a (u,v) couple so that :
// // 			//		x0 = P0 + [P2 - P0].u	<= Position along the left segment [P0,P1]
// // 			//		x1 = P3 + [P2 - P3].u	<= Position along the right segment [P3,P2]
// // 			// and	p = x0 + [x1 - x0].v
// // 			//
// // 			// In this case, we know P as it's the projection of any WORLD space position into the
// // 			//	shadow plane and we need to retrieve the (u,v) parameters to sample the shadow map.
// // 			//
// // 			// We start by writing:
// // 			//	p(u) = P0 + D.u						where D = P1 - P0
// // 			//	v(u) = P3 + D2.u - p(u)				where D2 = P2 - P3
// // 			//		 = [P3 - P0] + [D2 - D].u
// // 			//		 = A + B.u						where A = P3 - P0  and B = D2 - D = P0 + P2 - P1 - P3 
// // 			//
// // 			// Next, we can write that if P (our known position) stands on the [p(u),v(u)] segment then:
// // 			//
// // 			//	[P-p(u)] x v(u) = 0
// // 			//	[P-P0 - D.u] x v(u) = 0
// // 			//	[C - D.u] x [A + B.u] = 0			where C = P - P0
// // 			//
// // 			// Developping:
// // 			//	[C x A] - [D x A].u + [C x B].u - [B x D].u² = 0
// // 			//
// // 			// And we get u !
// // 			// v is really simple to get from here...
// // 			//
// // 			Vector2	P0 = m_ShadowQuadKm[0];
// // 			Vector2	P3 = m_ShadowQuadKm[3];
// // 			Vector2	D = m_ShadowQuadKm[1] - P0;
// // 			Vector2	D2 = m_ShadowQuadKm[2] - P3;
// // 			Vector2	A = P3 - P0;
// // 			Vector2	B = D2 - D;
// // 			Vector2	C = P - P0;
// // 
// // 			double	a = -(B.X * D.Y - B.Y * D.X);
// // 			double	b = (C.X * B.Y - C.Y * B.X) - (D.X * A.Y - D.Y * A.X);
// // 			double	c = C.X * A.Y - C.Y * A.X;
// // 
// // 			double	Delta = b*b - 4.0f * a*c;
// // 			if ( Delta < 0.0f )
// // 				throw new Exception( "Negative fucking delta !" );
// // 
// // 			Delta = Math.Sqrt( Delta );
// // 
// // 			double	u = 0.5 * (-b + Delta) / a;
// // 			double	u2 = 0.5 * (-b - Delta) / a;
// // 			if ( u < 0.0f || u > 1.0f )
// // 				throw new Exception( "ARGH!" );
// // 
// // 			// Find v
// // 			Vector2	x0 = P0 + (float) u * D;
// // 			Vector2	x1 = P3 + (float) u * D2;
// // 			float	v = Vector2.Dot( P - x0, x1 - x0 ) / (x1 - x0).LengthSquared();
// // 
// // // CHECK
// // float	Cross = (P - x0).X * (x1 - x0).Y - (P - x0).Y * (x1 - x0).X;	// Should always be 0 !
// // 
// // 			return new Vector2( (float) u, v );
// 
// 			// Best yet is http://www.geometrictools.com/Documentation/PerspectiveMappings.pdf
// 			// To sum up the idea, any 2D convex quadrilateral can be considered as a projection of
// 			//	a rotated 3D quadrilateral seen from a given point of view E : a typical perspective
// 			//	transform.
// 			//
// 			// Let q00, q10, q01 and q11 be the corners of this 2D quadrilateral
// 			//
// 			// 1] First we search for a0 and a1 such as :
// 			//	q11-q00 = a0.(q10 - q00) + a1.(q01 - q00)
// 			//
// 			// This means we're looking for the unique combination of the edges (q10-q00) and (q01-q00) that will yield the vector (q11-q00)
// 			// 2 equations, 2 unknowns : easy !
// 			//
// 			Vector2	q00 = m_ShadowQuadKm[0];
// 			Vector2	q10 = m_ShadowQuadKm[1];
// 			Vector2	q11 = m_ShadowQuadKm[2];
// 			Vector2	q01 = m_ShadowQuadKm[3];
// 			Vector2	D10 = q10 - q00;
// 			Vector2	D01 = q01 - q00;
// 			Vector2	D11 = q11 - q00;
// 
// 			float	a0 = (D11.Y * D01.X - D11.X * D01.Y) / (D10.Y * D01.X - D10.X * D01.Y);
// 			float	a1 = (D11.Y * D10.X - D11.X * D10.Y) / (D10.X * D01.Y - D10.Y * D01.X);
// 
// // CHECK
// Vector2	Pipo = a0 * D10 + a1 * D01;
// 
// 			// Let r00, r10, r01, r11 be the 3D coordinates of the 4 corners q00,q01,q10&q11 of the quadrilateral
// 			// These coordinates are obtained by assuming a rotation of the 2D coordinates by an arbitrary matrix R.
// 			// Since rotations preserve the relative positions of the vertices, we can also write :
// 			//
// 			//	r11 = a0.r10 + a1.r01
// 			//
// 			// We also choose the coordinates system so that r00 = (0,0,0) (the origin)
// 			//
// 			// Assuming any projection of a 3D point r on the quadrilateral r00,r10,r01,r11 to the plane z=0 can be written:
// 			//
// 			//	P = E + t.(r - E) for an arbitrary t
// 			//
// 			// We can write the special cases of the 4 corners that must project to the unit square:
// 			//
// 			//	E + t00.(r00 - E) = (0,0,0)	<= Projects to the origin
// 			//	E + t10.(r10 - E) = (1,0,0)
// 			//	E + t01.(r01 - E) = (0,1,0)
// 			//	E + t11.(r11 - E) = (1,1,0)
// 			//
// 			// We can easily find that t00 = 0
// 			// Let N be the normal to the rotated quadrilateral r00,r01,r10,r11
// 			// Dotting any r on that quadrilateral with N will yield r.N = 0
// 			// We can thus write:
// 			// 
// 			//		E.N + t10.(r10.N - E.N) = N.x		(by dotting line 2 of our 4 equations above)
// 			// =>	E.N - t10.E.N = N.x
// 			// And:
// 			//		E.N + t01.(r01.N - E.N) = N.y		(by dotting line 3 of our 4 equations above)
// 			// =>	E.N - t01.E.N = N.y
// 			// And:
// 			//		E.N + t11.(r11.N - E.N) = N.x + N.y	(by dotting line 4 of our 4 equations above)
// 			// =>	E.N - t11.E.N = N.x + N.y
// 			//
// 			// So:
// 			//		E.N - t11.E.N = E.N - t10.E.N + E.N - t01.E.N
// 			//
// 			// Assuming E.N != 0 we obtain:
// 			//		1 - t11 = 2 - t10 - t01
// 			//		t11 = t10 + t01 - 1
// 			//
// 			// Rewriting the last line of the 4 equations with our knowledge of t11 and r11:
// 			//
// 			//	E + (t10 + t01 - 1).(a0.r10 + a1.r01 - E) = (1,1,0)
// 			//	(2 - t10 - t01).E + (t10 + t01 - 1).(a0.r10 + a1.r01) = (1,1,0)
// 			//
// 			// Adding line 2 and 3 of the 4 equations gives us:
// 			//
// 			//	2E + t10.(r10 - E) + t01.(r01 - E) = (1,1,0)
// 			//	(2 - t10 - t01).E + t10.r10 + t01.r01 = (1,1,0)
// 			//
// 			// Subtracting these last 2 equations yields:
// 			//
// 			//	(2 - t10 - t01).E + (t10 + t01 - 1).(a0.r10 + a1.r01) - (2 - t10 - t01).E - t10.r10 - t01.r01 = (0,0,0)
// 			//	[(t10 + t01 - 1).a0 - t10].r10 + [(t10 + t01 - 1).a1 - t01].r01 = (0,0,0)
// 			//
// 			// Since r10 and r01 are not colinear, each coefficient must be 0:
// 			//
// 			//	[(t10 + t01 - 1).a0 - t10] = 0
// 			//	[(t10 + t01 - 1).a1 - t01] = 0
// 			//
// 			// Thus, solving for t01 and t10:
// 			//
// 			//	t10 = a0 / (a0 + a1 - 1)
// 			//	t01 = a1 / (a0 + a1 - 1)
// 			//	t11 =  1 / (a0 + a1 - 1)
// 			//
// 			// At this point, we could compute E and N and retrieve the rotation and projection matrices to create
// 			//	our perspective transform but we can write the transform in a more convenient way:
// 			//
// 			// Consider a point R standing on the rotated quadrilateral that is projected to a square point P(x,y,0).
// 			// We may write R as combination of the 2 vectors r10 and r01 such as :
// 			//	R = Rx.r10 + Ry.r01
// 			//
// 			// The ray equation for that pair of points is:
// 			//
// 			//	P = E + t.(R-E) = E + t.(Rx.r10 + Ry.r01 - E)
// 			//
// 			// Now that we know our values for t00, t01, t10 and t11, we can rewrite line 2 and 3 of the 4 equations above to solve for r10 and r01:
// 			//
// 			//	E + a0/(a0+a1-1).(r10 - E) = (1,0,0)
// 			//	E + a1/(a0+a1-1).(r01 - E) = (1,0,0)
// 			//
// 			//	r10 = [(1,0,0) + E.(t10 - 1)] / t10
// 			//	r01 = [(0,1,0) + E.(t01 - 1)] / t01
// 			//
// 			// Substituting in the ray equation:
// 			//
// 			//	P = E + t.(Rx.[(1,0,0) + E.(t10 - 1)] / t10 + Ry.[(0,1,0) + E.(t01 - 1)] / t01 - E)
// 			//
// 			// Grouping:
// 			//
// 			//	(1 - t.Rx.(1 - t10)/t10 - t.Ry.(1 - t01)/t01 - t).E + (t.Rx/t10 - Px).(1,0,0) + (t.Ry/t01 - Py).(0,1,0) = (0,0,0)
// 			//
// 			// We know that E, (1,0,0) and (0,1,0) are not colinear so it must mean that each of the 3 coefficients must be 0.
// 			// Solving for t in the first coefficient and replacing t in the 2nd and 3rd coefficients, we get:
// 			//
// 			//	Px = a1.(a0 + a1 - 1).Rx / [a0.a1 + a1.(a1 - 1).Rx + a0.(a0 - 1).Ry]
// 			//	Py = a0.(a0 + a1 - 1).Ry / [a0.a1 + a1.(a1 - 1).Rx + a0.(a0 - 1).Ry]
// 			//
// 			// Inversely:
// 			//
// 			//	Rx = a0.Px / [(a0 + a1 - 1) + (1 - a1).Px + (1 - a0).Py]
// 			//	Ry = a1.Py / [(a0 + a1 - 1) + (1 - a1).Px + (1 - a0).Py]
// 			//
// // 			float		t10 = a0 / (a0 + a1 - 1.0f);
// // 			float		t01 = a1 / (a0 + a1 - 1.0f);
// // 			float		t11 = 1.0f / (a0 + a1 - 1.0f);
// // 
// // 			float[]	CoeffsE = new float[3]
// // 			{
// // 				1.0f - t11,
// // 				1.0f - t10,
// // 				1.0f - t01
// // 			};
// // 
// // 			float[]	CoeffsR10 = new float[3]
// // 			{
// // 				t11*a0,
// // 				t10,
// // 				0.0f
// // 			};
// // 
// // 			float[]	CoeffsR01 = new float[3]
// // 			{
// // 				t11*a1,
// // 				0.0f,
// // 				t01
// // 			};
// // 
// // 			float[]	Results = new float[]
// // 			{
// // 				-1, -1, 0,
// // 				-1,  0, 0,
// // 				 0, -1, 0,
// // 				 1
// // 			};
// // 
// //			WMath.Matrix	F = new WMath.Matrix( 10 );
// // 			for ( int i=0; i < 3; i++ )
// // 				for ( int j=0; j < 3; j++ )
// // 				{
// // 					F[3*0+i,3*0+j] = CoeffsE[0];
// // 					F[3*1+i,3*0+j] = CoeffsE[1];
// // 					F[3*2+i,3*0+j] = CoeffsE[2];
// // 
// // 					F[3*0+i,3*1+j] = CoeffsR10[0];
// // 					F[3*1+i,3*1+j] = CoeffsR10[1];
// // 					F[3*2+i,3*1+j] = CoeffsR10[2];
// // 
// // 					F[3*0+i,3*2+j] = CoeffsR01[0];
// // 					F[3*1+i,3*2+j] = CoeffsR01[1];
// // 					F[3*2+i,3*2+j] = CoeffsR01[2];
// // 				}
// // 			for ( int i=0; i < 10; i++ )
// // 				F[i,9] = Results[i];
// 
// // 			WMath.Matrix	F = new WMath.Matrix( 9 );
// // 			F[0,0] = CoeffsE[0];		F[0,3] = CoeffsR10[0];		F[0,6] = CoeffsR01[0];
// // 			F[1,1] = CoeffsE[0];		F[1,4] = CoeffsR10[0];		F[1,7] = CoeffsR01[0];
// // 			F[2,2] = CoeffsE[0];		F[2,5] = CoeffsR10[0];		F[2,8] = CoeffsR01[0];
// // 
// // 			F[3,0] = CoeffsE[1];		F[3,3] = CoeffsR10[1];		F[3,6] = CoeffsR01[1];
// // 			F[4,1] = CoeffsE[1];		F[4,4] = CoeffsR10[1];		F[4,7] = CoeffsR01[1];
// // 			F[5,2] = CoeffsE[1];		F[5,5] = CoeffsR10[1];		F[5,8] = CoeffsR01[1];
// // 
// // 			F[6,0] = CoeffsE[2];		F[6,3] = CoeffsR10[2];		F[6,6] = CoeffsR01[2];
// // 			F[7,1] = CoeffsE[2];		F[7,4] = CoeffsR10[2];		F[7,7] = CoeffsR01[2];
// // 			F[8,2] = CoeffsE[2];		F[8,5] = CoeffsR10[2];		F[8,8] = CoeffsR01[2];
// // 
// // //			WMath.Matrix	Inverse = F.Invert();
// // 
// // 			double[]	Result = F.Solve( new double[]
// // 				{
// // 					1, 1, 0,
// // 					1, 0, 0,
// // 					0, 1, 0
// // 				} );
// 
// // 			float	InvDen = (a0 + a1 - 1.0f) + (1.0f - a1) * P.X + (1.0f - a0) * P.Y;
// // 					InvDen = 1.0f / InvDen;
// // 
// // 			return InvDen * new Vector2( a0 * P.X, a1 * P.Y );
//  
// // 			float		t10 = a0 / (a0 + a1 - 1.0f);
// // 			float		t01 = a1 / (a0 + a1 - 1.0f);
// // 			float		t11 = 1.0f / (a0 + a1 - 1.0f);
// // 
// // 			Vector2	E = (-t01 * t11 * a0 * Vector2.UnitX - t10*t11*a1*Vector2.UnitY)
// // 					  / (t10*t01*(1-t11) + t01*t11*a0*(t10-1) * t10*t11*a1*(t01-1));
// // 
// // 			Vector2	r10 = (Vector2.UnitX + (t10 - 1) * E) / t10;
// // 			Vector2	r01 = (Vector2.UnitY + (t01 - 1) * E) / t01;
// // 
// // 			float	y0 = Vector2.Dot( P - q00, r10 );
// // 			float	y1 = Vector2.Dot( P - q00, r01 );
// // 
// // // 			D10.Normalize();
// // // 			D01.Normalize();
// // // 			float	y0 = Vector2.Dot( P - q00, D10 ) / D10.Length();
// // // 			float	y1 = Vector2.Dot( P - q00, D01 ) / D01.Length();
// // 
// // // 			float	y0 = P.X - q00.X;
// // // 			float	y1 = P.Y - q00.Y;
// // 
// // // 			float	y0 = P.X;
// // // 			float	y1 = P.Y;
// // 
// // 			float	InvDen = a0*a1 + a1*(a1-1.0f)*y0 + a0*(a0-1.0f)*y1;
// // 					InvDen = 1.0f / InvDen;
// // 
// // 			return InvDen * new Vector2( a1*(a0 + a1 - 1.0f) * y0, a0*(a0 + a1 - 1.0f) * y1 );
// 
// 
// 			float	x0 = m_ShadowQuadKm[0].X;
// 			float	y0 = m_ShadowQuadKm[0].Y;
// 			float	x1 = m_ShadowQuadKm[1].X;
// 			float	y1 = m_ShadowQuadKm[1].Y;
// 			float	x2 = m_ShadowQuadKm[3].X;
// 			float	y2 = m_ShadowQuadKm[3].Y;
// 			float	x3 = m_ShadowQuadKm[2].X;
// 			float	y3 = m_ShadowQuadKm[2].Y;
// 
// 			float	c = x0;
// 			float	f = y0;
// 
// 			float	Kx = x1 + x2 - x0 - x3;
// 			float	Ky = y1 + y2 - y0 - y3;
// 			float	Den = y2 - y3 + (y1 - y3) / (x1 - x3);
// 			float	h = (Kx * (y1 - y3) / (x1 - x3) - Ky) / Den;
// 
// 			float	g = ((x3 - x2) * h - Kx) / (x1 - x3);
// 
// 			float	a = (1+g) * x1 - x0;
// 			float	d = (1+g) * y1 - y0;
// 
// 			float	b = (1+h) * x2 - x0;
// 			float	e = (1+h) * y2 - y0;
// 
// 			WMath.Matrix3x3	M = new WMath.Matrix3x3();
// 			M.m[0,0] = a; M.m[0,1] = d; M.m[0,2] = g;
// 			M.m[1,0] = b; M.m[1,1] = e; M.m[1,2] = h;
// 			M.m[2,0] = c; M.m[2,1] = f; M.m[2,2] = 1;
// 
// 			WMath.Matrix3x3	Mi = M.Invert();
// 
// // CHECK
// WMath.Vector	Test00 = new WMath.Vector( x0, y0, 1 ) * Mi;
// Test00 /= Test00.z;
// WMath.Vector	Test01 = new WMath.Vector( x1, y1, 1 ) * Mi;
// Test01 /= Test01.z;
// WMath.Vector	Test10 = new WMath.Vector( x2, y2, 1 ) * Mi;
// Test10 /= Test10.z;
// WMath.Vector	Test11 = new WMath.Vector( x3, y3, 1 ) * Mi;
// Test11 /= Test11.z;
// // CHECK
// 			
// 			WMath.Vector	Result = new WMath.Vector( P.X, P.Y, 1 ) * Mi;
// 			Result /= Result.z;
// 
// 			return new Vector2( Result.x, Result.y );
// 		}
// 
// 		/// <summary>
// 		/// Transforms a parametric UV coordinate within the shadow quadrilateral into a XY position in shadow plane space
// 		/// </summary>
// 		/// <param name="P"></param>
// 		/// <returns></returns>
// 		protected Vector2	UV2XY( Vector2 P )
// 		{
// 			// Best yet is http://www.geometrictools.com/Documentation/PerspectiveMappings.pdf
// 			// To sum up the idea, any 2D convex quadrilateral can be considered as a projection of
// 			//	a rotated 3D quadrilateral seen from a given point of view E : a typical perspective
// 			//	transform.
// 			//
// 			// Let q00, q10, q01 and q11 be the corners of this 2D quadrilateral
// 			//
// 			// 1] First we search for a0 and a1 such as :
// 			//	q11-q00 = a0.(q10 - q00) + a1.(q01 - q00)
// 			//
// 			// This means we're looking for the unique combination of the edges (q10-q00) and (q01-q00) that will yield the vector (q11-q00)
// 			// 2 equations, 2 unknowns : easy !
// 			//
// 			Vector2	q00 = m_ShadowQuadKm[0];
// 			Vector2	q10 = m_ShadowQuadKm[1];
// 			Vector2	q01 = m_ShadowQuadKm[2];
// 			Vector2	q11 = m_ShadowQuadKm[3];
// 			Vector2	D10 = q10 - q00;
// 			Vector2	D01 = q01 - q00;
// 			Vector2	D11 = q11 - q00;
// 
// 			float	a0 = (D11.Y * D01.X - D11.X * D01.Y) / (D10.Y * D01.X - D10.X * D01.Y);
// 			float	a1 = (D11.Y * D10.X - D11.X * D10.Y) / (D10.X * D01.Y - D10.Y * D01.X);
// 
// // CHECK
// Vector2	Pipo = a0 * D10 + a1 * D01;
// 
// 			// Let r00, r10, r01, r11 be the 3D coordinates of the 4 corners q00,q01,q10&q11 of the quadrilateral
// 			// These coordinates are obtained by assuming a rotation of the 2D coordinates by an arbitrary matrix R.
// 			// Since rotations preserve the relative positions of the vertices, we can also write :
// 			//
// 			//	r11 = a0.r10 + a1.r01
// 			//
// 			// We also choose the coordinates system so that r00 = (0,0,0) (the origin)
// 			//
// 			// Assuming any projection of a 3D point r on the quadrilateral r00,r10,r01,r11 to the plane z=0 can be written:
// 			//
// 			//	P = E + t.(r - E) for an arbitrary t
// 			//
// 			// We can write the special cases of the 4 corners that must project to the unit square:
// 			//
// 			//	E + t00.(r00 - E) = (0,0,0)	<= Projects to the origin
// 			//	E + t10.(r10 - E) = (1,0,0)
// 			//	E + t01.(r01 - E) = (0,1,0)
// 			//	E + t11.(r11 - E) = (1,1,0)
// 			//
// 			// We can easily find that t00 = 0
// 			// Let N be the normal to the rotated quadrilateral r00,r01,r10,r11
// 			// Dotting any r on that quadrilateral with N will yield r.N = 0
// 			// We can thus write:
// 			// 
// 			//		E.N + t10.(r10.N - E.N) = N.x		(by dotting line 2 of our 4 equations above)
// 			// =>	E.N - t10.E.N = N.x
// 			// And:
// 			//		E.N + t01.(r01.N - E.N) = N.y		(by dotting line 3 of our 4 equations above)
// 			// =>	E.N - t01.E.N = N.y
// 			// And:
// 			//		E.N + t11.(r11.N - E.N) = N.x + N.y	(by dotting line 4 of our 4 equations above)
// 			// =>	E.N - t11.E.N = N.x + N.y
// 			//
// 			// So:
// 			//		E.N - t11.E.N = E.N - t10.E.N + E.N - t01.E.N
// 			//
// 			// Assuming E.N != 0 we obtain:
// 			//		1 - t11 = 2 - t10 - t01
// 			//		t11 = t10 + t01 - 1
// 			//
// 			// Rewriting the last line of the 4 equations with our knowledge of t11 and r11:
// 			//
// 			//	E + (t10 + t01 - 1).(a0.r10 + a1.r01 - E) = (1,1,0)
// 			//	(2 - t10 - t01).E + (t10 + t01 - 1).(a0.r10 + a1.r01) = (1,1,0)
// 			//
// 			// Adding line 2 and 3 of the 4 equations gives us:
// 			//
// 			//	2E + t10.(r10 - E) + t01.(r01 - E) = (1,1,0)
// 			//	(2 - t10 - t01).E + t10.r10 + t01.r01 = (1,1,0)
// 			//
// 			// Subtracting these last 2 equations yields:
// 			//
// 			//	(2 - t10 - t01).E + (t10 + t01 - 1).(a0.r10 + a1.r01) - (2 - t10 - t01).E - t10.r10 - t01.r01 = (0,0,0)
// 			//	[(t10 + t01 - 1).a0 - t10].r10 + [(t10 + t01 - 1).a1 - t01].r01 = (0,0,0)
// 			//
// 			// Since r10 and r01 are not colinear, each coefficient must be 0:
// 			//
// 			//	[(t10 + t01 - 1).a0 - t10] = 0
// 			//	[(t10 + t01 - 1).a1 - t01] = 0
// 			//
// 			// Thus, solving for t01 and t10:
// 			//
// 			//	t10 = a0 / (a0 + a1 - 1)
// 			//	t01 = a1 / (a0 + a1 - 1)
// 			//	t11 =  1 / (a0 + a1 - 1)
// 			//
// 			// At this point, we could compute E and N and retrieve the rotation and projection matrices to create
// 			//	our perspective transform but we can write the transform in a more convenient way:
// 			//
// 			// Consider a point R standing on the rotated quadrilateral that is projected to a square point P(x,y,0).
// 			// We may write R as combination of the 2 vectors r10 and r01 such as :
// 			//	R = Rx.r10 + Ry.r01
// 			//
// 			// The ray equation for that pair of points is:
// 			//
// 			//	P = E + t.(R-E) = E + t.(Rx.r10 + Ry.r01 - E)
// 			//
// 			// Now that we know our values for t00, t01, t10 and t11, we can rewrite line 2 and 3 of the 4 equations above to solve for r10 and r01:
// 			//
// 			//	E + a0/(a0+a1-1).(r10 - E) = (1,0,0)
// 			//	E + a1/(a0+a1-1).(r01 - E) = (1,0,0)
// 			//
// 			//	r10 = [(1,0,0) + E.(t10 - 1)] / t10
// 			//	r01 = [(0,1,0) + E.(t01 - 1)] / t01
// 			//
// 			// Substituting in the ray equation:
// 			//
// 			//	P = E + t.(Rx.[(1,0,0) + E.(t10 - 1)] / t10 + Ry.[(0,1,0) + E.(t01 - 1)] / t01 - E)
// 			//
// 			// Grouping:
// 			//
// 			//	(1 - t.Rx.(1 - t10)/t10 - t.Ry.(1 - t01)/t01 - t).E + (t.Rx/t10 - Px).(1,0,0) + (t.Ry/t01 - Py).(0,1,0) = (0,0,0)
// 			//
// 			// We know that E, (1,0,0) and (0,1,0) are not colinear so it must mean that each of the 3 coefficients must be 0.
// 			// Solving for t in the first coefficient and replacing t in the 2nd and 3rd coefficients, we get:
// 			//
// 			//	Px = a1.(a0 + a1 - 1).Rx / [a0.a1 + a1.(a1 - 1).Rx + a0.(a0 - 1).Ry]
// 			//	Py = a0.(a0 + a1 - 1).Ry / [a0.a1 + a1.(a1 - 1).Rx + a0.(a0 - 1).Ry]
// 			//
// 			// Inversely:
// 			//
// 			//	Rx = a0.Px / [(a0 + a1 - 1) + (1 - a1).Px + (1 - a0).Py]
// 			//	Ry = a1.Py / [(a0 + a1 - 1) + (1 - a1).Px + (1 - a0).Py]
// 			//
// 			float	InvDen = (a0 + a1 - 1.0f) + (1.0f - a1) * P.X + (1.0f - a0) * P.Y;
// 					InvDen = 1.0f / InvDen;
// 
// 			return InvDen * new Vector2( a0 * P.X, a1 * P.Y );
// 		}

		protected void		ComputeShadowTransforms_()
		{
			float	x0 = m_ShadowQuadKm[0].X;
			float	y0 = m_ShadowQuadKm[0].Y;
			float	x1 = m_ShadowQuadKm[1].X;
			float	y1 = m_ShadowQuadKm[1].Y;
			float	x2 = m_ShadowQuadKm[3].X;
			float	y2 = m_ShadowQuadKm[3].Y;
			float	x3 = m_ShadowQuadKm[2].X;
			float	y3 = m_ShadowQuadKm[2].Y;

			double	c = x0;
			double	f = y0;

			double	Kx = x1 + x2 - x0 - x3;
			double	Ky = y1 + y2 - y0 - y3;
			double	InvX1X3 = Math.Abs( x1 - x3 ) < 1e-1f ? 1.0f : 1.0f / (x1 - x3);
			double	Den = y2 - y3 + (y1 - y3) * InvX1X3;
			double	h = (Kx * (y1 - y3) * InvX1X3 - Ky) / Den;

			double	g = ((x3 - x2) * h - Kx) * InvX1X3;

			double	a = (1+g) * x1 - x0;
			double	d = (1+g) * y1 - y0;

			double	b = (1+h) * x2 - x0;
			double	e = (1+h) * y2 - y0;

			WMath.Matrix3x3	M = new WMath.Matrix3x3();
			M.m[0,0] = (float) a; M.m[0,1] = (float) d; M.m[0,2] = (float) g;
			M.m[1,0] = (float) b; M.m[1,1] = (float) e; M.m[1,2] = (float) h;
			M.m[2,0] = (float) c; M.m[2,1] = (float) f; M.m[2,2] = 1.0f;

			m_UV2ShadowQuad.M11 = M[0,0];
			m_UV2ShadowQuad.M12 = M[0,1];
			m_UV2ShadowQuad.M13 = M[0,2];
			m_UV2ShadowQuad.M21 = M[1,0];
			m_UV2ShadowQuad.M22 = M[1,1];
			m_UV2ShadowQuad.M23 = M[1,2];
			m_UV2ShadowQuad.M31 = M[2,0];
			m_UV2ShadowQuad.M32 = M[2,1];
			m_UV2ShadowQuad.M33 = M[2,2];

			M.Invert();

			m_ShadowQuad2UV.M11 = M[0,0];
			m_ShadowQuad2UV.M12 = M[0,1];
			m_ShadowQuad2UV.M13 = M[0,2];
			m_ShadowQuad2UV.M21 = M[1,0];
			m_ShadowQuad2UV.M22 = M[1,1];
			m_ShadowQuad2UV.M23 = M[1,2];
			m_ShadowQuad2UV.M31 = M[2,0];
			m_ShadowQuad2UV.M32 = M[2,1];
			m_ShadowQuad2UV.M33 = M[2,2];

// CHECK
Vector2	Test00 = XY2UV( new Vector2( x0, y0 ) );
Vector2	Test01 = XY2UV( new Vector2( x1, y1 ) );
Vector2	Test10 = XY2UV( new Vector2( x2, y2 ) );
Vector2	Test11 = XY2UV( new Vector2( x3, y3 ) );
// CHECK
		}

		Vector2	m_NU0, m_NU1;
		Vector2	m_NV0, m_NV1;

		Vector3	m_ABC, m_DEF;
		Vector3	m_GHI, m_JKL;
		protected void		ComputeShadowTransforms()
		{
			Vector2	p00 = m_ShadowQuadKm[0];
			Vector2	p10 = m_ShadowQuadKm[1];
			Vector2	p11 = m_ShadowQuadKm[2];
			Vector2	p01 = m_ShadowQuadKm[3];

			// Compute edge normals for U coordinate
			Vector2	NU0 = p00 - p01;
					NU0.Normalize();
					NU0 = new Vector2( -NU0.Y, NU0.X );
			Vector2	NU1 = p11 - p10;
					NU1.Normalize();
					NU1 = new Vector2( -NU1.Y, NU1.X );

			// Compute edge normals for V coordinates
			Vector2	NV0 = p10 - p00;
					NV0.Normalize();
					NV0 = new Vector2( -NV0.Y, NV0.X );
			Vector2	NV1 = p01 - p11;
					NV1.Normalize();
					NV1 = new Vector2( -NV1.Y, NV1.X );

			m_NU0 = NU0;
			m_NU1 = NU1;
			m_NV0 = NV0;
			m_NV1 = NV1;


//Vector2	Test00 = UV2XY( new Vector2( 0, 0 ) );
//Vector2	Test01 = UV2XY( new Vector2( 0, 1 ) );
//Vector2	Test11 = UV2XY( new Vector2( 1, 1 ) );
//Vector2	Test10 = UV2XY( new Vector2( 1, 0 ) );

			// Compute data for (u,v) => (x,y) transform
			float	A = m_NU0.X,			B = m_NU0.Y,			C = -Vector2.Dot( p00, m_NU0 );
			float	D = m_NU0.X + m_NU1.X,	E = m_NU0.Y + m_NU1.Y,	F = -Vector2.Dot( p00, m_NU0 ) - Vector2.Dot( p11, m_NU1 );

			float	G = m_NV0.X,			H = m_NV0.Y,			I = -Vector2.Dot( p00, m_NV0 );
			float	J = m_NV0.X + m_NV1.X,	K = m_NV0.Y + m_NV1.Y,	L = -Vector2.Dot( p00, m_NV0 ) - Vector2.Dot( p11, m_NV1 );

			m_ABC = new Vector3( A, B, C );
			m_DEF = new Vector3( D, E, F );
			m_GHI = new Vector3( G, H, I );
			m_JKL = new Vector3( J, K, L );
		}

/*
 * 
 *   // Returns DotPerp((x,y),(V.x,V.y)) = x*V.y - y*V.x.
    inline float DotPerp (const Vector2& vec) const;

//----------------------------------------------------------------------------
BiQuadToSqr<float>::BiQuadToSqr (const Vector2& P00, const Vector2& P10, const Vector2& P11, const Vector2& P01) : mP00(P00)
{
    mB = P10 - P00;
    mC = P01 - P00;
    mD = P11 + P00 - P10 - P01;
    mBC = mB.DotPerp(mC);	// B.x*C.y - B.y*C.x
    mBD = mB.DotPerp(mD);
    mCD = mC.DotPerp(mD);
}
//----------------------------------------------------------------------------
Vector2 BiQuadToSqr<float>::Transform (const Vector2& P)
{
    Vector2 A = mP00 - P;
    float AB = A.DotPerp(mB);
    float AC = A.DotPerp(mC);

    // 0 = ac*bc+(bc^2+ac*bd-ab*cd)*s+bc*bd*s^2 = k0 + k1*s + k2*s^2
    float k0 = AC*mBC;
    float k1 = mBC*mBC + AC*mBD - AB*mCD;
    float k2 = mBC*mBD;

    if (Math<float>::FAbs(k2) >= Math<float>::ZERO_TOLERANCE)
    {
        // The s-equation is quadratic.
        float inv = (0.0f.5)/k2;
        float discr = k1*k1 - ((float)4)*k0*k2;
        float root = Math<float>::Sqrt(Math<float>::FAbs(discr));

        Vector2 result0;
        result0.X() = (-k1 - root)*inv;
        result0.Y() = AB/(mBC + mBD*result0.X());
        float deviation0 = Deviation(result0);
        if (deviation0 == 0.0f)
        {
            return result0;
        }

        Vector2 result1;
        result1.X() = (-k1 + root)*inv;
        result1.Y() = AB/(mBC + mBD*result1.X());
        float deviation1 = Deviation(result1);
        if (deviation1 == 0.0f)
        {
            return result1;
        }

        if (deviation0 <= deviation1)
        {
            if (deviation0 <= Math<float>::ZERO_TOLERANCE)
            {
                return result0;
            }
        }
        else
        {
            if (deviation1 <= Math<float>::ZERO_TOLERANCE)
            {
                return result1;
            }
        }
    }
    else
    {
        // The s-equation is linear.
        Vector2 result;

        result.X() = -k0/k1;
        result.Y() = AB/(mBC + mBD*result.X());
        float deviation = Deviation(result);
        if (deviation <= Math<float>::ZERO_TOLERANCE)
        {
            return result;
        }
    }

    // Point is outside the quadrilateral, return invalid.
    return Vector2(Math<float>::MAX_REAL, Math<float>::MAX_REAL);
}
//----------------------------------------------------------------------------
float BiQuadToSqr<float>::Deviation (const Vector2& SPoint)
{
    // Deviation is the squared distance of the point from the unit square.
    float deviation = 0.0f;
    float delta;

    if (SPoint.X() < 0.0f)
    {
        deviation += SPoint.X()*SPoint.X();
    }
    else if (SPoint.X() > 1.0f)
    {
        delta = SPoint.X() - 1.0f;
        deviation += delta*delta;
    }

    if (SPoint.Y() < 0.0f)
    {
        deviation += SPoint.Y()*SPoint.Y();
    }
    else if (SPoint.Y() > 1.0f)
    {
        delta = SPoint.Y() - 1.0f;
        deviation += delta*delta;
    }

    return deviation;
} */
		/// <summary>
		/// Transforms a shadow quad position into an UV parametric position
		/// </summary>
		/// <param name="P"></param>
		/// <returns></returns>
		public Vector2	XY2UV( Vector2 P )
		{
// 			Vector3	Result = Vector3.TransformNormal( new Vector3( P, 1 ), m_ShadowQuad2UV );
// 			Result /= Result.Z;
// 			return new Vector2( Result.X, Result.Y );



			Vector2	p00 = m_ShadowQuadKm[0];
			Vector2	p10 = m_ShadowQuadKm[1];
			Vector2	p11 = m_ShadowQuadKm[2];
			Vector2	p01 = m_ShadowQuadKm[3];

			float	dU0 = Vector2.Dot( P - p00, m_NU0 );
			float	dU1 = Vector2.Dot( P - p11, m_NU1 );
//			dU1 = Vector2.Dot( P - p10, m_NU1 );
			float	U = dU0 / (dU0 + dU1);

			float	dV0 = Vector2.Dot( P - p00, m_NV0 );
			float	dV1 = Vector2.Dot( P - p11, m_NV1 );
//			dV1 = Vector2.Dot( P - p01, m_NV1 );
			float	V = dV0 / (dV0 + dV1);

			return new Vector2( U, V );
		}

		/// <summary>
		/// Transforms an UV parametric position into a shadow quad position
		/// </summary>
		/// <param name="P"></param>
		/// <returns></returns>
		public Vector2	UV2XY( Vector2 P )
		{
			Vector2	p00 = m_ShadowQuadKm[0];
			Vector2	p10 = m_ShadowQuadKm[1];
			Vector2	p11 = m_ShadowQuadKm[2];
			Vector2	p01 = m_ShadowQuadKm[3];

			Vector3	U = P.X * m_DEF - m_ABC;
			Vector3	V = P.Y * m_JKL - m_GHI;

			float	Den = V.X*U.Y - V.Y*U.X;
			float	X = V.Y*U.Z - V.Z*U.Y;
			float	Y = V.Z*U.X - V.X*U.Z;

			return new Vector2( X, Y ) / Den;

// 			float	A = m_NU0.X,			B = m_NU0.Y,			C = -Vector2.Dot( p00, m_NU0 );
// 			float	D = m_NU0.X + m_NU1.X,	E = m_NU0.Y + m_NU1.Y,	F = -Vector2.Dot( p00, m_NU0 ) - Vector2.Dot( p11, m_NU1 );
// 
// 			float	G = m_NV0.X,			H = m_NV0.Y,			I = -Vector2.Dot( p00, m_NV0 );
// 			float	J = m_NV0.X + m_NV1.X,	K = m_NV0.Y + m_NV1.Y,	L = -Vector2.Dot( p00, m_NV0 ) - Vector2.Dot( p11, m_NV1 );
// 
// // 
// // // Hand evaluation of p11 (u=1, v=1)
// // float	PipoX = (m_NU1.X * Vector2.Dot( p11, m_NV1 ) - m_NV1.X * Vector2.Dot( p11, m_NU1 )) / (m_NU1.X*m_NV1.Y - m_NV1.X*m_NU1.Y);
// // float	PipoY = (m_NV1.Y * Vector2.Dot( p11, m_NU1 ) - m_NU1.Y * Vector2.Dot( p11, m_NV1 )) / (m_NU1.X*m_NV1.Y - m_NV1.X*m_NU1.Y);
// // 
// 
// 
// 			float	u = P.X;
// 			float	v = P.Y;
// 
// 			float	uDA = u*D-A;
// 			float	uEB = u*E-B;
// 			float	uFC = u*F-C;
// 			float	vJG = v*J-G;
// 			float	vKH = v*K-H;
// 			float	vLI = v*L-I;
// 
// 			float	Den = vJG * uEB - vKH * uDA;
// 
// 			float	Num = vKH * uFC - vLI*uEB;
// 			float	X = Num / Den;
// 
// 			Num = vLI * uDA - vJG * uFC;
// 			float	Y = Num / Den;
// 
// 			return new Vector2( X, Y );
		}

		#endregion

		#region EVENT HANDLERS

		private void Form1_Load( object sender, EventArgs e )
		{

		}

		private void floatTrackbarControlSunPhi_ValueChanged( Nuaj.Cirrus.Utility.FloatTrackbarControl _Sender, float _fFormerValue )
		{
			ComputeShadowData();
		}

		private void panelOutput_MouseDown( object sender, MouseEventArgs e )
		{
			panelOutput.m_P = panelOutput.TransformInverse( e.Location );
			panelOutput.m_UV = XY2UV( panelOutput.m_P );
//			panelOutput.m_P = UV2XY( new Vector2( (float) e.X / panelOutput.Width, (float) e.Y / panelOutput.Height ) );
			panelOutput.UpdateBitmap();
		}

		#endregion
	}
}
