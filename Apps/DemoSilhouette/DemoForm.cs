//////////////////////////////////////////////////////////////////////////
// This examples demonstrates advanced parallax occlusion mapping (POM ^_^).
// Not only it displays the cylinder geometry using standard parallax occlusion
//	but it also extrudes edge silhouettes using a geometry shader and renders
//	the continuity of the cylinder's "extruded relief" through the silhouette
//	slabs, discarding pixels if it misses to hit the height map.
//
// The result is quite convincing in giving the impression of a detailed
//	displacement as you cannot see the typical sharp polygonal edges anymore.
//
//
// NOTE: The shader may seem complicated but really it's plenty of tests I've been
//	 performing along time, like automatic extraction of curvature, extrusion of
//	 fins from silhouette edges when using geometry with adjacency, etc.
//	To properly extract the relevant code, follow back the techniques at the end
//	 and see what functions are really useful : you should end up with only a
//	 small amount of code...
//
//////////////////////////////////////////////////////////////////////////
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

using Nuaj;

namespace Demo
{
	public partial class DemoForm : Form, IGeometryWriter<VS_P3N3G3B3T2Cu2,int>, IIndexAccessor<int>
	{
		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// Object primitive
		protected Material<VS_P3N3G3B3T2Cu2>		m_ObjectMaterial = null;
		protected Primitive<VS_P3N3G3B3T2Cu2,int>	m_ObjectCylinder = null;
		protected Primitive<VS_P3N3G3B3T2Cu2,int>	m_ObjectPlane = null;
		protected Primitive<VS_P3N3G3B3T2Cu2,int>	m_ObjectSphere = null;
		protected Primitive<VS_P3N3G3B3T2Cu2,int>	m_ObjectCube = null;

		protected Texture2D<PF_R8>			m_TextureHeight = null;
		protected Texture2D<PF_RGBA8>		m_TextureDiffuseSpecular = null;
		protected Texture2D<PF_RGBA8>		m_TextureNormal = null;
		protected Texture2D<PF_R8>			m_TextureHeightDerivatives = null;

		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		#endregion

		#region METHODS

		public DemoForm()
		{
			InitializeComponent();

			//////////////////////////////////////////////////////////////////////////
			// Create the device
			try
			{
				SwapChainDescription	Desc = new SwapChainDescription()
				{
					BufferCount = 1,
					ModeDescription = new ModeDescription( ClientSize.Width, ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm ),
					IsWindowed = true,
					OutputHandle = Handle,
					SampleDescription = new SampleDescription( 1, 0 ),
					SwapEffect = SwapEffect.Discard,
					Usage = Usage.RenderTargetOutput
				};

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, this ) );
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}

			//////////////////////////////////////////////////////////////////////////
			// Build the object components

			// Create the material
			try
			{
				m_ObjectMaterial = ToDispose( new Material<VS_P3N3G3B3T2Cu2>( m_Device, "ObjectMaterial", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/SilhouetteRendering/DemoSilhouette.fx" ) ) );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			// Create the primitives
			Nuaj.Helpers.GeometryBuilder<VS_P3N3G3B3T2Cu2,int>.PostProcessMeshDelegate	MeshPostProcess = new Nuaj.Helpers.GeometryBuilder<VS_P3N3G3B3T2Cu2,int>.PostProcessMeshDelegate( PostProcessGeometry );

 			m_ObjectCylinder = ToDispose( Nuaj.Helpers.Cylinder<VS_P3N3G3B3T2Cu2,int>.Build( m_Device, "Cylinder", 80, new Nuaj.Helpers.GeometryMapperCylindrical( new Vector3( 3.0f, 1.0f, 1.0f ), Vector3.One ), this, ( _Vertices, _Indices ) =>
			{
//				Nuaj.Helpers.Curvature<VS_P3N3G3B3T2Cu2,int>.BuildCurvature( _Vertices, _Indices, this );
				for ( int VertexIndex=0; VertexIndex < _Vertices.Length; VertexIndex++ )
				{
					_Vertices[VertexIndex].Curvature.X = 1.0f;
					_Vertices[VertexIndex].Curvature.Y = 1e4f;
				}
			}, false, 0.01f ) );
 			m_ObjectCylinder.Material = m_ObjectMaterial;

			m_ObjectSphere = ToDispose( Nuaj.Helpers.Sphere<VS_P3N3G3B3T2Cu2,int>.Build( m_Device, "Sphere", 40, new Vector2( 4.0f, 2.0f ), this, ( _Vertices, _Indices ) =>
			{
//				Nuaj.Helpers.Curvature<VS_P3N3G3B3T2Cu2,int>.BuildCurvature( _Vertices, _Indices, this );
				for ( int VertexIndex=0; VertexIndex < _Vertices.Length; VertexIndex++ )
				{
					_Vertices[VertexIndex].Curvature.X = 1.0f;
					_Vertices[VertexIndex].Curvature.Y = 1.0f;
				}
			}, false, 0.01f ) );
			m_ObjectSphere.Material = m_ObjectMaterial;

			m_ObjectCube = ToDispose( Nuaj.Helpers.Cube<VS_P3N3G3B3T2Cu2,int>.Build( m_Device, "Cube", new Vector2( 1.0f, 1.0f ), this, ( _Vertices, _Indices ) =>
			{
//				Nuaj.Helpers.Curvature<VS_P3N3G3B3T2Cu2,int>.BuildCurvature( _Vertices, _Indices, this );
				// Write "infinite" curvature for the cube
				for ( int VertexIndex=0; VertexIndex < _Vertices.Length; VertexIndex++ )
				{
					_Vertices[VertexIndex].Curvature.X = 1e4f;
					_Vertices[VertexIndex].Curvature.Y = 1e4f;
				}
			}, false, false, 0.01f ) );
			m_ObjectCube.Material = m_ObjectMaterial;

			// Plane
			VS_P3N3G3B3T2Cu2[]	Vertices = new VS_P3N3G3B3T2Cu2[]
				{
					new VS_P3N3G3B3T2Cu2() { Position=new Vector3( -1.0f, 0.0f, -1.0f ), Normal=new Vector3( 0.0f, 1.0f, 0.0f ), Tangent=new Vector3( 1.0f, 0.0f, 0.0f ), BiTangent=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 0.0f, 0.0f ), Curvature=new Vector2( 1e4f, 1e4f ) },
					new VS_P3N3G3B3T2Cu2() { Position=new Vector3( -1.0f, 0.0f, +1.0f ), Normal=new Vector3( 0.0f, 1.0f, 0.0f ), Tangent=new Vector3( 1.0f, 0.0f, 0.0f ), BiTangent=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 0.0f, 1.0f ), Curvature=new Vector2( 1e4f, 1e4f ) },
					new VS_P3N3G3B3T2Cu2() { Position=new Vector3( +1.0f, 0.0f, -1.0f ), Normal=new Vector3( 0.0f, 1.0f, 0.0f ), Tangent=new Vector3( 1.0f, 0.0f, 0.0f ), BiTangent=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 1.0f, 0.0f ), Curvature=new Vector2( 1e4f, 1e4f ) },
					new VS_P3N3G3B3T2Cu2() { Position=new Vector3( -1.0f, 0.0f, +1.0f ), Normal=new Vector3( 0.0f, 1.0f, 0.0f ), Tangent=new Vector3( 1.0f, 0.0f, 0.0f ), BiTangent=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 0.0f, 1.0f ), Curvature=new Vector2( 1e4f, 1e4f ) },
					new VS_P3N3G3B3T2Cu2() { Position=new Vector3( +1.0f, 0.0f, +1.0f ), Normal=new Vector3( 0.0f, 1.0f, 0.0f ), Tangent=new Vector3( 1.0f, 0.0f, 0.0f ), BiTangent=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 1.0f, 1.0f ), Curvature=new Vector2( 1e4f, 1e4f ) },
					new VS_P3N3G3B3T2Cu2() { Position=new Vector3( +1.0f, 0.0f, -1.0f ), Normal=new Vector3( 0.0f, 1.0f, 0.0f ), Tangent=new Vector3( 1.0f, 0.0f, 0.0f ), BiTangent=new Vector3( 0.0f, 0.0f, 1.0f ), UV=new Vector2( 1.0f, 0.0f ), Curvature=new Vector2( 1e4f, 1e4f ) },
				};

			int[]	Indices = new int[]
			{
				0, 1, 2,
				3, 4, 5
			};
			Nuaj.Helpers.Curvature<VS_P3N3G3B3T2Cu2,int>.BuildCurvature( Vertices, Indices, this );
			Nuaj.Helpers.Adjacency<VS_P3N3G3B3T2Cu2,int>.BuildTriangleListAdjacency( Vertices, Indices, this, 0.01f, out Indices );

			m_ObjectPlane = ToDispose( new Primitive<VS_P3N3G3B3T2Cu2,int>( m_Device, "Pipo", PrimitiveTopology.TriangleListWithAdjacency, Vertices, Indices, m_ObjectMaterial ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the textures
			Image<PF_R8>	ImageHeight = new Image<PF_R8>( m_Device, "ImageHeight", Bitmap.FromFile( "./Media/SilhouetteClipping/Displacement.png" ) as Bitmap, 1, 1.0f );
			m_TextureHeight = ToDispose( new Texture2D<PF_R8>( m_Device, "TextureHeight", ImageHeight ) );
			ImageHeight.Dispose();

			Image<PF_RGBA8>	ImageDiffuseSpecular = new Image<PF_RGBA8>( m_Device, "ImageDiffuse", Bitmap.FromFile( "./Media/SilhouetteClipping/Diffuse.png" ) as Bitmap, Bitmap.FromFile( "./Media/SilhouetteClipping/Specular.png" ) as Bitmap, 0, 1.0f );
			m_TextureDiffuseSpecular = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "TextureDiffuseSpecular", ImageDiffuseSpecular ) );
			ImageHeight.Dispose();

			Image<PF_RGBA8>	ImageNormal = new Image<PF_RGBA8>( m_Device, "ImageNormal", Bitmap.FromFile( "./Media/SilhouetteClipping/Normal.png" ) as Bitmap, 0, 1.0f );
			m_TextureNormal = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "TextureNormal", ImageNormal ) );
			ImageNormal.Dispose();
		}

		protected void	PostProcessGeometry( VS_P3N3G3B3T2Cu2[] _Vertices, int[] _Indices )
		{

		}

		protected override void OnClosing( CancelEventArgs e )
		{
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();

			base.OnClosing( e );
		}

		/// <summary>
		/// We'll keep you busy !
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void	RunMessageLoop()
		{
			//////////////////////////////////////////////////////////////////////////
			// Create a perspective camera
			Camera		Cam = ToDispose( new Camera( m_Device, "Default Camera" ) );
						Cam.CreatePerspectiveCamera( 0.5f * (float) Math.PI, (float) ClientSize.Width / ClientSize.Height, 0.01f, 100.0f );

			Cam.Activate();


			//////////////////////////////////////////////////////////////////////////
			// Create the fantastic plastic manipulator
			Nuaj.Helpers.CameraManipulator	Manipulator = new Nuaj.Helpers.CameraManipulator();
			Manipulator.Attach( this, Cam );
			Manipulator.InitializeCamera( new Vector3( 0.0f, 1.0f, 2.0f ), Vector3.Zero, Vector3.UnitY );

			//////////////////////////////////////////////////////////////////////////
			// Create a directional light
			DirectionalLight	Light = ToDispose( new DirectionalLight( m_Device, "Light", true ) );
			Light.Direction = new Vector3( 1.0f, 1.0f, 1.0f );	// Make it light from above and front
//			Light.Color = Color.Gold;
			Light.Activate();


			RasterizerStateDescription	Pipo = new RasterizerStateDescription();
			Pipo.CullMode = CullMode.None;
			Pipo.FillMode = FillMode.Solid;

			RasterizerState	PS = ToDispose( new RasterizerState( m_Device.DirectXDevice, Pipo ) );
//			m_Device.Rasterizer.State = PS;


			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Render Scene ===============

				// Setup textures
				VariableResource	vTexHeight = m_ObjectMaterial.GetVariableBySemantic( "TEX_HEIGHT" ).AsResource;
				vTexHeight.SetResource( m_TextureHeight.TextureView );

				VariableResource	vTexDiffuseSpecular = m_ObjectMaterial.GetVariableBySemantic( "TEX_DIFFUSE" ).AsResource;
				vTexDiffuseSpecular.SetResource( m_TextureDiffuseSpecular.TextureView );

				VariableResource	vTexNormal = m_ObjectMaterial.GetVariableBySemantic( "TEX_NORMAL" ).AsResource;
				vTexNormal.SetResource( m_TextureNormal.TextureView );

				// Setup height
				VariableVector	vHeight = m_ObjectMaterial.GetVariableByName( "Height" ).AsVector;
				vHeight.Set( new Vector2( 2*0.05f, 2*0.05f ) );

				// Setup dUdV
				VariableVector	vdUdV = m_ObjectMaterial.GetVariableByName( "dUdV" ).AsVector;
				// Values for the cylinder
				vdUdV.Set( new Vector2(
					3.0f / (2.0f * (float) Math.PI),	// U varies of 3 units over 2PI world units
					1.0f / 2.0f							// V varies of 1 over 2 world units
					) );
				// Values for the plane
// 				vdUdV.Set( new Vector2(
// 					1.0f / 2.0f,
// 					1.0f / 2.0f
// 					) );
				// Values for the sphere
// 				vdUdV.Set( new Vector2(
// 					4.0f / (2.0f * (float) Math.PI),	// U varies of 3 units over 2PI world units
// 					2.0f / (float) Math.PI				// V varies of 1 over 2 world units
// 					) );

				// Setup silhouette horizon angle threshold
				VariableScalar	vHorizonThreshold = m_ObjectMaterial.GetVariableByName( "SilhouetteAngleThreshold" ).AsScalar;
				vHorizonThreshold.Set( (float) Math.Cos( 80.0 * Math.PI / 180.0 ) );	// Start generating silhouette below 60°


				// Clear
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				// Draw
				m_ObjectMaterial.CurrentTechnique = m_ObjectMaterial.GetTechniqueByName( "Render" );
//				m_ObjectPlane.Render();
//				m_ObjectCube.Render();
				m_ObjectCylinder.Render();
//				m_ObjectSphere.Render();

				// Show !
				m_Device.Present();
			});
		}

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

		#region IGeometryBuilderWriteable<VS_P3N3G3B3T2Cu2,int> Members

		public void WriteVertexData( ref VS_P3N3G3B3T2Cu2 _Vertex, Vector3 _Position, Vector3 _Normal, Vector3 _Tangent, Vector3 _BiTangent, Vector3 _UVW, Color4 _Color )
		{
			_Vertex.Position = _Position;
			_Vertex.Normal = _Normal;
			_Vertex.Tangent = _Tangent;
			_Vertex.BiTangent = _BiTangent;
			_Vertex.UV = new Vector2( _UVW.X, _UVW.Y );
		}

		public void WriteIndexData( ref int _Index, int _Value )
		{
			_Index = _Value;
		}

		public int	ReadIndexData( int _Index )
		{
			return _Index;
		}

		#endregion

		#region IIndexAccessor<int> Members

		public int ToInt( int _Index )
		{
			return _Index;
		}

		public int FromInt( int _Index )
		{
			return _Index;
		}

		#endregion

		#endregion
	}
}
