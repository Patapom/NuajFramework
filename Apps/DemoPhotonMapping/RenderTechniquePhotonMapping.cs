//#define DEBUG_PHOTON_MAP

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
	/// Photon-mapping rendering
	/// </example>
	public class RenderTechniquePhotonMapping : RenderTechnique, IGeometryWriter<VS_P3N3G3,int>
	{
		#region CONSTANTS

		protected const int		PHOTON_TEXTURE_SIZE = 64;				// Size of the photon 3D texture
		protected const int		DISTANCE_TEXTURE_SIZE = 64;				// Size of the distance 3D texture
		protected const int		DISTANCE_PROPAGATION_PASSES_COUNT = 32;	// A maximum of DISTANCE_TEXTURE_SIZE / 2

		protected const int		PHOTONS_COUNT = 200000;					// The amount of photons to trace in the scene

		#endregion

		#region NESTED TYPES

		#endregion

		#region FIELDS

		// Materials
		protected Material<VS_P3N3>				m_MaterialPhotonTracer = null;
		protected Material<VS_Pt4>				m_MaterialGenerateMipMaps = null;
		protected Material<VS_Pt4>				m_MaterialGenerateDistanceField = null;
		protected Material<VS_P3>				m_MaterialDebugPhotonMap = null;
		protected Material<VS_P3N3G3>			m_MaterialRender = null;

		// Textures & Targets
		protected RenderTarget3D<PF_RGBA16F>	m_PhotonsDirection = null;		// The target that will store [IncomingDirection(RGB)+Count(Alpha)]
		protected RenderTarget3D<PF_RGBA16F>	m_PhotonsFlux = null;			// The target that will store [PhotonFlux(RGB)+?(Alpha)]
		protected RenderTarget3D<PF_RGBA16F>[]	m_PhotonsDistanceFields = new RenderTarget3D<PF_RGBA16F>[2];	// The target that will store [DirectionToNearestPhoton(RGB)+DistanceToNearestPhoton(Alpha)]

		// Primitives
		protected VertexBuffer<VS_P3N3>			m_Photons = null;
		protected VertexBuffer<VS_P3>			m_DebugPhotonMap = null;
		protected VertexBuffer<VS_Pt4>			m_GenerateMipMapQuad = null;
		protected Primitive<VS_P3N3G3,int>		m_CubeRoom = null;
		protected Primitive<VS_P3N3G3,int>		m_CubeInside = null;

		#endregion

		#region PROPERTIES

		#endregion

		#region METHODS

		public RenderTechniquePhotonMapping( Device _Device, string _Name ) : base( _Device, _Name )
		{
			// Create our main materials
			m_MaterialPhotonTracer = ToDispose( new Material<VS_P3N3>( m_Device, "Photon Tracer Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/PhotonMapping/PhotonTracer.fx" ) ) );
			m_MaterialGenerateMipMaps = ToDispose( new Material<VS_Pt4>( m_Device, "Generate Mip-Maps Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/PhotonMapping/GenerateMipMaps.fx" ) ) );
			m_MaterialGenerateDistanceField = ToDispose( new Material<VS_Pt4>( m_Device, "Generate Distance Field Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/PhotonMapping/GenerateDistanceField.fx" ) ) );
			m_MaterialDebugPhotonMap = ToDispose( new Material<VS_P3>( m_Device, "Photon Map Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/PhotonMapping/DebugPhotonMap.fx" ) ) );
			m_MaterialRender = ToDispose( new Material<VS_P3N3G3>( m_Device, "Render with Photon Map Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/PhotonMapping/RenderWithPhotonMap.fx" ) ) );

			// Create the render targets
			m_PhotonsDirection = ToDispose( new RenderTarget3D<PF_RGBA16F>( m_Device, "PhotonsDirection+Count", PHOTON_TEXTURE_SIZE, PHOTON_TEXTURE_SIZE, PHOTON_TEXTURE_SIZE, 0 ) );
			m_PhotonsFlux = ToDispose( new RenderTarget3D<PF_RGBA16F>( m_Device, "PhotonsFlux+?", PHOTON_TEXTURE_SIZE, PHOTON_TEXTURE_SIZE, PHOTON_TEXTURE_SIZE, 0 ) );
			m_PhotonsDistanceFields[0] = ToDispose( new RenderTarget3D<PF_RGBA16F>( m_Device, "PhotonsDistanceField", DISTANCE_TEXTURE_SIZE, DISTANCE_TEXTURE_SIZE, DISTANCE_TEXTURE_SIZE, 0 ) );
			m_PhotonsDistanceFields[1] = ToDispose( new RenderTarget3D<PF_RGBA16F>( m_Device, "PhotonsDistanceField", DISTANCE_TEXTURE_SIZE, DISTANCE_TEXTURE_SIZE, DISTANCE_TEXTURE_SIZE, 0 ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the photons
			// NOTE: The random directions are not balanced at all !
			Random		RNG = new Random( 1 );
			VS_P3N3[]	Photons = new VS_P3N3[PHOTONS_COUNT];
			for ( int PhotonIndex=0; PhotonIndex < PHOTONS_COUNT; PhotonIndex++ )
			{
				float	Theta = (float) Math.Asin( Math.Sqrt( RNG.NextDouble() ) );
				float	Phi = 2.0f * (float) Math.PI * (float) RNG.NextDouble();

				Photons[PhotonIndex] = new VS_P3N3()
				{
					Position = new Vector3( 0.0f, 1.98f, 0.0f ),	// Start from the ceiling
					Normal = new Vector3( (float) (Math.Cos( Phi ) * Math.Sin( Theta )), -(float) Math.Cos( Theta ), (float) (Math.Sin( Phi ) * Math.Sin( Theta )) )
				};
			}
			m_Photons = ToDispose( new VertexBuffer<VS_P3N3>( m_Device, "Photons", Photons ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the debug primitive
			VS_P3[]	QuadVertices = new VS_P3[4]
			{
				new VS_P3() { Position = new Vector3( -1.0f, +1.0f, 0.0f ) },
				new VS_P3() { Position = new Vector3( -1.0f, -1.0f, 0.0f ) },
				new VS_P3() { Position = new Vector3( +1.0f, +1.0f, 0.0f ) },
				new VS_P3() { Position = new Vector3( +1.0f, -1.0f, 0.0f ) },
			};
			m_DebugPhotonMap = ToDispose( new VertexBuffer<VS_P3>( m_Device, "DebugPhotonMapVB", QuadVertices ) );

			//////////////////////////////////////////////////////////////////////////
			// Creates the quad primitive to generate mip-maps
			VS_Pt4[]	MipMapQuadVertices = new VS_Pt4[4]
			{
				new VS_Pt4() { Position = new Vector4( -1.0f, +1.0f, 0.0f, 1.0f ) },
				new VS_Pt4() { Position = new Vector4( -1.0f, -1.0f, 0.0f, 1.0f ) },
				new VS_Pt4() { Position = new Vector4( +1.0f, +1.0f, 0.0f, 1.0f ) },
				new VS_Pt4() { Position = new Vector4( +1.0f, -1.0f, 0.0f, 1.0f ) },
			};
			m_GenerateMipMapQuad = ToDispose( new VertexBuffer<VS_Pt4>( m_Device, "Generate MipMaps Quad", MipMapQuadVertices ) );

			//////////////////////////////////////////////////////////////////////////
			// Creates the actual test cubes

			// Room cube first, with normals pointing inward
			m_CubeRoom = ToDispose( Helpers.Cube<VS_P3N3G3,int>.Build( m_Device, "Test Cube", Vector2.One, this, null, true ) );
			// Furniture boxes, normals pointing outward
			m_CubeInside = ToDispose( Helpers.Cube<VS_P3N3G3,int>.Build( m_Device, "Test Cube", Vector2.One, this, null, false ) );
		}

		bool bMerde = true;
		public override void	Render( int _FrameToken )
		{
			Vector3	SceneBBoxMin = -2.01f * Vector3.One;
			Vector3	SceneBBoxMax = +2.01f * Vector3.One;

			if ( bMerde )
			{
				//////////////////////////////////////////////////////////////////////////
				// Start by tracing photons into the scene
				m_Device.AddProfileTask( this, "Photon Mapping", "Trace Photons" );

				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE );

				using ( m_MaterialPhotonTracer.UseLock() )
				{
					m_Device.InputAssembler.SetPrimitiveTopology( PrimitiveTopology.PointList );
					m_Device.SetMultipleRenderTargets( new RenderTargetView[] { m_PhotonsDirection.RenderTargetView, m_PhotonsFlux.RenderTargetView }, null );
					m_Device.SetViewport( 0, 0, PHOTON_TEXTURE_SIZE, PHOTON_TEXTURE_SIZE, 0.0f, 1.0f );

					m_Device.ClearRenderTarget( m_PhotonsDirection, Vector4.Zero );
					m_Device.ClearRenderTarget( m_PhotonsFlux, Vector4.Zero );

					CurrentMaterial.GetVariableByName( "SceneBBoxMin" ).AsVector.Set( SceneBBoxMin );
					CurrentMaterial.GetVariableByName( "SceneBBoxMax" ).AsVector.Set( SceneBBoxMax );

					CurrentMaterial.ApplyPass( 0 );
					m_Photons.Use();
					m_Photons.Draw();
				}

				//////////////////////////////////////////////////////////////////////////
				// Generate mip maps
				m_Device.AddProfileTask( this, "Photon Mapping", "Generate Photon MipMaps" );

				GenerateMipMaps( m_PhotonsDirection );
				GenerateMipMaps( m_PhotonsFlux );

				//////////////////////////////////////////////////////////////////////////
				// Generate directed distance map
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				using ( m_MaterialGenerateDistanceField.UseLock() )
				{
					m_Device.InputAssembler.SetPrimitiveTopology( PrimitiveTopology.TriangleStrip );
					m_GenerateMipMapQuad.Use();

					// Initialize the distance field
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "InitializeDistanceField" );
					m_Device.SetRenderTarget( m_PhotonsDistanceFields[0] );
					m_Device.SetViewport( 0, 0, DISTANCE_TEXTURE_SIZE, DISTANCE_TEXTURE_SIZE, 0.0f, 1.0f );
					CurrentMaterial.GetVariableByName( "PhotonMap" ).AsResource.SetResource( m_PhotonsDirection );
					CurrentMaterial.GetVariableByName( "dUVWTarget" ).AsVector.Set( m_PhotonsDistanceFields[0].InvSize4 );
					CurrentMaterial.GetVariableByName( "dUVWSource" ).AsVector.Set( m_PhotonsDirection.InvSize4 );
					CurrentMaterial.ApplyPass( 0 );
					m_GenerateMipMapQuad.DrawInstanced( DISTANCE_TEXTURE_SIZE );

					// Propagate distance
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "PropagateDistanceField" );
					EffectPass			Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
					VariableResource	vPreviousDistanceField = CurrentMaterial.GetVariableByName( "PreviousDistanceField" ).AsResource;

					for ( int DistancePassIndex=0; DistancePassIndex < DISTANCE_PROPAGATION_PASSES_COUNT; DistancePassIndex++ )
					{
						m_Device.SetRenderTarget( m_PhotonsDistanceFields[1] );
						vPreviousDistanceField.SetResource( m_PhotonsDistanceFields[0] );

						Pass.Apply();
						m_GenerateMipMapQuad.DrawInstanced( DISTANCE_TEXTURE_SIZE );

						// Swap fields
						RenderTarget3D<PF_RGBA16F>	Temp = m_PhotonsDistanceFields[0];
						m_PhotonsDistanceFields[0] = m_PhotonsDistanceFields[1];
						m_PhotonsDistanceFields[1] = Temp;
					}
				}
				GenerateMipMaps( m_PhotonsDistanceFields[0] );
				bMerde = false;
			}

#if DEBUG_PHOTON_MAP
			//////////////////////////////////////////////////////////////////////////
			// Debug photon map
			m_Device.AddProfileTask( this, "Photon Mapping", "Debug Photon Map" );

			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE );
//			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialDebugPhotonMap.UseLock() )
			{
				m_Device.InputAssembler.SetPrimitiveTopology( PrimitiveTopology.TriangleStrip );
				m_Device.SetDefaultRenderTarget();

				CurrentMaterial.GetVariableByName( "TexPhotonDirections" ).AsResource.SetResource( m_PhotonsDirection );
				CurrentMaterial.GetVariableByName( "TexPhotonFlux" ).AsResource.SetResource( m_PhotonsFlux );
				CurrentMaterial.GetVariableByName( "TexDistanceField" ).AsResource.SetResource( m_PhotonsDistanceFields[0] );
				CurrentMaterial.GetVariableByName( "QuadSize" ).AsScalar.Set( 2.0f );
				CurrentMaterial.GetVariableByName( "SlicesCount" ).AsScalar.Set( PHOTON_TEXTURE_SIZE );

				CurrentMaterial.ApplyPass( 0 );
				m_DebugPhotonMap.Use();
				m_DebugPhotonMap.DrawInstanced( PHOTON_TEXTURE_SIZE );
			}

#else
			//////////////////////////////////////////////////////////////////////////
			// Display the physical cube mesh that will utilize the photon map + distance field
			m_Device.AddProfileTask( this, "Photon Mapping", "Render with Photon Map" );

			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialRender.UseLock() )
			{
				m_Device.SetDefaultRenderTarget();

				CurrentMaterial.GetVariableByName( "SceneBBoxMin" ).AsVector.Set( SceneBBoxMin );
				CurrentMaterial.GetVariableByName( "SceneBBoxMax" ).AsVector.Set( SceneBBoxMax );
				CurrentMaterial.GetVariableByName( "dUVW" ).AsVector.Set( m_PhotonsDirection.InvSize4 );
				CurrentMaterial.GetVariableByName( "TexPhotonDirections" ).AsResource.SetResource( m_PhotonsDirection );
				CurrentMaterial.GetVariableByName( "TexPhotonFlux" ).AsResource.SetResource( m_PhotonsFlux );
				CurrentMaterial.GetVariableByName( "TexDistanceField" ).AsResource.SetResource( m_PhotonsDistanceFields[0] );

				VariableVector	vBoxCenter = CurrentMaterial.GetVariableByName( "BoxCenter" ).AsVector;
				VariableVector	vBoxHalfSize = CurrentMaterial.GetVariableByName( "BoxHalfSize" ).AsVector;
				VariableVector	vBoxRotationAxis = CurrentMaterial.GetVariableByName( "BoxRotationAxis" ).AsVector;

				// Room
				vBoxCenter.Set( Vector3.Zero );
				vBoxHalfSize.Set( new Vector3( 2.0f, 2.0f, 2.0f ) );
				vBoxRotationAxis.Set( Vector3.UnitX );
				CurrentMaterial.ApplyPass( 0 );
				m_CubeRoom.RenderOverride();

				// Tall box
				vBoxCenter.Set( new Vector3( -0.7f, -1.0f, -0.8f ) );
				vBoxHalfSize.Set( new Vector3( 0.5f, 1.0f, 0.5f ) );
				vBoxRotationAxis.Set( new Vector3( 0.86602540378443864676372317075294f, 0.0f, -0.5f ) );	// Small 30° rotation
				CurrentMaterial.ApplyPass( 0 );
				m_CubeInside.RenderOverride();

				// Small box
				vBoxCenter.Set( new Vector3( +0.9f, -1.5f, -0.8f ) );
				vBoxHalfSize.Set( new Vector3( 0.5f, 0.5f, 0.5f ) );
				vBoxRotationAxis.Set( Vector3.UnitX );
				CurrentMaterial.ApplyPass( 0 );
				m_CubeInside.RenderOverride();
			}
#endif
		}

		/// <summary>
		/// Generates all the mip levels of a 3D target
		/// </summary>
		/// <param name="_RenderTarget"></param>
		protected void	GenerateMipMaps( IRenderTarget3D _RenderTarget )
		{
			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_MaterialGenerateMipMaps.UseLock() )
			{
				m_Device.InputAssembler.SetPrimitiveTopology( PrimitiveTopology.TriangleStrip );
				m_GenerateMipMapQuad.Use();

				VariableResource	vHigherMipLevelTex = CurrentMaterial.GetVariableByName( "HigherMipLevel" ).AsResource;
				VariableVector		vdUVWSource = CurrentMaterial.GetVariableByName( "dUVWSource" ).AsVector;
				VariableVector		vdUVWTarget = CurrentMaterial.GetVariableByName( "dUVWTarget" ).AsVector;
				EffectPass			Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

				int	Width = _RenderTarget.Width;
				int	Height = _RenderTarget.Height;
				int	Depth = _RenderTarget.Depth;
				for ( int MipLevel=1; MipLevel < _RenderTarget.MipLevelsCount; MipLevel++ )
				{
					vdUVWSource.Set( new Vector4( 1.0f / Width, 1.0f / Height, 1.0f / Depth, 0.0f ) );

					Width >>= 1;
					Height >>= 1;
					Depth >>= 1;
					Width = Math.Max( 1, Width );
					Height = Math.Max( 1, Height );
					Depth = Math.Max( 1, Depth );

					vdUVWTarget.Set( new Vector4( 1.0f / Width, 1.0f / Height, 1.0f / Depth, 0.0f ) );

					m_Device.SetRenderTarget( _RenderTarget.GetSingleRenderTargetView( MipLevel ) );
					m_Device.SetViewport( 0, 0, Width, Height, 0.0f, 1.0f );

					vHigherMipLevelTex.SetResource( _RenderTarget.GetSingleTextureView( MipLevel-1 ) );

					Pass.Apply();
					m_GenerateMipMapQuad.DrawInstanced( Depth );
				}
			}
		}

		#region IGeometryWriter<VS_P3N3,int> Members

		public void WriteVertexData( ref VS_P3N3G3 _Vertex, Vector3 _Position, Vector3 _Normal, Vector3 _Tangent, Vector3 _BiTangent, Vector3 _UVW, Color4 _Color )
		{
			_Vertex.Position = _Position;
			_Vertex.Normal = _Normal;
			_Vertex.Tangent = _Tangent;
		}

		public void WriteIndexData( ref int _Index, int _Value )
		{
			_Index = _Value;
		}

		public int ReadIndexData( int _Index )
		{
			return _Index;
		}

		#endregion

		#endregion
	}
}
