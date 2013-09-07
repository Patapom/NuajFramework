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
	/// Shadow Map rendering
	/// This is a brute force single shadow map rendering
	/// </example>
	public class RenderTechniqueSceneShadowMap : RenderTechnique, IShaderInterfaceProvider
	{
		#region CONSTANTS

		protected const int		SHADOW_MAP_SIZE = 1024;

		#endregion

		#region NESTED TYPES

		protected class	IShadowMapSupport : ShaderInterfaceBase
		{
			[Semantic( "SHADOWMAPINVSIZE" )]
			public Vector3		ShadowMapInvSize	{ set { SetVector( "SHADOWMAPINVSIZE", value ); } }
			[Semantic( "SHADOWMAPPROJFACTOR" )]
			public Vector2		ShadowMapProjFactor	{ set { SetVector( "SHADOWMAPPROJFACTOR", value ); } }
			[Semantic( "WORLD2LIGHT" )]
			public Matrix		World2Light			{ set { SetMatrix( "WORLD2LIGHT", value ); } }
			[Semantic( "LIGHT2SHADOWMAP" )]
			public Matrix		Light2ShadowMap		{ set { SetMatrix( "LIGHT2SHADOWMAP", value ); } }
			[Semantic( "WORLD2SHADOWMAP" )]
			public Matrix		World2ShadowMap		{ set { SetMatrix( "WORLD2SHADOWMAP", value ); } }
			[Semantic( "SHADOWMAP" )]
			public ITexture2D	ShadowMap			{ set { SetResource( "SHADOWMAP", value ); } }
		}

		#endregion

		#region FIELDS

		protected Material<VS_P3>		m_Material = null;

		// The list of primitives to render in the shadow map
		protected ITechniqueSupportsObjects		m_SceneRenderer = null;
		protected List<Scene.Mesh.Primitive>	m_Primitives = new List<Scene.Mesh.Primitive>();

		// Shadow map data
		protected DepthStencil<PF_D32>	m_ShadowMap = null;
		protected Matrix				m_World2Light = Matrix.Identity;
		protected Matrix				m_Light2ShadowMap = Matrix.Identity;
		protected Matrix				m_World2ShadowMap = Matrix.Identity;
		protected Vector2				m_ShadowMapProjFactor = Vector2.Zero;

		// Light and stuff
		protected Vector3				m_LightDirection;

		#endregion

		#region PROPERTIES

		public Vector3				LightDirection	{ get { return m_LightDirection; } set { m_LightDirection = value; m_LightDirection.Normalize(); } }

		public ITechniqueSupportsObjects	SceneRenderer
		{
			get { return m_SceneRenderer; }
			set
			{
				if ( value == m_SceneRenderer )
					return;

				if ( m_SceneRenderer != null )
					m_SceneRenderer.PrimitiveAdded -= new PrimitiveCollectionChangedEventHandler(SceneRenderer_PrimitiveAdded);

				m_SceneRenderer = value;

				if ( m_SceneRenderer != null )
					m_SceneRenderer.PrimitiveAdded += new PrimitiveCollectionChangedEventHandler(SceneRenderer_PrimitiveAdded);
			}
		}

		#endregion

		#region METHODS

		public RenderTechniqueSceneShadowMap( Device _Device, string _Name ) : base( _Device, _Name )
		{
			// Register the shadow map support interface
			m_Device.DeclareShaderInterface( typeof(IShadowMapSupport) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IShadowMapSupport), this );

			// Create our main material
			m_Material = ToDispose( new Material<VS_P3>( m_Device, "ShadowMap Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/VolumeRadiosityBuilder/ShadowMapRendering.fx" ) ) );

			// Create our shadow map
			m_ShadowMap = ToDispose( new DepthStencil<PF_D32>( m_Device, "ShadowMap", SHADOW_MAP_SIZE, SHADOW_MAP_SIZE, true ) );
			m_ShadowMapProjFactor = new Vector2( 0.5f * (float) (SHADOW_MAP_SIZE-1) / SHADOW_MAP_SIZE, 0.5f * (float) (SHADOW_MAP_SIZE-1) / SHADOW_MAP_SIZE );
		}

		public override void	Render( int _FrameToken )
		{
			//////////////////////////////////////////////////////////////////////////
			// Compute the shadow map transforms
			//

			// Compute scene BBox in WORLD space
			BoundingBox	WorldBBox = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
			foreach ( Scene.Mesh.Primitive P in m_Primitives )
			{
				WorldBBox.Minimum = Vector3.Min( WorldBBox.Minimum, P.Parent.WorldBBox.Minimum );
				WorldBBox.Maximum = Vector3.Max( WorldBBox.Maximum, P.Parent.WorldBBox.Maximum );
			}

			// Compute ortho view from light direction
			Vector3	Up = Vector3.UnitY;	// Risky if the Sun is at zenith
			Vector3	X = Vector3.Cross( Up, m_LightDirection );
					X.Normalize();
			Vector3	Y = Vector3.Cross( m_LightDirection, X );
			Vector3	Z = -m_LightDirection;

			Vector3	SceneCenter = 0.5f * (WorldBBox.Minimum + WorldBBox.Maximum);

			Matrix	Light2World = Matrix.Identity;
					Light2World.Row1 = new Vector4( X, 0.0f );
					Light2World.Row2 = new Vector4( Y, 0.0f );
					Light2World.Row3 = new Vector4( Z, 0.0f );
					Light2World.Row4 = new Vector4( SceneCenter, 1.0f );
			Matrix	World2Light = Light2World;
					World2Light.Invert();

			// Compute scene BBox in LIGHT space
			Vector3[]	LightCorners = new Vector3[8];
			Vector3.TransformCoordinate( WorldBBox.GetCorners(), ref World2Light, LightCorners );

			BoundingBox	LightBBox = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
			foreach ( Vector3 Corner in LightCorners )
			{
				LightBBox.Minimum = Vector3.Min( LightBBox.Minimum, Corner );
				LightBBox.Maximum = Vector3.Max( LightBBox.Maximum, Corner );
			}

			// Update Light transform
			Light2World.Row4 += new Vector4( LightBBox.Minimum.Z * Z, 0.0f );
			World2Light = Light2World;
			World2Light.Invert();

			// Compute orthogonal projection transform
			Vector3	LightBBoxDim = LightBBox.Maximum - LightBBox.Minimum;
			Matrix	Light2Shadow = Matrix.OrthoLH( LightBBoxDim.X, LightBBoxDim.Y, 0.0f, LightBBoxDim.Z );

			// Compute final WORLD=>SHADOW transform
			m_World2Light = World2Light;
			m_Light2ShadowMap = Light2Shadow;
			m_World2ShadowMap = m_World2Light * m_Light2ShadowMap;


			// DEBUG
			BoundingBox	CheckLightBBox = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
			BoundingBox	CheckBBox = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
			foreach ( Vector3 Corner in WorldBBox.GetCorners() )
			{
				Vector3	LightCorner = Vector3.TransformCoordinate( Corner, m_World2Light );
				CheckLightBBox.Minimum = Vector3.Min( CheckLightBBox.Minimum, LightCorner );
				CheckLightBBox.Maximum = Vector3.Max( CheckLightBBox.Maximum, LightCorner );

				Vector3	ShadowCorner = Vector3.TransformCoordinate( Corner, m_World2ShadowMap );
				CheckBBox.Minimum = Vector3.Min( CheckBBox.Minimum, ShadowCorner );
				CheckBBox.Maximum = Vector3.Max( CheckBBox.Maximum, ShadowCorner );
			}
			// DEBUG


			//////////////////////////////////////////////////////////////////////////
			// Render the scene into the shadow map
			//
			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				m_Device.ClearDepthStencil( m_ShadowMap, DepthStencilClearFlags.Depth, 1.0f, 0 );
				m_Device.SetRenderTarget( null as IRenderTarget, m_ShadowMap );
				m_Device.SetViewport( 0, 0, SHADOW_MAP_SIZE, SHADOW_MAP_SIZE, 0.0f, 1.0f );

				CurrentMaterial.GetVariableByName( "World2ShadowMap" ).AsMatrix.SetMatrix( m_World2ShadowMap );

				VariableMatrix	vLocal2World = CurrentMaterial.GetVariableByName( "Local2World" ).AsMatrix;
				EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

				foreach ( Scene.Mesh.Primitive P in m_Primitives )
				{
					vLocal2World.SetMatrix( P.Parent.Local2World );
					Pass.Apply();
					P.Render( _FrameToken-10 );
				}
			}
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			IShadowMapSupport	I = _Interface as IShadowMapSupport;
			I.ShadowMapInvSize = m_ShadowMap.InvSize3;
			I.ShadowMapProjFactor = m_ShadowMapProjFactor;
			I.World2Light = m_World2Light;
			I.Light2ShadowMap = m_Light2ShadowMap;
			I.World2ShadowMap = m_World2ShadowMap;
			I.ShadowMap = m_ShadowMap;
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		void  SceneRenderer_PrimitiveAdded(ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive )
		{
 			if ( !_Primitive.Visible || !_Primitive.CastShadow )
				return;

			m_Primitives.Add( _Primitive );
		}

		#endregion
	}
}
