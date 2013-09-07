using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;
using Nuaj.Cirrus;

namespace Demo
{
/*
	* TODO: Don't clip light frustum with camera near plane
	*/

	/// <summary>
	/// Shadow mapping pre-process
	/// </example>
	public class RenderTechniqueShadowMap : RenderTechniqueBase, IShaderInterfaceProvider
	{
		#region CONSTANTS

		protected const int		SHADOW_MAP_SIZE = 512;
		protected const int		SPLITS_COUNT = 1;
		protected const float	LIGHT_CLIP_NEAR = 1e-1f;
		protected const float	INFINITY = 1e4f;

		#endregion

		#region NESTED TYPES

		protected class IShadowMapSupport : ShaderInterfaceBase
		{
			[Semantic( "SHADOWMAP_KEY" )]
			public ShaderResourceView		ShadowMapKey		{ set { SetResource( "SHADOWMAP_KEY", value ); } }
			[Semantic( "WORLD2LIGHTPROJ_KEY" )]
			public Matrix[]					World2LightProjKey	{ set { SetMatrix( "WORLD2LIGHTPROJ_KEY", value ); } }
			[Semantic( "SHADOW_ENABLED_KEY" )]
			public bool						ShadowEnabledKey	{ set { SetScalar( "SHADOW_ENABLED_KEY", value ); } }

			[Semantic( "SHADOWMAP_RIM" )]
			public ShaderResourceView		ShadowMapRim		{ set { SetResource( "SHADOWMAP_RIM", value ); } }
			[Semantic( "WORLD2LIGHTPROJ_RIM" )]
			public Matrix[]					World2LightProjRim	{ set { SetMatrix( "WORLD2LIGHTPROJ_RIM", value ); } }
			[Semantic( "SHADOW_ENABLED_RIM" )]
			public bool						ShadowEnabledRim	{ set { SetScalar( "SHADOW_ENABLED_RIM", value ); } }

			[Semantic( "SHADOWMAP_FILL" )]
			public ShaderResourceView		ShadowMapFill		{ set { SetResource( "SHADOWMAP_FILL", value ); } }
			[Semantic( "WORLD2LIGHTPROJ_FILL" )]
			public Matrix[]					World2LightProjFill	{ set { SetMatrix( "WORLD2LIGHTPROJ_FILL", value ); } }
			[Semantic( "SHADOW_ENABLED_FILL" )]
			public bool						ShadowEnabledFill	{ set { SetScalar( "SHADOW_ENABLED_FILL", value ); } }

			[Semantic( "SHADOW_SPLITS" )]
			public Vector4					ShadowSplits		{ set { SetVector( "SHADOW_SPLITS", value ); } }

			[Semantic( "SHADOW_EXPONENT" )]
			public float					ShadowExponent		{ set { SetScalar( "SHADOW_EXPONENT", value ); } }
		}

		protected class	ShadowMapSpot : IDisposable
		{
			#region FIELDS

			protected RenderTechniqueShadowMap	m_Owner = null;
			protected SpotLight					m_Light = null;
			protected bool						m_bEnabled = true;
			protected RenderTarget<PF_R32F>		m_ShadowMap = null;

			// Light transforms
			protected Matrix					m_Light2World = Matrix.Identity;
			protected Matrix					m_World2Light = Matrix.Identity;
			protected Matrix[]					m_World2LightProjs = new Matrix[SPLITS_COUNT];

			#endregion

			#region PROPERTIES

			public SpotLight					Light				{ get { return m_Light; } set { m_Light = value; } }
			public bool							Enabled				{ get { return m_bEnabled && m_Light != null; } set { m_bEnabled = value; } }

			public RenderTarget<PF_R32F>		ShadowMap			{ get { return m_ShadowMap; } }
			public Matrix						Light2World			{ get { return m_Light2World; } }
			public Matrix						World2Light			{ get { return m_World2Light; } }
			public Matrix[]						World2LightProjs	{ get { return m_World2LightProjs; } }

			#endregion

			#region METHODS

			public ShadowMapSpot( RenderTechniqueShadowMap _Owner, string _Name )
			{
				m_Owner = _Owner;
				m_ShadowMap = new RenderTarget<PF_R32F>( _Owner.m_Device, _Name, SHADOW_MAP_SIZE, SHADOW_MAP_SIZE, 1, SPLITS_COUNT, 1 );
			}

			#region IDisposable Members

			public void Dispose()
			{
				m_ShadowMap.Dispose();
			}

			#endregion

			public void		ComputeLightTransforms( List<BoundingBox> _Casters, List<BoundingBox> _Receivers )
			{
				if ( !m_bEnabled || m_Light == null )
					return;

				// Compute light's characteristics
				Vector3		LightPosition = m_Light.Position;
				Vector3		LightDirection = m_Light.Direction;
				float		LightFOV = m_Light.ConeAngleMax;

				Vector3		Right = Vector3.Cross( Vector3.UnitY, LightDirection );
				Right.Normalize();
				Vector3		Up = Vector3.Cross( LightDirection, Right );

				// Compute LOCAL2WORLD matrix
				m_Light2World = Matrix.Identity;
				m_Light2World.Row1 = new Vector4( Right, 0.0f );
				m_Light2World.Row2 = new Vector4( Up, 0.0f );
				m_Light2World.Row3 = new Vector4( -LightDirection, 0.0f );
				m_Light2World.Row4 = new Vector4( LightPosition, 1.0f );

				m_World2Light = m_Light2World;
				m_World2Light.Invert();

				// Compute PROJECTION MATRIX
				Matrix		Light2Proj = Matrix.PerspectiveFovLH( LightFOV, 1.0f, 1e-1f, Light.RangeMax );
				Matrix		World2LightProj = m_World2Light * Light2Proj;
				Matrix		Camera2Light = m_Owner.m_Renderer.Camera.Camera2World * m_World2Light;

				// Project casters
				BoundingBox	CastersAABB = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
				foreach ( BoundingBox CasterAABB in _Casters )
					Union( ref CastersAABB, CasterAABB.GetCorners(), ref m_World2Light, ref Light2Proj );

				// Project receivers
				BoundingBox	ReceiversAABB = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
				foreach ( BoundingBox ReceiverAABB in _Receivers )
					Union( ref ReceiversAABB, ReceiverAABB.GetCorners(), ref m_World2Light, ref Light2Proj );

				// Compute AABB in LIGHT space for each split
				for ( int SplitIndex=0; SplitIndex < SPLITS_COUNT; SplitIndex++ )
				{
					// Project camera frustum into light space
 					BoundingBox	FrustumAABB = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
					Union( ref FrustumAABB, m_Owner.m_CameraFrustumBoxes[SplitIndex], ref Camera2Light, ref Light2Proj );

					// Compute ideal AABB for the shadow map
					BoundingBox	Crop;
					Crop.Minimum.X = Math.Max( Math.Max( CastersAABB.Minimum.X, ReceiversAABB.Minimum.X ), FrustumAABB.Minimum.X );
					Crop.Maximum.X = Math.Min( Math.Min( CastersAABB.Maximum.X, ReceiversAABB.Maximum.X ), FrustumAABB.Maximum.X );
					Crop.Minimum.Y = Math.Max( Math.Max( CastersAABB.Minimum.Y, ReceiversAABB.Minimum.Y ), FrustumAABB.Minimum.Y );
					Crop.Maximum.Y = Math.Min( Math.Min( CastersAABB.Maximum.Y, ReceiversAABB.Maximum.Y ), FrustumAABB.Maximum.Y );
					Crop.Minimum.Z = Math.Max( CastersAABB.Minimum.Z, FrustumAABB.Minimum.Z );
					Crop.Maximum.Z = Math.Min( CastersAABB.Maximum.Z, FrustumAABB.Maximum.Z );

					// Build final crop & projection matrices
					float	ScaleX = 2.0f / (Crop.Maximum.X - Crop.Minimum.X);
					float	ScaleY = 2.0f / (Crop.Maximum.Y - Crop.Minimum.Y);
					float	ScaleZ = 1.0f / (Crop.Maximum.Z - Crop.Minimum.Z);
					float	OffsetX = -0.5f * (Crop.Maximum.X + Crop.Minimum.X) * ScaleX;
					float	OffsetY = -0.5f * (Crop.Maximum.Y + Crop.Minimum.Y) * ScaleY;
					float	OffsetZ = -Crop.Minimum.Z * ScaleZ;

					Matrix	CropMatrix = Matrix.Identity;
					CropMatrix.M11 = ScaleX;
					CropMatrix.M22 = ScaleY;
					CropMatrix.M33 = ScaleZ;
					CropMatrix.M41 = OffsetX;
					CropMatrix.M42 = OffsetY;
					CropMatrix.M43 = OffsetZ;

					m_World2LightProjs[SplitIndex] = World2LightProj * CropMatrix;

// DEBUG
// BoundingBox	BBOXTest = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );
// Vector3		Proj;
// foreach ( BoundingBox CasterAABB in _Casters )
// 	foreach ( Vector3 Corner in CasterAABB.GetCorners() )
// 	{
// 		TransformCoordinate( Corner, ref m_World2Light, ref Light2Proj, out Proj );
// 		BBOXTest.Minimum = Vector3.Min( BBOXTest.Minimum, Proj );
// 		BBOXTest.Maximum = Vector3.Max( BBOXTest.Maximum, Proj );
// 	}
// DEBUG

				}
			}

			public void		SetAsRenderTarget( VariableMatrix _vWorld2LightProj )
			{
				m_Owner.m_Device.SetRenderTarget( m_ShadowMap );
				m_Owner.m_Device.ClearRenderTarget( m_ShadowMap, new Color4( +float.MaxValue, +float.MaxValue, +float.MaxValue, +float.MaxValue ) );
				_vWorld2LightProj.SetMatrix( m_World2LightProjs );
			}

			/// <summary>
			/// Transforms an object's WORLD AABB into light projective space and merges the resulting AABB with the global AABB
			/// </summary>
			/// <param name="_GroupAABB"></param>
			/// <param name="_ObjectAABB"></param>
			/// <param name="_ToLight">The transform to light space</param>
			/// <param name="_Light2Proj">The projection matrix</param>

			static int[][]	ms_BoxEdges = new int[12][]
			{
				new int [2] { 0, 1 },
				new int [2] { 1, 2 },
				new int [2] { 2, 3 },
				new int [2] { 3, 0 },
				new int [2] { 4, 5 },
				new int [2] { 5, 6 },
				new int [2] { 6, 7 },
				new int [2] { 7, 4 },
				new int [2] { 0, 4 },
				new int [2] { 1, 5 },
				new int [2] { 2, 6 },
				new int [2] { 3, 7 },
			};
			protected Vector3[]	m_BoxCornersTransformed = new Vector3[8];
			protected int		m_CutBoxCornersCount = 0;
			protected Vector3[]	m_CutBoxCorners = new Vector3[12];
			protected bool[]	m_bBoxCornerInFront = new bool[8];
			protected void	Union( ref BoundingBox _GroupAABB, Vector3[] _BoxCorners, ref Matrix _ToLight, ref Matrix _Light2Proj )
			{
				// Transform all 8 corners
				Vector3.TransformCoordinate( _BoxCorners, ref _ToLight, m_BoxCornersTransformed );

				// Check for cull/cut
				int	FrontCount = 0;
				for ( int CornerIndex=0; CornerIndex < 8; CornerIndex++ )
					FrontCount += (m_bBoxCornerInFront[CornerIndex] = m_BoxCornersTransformed[CornerIndex].Z > LIGHT_CLIP_NEAR) ? 1 : 0;

				if ( FrontCount == 0 )
					return;		// The box is fully behind the light ! Cull...

				Vector3[]	BoxCorners = m_BoxCornersTransformed;
				int			BoxCornersCount = 8;
				if ( FrontCount != 8 )
				{	// Cut !
					CutBox( m_BoxCornersTransformed, LIGHT_CLIP_NEAR, m_bBoxCornerInFront, m_CutBoxCorners, out m_CutBoxCornersCount );
					BoxCorners = m_CutBoxCorners;
					BoxCornersCount = m_CutBoxCornersCount;
				}

				// Update BBox
				Vector3	CornerLightProj;
				for ( int CornerIndex=0; CornerIndex < BoxCornersCount; CornerIndex++ )
				{
					Vector3.TransformCoordinate( ref BoxCorners[CornerIndex], ref _Light2Proj, out CornerLightProj );

					// For spotlights that use perspective projection, it's no use to account for
					// points that are outside of the frustum so we clip XYZ
					CornerLightProj.X = Math.Max( -1.0f, Math.Min( 1.0f, CornerLightProj.X ) );
					CornerLightProj.Y = Math.Max( -1.0f, Math.Min( 1.0f, CornerLightProj.Y ) );
					CornerLightProj.Z = Math.Min( 1.0f, CornerLightProj.Z );

					// Grow the box
					_GroupAABB.Minimum.X = Math.Min( _GroupAABB.Minimum.X, CornerLightProj.X );
					_GroupAABB.Minimum.Y = Math.Min( _GroupAABB.Minimum.Y, CornerLightProj.Y );
					_GroupAABB.Minimum.Z = Math.Min( _GroupAABB.Minimum.Z, CornerLightProj.Z );
					_GroupAABB.Maximum.X = Math.Max( _GroupAABB.Maximum.X, CornerLightProj.X );
					_GroupAABB.Maximum.Y = Math.Max( _GroupAABB.Maximum.Y, CornerLightProj.Y );
					_GroupAABB.Maximum.Z = Math.Max( _GroupAABB.Maximum.Z, CornerLightProj.Z );
				}
			}

			protected void	CutBox( Vector3[] _BoxCorners, float _CutPlane, bool[] _CornersInFront, Vector3[] _CutCorners, out int _CutCornersCount )
			{
				// Copy corners that are in front
				_CutCornersCount = 0;
				for ( int CornerIndex=0; CornerIndex < 8; CornerIndex++ )
					if ( _CornersInFront[CornerIndex] )
						_CutCorners[_CutCornersCount++] = _BoxCorners[CornerIndex];

				// Cut edges that span the cut plane
				for ( int EdgeIndex=0; EdgeIndex < 12; EdgeIndex++ )
				{
					int[]	EdgeIndices = ms_BoxEdges[EdgeIndex];
					if ( _CornersInFront[EdgeIndices[0]] ^ _CornersInFront[EdgeIndices[1]] )
					{
						Vector3	Start = _BoxCorners[EdgeIndices[0]];
						Vector3	Edge = _BoxCorners[EdgeIndices[1]] - Start;
						if ( Math.Abs( Edge.Z ) < 1e-3f )
							continue;	// Too small to cut...

						_CutCorners[_CutCornersCount] = Start + ((_CutPlane - Start.Z) / Edge.Z) * Edge;
						_CutCorners[_CutCornersCount++].Z = LIGHT_CLIP_NEAR;
					}
				}
			}

			/// <summary>
			/// Transforms a coordinate using the special projection transform but does not divide Z by W as Z is not changed by the transform
			/// </summary>
			/// <param name="_Point"></param>
			/// <param name="_ToLight">The transform to light space</param>
			/// <param name="_Light2Proj">The projection matrix</param>
			/// <param name="_Result"></param>
			protected bool	TransformCoordinate( Vector3 _Point, ref Matrix _ToLight, ref Matrix _Light2Proj, out Vector3 _Result )
			{
				// Transform into light space
				Vector3.TransformCoordinate( ref _Point, ref _ToLight, out _Result );

				// Near clip
				if ( _Result.Z < LIGHT_CLIP_NEAR )
					return false;

				// Project
				Vector3.TransformCoordinate( ref _Result, ref _Light2Proj, out _Result );

				return true;
			}

			#endregion
		}

		#endregion

		#region FIELDS

		protected Material<VS_P3>				m_Material = null;
		protected List<IShadowMapRenderable>	m_Renderables = new List<IShadowMapRenderable>();

		protected ShadowMapSpot[]				m_ShadowMaps = new ShadowMapSpot[3];

		// Parameters
		protected float							m_Lambda = 0.85f;	// Correction factor between a full exponential split and a linear one (cf. "Cascaded Shadow Maps" by Rouslan Dimitrov)
		protected bool							m_bUseCameraNearFarOverride = false;
		protected float							m_CameraNear = 1.0f;
		protected float							m_CameraFar = 50.0f;
		protected float							m_ShadowExponent = 40.0f;

		// The list of camera frustums & near/far ranges
		protected Vector3[][]					m_CameraFrustumBoxes = new Vector3[SPLITS_COUNT][];
		protected Vector4						m_SplitRanges = Vector4.Zero;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the near/far override
		/// </summary>
		/// <remarks>If true, the technique will use its embedded near/far values for the camera frustum</remarks>
		public bool							UseCameraNearFarOverride	{ get { return m_bUseCameraNearFarOverride; } set { m_bUseCameraNearFarOverride = value; UpdateCameraData(); } }

		/// <summary>
		/// Gets or sets the camera near override value
		/// </summary>
		public float						CameraNearOverride	{ get { return m_CameraNear; } set { m_CameraNear = value; UpdateCameraData(); } }

		/// <summary>
		/// Gets or sets the camera far override value
		/// </summary>
		public float						CameraFarOverride	{ get { return m_CameraFar; } set { m_CameraFar = value; UpdateCameraData(); } }

		/// <summary>
		/// Gets or sets the lambda correction factor
		/// </summary>
		/// <remarks>
		/// The actual slice plane distances are computed like this:
		/// 
		///                i/N   
		///  z  = λ n (f/n)    + (1−λ) (n + (i/N)(f-n))
		///   i
		/// 
		/// where λ controls the strength of the correction.
		/// This is simply an interpolation between a fully exponential split (first part of the expression)
		///  and a regular linear split (second part of the expression).
		/// </remarks>
		public float						LambdaCorrection	{ get { return m_Lambda; } set { m_Lambda = value; UpdateCameraData(); } }

		/// <summary>
		/// Gets or sets the shadow's exponential factor (i.e. ShadowValue = exp( ShadowExponent * z ) )
		/// </summary>
		public float						ShadowExponent		{ get { return m_ShadowExponent; } set { m_ShadowExponent = value; } }

		#endregion

		#region METHODS

		public RenderTechniqueShadowMap( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			m_Device.DeclareShaderInterface( typeof(IShadowMapSupport) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IShadowMapSupport), this );

			// Create our main materials
			m_Material = m_Renderer.LoadMaterial<VS_P3>( "ShadowMap Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/BuildShadowMap.fx" ) );

			// Create the key/rim/fill shadow maps
			m_ShadowMaps[0] = ToDispose( new ShadowMapSpot( this, "ShadowMap Key" ) );
			m_ShadowMaps[1] = ToDispose( new ShadowMapSpot( this, "ShadowMap Rim" ) );
			m_ShadowMaps[2] = ToDispose( new ShadowMapSpot( this, "ShadowMap Fill" ) );

			// Subscribe to any camera change
			for ( int SplitIndex=0; SplitIndex < SPLITS_COUNT; SplitIndex++ )
				m_CameraFrustumBoxes[SplitIndex] = new Vector3[8];

			m_Renderer.CameraChanged += new EventHandler( Renderer_CameraChanged );
			Renderer_CameraChanged( m_Renderer, EventArgs.Empty );
		}

		/// <summary>
		/// Adds a renderable object/technique
		/// </summary>
		/// <param name="_Renderable"></param>
		public void		AddRenderable( IShadowMapRenderable _Renderable )
		{
			m_Renderables.Add( _Renderable );
		}

		public override void	Render( int _FrameToken )
		{
			m_Device.AddProfileTask( this, "Shadow Pass", "<START>" );

			m_Device.SetViewport( 0, 0, SHADOW_MAP_SIZE, SHADOW_MAP_SIZE, 0.0f, 1.0f );
 			m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.MIN );

			string[]	LightNames = new string[] { "KEY", "RIM", "FILL" };
			SpotLight[]	Lights = new SpotLight[] { m_Renderer.LightKey, m_Renderer.LightRim, m_Renderer.LightFill };

			m_ShadowMaps[0].Enabled = m_Renderer.LightKeyCastShadow;
			m_ShadowMaps[1].Enabled = m_Renderer.LightRimCastShadow;
			m_ShadowMaps[2].Enabled = m_Renderer.LightFillCastShadow;

			//////////////////////////////////////////////////////////////////////////
			// Prepare receivers & casters bounding box lists
			List<BoundingBox>	CastersBBox = new List<BoundingBox>();
			List<BoundingBox>	ReceiversBBox = new List<BoundingBox>();
			foreach ( IShadowMapRenderable Renderable in m_Renderables )
			{
				CastersBBox.AddRange( Renderable.ShadowCastersWorldAABB );
				ReceiversBBox.AddRange( Renderable.ShadowReceiversWorldAABB );
			}

			//////////////////////////////////////////////////////////////////////////
			// Compute lights' projection matrices
			for ( int LightIndex=0; LightIndex < 3; LightIndex++ )
			{
				m_ShadowMaps[LightIndex].Light = Lights[LightIndex];
				m_ShadowMaps[LightIndex].ComputeLightTransforms( CastersBBox, ReceiversBBox );
			}

			//////////////////////////////////////////////////////////////////////////
			// Render every light
			using ( m_Material.UseLock() )
			{
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "BuildShadowMap" );

				int FrameToken = m_FrameToken-100;	// -100 so we can always render objects for the ShadowPass and so they can render again in normal passes afterward

				EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				CurrentMaterial.GetVariableByName( "ShadowExponent" ).AsScalar.Set( m_ShadowExponent );
				VariableMatrix	vLocal2World = CurrentMaterial.GetVariableByName( "Local2World" ).AsMatrix;
				VariableMatrix	vWorld2LightProj = CurrentMaterial.GetVariableByName( "World2LightProj" ).AsMatrix;

				for ( int LightIndex=0; LightIndex < 3; LightIndex++ )
					if ( m_ShadowMaps[LightIndex].Enabled )
					{
						m_Device.AddProfileTask( this, "Shadow Pass", LightNames[LightIndex] );

						// Prepare shadow map
						m_ShadowMaps[LightIndex].SetAsRenderTarget( vWorld2LightProj );

						// Render every renderable objects
						foreach ( IShadowMapRenderable Renderable in m_Renderables )
							Renderable.RenderDepthPass( FrameToken, Pass, vLocal2World );

						FrameToken++;
					}
			}

			m_Device.AddProfileTask( this, "Depth Pass", "<END>" );
		}

		public void	RenderShadowMapDebug( int _LightIndex, int _SplitIndex )
		{
			m_Device.SetViewport( 0, 0, 128, 128, 0.0f, 1.0f );

			using ( m_Material.UseLock() )
			{
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DebugShadowMap" );
				CurrentMaterial.GetVariableByName( "DEBUGShadowMap" ).AsResource.SetResource( m_ShadowMaps[_LightIndex].ShadowMap );
				CurrentMaterial.GetVariableByName( "DEBUGSplitIndex" ).AsScalar.Set( _SplitIndex );

				m_Material.ApplyPass(0);
				m_Renderer.RenderPostProcessQuad();
			}
		}

		/// <summary>
		/// Call this if you made changes to the camera projection data
		/// </summary>
		public void	UpdateCameraData()
		{
			Camera	Camera = m_Renderer.Camera;
			if ( Camera == null )
				return;

			// Build camera rays that will define the frustum's corners
			Vector3[]	RayOrigins = new Vector3[4];
			Vector3[]	RayDirections = new Vector3[4];

			if ( Camera.IsPerspective )
			{
				RayOrigins[0] = RayOrigins[1] = RayOrigins[2] = RayOrigins[3] = Vector3.Zero;

				float	HalfTanY = (float) Math.Tan( 0.5f * Camera.PerspectiveFOV );
				float	HalfTanX = Camera.AspectRatio * HalfTanY;
				RayDirections[0] = new Vector3( -HalfTanX, +HalfTanY, 1.0f );
				RayDirections[1] = new Vector3( -HalfTanX, -HalfTanY, 1.0f );
				RayDirections[2] = new Vector3( +HalfTanX, -HalfTanY, 1.0f );
				RayDirections[3] = new Vector3( +HalfTanX, +HalfTanY, 1.0f );
			}
			else
			{
				RayDirections[0] = RayDirections[1] = RayDirections[2] = RayDirections[3] = Vector3.UnitZ;

				float	HalfHeight = 0.5f * Camera.OrthographicHeight;
				float	HalfWidth = Camera.AspectRatio * HalfHeight;

				RayOrigins[0] = new Vector3( -HalfWidth, +HalfHeight, 0.0f );
				RayOrigins[1] = new Vector3( -HalfWidth, -HalfHeight, 0.0f );
				RayOrigins[2] = new Vector3( +HalfWidth, -HalfHeight, 0.0f );
				RayOrigins[3] = new Vector3( +HalfWidth, +HalfHeight, 0.0f );
			}

			// Rebuild frustums and ranges
			float	fCameraNear = m_bUseCameraNearFarOverride ? m_CameraNear : Camera.Near;
			float	fCameraFar = m_bUseCameraNearFarOverride ? m_CameraFar : Camera.Far;

			Vector2[]	SplitRanges = new Vector2[SPLITS_COUNT];

			float	fSliceFar = fCameraNear;
			for ( int SplitIndex=0; SplitIndex < SPLITS_COUNT; SplitIndex++ )
			{
				float	fSliceNear = fSliceFar;

				// Compute new far clip distance for that slice
				float	fExponentialFar = fCameraNear * (float) Math.Pow( fCameraFar / fCameraNear, (float) (SplitIndex+1) / SPLITS_COUNT );
				float	fLinearFar = fCameraNear + (fCameraFar - fCameraNear) * (SplitIndex+1) / SPLITS_COUNT;
				fSliceFar = m_Lambda * fExponentialFar + (1.0f - m_Lambda) * fLinearFar;

				SplitRanges[SplitIndex].X = fSliceNear;
				SplitRanges[SplitIndex].Y = fSliceFar;

				// Build the appropriate frustum (in camera local space)
				m_CameraFrustumBoxes[SplitIndex][0] = RayOrigins[0] + fSliceNear * RayDirections[0];
				m_CameraFrustumBoxes[SplitIndex][1] = RayOrigins[1] + fSliceNear * RayDirections[1];
				m_CameraFrustumBoxes[SplitIndex][2] = RayOrigins[2] + fSliceNear * RayDirections[2];
				m_CameraFrustumBoxes[SplitIndex][3] = RayOrigins[3] + fSliceNear * RayDirections[3];
				m_CameraFrustumBoxes[SplitIndex][4] = RayOrigins[0] + fSliceFar  * RayDirections[0];
				m_CameraFrustumBoxes[SplitIndex][5] = RayOrigins[1] + fSliceFar  * RayDirections[1];
				m_CameraFrustumBoxes[SplitIndex][6] = RayOrigins[2] + fSliceFar  * RayDirections[2];
				m_CameraFrustumBoxes[SplitIndex][7] = RayOrigins[3] + fSliceFar  * RayDirections[3];
			}

			// Collapse ranges into a single vector
			switch ( SPLITS_COUNT )
			{
				case 1:
					m_SplitRanges.X = SplitRanges[0].X;
					m_SplitRanges.Y = SplitRanges[0].Y;
					break;
				case 2:
					m_SplitRanges.X = SplitRanges[0].X;
					m_SplitRanges.Y = SplitRanges[1].X;
					m_SplitRanges.Z = SplitRanges[1].Y;
					break;
				case 3:
					m_SplitRanges.X = SplitRanges[0].X;
					m_SplitRanges.Y = SplitRanges[1].X;
					m_SplitRanges.Z = SplitRanges[2].X;
					m_SplitRanges.W = SplitRanges[2].Y;
					break;
				default:
					throw new NException( this, "Unsupported amount of splits ! (Min is 1 and max is 3)" );
			}
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			IShadowMapSupport	I = _Interface as IShadowMapSupport;
			I.ShadowMapKey = m_ShadowMaps[0].ShadowMap.TextureView;
			I.World2LightProjKey = m_ShadowMaps[0].World2LightProjs;
			I.ShadowEnabledKey = m_ShadowMaps[0].Enabled;
			I.ShadowMapRim = m_ShadowMaps[1].ShadowMap.TextureView;
			I.World2LightProjRim = m_ShadowMaps[1].World2LightProjs;
			I.ShadowEnabledRim = m_ShadowMaps[1].Enabled;
			I.ShadowMapFill = m_ShadowMaps[2].ShadowMap.TextureView;
			I.World2LightProjFill = m_ShadowMaps[2].World2LightProjs;
			I.ShadowEnabledFill = m_ShadowMaps[2].Enabled;
			I.ShadowSplits = m_SplitRanges;
			I.ShadowExponent = m_ShadowExponent;
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		protected void Renderer_CameraChanged( object sender, EventArgs e )
		{
			UpdateCameraData();
		}

		#endregion
	}
}
