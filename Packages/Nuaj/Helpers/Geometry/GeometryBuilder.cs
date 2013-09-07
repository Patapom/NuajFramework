using System;
using System.Collections.Generic;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;

namespace Nuaj.Helpers
{
	public class	GeometryBuilder<VS,I> where VS:struct where I:struct
	{
		protected class		IndexAccessor : IIndexAccessor<I>
		{
			protected IGeometryWriter<VS,I>	m_Writer = null;

			public	IndexAccessor( IGeometryWriter<VS,I> _Writer )
			{
				m_Writer = _Writer;
			}

			#region IIndexAccessor<I> Members

			public int ToInt( I _Index )
			{
				return m_Writer.ReadIndexData( _Index );
			}

			public I FromInt( int _Index )
			{
				I	Result = new I();
				m_Writer.WriteIndexData( ref Result, _Index );
				return Result;
			}

			#endregion
		}

		/// <summary>
		/// Use this delegate to have a chance to post-process the generated mesh before it's sent to the card
		/// </summary>
		/// <param name="_Vertices"></param>
		/// <param name="_Indices"></param>
		public delegate void	PostProcessMeshDelegate( VS[] _Vertices, I[] _Indices );
	}

	#region Standard Geometry Mappers

	/// <summary>
	/// A geometry mapper is able to compute the Tangent, BiTangent and UVW coordinates from a given position whose components are in [-1,+1]
	///  and also a vertex normal and a face normal
	/// </summary>
	public abstract class	GeometryMapper
	{
		#region FIELDS

		protected Vector3	m_Tiling = new Vector3( 1.0f, 1.0f, 1.0f );

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the tiling factor to apply to UVW
		/// </summary>
		public Vector3		TilingFactors
		{
			get { return m_Tiling; }
			set { m_Tiling = value; }
		}

		#endregion

		#region METHODS

		public GeometryMapper()
		{
		}

		public GeometryMapper( Vector3 _TilingFactors )
		{
			m_Tiling = _TilingFactors;
		}

		public abstract void BuildUVW( Vector3 _Position, Vector3 _VertexNormal, Vector3 _FaceNormal, ref Vector3 _Tangent, ref Vector3 _BiTangent, ref Vector3 _UVW, bool _bWrap );

		#endregion
	}

	/// <summary>
	/// This maps UVWs around a sphere
	/// </summary>
	public class GeometryMapperSpherical : GeometryMapper
	{
		#region FIELDS

		/// <summary>
		/// Default spherical mapper that computes UV by projecting vertex to a unit sphere
		/// </summary>
		public static GeometryMapperSpherical	DEFAULT = new GeometryMapperSpherical( new Vector3( 2.0f, 1.0f, 1.0f ) );

		#endregion

		#region METHODS

		public GeometryMapperSpherical() : base()	{}
		public GeometryMapperSpherical( Vector3 _TilingFactors ) : base( _TilingFactors )	{}

		public override void BuildUVW( Vector3 _Position, Vector3 _VertexNormal, Vector3 _FaceNormal, ref Vector3 _Tangent, ref Vector3 _BiTangent, ref Vector3 _UVW, bool _bWrap )
		{
			_Tangent = Vector3.Cross( Vector3.UnitY, _VertexNormal );
			if ( _Tangent.LengthSquared() < 1e-4f )
				_Tangent = new Vector3( _FaceNormal.Z, 0.0f, -_FaceNormal.X );	// Too close to vertical : use face normal instead
			_Tangent.Normalize();
			_BiTangent = Vector3.Cross( _VertexNormal, _Tangent );

			double	Phi = (2.0*Math.PI + Math.Atan2( _VertexNormal.X, _VertexNormal.Z )) % (2.0 * Math.PI);
			if ( _bWrap )
				Phi += 2.0 * Math.PI;

			_UVW = new Vector3( m_Tiling.X * (float) (0.5 * Phi / Math.PI), m_Tiling.Y * (float) (1.0 - Math.Acos( _VertexNormal.Y ) / Math.PI), 0.0f );
		}

		#endregion
	}

	/// <summary>
	/// This maps UVWs around a cylinder
	/// </summary>
	public class GeometryMapperCylindrical : GeometryMapper
	{
		#region FIELDS

		protected Vector3	m_TopTiling = new Vector3( 1.0f, 1.0f, 1.0f );

		/// <summary>
		/// Default cylindrical mapper that computes UV by projecting vertex to a unit cylinder
		/// </summary>
		public static GeometryMapperCylindrical	DEFAULT = new GeometryMapperCylindrical();

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the tiling factor to apply to UVW for top vertices
		/// </summary>
		public Vector3		TopTilingFactors
		{
			get { return m_TopTiling; }
			set { m_TopTiling = value; }
		}

		#endregion

		#region METHODS

		public GeometryMapperCylindrical() : base()	{}
		public GeometryMapperCylindrical( Vector3 _TilingFactors, Vector3 _TopTilingFactor ) : base( _TilingFactors )	{ m_TopTiling = _TopTilingFactor; }

		public override void BuildUVW( Vector3 _Position, Vector3 _VertexNormal, Vector3 _FaceNormal, ref Vector3 _Tangent, ref Vector3 _BiTangent, ref Vector3 _UVW, bool _bWrap )
		{
			if ( Math.Abs( _VertexNormal.Y ) > 0.70710678118654752440084436210485f )
			{	// Top/Bottom cap aboce 45°
				_Tangent.X = 1.0f;
				_Tangent.Y = 0.0f;
				_Tangent.Z = 0.0f;
				_BiTangent.X = 0.0f;
				_BiTangent.Y = 0.0f;
				_BiTangent.Z = -1.0f;
				_UVW.X = 0.5f * (1.0f + m_TopTiling.X * _Position.X);
				_UVW.Y = 0.5f * (1.0f - m_TopTiling.Y * _Position.Z);
				_UVW.Z = 0.0f;
			}
			else
			{	// Sides
				_Tangent.X = _VertexNormal.Z;
				_Tangent.Y = 0.0f;
				_Tangent.Z = -_VertexNormal.X;
				_BiTangent.X = 0.0f;
				_BiTangent.Y = 1.0f;
				_BiTangent.Z = 0.0f;

				double	Phi = (2.0*Math.PI + Math.Atan2( _VertexNormal.X, _VertexNormal.Z )) % (2.0 * Math.PI);
				if ( _bWrap )
					Phi += 2.0 * Math.PI;

				_UVW.X = m_Tiling.X * (float) (0.5 * Phi / Math.PI);
				_UVW.Y = m_Tiling.Y * 0.5f * (1.0f + _Position.Y);
				_UVW.Z = 0.0f;
			}
		}

		#endregion
	}

	/// <summary>
	/// This maps UVWs to the faces of a cube
	/// </summary>
	public class GeometryMapperCube : GeometryMapper
	{
		#region FIELDS

		/// <summary>
		/// Default cube mapper that computes UV by projecting vertex to a unit cube along the vertex's normal direction
		/// </summary>
		public static GeometryMapperCube	DEFAULT = new GeometryMapperCube( false );

		/// <summary>
		/// Default planar cube mapper that computes UV by projecting vertex to a unit cube by performing a planar projection of the vertex normal 
		/// </summary>
		public static GeometryMapperCube	DEFAULT_PLANAR = new GeometryMapperCube( true );

		protected bool	m_bPlanarProjection = false;	// If true, the vectors simply mapped in cartesian coordinates rather than angular coordinates

		#endregion

		#region METHODS

		public GeometryMapperCube( bool _bPlanarProjection ) : base()	{ m_bPlanarProjection = _bPlanarProjection; }
		public GeometryMapperCube( bool _bPlanarProjection, Vector3 _TilingFactors ) : base( _TilingFactors )	{ m_bPlanarProjection = _bPlanarProjection; }

		public override void BuildUVW( Vector3 _Position, Vector3 _VertexNormal, Vector3 _FaceNormal, ref Vector3 _Tangent, ref Vector3 _BiTangent, ref Vector3 _UVW, bool _bWrap )
		{
			// Determine face index
			float	X = Math.Abs( _FaceNormal.X );
			float	Y = Math.Abs( _FaceNormal.Y );
			float	Z = Math.Abs( _FaceNormal.Z );
			int		FaceIndex = 0;
			if ( X >= Y )
			{
				if ( X >= Z )
					FaceIndex = _FaceNormal.X >= 0 ? 0 : 1;
				else
					FaceIndex = _FaceNormal.Z >= 0 ? 4 : 5;
			}
			else
			{
				if ( Y >= Z )
					FaceIndex = _FaceNormal.Y >= 0 ? 2 : 3;
				else
					FaceIndex = _FaceNormal.Z >= 0 ? 4 : 5;
			}

			// Switch components according to cube face
			float	Temp;
			switch ( FaceIndex )
			{
				case 0:	// +X
					Temp = _VertexNormal.X;
					_VertexNormal.X = -_VertexNormal.Z;
					_VertexNormal.Z = Temp;

					_Tangent.X = 0.0f;
					_Tangent.Y = 0.0f;
					_Tangent.Z = -1.0f;
					_BiTangent.X = 0.0f;
					_BiTangent.Y = 1.0f;
					_BiTangent.Z = 0.0f;
					break;
				case 1:	// -X
					Temp = _VertexNormal.X;
					_VertexNormal.X = _VertexNormal.Z;
					_VertexNormal.Z = -Temp;

					_Tangent.X = 0.0f;
					_Tangent.Y = 0.0f;
					_Tangent.Z = 1.0f;
					_BiTangent.X = 0.0f;
					_BiTangent.Y = 1.0f;
					_BiTangent.Z = 0.0f;
					break;
				case 2:	// +Y
					Temp = _VertexNormal.Y;
					_VertexNormal.Y = -_VertexNormal.Z;
					_VertexNormal.Z = Temp;

					_Tangent.X = 1.0f;
					_Tangent.Y = 0.0f;
					_Tangent.Z = 0.0f;
					_BiTangent.X = 0.0f;
					_BiTangent.Y = 0.0f;
					_BiTangent.Z = -1.0f;
					break;
				case 3:	// -Y
					Temp = _VertexNormal.Y;
					_VertexNormal.Y = _VertexNormal.Z;
					_VertexNormal.Z = -Temp;

					_Tangent.X = 1.0f;
					_Tangent.Y = 0.0f;
					_Tangent.Z = 0.0f;
					_BiTangent.X = 0.0f;
					_BiTangent.Y = 0.0f;
					_BiTangent.Z = 1.0f;
					break;
				case 4:	// +Z
					_Tangent.X = 1.0f;
					_Tangent.Y = 0.0f;
					_Tangent.Z = 0.0f;
					_BiTangent.X = 0.0f;
					_BiTangent.Y = 1.0f;
					_BiTangent.Z = 0.0f;
					break;
				case 5:	// -Z
					_VertexNormal.X = -_VertexNormal.X;
					_VertexNormal.Z = -_VertexNormal.Z;

					_Tangent.X = -1.0f;
					_Tangent.Y = 0.0f;
					_Tangent.Z = 0.0f;
					_BiTangent.X = 0.0f;
					_BiTangent.Y = 1.0f;
					_BiTangent.Z = 0.0f;
					break;
			}

			float	U, V;
			if ( m_bPlanarProjection )
			{	// Simply denormalize normal and use its XY coordinates as UV
				_VertexNormal /= _VertexNormal.Z;
				U = 0.5f * (1.0f + _VertexNormal.X);
				V = 0.5f * (1.0f + _VertexNormal.Y);
			}
			else
			{	// Project X/Y on a sphere sextant
				double	ThetaX = Math.Asin( _VertexNormal.X );
				double	ThetaY = Math.Asin( _VertexNormal.Y );

				// Retrieve UVs
				U = (float) (2.0 * (ThetaX + 0.25 * Math.PI) / Math.PI);
				V = (float) (2.0 * (ThetaY + 0.25 * Math.PI) / Math.PI);
			}

			_UVW.X = m_Tiling.X * U;
			_UVW.Y = m_Tiling.Y * V;
			_UVW.Z = 0.0f;
		}

		#endregion
	}

	#endregion
}
