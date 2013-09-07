using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj.Helpers
{
	/// <summary>
	/// This is a class that helps you draw a screen quad, which is quite useful for post-process passes
	/// Your material/shaders must be compatible with VS_Pt4V3T2
	/// </summary>
	public class ScreenQuad : Component
	{
		#region FIELDS

		protected Primitive<VS_Pt4V3T2,int>	m_Primitive = null;

		#endregion

		#region PROPERTIES

		public IPrimitive	Primitive		{ get { return m_Primitive; } }

		#endregion

		#region METHODS

		public	ScreenQuad( Device _Device, string _Name ) : this( _Device, _Name, 1.0f, true )
		{
		}
		public	ScreenQuad( Device _Device, string _Name, float _AspectRatio ) : this( _Device, _Name, _AspectRatio, true )
		{
		}
		public	ScreenQuad( Device _Device, string _Name, float _AspectRatio, bool _Front ) : base( _Device, _Name )
		{
			// Vertices are organized for a triangle strip with 2 triangles
			float	Z = _Front ? 0.0f : 1.0f;
			VS_Pt4V3T2[]	Vertices = new VS_Pt4V3T2[]
			{
				new VS_Pt4V3T2() { Position=new Vector4( -1.0f, 1.0f, Z, 1.0f ), View=new Vector3( -_AspectRatio, 1.0f, 1.0f ), UV=new Vector2( 0.0f, 0.0f ) },
				new VS_Pt4V3T2() { Position=new Vector4( -1.0f, -1.0f, Z, 1.0f ), View=new Vector3( -_AspectRatio, -1.0f, 1.0f ), UV=new Vector2( 0.0f, 1.0f ) },
				new VS_Pt4V3T2() { Position=new Vector4( +1.0f, 1.0f, Z, 1.0f ), View=new Vector3( +_AspectRatio, 1.0f, 1.0f ), UV=new Vector2( 1.0f, 0.0f ) },
				new VS_Pt4V3T2() { Position=new Vector4( +1.0f, -1.0f, Z, 1.0f ), View=new Vector3( +_AspectRatio, -1.0f, 1.0f ), UV=new Vector2( 1.0f, 1.0f ) },
			};

			m_Primitive = ToDispose( new Primitive<VS_Pt4V3T2,int>( _Device, _Name, PrimitiveTopology.TriangleStrip, Vertices ) );
		}

		public	ScreenQuad( Device _Device, string _Name, int _Width, int _Height ) : this( _Device, _Name, _Width, _Height, true )
		{
		}
		public	ScreenQuad( Device _Device, string _Name, int _Width, int _Height, bool _Front ) : base( _Device, _Name )
		{
			float	IDx = 1.0f / _Width;
			float	IDy = 1.0f / _Height;
			float	AspectRatio = (float) _Width / _Height;

			// Vertices are organized for a triangle strip with 2 triangles
			float	Z = _Front ? 0.0f : 1.0f;
			VS_Pt4V3T2[]	Vertices = new VS_Pt4V3T2[]
			{
				new VS_Pt4V3T2() { Position=new Vector4( -1.0f, 1.0f, Z, 1.0f ), View=new Vector3( -AspectRatio, 1.0f, 1.0f ), UV=new Vector2( 0.0f, 0.0f ) },
				new VS_Pt4V3T2() { Position=new Vector4( -1.0f, -1.0f, Z, 1.0f ), View=new Vector3( -AspectRatio, -1.0f, 1.0f ), UV=new Vector2( 0.0f, 1.0f-IDy ) },
				new VS_Pt4V3T2() { Position=new Vector4( +1.0f, 1.0f, Z, 1.0f ), View=new Vector3( +AspectRatio, 1.0f, 1.0f ), UV=new Vector2( 1.0f-IDx, 0.0f ) },
				new VS_Pt4V3T2() { Position=new Vector4( +1.0f, -1.0f, Z, 1.0f ), View=new Vector3( +AspectRatio, -1.0f, 1.0f ), UV=new Vector2( 1.0f-IDx, 1.0f-IDy ) },
			};

			m_Primitive = ToDispose( new Primitive<VS_Pt4V3T2,int>( _Device, _Name, PrimitiveTopology.TriangleStrip, Vertices ) );
		}

		public void		Render()
		{
			m_Primitive.RenderOverride();
		}

		public void		RenderInstanced( int _StartInstance, int _InstancesCount )
		{
			m_Primitive.RenderInstancedOverride( _StartInstance, _InstancesCount );
		}

		#endregion
	}
}
