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
	/// Tone-mapping post process
	/// </example>
	public class RenderTechniqueInferredLighting : DeferredRenderingScene
	{
		#region CONSTANTS

		protected const int		LODS_COUNT = 5;					// Amount of LODs for omni & spot lights
		protected const int		OMNI_MAX_SIDES = 40;			// Maximum sides for omni lights at maximum LOD
		protected const int		OMNI_MIN_SIDES = 10;			// Minimum sides for omni lights at minimum LOD
		protected const int		SPOT_MAX_SIDES = 40;			// Maximum sides for spot lights at maximum LOD
		protected const int		SPOT_MIN_SIDES = 10;			// Minimum sides for spot lights at minimum LOD

		#endregion

		#region NESTED TYPES

		public class	LightObject
		{
			#region FIELDS

			protected string	m_Name = "";
			protected Vector3	m_Position = Vector3.Zero;
			protected Vector3	m_Color = Vector3.Zero;
			protected Vector4	m_PackedParams = Vector4.Zero;

			#endregion

			#region PROPERTIES

			public string	Name			{ get { return m_Name; } set { m_Name = value; } }
			public Vector3	Position		{ get { return m_Position; } set { m_Position = value; } }
			public Vector3	Color			{ get { return m_Color; } set { m_Color = value; } }
			public Vector4	PackedParams	{ get { return m_PackedParams; } }

			#endregion

			#region METHODS

			public LightObject( string _Name )
			{

			}

			#endregion
		}

		public class	LightOmni : LightObject
		{
			#region FIELDS

			protected float		m_InnerRadius = 0.0f;	// Radius at which lighting starts to decrease
			protected float		m_OuterRadius = 0.0f;	// Radius at which lighting is 0

			#endregion

			#region PROPERTIES

			public float		InnerRadius	{ get { return m_InnerRadius; } set { m_InnerRadius = value; RepackParams(); } }
			public float		OuterRadius	{ get { return m_OuterRadius; } set { m_OuterRadius = value; RepackParams(); } }

			#endregion

			#region METHODS

			public LightOmni( string _Name ) : base( _Name )
			{
			}

			public virtual void	RepackParams()
			{
				m_PackedParams.X = m_InnerRadius;
				m_PackedParams.Y = m_OuterRadius;
			}

			#endregion
		}

		public class	LightSpot : LightOmni
		{
			#region FIELDS

			protected Vector3	m_Direction = Vector3.UnitY;
			protected float		m_InnerAngle = 0.0f;	// Angle at which lighting starts to decrease
			protected float		m_OuterAngle = 0.0f;	// Angle at which lighting is 0
			protected Vector4	m_PackedParams2 = Vector4.Zero;

			#endregion

			#region PROPERTIES

			public Vector3		Direction		{ get { return m_Direction; } set { m_Direction = value; m_Direction.Normalize(); } }
			public float		InnerAngle		{ get { return m_InnerAngle; } set { m_InnerAngle = value; RepackParams(); } }
			public float		OuterAngle		{ get { return m_OuterAngle; } set { m_OuterAngle = value; RepackParams(); } }
			public Vector4		PackedParams2	{ get { return m_PackedParams2; } }

			#endregion

			#region METHODS

			public LightSpot( string _Name ) : base( _Name )
			{
			}

			public override void	RepackParams()
			{
				base.RepackParams();
				m_PackedParams.Z = (float) Math.Cos( 0.5f * m_InnerAngle );
				m_PackedParams.W = (float) Math.Cos( 0.5f * m_OuterAngle );
				m_PackedParams2.X = m_OuterRadius / m_PackedParams.W * (float) Math.Sqrt( 1.0f - m_PackedParams.W*m_PackedParams.W );	// Cone radius
			}

			#endregion
		}

		public class	LightDirectional : LightOmni
		{
			#region FIELDS

			protected Vector3	m_Direction = Vector3.UnitY;

			#endregion

			#region PROPERTIES

			public Vector3		Direction		{ get { return m_Direction; } set { m_Direction = value; m_Direction.Normalize(); } }

			#endregion

			#region METHODS

			public LightDirectional( string _Name ) : base( _Name )
			{
			}

			#endregion
		}

		#endregion

		#region FIELDS

		protected Camera						m_Camera = null;

		protected IRenderTarget					m_SourceGeometryBuffer = null;
		protected IDepthStencil					m_SourceDepthStencil = null;

		// GBuffers at downscaled resolution
		protected RenderTarget<PF_RGBA16F>		m_GeometryBuffer = null;	// The downscaled geometry buffer
		protected RenderTarget<PF_RGBA16F>		m_LightBuffer = null;		// The downscaled light buffer
		protected DepthStencil<PF_D32>			m_DepthStencil = null;		// The downscaled depth stencil buffer

		// Material & primitive to downsample the depth-stencil buffer
		protected Material<VS_Pt4V3T2>			m_DepthStencilDownsampleMaterial = null;
		protected Helpers.ScreenQuad			m_Quad = null;

		// The list of different light types
		protected bool							m_bRenderOmnis = false;
		protected List<LightOmni>				m_LightsOmni = new List<LightOmni>();
		protected bool							m_bRenderSpots = true;
		protected List<LightSpot>				m_LightsSpot = new List<LightSpot>();
		protected bool							m_bRenderDirectionals = true;
		protected List<LightDirectional>		m_LightsDirectional = new List<LightDirectional>();
		protected bool							m_bRenderAmbient = true;

		// LOD Primitives for the difference light types
		protected Primitive<VS_P3,short>[]		m_PrimitivesOmni = new Primitive<VS_P3,short>[LODS_COUNT];
		protected Primitive<VS_P3,short>[]		m_PrimitivesSpot = new Primitive<VS_P3,short>[LODS_COUNT];

		#endregion

		#region PROPERTIES

		[System.ComponentModel.Browsable( false )]
		public Camera					Camera
		{
			get { return m_Camera; }
			set { m_Camera = value; }
		}
		[System.ComponentModel.Browsable( false )]
		public IRenderTarget			SourceGeometryBuffer	{ get { return m_SourceGeometryBuffer; } set { m_SourceGeometryBuffer = value; } }
		[System.ComponentModel.Browsable( false )]
		public IDepthStencil			SourceDepthStencil		{ get { return m_SourceDepthStencil; } set { m_SourceDepthStencil = value; } }
		[System.ComponentModel.Browsable( false )]
 		public RenderTarget<PF_RGBA16F>	LightBuffer				{ get { return m_LightBuffer; } }
		[System.ComponentModel.Browsable( false )]
 		public RenderTarget<PF_RGBA16F>	GeometryBuffer			{ get { return m_GeometryBuffer; } }
		[System.ComponentModel.Browsable( false )]
 		public DepthStencil<PF_D32>		DepthStencil			{ get { return m_DepthStencil; } }

		public bool						RenderOmnis				{ get { return m_bRenderOmnis; } set { m_bRenderOmnis = value; } }
		public bool						RenderSpots				{ get { return m_bRenderSpots; } set { m_bRenderSpots = value; } }
		public bool						RenderDirectionals		{ get { return m_bRenderDirectionals; } set { m_bRenderDirectionals = value; } }
		public bool						RenderAmbient			{ get { return m_bRenderAmbient; } set { m_bRenderAmbient = value; } }

		#endregion

		#region METHODS

		public RenderTechniqueInferredLighting( Renderer _Renderer, string _Name, int _LightBufferWidth, int _LightBufferHeight ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = ToDispose( new Material<VS_P3>( m_Device, "Inferred Lighting Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/InferredLighting.fx" ) ) );
			m_DepthStencilDownsampleMaterial = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "Geometry Downsample Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/DepthStencilDownsample.fx" ) ) );

			m_DepthStencilDownsampleMaterial.GetVariableByName( "Offset" ).AsVector.Set( new Vector3( 1.0f / _LightBufferWidth, 1.0f / _LightBufferHeight, 0.0f ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the render targets for lighting
			m_GeometryBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "GeometryBuffer", _LightBufferWidth, _LightBufferHeight, 1 ) );
			m_LightBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "LightBuffer", _LightBufferWidth, _LightBufferHeight, 1 ) );
			m_DepthStencil = ToDispose( new DepthStencil<PF_D32>( m_Device, "DepthStencil", _LightBufferWidth, _LightBufferHeight, true ) );

			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "DepthStencil Downsample Quad", _LightBufferWidth, _LightBufferHeight ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the light primitives
			// Omnis need several LODs depending on their distance to the observer
			VS_P3[]	Vertices = null;
			short[]	Indices = null;
			for ( int LOD=0; LOD < LODS_COUNT; LOD++ )
			{
				int	SidesCount = OMNI_MAX_SIDES + (OMNI_MIN_SIDES - OMNI_MAX_SIDES) * LOD / (LODS_COUNT-1);
				Vertices = new VS_P3[1+SidesCount];
				Indices = new short[3*SidesCount];

				Vertices[0] = new VS_P3() { Position=Vector3.Zero };
				for ( int SideIndex=0; SideIndex < SidesCount; SideIndex++ )
				{
					Vertices[1+SideIndex] = new VS_P3() { Position=new Vector3( (float) Math.Cos( 2.0 * Math.PI * SideIndex / SidesCount ), (float) Math.Sin( 2.0 * Math.PI * SideIndex / SidesCount ), 0.0f ) };

					Indices[3*SideIndex+0] = 0;
					Indices[3*SideIndex+1] = (short) (1 + SideIndex);
					Indices[3*SideIndex+2] = (short) (1 + ((SideIndex+1) % SidesCount));
				}

				m_PrimitivesOmni[LOD] = ToDispose( new Primitive<VS_P3,short>( m_Device, "OmniPrimitive LOD#"+LOD, PrimitiveTopology.TriangleList, Vertices, Indices ) );
			}

			// Same for spots
			for ( int LOD=0; LOD < LODS_COUNT; LOD++ )
			{
				int	SidesCount = SPOT_MAX_SIDES + (SPOT_MIN_SIDES - SPOT_MAX_SIDES) * LOD / (LODS_COUNT-1);
				Vertices = new VS_P3[1+SidesCount];
				Indices = new short[3*SidesCount];

				Vertices[0] = new VS_P3() { Position=new Vector3( 0.0f, 0.0f, 0.0f ) };
				for ( int SideIndex=0; SideIndex < SidesCount; SideIndex++ )
				{
					Vertices[1+SideIndex] = new VS_P3() { Position=new Vector3( (float) Math.Cos( 2.0 * Math.PI * SideIndex / SidesCount ), (float) Math.Sin( 2.0 * Math.PI * SideIndex / SidesCount ), 1.0f ) };

					Indices[3*SideIndex+0] = 0;
					Indices[3*SideIndex+1] = (short) (1 + SideIndex);
					Indices[3*SideIndex+2] = (short) (1 + ((SideIndex+1) % SidesCount));
				}

				m_PrimitivesSpot[LOD] = ToDispose( new Primitive<VS_P3,short>( m_Device, "SpotPrimitive LOD#"+LOD, PrimitiveTopology.TriangleList, Vertices, Indices ) );
			}
		}

		#region Lights Creation/Destruction

		public LightOmni	CreateLightOmni( string _Name, Vector3 _Position, Vector3 _Color, float _InnerRadius, float _OuterRadius )
		{
			LightOmni	Result = new LightOmni( _Name );
			Result.Position = _Position;
			Result.Color = _Color;
			Result.InnerRadius = _InnerRadius;
			Result.OuterRadius = _OuterRadius;
			m_LightsOmni.Add( Result );

			return Result;
		}

		public void		RemoveOmni( LightOmni _Light )
		{
			m_LightsOmni.Remove( _Light );
		}

		public LightSpot	CreateLightSpot( string _Name, Vector3 _Position, Vector3 _Direction, Vector3 _Color, float _InnerRadius, float _OuterRadius, float _InnerAngle, float _OuterAngle )
		{
			LightSpot	Result = new LightSpot( _Name );
			Result.Position = _Position;
			Result.Direction = _Direction;
			Result.Color = _Color;
			Result.InnerRadius = _InnerRadius;
			Result.OuterRadius = _OuterRadius;
			Result.InnerAngle = _InnerAngle;
			Result.OuterAngle = _OuterAngle;
			m_LightsSpot.Add( Result );

			return Result;
		}

		public void		RemoveSpot( LightSpot _Light )
		{
			m_LightsSpot.Remove( _Light );
		}

		public LightDirectional	CreateLightDirectional( string _Name, Vector3 _Direction, Vector3 _Color )
		{
			LightDirectional	Result = new LightDirectional( _Name );
			Result.Direction = _Direction;
			Result.Color = _Color;
			m_LightsDirectional.Add( Result );

			return Result;
		}

		public void		RemoveDirectional( LightDirectional _Light )
		{
			m_LightsDirectional.Remove( _Light );
		}

		#endregion

		protected override void	Render( List<Scene.Mesh.Primitive> _Primitives )
		{
			//////////////////////////////////////////////////////////////////////////
			// Downsample source geometry & depth-stencil buffers
			m_Device.AddProfileTask( this, "Inferred Lighting", "Downsample Normal+Depth" );

			m_Device.SetRenderTarget( m_GeometryBuffer, m_DepthStencil );
			m_Device.SetViewport( 0, 0, m_GeometryBuffer.Width, m_GeometryBuffer.Height, 0.0f, 1.0f );

			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_ALWAYS );	// Always write the depth
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_DepthStencilDownsampleMaterial.UseLock() )
			{
				CurrentMaterial.GetVariableByName( "Geometry" ).AsResource.SetResource( m_SourceGeometryBuffer );
				CurrentMaterial.GetVariableByName( "DepthStencil" ).AsResource.SetResource( m_SourceDepthStencil );
				CurrentMaterial.ApplyPass( 0 );
				
				m_Quad.Render();
			}

//			TODO: Downscale geometry Buffer further to get Z we can feed to the EmissiveRenderingSky & perform volumetric scattering

			//////////////////////////////////////////////////////////////////////////
			// Perform lighting in downscaled resolution
			m_Device.AddProfileTask( this, "Inferred Lighting", "Clear Light Buffer" );

			m_Device.SetRenderTarget( m_LightBuffer, m_DepthStencil );
			m_Device.ClearRenderTarget( m_LightBuffer, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );

			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.NOWRITE_CLOSEST );	// Only read depth to cull polygons
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE );

			using ( m_Material.UseLock() )
			{
				EffectPass	P = null;

				// Use downsampled geometry buffer for lighting here...
				m_Material.GetVariableBySemantic( "GBUFFER_TEX1" ).AsResource.SetResource( m_GeometryBuffer );

				CurrentMaterial.GetVariableByName( "ScreenInfos" ).AsVector.Set( new Vector4( 1.0f / m_LightBuffer.Width, 1.0f / m_LightBuffer.Height, 0.0f, 0.0f ) );
				VariableVector	vPosition = CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector;
				VariableVector	vDirection = CurrentMaterial.GetVariableByName( "LightDirection" ).AsVector;
				VariableVector	vColor = CurrentMaterial.GetVariableByName( "LightColor" ).AsVector;
				VariableVector	vParams0 = CurrentMaterial.GetVariableByName( "LightParams" ).AsVector;
				VariableVector	vParams1 = CurrentMaterial.GetVariableByName( "LightParams2" ).AsVector;

				Frustum	F = m_Camera.Frustum;
				Vector3	CameraPosition = new Vector3( m_Camera.Camera2World.M41, m_Camera.Camera2World.M42, m_Camera.Camera2World.M43 );
				float	InvTanHalfFOV = 1.0f / (float) Math.Tan( 0.5f * m_Camera.PerspectiveFOV );
				Matrix	World2Camera = m_Camera.World2Camera;

				// =============================================
				// Render spot lights
				m_Device.AddProfileTask( this, "Inferred Lighting", "Render Spot Lights" );
				if ( m_bRenderSpots )
				{
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DrawSpotLights" );
					P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
					foreach ( LightSpot L in m_LightsSpot )
					{
						Vector3	LightPositionCameraSpace = Vector3.TransformCoordinate( L.Position, World2Camera );
						if ( !F.IsInsideInclusive( LightPositionCameraSpace, L.OuterRadius ) )
							continue;	// Skip

						Vector3	LightDirectionCameraSpace = Vector3.TransformNormal( L.Direction, World2Camera );

						vPosition.Set( LightPositionCameraSpace );
						vDirection.Set( LightDirectionCameraSpace );
						vColor.Set( L.Color );
						vParams0.Set( L.PackedParams );
						vParams1.Set( L.PackedParams2 );
						P.Apply();

						// Project position to see how many pixels that spot is covering and determine LOD
						float	ScreenRatio = 1.0f - Math.Min( 1.0f, 0.5f * L.OuterRadius * InvTanHalfFOV / Math.Max( m_Camera.Near, LightPositionCameraSpace.Z ) );
						int		LOD = (int) Math.Floor( ScreenRatio * LODS_COUNT );

						// Draw the appropriate LOD
						m_PrimitivesSpot[LOD].RenderOverride();
					}
				}

				// =============================================
				// Render omni lights
				m_Device.AddProfileTask( this, "Inferred Lighting", "Render Omni Lights" );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );	// Disable for omnis as we render a 2D disc that will be wrongly clipped otherwise

				if ( m_bRenderOmnis )
				{
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DrawOmniLights" );
					P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
					foreach ( LightOmni L in m_LightsOmni )
					{
						Vector3	LightPositionCameraSpace = Vector3.TransformCoordinate( L.Position, World2Camera );
						if ( !F.IsInsideInclusive( LightPositionCameraSpace, L.OuterRadius ) )
							continue;	// Skip

						vPosition.Set( LightPositionCameraSpace );
						vColor.Set( L.Color );
						vParams0.Set( L.PackedParams );
						P.Apply();

						// Project position to see how many pixels that omni is covering and determine LOD
						float	ScreenRatio = 1.0f - Math.Min( 1.0f, L.OuterRadius * InvTanHalfFOV / Math.Max( m_Camera.Near, LightPositionCameraSpace.Z ) );
						int		LOD = (int) Math.Floor( ScreenRatio * LODS_COUNT );

						// Draw the appropriate LOD
						m_PrimitivesOmni[LOD].RenderOverride();
					}
				}

				// =============================================
				// Render directional lights
				m_Device.AddProfileTask( this, "Inferred Lighting", "Render Directional Lights" );
				if ( m_bRenderDirectionals )
				{
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DrawDirectionalLights" );
					P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
					foreach ( LightDirectional L in m_LightsDirectional )
					{
						Vector3	LightDirectionCameraSpace = Vector3.TransformNormal( L.Direction, World2Camera );
						vDirection.Set( LightDirectionCameraSpace );
						vColor.Set( L.Color );
						P.Apply();

						// Render an entire screen quad
						m_Quad.Render();
					}
				}

				// =============================================
				// Render ambient SH
				m_Device.AddProfileTask( this, "Inferred Lighting", "Render Ambient SH" );
				if ( m_bRenderAmbient )
				{
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DrawAmbientSH" );
					P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
					P.Apply();

					// Render an entire screen quad
					m_Quad.Render();
				}
			}
		}

		#endregion
	}
}
