using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// Shadow mapping pre-process
	/// </example>
	public class RenderTechniqueShadowMapping : DeferredRenderTechnique, IShaderInterfaceProvider
	{
		#region CONSTANTS

		protected const int		SHADOW_MAP_SIZE = 1024;

		#endregion

		#region NESTED TYPES

		protected class		IShadowMap : ShaderInterfaceBase
		{
			[Semantic( "CAMERA2SHADOW" )]
			public Matrix			Camera2Shadow	{ set { SetMatrix( "CAMERA2SHADOW", value ); } }
			[Semantic( "LIGHT2CAMERA" )]
			public Matrix			Light2Camera	{ set { SetMatrix( "LIGHT2CAMERA", value ); } }
			[Semantic( "LIGHT_CAMERA_CENTER" )]
			public Vector3			LightCameraCenter	{ set { SetVector( "LIGHT_CAMERA_CENTER", value ); } }
			[Semantic( "SHADOWMAP" )]
			public IDepthStencil	ShadowMap		{ set { SetResource( "SHADOWMAP", value ); } }
		}

		#endregion

		#region FIELDS

		protected Material<VS_P3>				m_Material = null;

		protected Camera						m_Camera = null;
		protected RenderTechniqueInferredLighting.LightDirectional	m_Light = null;
		protected List<IDepthPassRenderable>	m_Renderables = new List<IDepthPassRenderable>();

		protected Camera						m_LightCamera = null;
		protected DepthStencil<PF_D32>			m_ShadowMap = null;

		protected Matrix						m_Camera2Shadow = Matrix.Identity;
		protected Matrix						m_Light2Camera = Matrix.Identity;
		protected Vector3						m_Center = Vector3.Zero;

		#endregion

		#region PROPERTIES

		[System.ComponentModel.Browsable( false )]
		public Camera				Camera		{ get { return m_Camera; } set { m_Camera = value; } }

		[System.ComponentModel.Browsable( false )]
		public RenderTechniqueInferredLighting.LightDirectional	Light		{ get { return m_Light; } set { m_Light = value; } }

		#endregion

		#region METHODS

		public RenderTechniqueShadowMapping( Renderer _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = ToDispose( new Material<VS_P3>( m_Device, "ShadowMapping Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/ShadowMapping.fx" ) ) );

			// Create the shadow map
			m_ShadowMap = ToDispose( new DepthStencil<PF_D32>( m_Device, "Shadow Map #0", SHADOW_MAP_SIZE, SHADOW_MAP_SIZE, true ) );

			// Create the "light camera"
			m_LightCamera = new Camera( m_Device, "LightCamera" );

			// Register our shader interface
			m_Device.DeclareShaderInterface( typeof(IShadowMap) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IShadowMap), this );
		}

		/// <summary>
		/// Adds a renderable object/technique
		/// </summary>
		/// <param name="_Renderable"></param>
		public void		AddRenderable( IDepthPassRenderable _Renderable )
		{
			m_Renderables.Add( _Renderable );
		}

		public override void	Render( int _FrameToken )
		{
			m_Device.AddProfileTask( this, "Shadow Mapping", "<TODO>" );

			//////////////////////////////////////////////////////////////////////////
			// Prepare the shadow map projection

			// Pick up a position along the camera's view direction
			Vector3	Center = (Vector3) m_Camera.Camera2World.Row4;
			Vector3	View = (Vector3) m_Camera.Camera2World.Row3;
			Center += 2.0f * View;

 			// Create a look-at that looks at the light
			m_LightCamera.LookAt( Center, Center + m_Light.Direction, Vector3.UnitY );

			// Create a perspective projection (that's new !?)
			m_LightCamera.CreatePerspectiveCamera( 0.5f * (float) Math.PI, 1.0f, 0.1f, 400.0f );

			// Activate light camera so it now provides camera data
			m_LightCamera.Activate();


			m_Camera2Shadow = m_Camera.Camera2World * m_LightCamera.World2Proj;
			m_Light2Camera = m_LightCamera.Camera2World * m_Camera.World2Camera;
			m_Center = Center;


			//////////////////////////////////////////////////////////////////////////
			// Setup only a depth stencil (no render target) and clear it
			m_Device.SetRenderTarget( null as IRenderTarget, m_ShadowMap );
			m_Device.SetViewport( 0, 0, m_ShadowMap.Width, m_ShadowMap.Height, 0.0f, 1.0f );
			m_Device.ClearDepthStencil( m_ShadowMap, DepthStencilClearFlags.Depth, 1.0f, 0 );
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				VariableMatrix	vLocal2World = CurrentMaterial.GetVariableByName( "Local2World" ).AsMatrix;

				int FrameToken = m_FrameToken-9;	// -9 so we can always render objects for the ShadowPass and so they can render again in normal passes afterward

				foreach ( IDepthPassRenderable Renderable in m_Renderables )
					Renderable.RenderDepthPass( FrameToken, Pass, vLocal2World );
			}

			// De-activate light camera
			m_LightCamera.DeActivate();
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			(_Interface as IShadowMap).Camera2Shadow = m_Camera2Shadow;
			(_Interface as IShadowMap).Light2Camera = m_Light2Camera;
			(_Interface as IShadowMap).LightCameraCenter = m_Center;
			(_Interface as IShadowMap).ShadowMap = m_ShadowMap;
		}

		#endregion

		#endregion
	}
}
