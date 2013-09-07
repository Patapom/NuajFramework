using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This is the render technique that is able to render the scene into multiple, cascaded shadow maps
	/// 
	/// To setup the render technique you must provide :
	///		_ A valid camera object (using the Camera property)
	///		_ A valid directional light object (using the Light property)
	///		_ Add some shadow casters (using the AddShadowCaster() method)
	///	
	/// You can also override the camera near/far clipping planes and specify more narrow and more adapted
	///  values for the shadow maps (e.g. typically, even though your camera far plane may be at 1000 units,
	///  you may only cast shadows as far as 400 units, thus increasing the shadows quality).
	/// </example>
	public class RenderTechniqueShadowMap : RenderTechniqueDefault, IShaderInterfaceProvider
	{
		#region NESTED TYPES

		public class		InitParams
		{
			public int		SlicesCount = 0;
			public int		ShadowMapWidth = 0;
			public int		ShadowMapHeight = 0;
		}

		#endregion

		#region FIELDS

		protected InitParams				m_Params = null;
		protected Camera					m_Camera = null;
		protected DirectionalLight			m_Light = null;
		protected float						m_Lambda = 1.0f;	// Correction factor between a full exponential split and a linear one (cf. "Cascaded Shadow Maps" by Rouslan Dimitrov)
		protected bool						m_bUseCameraNearFarOverride = false;
		protected float						m_CameraNear = 1.0f;
		protected float						m_CameraFar = 500.0f;
		protected Vector3					m_ShadowBias = new Vector3( 1.0f, 0.1f, 2.0f );	// Shadow bias in WORLD units

		// The list of camera frustums & near/far ranges
		protected Frustum[]					m_CameraFrustums = null;
		protected Vector4[]					m_SliceRanges = null;


		// The list of shadow primitives
		// We don't use the parent's list of primitives as we don't own these primitives and don't want them to be disposed of
		protected Dictionary<Scene.Mesh,int>	m_ShadowMeshes2PrimitivesCount = new Dictionary<Scene.Mesh,int>();
		protected List<Scene.Mesh.Primitive>	m_ShadowPrimitives = new List<Scene.Mesh.Primitive>();

		// Shadow maps parameters
		protected RenderTarget<PF_R16F>		m_ShadowMaps = null;
		protected DepthStencil<PF_D32>		m_DepthStencil = null;
		protected float						m_LightClipNear = 0.0f;
		protected float						m_LightClipFar = 0.0f;
		protected Matrix[]					m_LightProjectionMatrices = null;
		protected Matrix[]					m_InverseLightProjectionMatrices = null;

		#endregion

		#region PROPERTIES

		public override IVertexSignature	RecognizedSignature	{ get { return m_Signature; } }
		public override IMaterial			MainMaterial		{ get { return m_Material; } }

		/// <summary>
		/// Gets the parameters used for initialization
		/// </summary>
		public InitParams					Params				{ get { return m_Params; } }

		/// <summary>
		/// Gets or sets the camera that will be used to receive shadows
		/// </summary>
		public Camera						Camera	{ get { return m_Camera; } set { m_Camera = value; UpdateCameraData(); } }

		/// <summary>
		/// Gets or sets the light that will be used to cast shadows
		/// </summary>
		public DirectionalLight				Light	{ get { return m_Light; } set { m_Light = value; } }

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
		/// Gets or sets the shadow bias in WORLD units that is added to the shadow depth
		/// </summary>
		public Vector3						ShadowBias			{ get { return m_ShadowBias; } set { m_ShadowBias = value; } }

		#endregion

		#region METHODS

		public	RenderTechniqueShadowMap( Device _Device, string _Name, InitParams _Params ) : base( _Device, _Name )
		{
			m_Params = _Params;
			if ( m_Params == null )
				throw new NException( this, "You must provide an instance of InitParams when calling the RenderTechniqueShadowMap constructor !" );

			// Build the signature we can support
			m_Signature.ClearFields();
			m_Signature.AddField( "Position", VERTEX_FIELD_USAGE.POSITION, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Normal", VERTEX_FIELD_USAGE.NORMAL, VERTEX_FIELD_TYPE.FLOAT3, 0 );

			// Create our main material
			m_Material = ToDispose( new Material<VS_P3N3G3B3T2>( m_Device, "ShadowMaterial", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/ComputeShadowMap.fx" ) ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the multiple render targets we'll render to
			if ( m_Params.SlicesCount == 0 )
				throw new NException( this, "You must specify at least one shadow slice !" );
			if ( m_Params.SlicesCount > 6 )
				throw new NException( this, "You cannot specify more than 6 shadow slices !" );

			m_ShadowMaps = ToDispose( new RenderTarget<PF_R16F>( m_Device, "ShadowMaps", m_Params.ShadowMapWidth, m_Params.ShadowMapHeight, 1, m_Params.SlicesCount, 1 ) );

			m_LightProjectionMatrices = new Matrix[m_Params.SlicesCount];
			m_InverseLightProjectionMatrices = new Matrix[m_Params.SlicesCount];
			m_CameraFrustums = new Frustum[m_Params.SlicesCount];
			m_SliceRanges = new Vector4[m_Params.SlicesCount];

			//////////////////////////////////////////////////////////////////////////
			// Create our depth-stencil target
			m_DepthStencil = ToDispose( new DepthStencil<PF_D32>( m_Device, "ShadowDepthStencil", m_Params.ShadowMapWidth, m_Params.ShadowMapHeight, false ) );

			//////////////////////////////////////////////////////////////////////////
			// Register ourselves as being a provider for shadow map shader interfaces
			m_Device.DeclareShaderInterface( typeof(IShadowMap) );	// Declare the interface we can provide data for...
			m_Device.RegisterShaderInterfaceProvider( typeof(IShadowMap), this );
		}

		protected override void	Render( List<Scene.Mesh.Primitive> _Primitives )
		{
			if ( m_Camera == null || m_Light == null )
				return;

			//////////////////////////////////////////////////////////////////////////
			// Compute the global near/far clip distances from light's point of view
			Matrix	Camera2World = m_Camera.Camera2World;
			Matrix	World2Camera = m_Camera.World2Camera;

		    Vector4 CameraPosition4 = m_Camera.Camera2World.Row4;
			Vector3	CameraPosition = new Vector3( CameraPosition4.X, CameraPosition4.Y, CameraPosition4.Z );
			Matrix	Light2World = Camera.CreateLookAt( CameraPosition + m_Light.Direction, CameraPosition, Vector3.UnitY );

			Matrix	World2Light = Light2World;
					World2Light.Invert();

			Matrix	Camera2Light = Camera2World * World2Light;

			m_LightClipFar = -float.MaxValue;
			m_LightClipNear = +float.MaxValue;
			Vector3[]	FrustumVertices = m_Camera.Frustum.Vertices;
			for ( int FrustumVertexIndex=0; FrustumVertexIndex < FrustumVertices.Length; FrustumVertexIndex++ )
			{
				// Transform into LIGHT space
				Vector3	Vertex;
				Vector3.TransformCoordinate( ref FrustumVertices[FrustumVertexIndex], ref Camera2Light, out Vertex );

				m_LightClipFar = Math.Max( m_LightClipFar, Vertex.Z );
				m_LightClipNear = Math.Min( m_LightClipNear, Vertex.Z );
			}

			//////////////////////////////////////////////////////////////////////////
			// Build projection matrices for every slice
			Vector3[]	Mins = new Vector3[m_Params.SlicesCount];
			Vector3[]	Maxs = new Vector3[m_Params.SlicesCount];
			Matrix[]	LightProjectionMatrices = new Matrix[m_Params.SlicesCount];

			for ( int SliceIndex=0; SliceIndex < m_Params.SlicesCount; SliceIndex++ )
			{
				Frustum	F = m_CameraFrustums[SliceIndex];

				// 1] Transform sliced camera frustum's corners into light space and compute their bounding box
				Mins[SliceIndex] = new Vector3( +float.MaxValue, +float.MaxValue, +float.MaxValue );
				Maxs[SliceIndex] = new Vector3( -float.MaxValue, -float.MaxValue, -float.MaxValue );
				FrustumVertices = F.Vertices;
				for ( int FrustumVertexIndex=0; FrustumVertexIndex < FrustumVertices.Length; FrustumVertexIndex++ )
				{
					// Transform into LIGHT space
					Vector3	Vertex;
					Vector3.TransformCoordinate( ref FrustumVertices[FrustumVertexIndex], ref Camera2Light, out Vertex );

					// Update bounding box
					Mins[SliceIndex].X = Math.Min( Mins[SliceIndex].X, Vertex.X );
					Mins[SliceIndex].Y = Math.Min( Mins[SliceIndex].Y, Vertex.Y );
					Mins[SliceIndex].Z = Math.Min( Mins[SliceIndex].Z, Vertex.Z );
					Maxs[SliceIndex].X = Math.Max( Maxs[SliceIndex].X, Vertex.X );
					Maxs[SliceIndex].Y = Math.Max( Maxs[SliceIndex].Y, Vertex.Y );
					Maxs[SliceIndex].Z = Math.Max( Maxs[SliceIndex].Z, Vertex.Z );
				}

				// 2] Compute light projection matrix
				float	fScaleX = 2.0f / (Maxs[SliceIndex].X - Mins[SliceIndex].X);
				float	fScaleY = 2.0f / (Maxs[SliceIndex].Y - Mins[SliceIndex].Y);
//				float	fScaleZ = 1.0f / (Maxs[SliceIndex].Z - Mins[SliceIndex].Z);
				float	fScaleZ = 1.0f;
				float	OffsetX = -0.5f * (Mins[SliceIndex].X + Maxs[SliceIndex].X) * fScaleX;
				float	OffsetY = -0.5f * (Mins[SliceIndex].Y + Maxs[SliceIndex].Y) * fScaleY;
//				float	OffsetZ = -Mins[SliceIndex].Z * fScaleZ;
				float	OffsetZ = 0.0f;
				LightProjectionMatrices[SliceIndex].M11 = fScaleX;
				LightProjectionMatrices[SliceIndex].M12 = 0.0f;
				LightProjectionMatrices[SliceIndex].M13 = 0.0f;
				LightProjectionMatrices[SliceIndex].M21 = 0.0f;
				LightProjectionMatrices[SliceIndex].M22 = fScaleY;
				LightProjectionMatrices[SliceIndex].M23 = 0.0f;
				LightProjectionMatrices[SliceIndex].M31 = 0.0f;
				LightProjectionMatrices[SliceIndex].M32 = 0.0f;
				LightProjectionMatrices[SliceIndex].M33 = fScaleZ;
				LightProjectionMatrices[SliceIndex].M41 = OffsetX;
				LightProjectionMatrices[SliceIndex].M42 = OffsetY;
				LightProjectionMatrices[SliceIndex].M43 = OffsetZ;
				LightProjectionMatrices[SliceIndex].M44 = 1.0f;

				// World2LightProj
				LightProjectionMatrices[SliceIndex] = World2Light * LightProjectionMatrices[SliceIndex];

// DEBUG
// Vector3	Min = new Vector3( +float.MaxValue, +float.MaxValue, +float.MaxValue );
// Vector3	Max = new Vector3( -float.MaxValue, -float.MaxValue, -float.MaxValue );
// for ( int FrustumVertexIndex=0; FrustumVertexIndex < FrustumVertices.Length; FrustumVertexIndex++ )
// {
// 	Vector3	WorldPos = Vector3.TransformCoordinate( FrustumVertices[FrustumVertexIndex], Camera2World );
// 	Vector3	LightPos = Vector3.TransformCoordinate( WorldPos, LightProjectionMatrices[SliceIndex] );
// 
// 	Min.X = Math.Min( Min.X, LightPos.X );
// 	Max.X = Math.Max( Max.X, LightPos.X );
// 	Min.Y = Math.Min( Min.Y, LightPos.Y );
// 	Max.Y = Math.Max( Max.Y, LightPos.Y );
// 	Min.Z = Math.Min( Min.Z, LightPos.Z );
// 	Max.Z = Math.Max( Max.Z, LightPos.Z );
// }
// DEBUG


				// Almost the same matrix but yields XY in [0,1] instead of [-1,+1]
				// These are the matrices passed to shaders to compute their position
				//	within the shadow maps
				fScaleX = 1.0f / (Maxs[SliceIndex].X - Mins[SliceIndex].X);
				fScaleY = -1.0f / (Maxs[SliceIndex].Y - Mins[SliceIndex].Y);
				fScaleZ = 1.0f / (Maxs[SliceIndex].Z - Mins[SliceIndex].Z);
				OffsetX = -Mins[SliceIndex].X * fScaleX;
				OffsetY = 1.0f - Mins[SliceIndex].Y * fScaleY;
				OffsetZ = -Mins[SliceIndex].Z * fScaleZ;
				m_LightProjectionMatrices[SliceIndex].M11 = fScaleX;
				m_LightProjectionMatrices[SliceIndex].M12 = 0.0f;
				m_LightProjectionMatrices[SliceIndex].M13 = 0.0f;
				m_LightProjectionMatrices[SliceIndex].M21 = 0.0f;
				m_LightProjectionMatrices[SliceIndex].M22 = fScaleY;
				m_LightProjectionMatrices[SliceIndex].M23 = 0.0f;
				m_LightProjectionMatrices[SliceIndex].M31 = 0.0f;
				m_LightProjectionMatrices[SliceIndex].M32 = 0.0f;
				m_LightProjectionMatrices[SliceIndex].M33 = fScaleZ;
				m_LightProjectionMatrices[SliceIndex].M41 = OffsetX;
				m_LightProjectionMatrices[SliceIndex].M42 = OffsetY;
				m_LightProjectionMatrices[SliceIndex].M43 = OffsetZ;
				m_LightProjectionMatrices[SliceIndex].M44 = 1.0f;

				// World2LightProj
				m_LightProjectionMatrices[SliceIndex] = World2Light * m_LightProjectionMatrices[SliceIndex];

// DEBUG
// Vector3	Min = new Vector3( +float.MaxValue, +float.MaxValue, +float.MaxValue );
// Vector3	Max = new Vector3( -float.MaxValue, -float.MaxValue, -float.MaxValue );
// for ( int FrustumVertexIndex=0; FrustumVertexIndex < FrustumVertices.Length; FrustumVertexIndex++ )
// {
// 	Vector3	WorldPos = Vector3.TransformCoordinate( FrustumVertices[FrustumVertexIndex], Camera2World );
// 	Vector3	LightPos = Vector3.TransformCoordinate( WorldPos, m_LightProjectionMatrices[SliceIndex] );
// 
// 	Min.X = Math.Min( Min.X, LightPos.X );
// 	Max.X = Math.Max( Max.X, LightPos.X );
// 	Min.Y = Math.Min( Min.Y, LightPos.Y );
// 	Max.Y = Math.Max( Max.Y, LightPos.Y );
// 	Min.Z = Math.Min( Min.Z, LightPos.Z );
// 	Max.Z = Math.Max( Max.Z, LightPos.Z );
// }
// DEBUG

				// LightProj2World
				m_InverseLightProjectionMatrices[SliceIndex] = m_LightProjectionMatrices[SliceIndex];
				m_InverseLightProjectionMatrices[SliceIndex].Invert();

				// 3] Write the light ranges in the ZW coordinates of the slice ranges (XY being the camera ranges)
				m_SliceRanges[SliceIndex].Z = Mins[SliceIndex].Z;
				m_SliceRanges[SliceIndex].W = Maxs[SliceIndex].Z;
			}

			//////////////////////////////////////////////////////////////////////////
			// Build cull list from light

			// TODO
			// At the moment, I render every primitives
			// If that's becoming too slow at some point then I'll build that cull list...


			//////////////////////////////////////////////////////////////////////////
			// Perform shadow rendering
			using ( m_Material.UseLock() )
			{
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				VariableMatrix	vLocal2World = m_Material.GetVariableBySemantic( "LOCAL2WORLD" ).AsMatrix;
				VariableMatrix	vWorld2LightProj = m_Material.GetVariableBySemantic( "WORLD2LIGHTPROJ" ).AsMatrix;
				VariableVector	vLightRanges = m_Material.GetVariableBySemantic( "LIGHT_RANGES" ).AsVector;
				VariableVector	vLightDirection = m_Material.GetVariableBySemantic( "LIGHT_DIRECTION" ).AsVector;
								vLightDirection.Set( m_Light.Direction );
				VariableVector	vShadowBias = m_Material.GetVariableBySemantic( "SHADOW_BIAS" ).AsVector;
								vShadowBias.Set( m_ShadowBias );

				EffectPass	ShadowPass = m_Material.CurrentTechnique.GetPassByIndex( 0 );

				m_Device.ClearRenderTarget( m_ShadowMaps, new Color4( Half.MaxValue, Half.MaxValue, Half.MaxValue, Half.MaxValue ) );

				for ( int SliceIndex=0; SliceIndex < m_Params.SlicesCount; SliceIndex++ )
				{
					int	FrameToken = m_FrameToken-10-SliceIndex;	// So that we never prevent further rendering

					// Setup render target & viewport
					m_Device.SetRenderTarget( m_ShadowMaps.GetSingleRenderTargetView( 0, SliceIndex ), m_DepthStencil.DepthStencilView );
					m_Device.Rasterizer.SetViewports( new Viewport( 0, 0, m_ShadowMaps.Width, m_ShadowMaps.Height, 0.0f, 1.0f ) );
					m_Device.ClearDepthStencil( m_DepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

					// Setup projection matrix
					vWorld2LightProj.SetMatrix( LightProjectionMatrices[SliceIndex] );
					vLightRanges.Set( m_SliceRanges[SliceIndex] );

					// Render primitives
					foreach ( Scene.Mesh.Primitive P in m_ShadowPrimitives )
						if ( P.Visible )
						{
							Matrix	Transform = P.Parent.Local2World;
							vLocal2World.SetMatrix( Transform );

							ShadowPass.Apply();

							P.Render( FrameToken );
						}
				}
			}

			// Restore default render target
			m_Device.SetDefaultRenderTarget();
		}

		/// <summary>
		/// Adds an external primitive as a shadow caster
		/// </summary>
		/// <param name="_Primitive"></param>
		public void	AddShadowCaster( Scene.Mesh.Primitive _Primitive )
		{
			if ( _Primitive == null )
				return;

			if ( !m_ShadowMeshes2PrimitivesCount.ContainsKey( _Primitive.Parent ) )
				m_ShadowMeshes2PrimitivesCount.Add( _Primitive.Parent, 0 );
			m_ShadowMeshes2PrimitivesCount[_Primitive.Parent]++;			// One more primitive to render for that mesh
			m_ShadowPrimitives.Add( _Primitive );
		}

		/// <summary>
		/// Removes an external primitive from the shadow castors
		/// </summary>
		/// <param name="_Primitive"></param>
		public void	RemoveShadowCaster( Scene.Mesh.Primitive _Primitive )
		{
			if ( !m_ShadowPrimitives.Contains( _Primitive ) )
				return;

			if ( --m_ShadowMeshes2PrimitivesCount[_Primitive.Parent] == 0 )
				m_ShadowMeshes2PrimitivesCount.Remove( _Primitive.Parent );	// No more primitive to render for that mesh so remove the mesh...
			m_ShadowPrimitives.Remove( _Primitive );
		}

		/// <summary>
		/// Call this if you made changes to the camera projection data
		/// </summary>
		public void	UpdateCameraData()
		{
			if ( m_Camera == null )
				return;

			int		SlicesCount = m_Params.SlicesCount;

			// Rebuild frustums and ranges
			float	fCameraNear = m_bUseCameraNearFarOverride ? m_CameraNear : m_Camera.Near;
			float	fCameraFar = m_bUseCameraNearFarOverride ? m_CameraFar : m_Camera.Far;

			float	fSliceFar = fCameraNear;
			for ( int SliceIndex=0; SliceIndex < SlicesCount; SliceIndex++ )
			{
				float	fSliceNear = fSliceFar;

				// Compute new far clip distance for that slice
				float	fExponentialFar = fCameraNear * (float) Math.Pow( fCameraFar / fCameraNear, (float) (SliceIndex+1) / SlicesCount );
				float	fLinearFar = fCameraNear + (fCameraFar - fCameraNear) * (SliceIndex+1) / SlicesCount;
				fSliceFar = m_Lambda * fExponentialFar + (1.0f - m_Lambda) * fLinearFar;

				m_SliceRanges[SliceIndex].X = fSliceNear;
				m_SliceRanges[SliceIndex].Y = fSliceFar;

				// Build the appropriate frustum (in camera local space)
				if ( m_Camera.IsPerspective )
					m_CameraFrustums[SliceIndex] = Frustum.FromPerspective( m_Camera.PerspectiveFOV, m_Camera.AspectRatio, m_SliceRanges[SliceIndex].X, m_SliceRanges[SliceIndex].Y );
				else
					m_CameraFrustums[SliceIndex] = Frustum.FromOrtho( m_Camera.OrthographicHeight, m_Camera.AspectRatio, m_SliceRanges[SliceIndex].X, m_SliceRanges[SliceIndex].Y );
			}
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			IShadowMap	Interface = _Interface as IShadowMap;

			Interface.ShadowSlicesCount = m_Params.SlicesCount;
			Interface.ShadowSliceRanges = m_SliceRanges;
			Interface.World2ShadowMaps = m_LightProjectionMatrices;
			Interface.ShadowMaps2World = m_InverseLightProjectionMatrices;
			Interface.ShadowMaps = m_ShadowMaps.TextureView;
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		#endregion
	}
}
