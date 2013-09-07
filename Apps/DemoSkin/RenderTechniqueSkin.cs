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
	/// This is the render technique that is able to render realistic skin using sub-surface scattering
	/// </example>
	public class RenderTechniqueSkin : RenderTechniqueDefault
	{
		#region NESTED TYPES

		protected class		PrimitiveData : IDisposable
		{
			#region FIELDS

			protected Scene.Mesh.Primitive		m_Primitive = null;
			protected RenderTarget<PF_RGBA16F>	m_IrradianceMap = null;			// The irradiance map in 1024x1024
			protected RenderTarget<PF_RGBA16F>	m_IrradianceDownscale0 = null;	// The downscaled map in 512x512
			protected RenderTarget<PF_RGBA16F>	m_IrradianceDownscale1 = null;	// Another downscaled map in 256x256
			protected RenderTarget<PF_RGBA16F>	m_IrradianceDownscale2 = null;	// Another downscaled map in 128x128
			protected RenderTarget<PF_RGBA16F>	m_IrradianceDownscale3 = null;	// Another downscaled map in 64x64

			#endregion

			#region PROPERTIES

			public Scene.Mesh.Primitive		Primitive				{ get { return m_Primitive; } }
			public RenderTarget<PF_RGBA16F>	IrradianceMap			{ get { return m_IrradianceMap; } }
			public RenderTarget<PF_RGBA16F>	IrradianceDownscale0	{ get { return m_IrradianceDownscale0; } }
			public RenderTarget<PF_RGBA16F>	IrradianceDownscale1	{ get { return m_IrradianceDownscale1; } }
			public RenderTarget<PF_RGBA16F>	IrradianceDownscale2	{ get { return m_IrradianceDownscale2; } }
			public RenderTarget<PF_RGBA16F>	IrradianceDownscale3	{ get { return m_IrradianceDownscale3; } }

			#endregion

			#region METHODS

			public PrimitiveData( Device _Device, Scene.Mesh.Primitive _Primitive, int _InitializeSize )
			{
				m_Primitive = _Primitive;

				m_IrradianceMap = new RenderTarget<PF_RGBA16F>( _Device, "IrradianceMap", _InitializeSize, _InitializeSize, 1 );
				_InitializeSize >>= 1;
				m_IrradianceDownscale0 = new RenderTarget<PF_RGBA16F>( _Device, "IrradianceDownscale0", _InitializeSize, _InitializeSize, 1 );
				_InitializeSize >>= 1;
				m_IrradianceDownscale1 = new RenderTarget<PF_RGBA16F>( _Device, "IrradianceDownscale1", _InitializeSize, _InitializeSize, 1 );
				_InitializeSize >>= 1;
				m_IrradianceDownscale2 = new RenderTarget<PF_RGBA16F>( _Device, "IrradianceDownscale2", _InitializeSize, _InitializeSize, 1 );
				_InitializeSize >>= 1;
				m_IrradianceDownscale3 = new RenderTarget<PF_RGBA16F>( _Device, "IrradianceDownscale3", _InitializeSize, _InitializeSize, 1 );
			}

			#region IDisposable Members

			public void Dispose()
			{
				m_IrradianceMap.Dispose();
				m_IrradianceDownscale0.Dispose();
				m_IrradianceDownscale1.Dispose();
				m_IrradianceDownscale2.Dispose();
				m_IrradianceDownscale3.Dispose();
			}

			#endregion

			#endregion
		}

		public enum		DEBUG_INFOS
		{
			NONE,
			IRRADIANCE,
			IRRADIANCE_DOWNSCALE0,
			IRRADIANCE_DOWNSCALE1,
			IRRADIANCE_DOWNSCALE2,
			IRRADIANCE_DOWNSCALE3,
		}

		#endregion

		#region FIELDS

		// Parameters
		protected float						m_NormalAmplitude = 1.0f;
		protected float						m_DiffusionDistance = 32.0f;
		protected DEBUG_INFOS				m_DebugInfos = DEBUG_INFOS.NONE;

		// Primitive data
		protected int						m_IrradianceMapSize = 1024;
		protected List<PrimitiveData>		m_PrimitiveData = new List<PrimitiveData>();
		protected Dictionary<Scene.Mesh.Primitive,PrimitiveData>	m_Primitive2Data = new Dictionary<Scene.Mesh.Primitive,PrimitiveData>();

		protected Helpers.ScreenQuad		m_Quad = null;

		// Texture scaler
		protected Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>	m_Scaler = null;

		#endregion

		#region PROPERTIES

		public override IVertexSignature	RecognizedSignature	{ get { return m_Signature; } }
		public override IMaterial			MainMaterial		{ get { return m_Material; } }

		public float						NormalAmpltiude		{ get { return m_NormalAmplitude; } set { m_NormalAmplitude = value; } }
		public float						DiffusionDistance	{ get { return m_DiffusionDistance; } set { m_DiffusionDistance = value; } }
		public DEBUG_INFOS					DebugInfos			{ get { return m_DebugInfos; } set { m_DebugInfos = value; } }

		#endregion

		#region METHODS

		public	RenderTechniqueSkin( Device _Device, string _Name, int _IrradianceMapSize ) : base( _Device, _Name )
		{
			// Build the signatures we can support
			m_Signature.AddField( "Position", VERTEX_FIELD_USAGE.POSITION, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Normal", VERTEX_FIELD_USAGE.NORMAL, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "Tangent", VERTEX_FIELD_USAGE.TANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "BiTangent", VERTEX_FIELD_USAGE.BITANGENT, VERTEX_FIELD_TYPE.FLOAT3, 0 );
			m_Signature.AddField( "TexCoord0", VERTEX_FIELD_USAGE.TEX_COORD2D, VERTEX_FIELD_TYPE.FLOAT2, 0 );

			// Create our main material
			m_Material = ToDispose( new Material<VS_P3N3G3B3T2>( m_Device, "SkinMaterial", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/SkinRendering/Skin.fx" ) ) );

			// Subscribe to primitive events
			this.PrimitiveAdded += new PrimitiveCollectionChangedEventHandler( RenderTechniqueSkin_PrimitiveAdded );
			this.PrimitiveRemoved += new PrimitiveCollectionChangedEventHandler( RenderTechniqueSkin_PrimitiveRemoved );

			// Create the texture scaler
			m_Scaler = ToDispose( new Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>( m_Device, "Downscaler",
				Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>.QUALITY.DEFAULT,
				Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>.METHOD.DEFAULT ) );

			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "ScreenQuad" ) );

			m_IrradianceMapSize = _IrradianceMapSize;
		}

		public override void Dispose()
		{
			base.Dispose();

			// Dispose of primitive data
			while ( m_PrimitiveData.Count > 0 )
				RenderTechniqueSkin_PrimitiveRemoved( this, m_PrimitiveData[0].Primitive );
		}

		protected override void	Render( List<Scene.Mesh.Primitive> _Primitives )
		{
			VariableMatrix		vLocal2World = m_Material.GetVariableBySemantic( "LOCAL2WORLD" ).AsMatrix;
			VariableResource	vIrradianceMap0 = m_Material.GetVariableBySemantic( "TEX_IRRADIANCE0" ).AsResource; 
			VariableResource	vIrradianceMap1 = m_Material.GetVariableBySemantic( "TEX_IRRADIANCE1" ).AsResource; 
			VariableResource	vIrradianceMap2 = m_Material.GetVariableBySemantic( "TEX_IRRADIANCE2" ).AsResource; 
			VariableResource	vIrradianceMap3 = m_Material.GetVariableBySemantic( "TEX_IRRADIANCE3" ).AsResource; 
			VariableResource	vIrradianceMap4 = m_Material.GetVariableBySemantic( "TEX_IRRADIANCE4" ).AsResource; 

			//////////////////////////////////////////////////////////////////////////
			// Render into the irradiance map
			m_Material.CurrentTechnique = m_Material.GetTechniqueByName( "IrradianceComputation" );
			using ( m_Material.UseLock() )
			{
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				m_Material.SetScalar( "NormalAmplitude", m_NormalAmplitude );
				m_Material.SetScalar( "DiffusionDistance", m_DiffusionDistance );

				Scene.MaterialParameters	PreviousParams = null;
				foreach ( Scene.Mesh.Primitive P in _Primitives )
					if ( !P.Culled && P.CanRender( m_FrameToken ) )
					{
						P.Parameters.ApplyDifference( PreviousParams );

						Matrix	Transform = P.Parent.Local2World;
						vLocal2World.SetMatrix( Transform );

						PrimitiveData	Data = m_Primitive2Data[P];
						m_Device.SetRenderTarget( Data.IrradianceMap );
						m_Device.SetViewport( 0, 0, Data.IrradianceMap.Width, Data.IrradianceMap.Height, 0.0f, 1.0f );

						m_Material.Render( ( _Sender, _Pass, _PassIndex ) => { P.Render( m_FrameToken-10 ); } );

						PreviousParams = P.Parameters;
					}

				//////////////////////////////////////////////////////////////////////////
				// Perform gaussian blurs
				PreviousParams = null;
				foreach ( Scene.Mesh.Primitive P in _Primitives )
					if ( !P.Culled && P.CanRender( m_FrameToken ) )
					{
						PrimitiveData	Data = m_Primitive2Data[P];

						m_Scaler.SetTexture( Data.IrradianceMap );
						m_Scaler.PreviouslyRenderedRenderTarget = Data.IrradianceDownscale0;
						m_Scaler.Scale( Data.IrradianceDownscale0.Width, Data.IrradianceDownscale0.Height, null );	// Scale once...
						m_Scaler.PreviouslyRenderedRenderTarget = Data.IrradianceDownscale1;
						m_Scaler.Scale( Data.IrradianceDownscale1.Width, Data.IrradianceDownscale1.Height, null );	// Scale again...
						m_Scaler.PreviouslyRenderedRenderTarget = Data.IrradianceDownscale2;
						m_Scaler.Scale( Data.IrradianceDownscale2.Width, Data.IrradianceDownscale2.Height, null );	// Scale again...
						m_Scaler.PreviouslyRenderedRenderTarget = Data.IrradianceDownscale3;
						m_Scaler.Scale( Data.IrradianceDownscale3.Width, Data.IrradianceDownscale3.Height, null );	// Scale again...
					}
			}

			//////////////////////////////////////////////////////////////////////////
			// Perform actual rendering
			m_Device.SetDefaultRenderTarget();	// Restore default render target

			m_Material.CurrentTechnique = m_Material.GetTechniqueByName( "SkinRendering" );
			using ( m_Material.UseLock() )
			{
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				Scene.MaterialParameters	PreviousParams = null;
				foreach ( Scene.Mesh.Primitive P in _Primitives )
					if ( !P.Culled && P.CanRender( m_FrameToken ) )
					{
						P.Parameters.ApplyDifference( PreviousParams );

						Matrix	Transform = P.Parent.Local2World;
						vLocal2World.SetMatrix( Transform );

						PrimitiveData	Data = m_Primitive2Data[P];
						vIrradianceMap0.SetResource( Data.IrradianceMap.TextureView );
						vIrradianceMap1.SetResource( Data.IrradianceDownscale0.TextureView );
						vIrradianceMap2.SetResource( Data.IrradianceDownscale1.TextureView );
						vIrradianceMap3.SetResource( Data.IrradianceDownscale2.TextureView );
						vIrradianceMap4.SetResource( Data.IrradianceDownscale3.TextureView );

						m_Material.Render( ( _Sender, _Pass, _PassIndex ) => { P.Render( m_FrameToken ); } );

						PreviousParams = P.Parameters;
					}
			}

			//////////////////////////////////////////////////////////////////////////
			// DEBUG
			if ( m_DebugInfos == DEBUG_INFOS.NONE )
				return;

			int	DebugWidth = m_Device.DefaultRenderTarget.Width / 4;
			int	DebugHeight = m_Device.DefaultRenderTarget.Height / 4;

			m_Device.SetViewport( 0, 0, DebugWidth, DebugHeight, 0.0f, 1.0f );
			using ( m_Material.UseLock() )
			{
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				m_Material.SetScalar( "DebugType", (int) m_DebugInfos );
				m_Material.SetVector( "BufferInvSize", new Vector4( 1.0f / DebugWidth, 1.0f / DebugHeight, 0.0f, 0.0f ) );

				m_Material.CurrentTechnique = m_Material.GetTechniqueByName( "Debug" );
				m_Material.ApplyPass( 0 );

				m_Quad.Render();
			}
		}

		#endregion

		#region EVENT HANDLERS

		void RenderTechniqueSkin_PrimitiveAdded( ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive )
		{
			PrimitiveData	NewData = new PrimitiveData( m_Device, _Primitive, m_IrradianceMapSize );
			m_PrimitiveData.Add( NewData );
			m_Primitive2Data.Add( _Primitive, NewData );
		}

		void RenderTechniqueSkin_PrimitiveRemoved( ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive )
		{
			PrimitiveData	Data = m_Primitive2Data[_Primitive];
			Data.Dispose();

			m_PrimitiveData.Remove( Data );
			m_Primitive2Data.Remove( _Primitive );
		}

		#endregion
	}
}
