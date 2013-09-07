using System;
using System.Collections.Generic;
using System.Linq;

using Nuaj;
using Nuaj.Cirrus;
using SharpDX;
using SharpDX.Direct3D10;

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// This shows a possible implementation of the ISHCubeMapsRenderer interface to make your renderer compatible with the SHEnvMap technique.
	/// This particular example is useable on deferred renderers that output at least the following 2 buffers :
	///		* GeometryBuffer with [Normal in Camera Space (RG)][Depth (B)][??? (A)]
	///		* MaterialBuffer with [Albedo (RGB)][??? (A)]
	///	
	/// Such a renderer exists in the "DemoDeferredRendering" demo application.
	/// </summary>
	public class SHCubeMapRendererExample : Component, ISHCubeMapsRenderer
	{
		#region CONSTANTS

		protected const float	DEPTH_BUFFER_INFINITY = 10000.0f;

		#endregion

		#region FIELDS

		protected int							m_CubeMapSize = -1;
		protected int							m_CubeMapFaceIndex = -1;
		protected Matrix						m_Camera2WorldMatrix;

		// Texture arrays to render the environment
		protected RenderTarget<PF_RGBA32F>		m_EnvironmentArrayMaterial = null;
		protected RenderTarget<PF_RGBA32F>		m_EnvironmentArrayGeometry = null;
		protected DepthStencil<PF_D32>			m_EnvironmentDepth = null;

		// Staging textures to read back results
		protected Texture2D						m_StagingEnvironmentCubeMapMaterial = null;
		protected Texture2D						m_StagingEnvironmentCubeMapGeometry = null;

		// Alternate renderer for environment cube maps
		protected Renderer						m_EnvMapRenderer = null;

		// Dynamic depth + material + geometry pipelines
		protected Pipeline						m_PipelineDepth = null;
		protected Pipeline						m_PipelineMaterial = null;

		#endregion

		#region PROPERTIES

		#region ISHCubeMapsRenderer Members

		public int CubeMapSize
		{
			get { return m_CubeMapSize; }
		}

		#endregion

		#endregion

		#region METHODS

		public SHCubeMapRendererExample( Device _Device, string _Name, int _CubeMapSize, Pipeline _DepthPass, Pipeline _MaterialPass, float _CameraNear, float _CameraFar, float _IndirectLightingBoostFactor ) : base( _Device, _Name )
		{
			//////////////////////////////////////////////////////////////////////////
			// Create the cube map renderer
			m_EnvMapRenderer = ToDispose( new Renderer( m_Device, "CubeMap Renderer" ) );

			//////////////////////////////////////////////////////////////////////////
			// We use the depth pass and main pipelines that will give us material albedos and normals+depths
			m_EnvMapRenderer.AddPipeline( _DepthPass );
			m_EnvMapRenderer.AddPipeline( _MaterialPass );
			m_PipelineDepth = _DepthPass;
			m_PipelineMaterial = _MaterialPass;
		}

		#region ISHCubeMapsRenderer Members

		public void BeginRender( int _CubeMapSize )
		{
			//////////////////////////////////////////////////////////////////////////
			// Create the cube map render targets
			m_CubeMapSize = _CubeMapSize;

			// 1] We first create texture arrays that we will be able to lock side by side for the post process

			// The material array that will contain the diffuse and specular surface albedos
			m_EnvironmentArrayMaterial = new RenderTarget<PF_RGBA32F>( m_Device, "ArrayMaterial", m_CubeMapSize, m_CubeMapSize, 1, 6, 1 );
			// The geometry array that will contain the normals, depth and surface roughness
			m_EnvironmentArrayGeometry = new RenderTarget<PF_RGBA32F>( m_Device, "ArrayGeometry", m_CubeMapSize, m_CubeMapSize, 1, 6, 1 );

			// 2] Then, the depth stencil for rendering the cube map
			m_EnvironmentDepth = new DepthStencil<PF_D32>( m_Device, "CubeMapDepth", m_CubeMapSize, m_CubeMapSize, false );


// 			//////////////////////////////////////////////////////////////////////////
// 			// Render with normal renderer so the scenes get updated at least once...
// 			// Indeed, scenes are created with a renderer (i.e. our main deferred renderer) and their
// 			//	nodes get updated when the renderer's frame token changes... If we decide to render
// 			//	an environment before the actual pipeline has rendered, scenes won't have their nodes
// 			//	reflect their world matrices correctly...
// 			Render();


			//////////////////////////////////////////////////////////////////////////
			// Create the staging resources where we will copy the cube map faces for encoding
			Texture2DDescription	Desc = new Texture2DDescription();
			Desc.BindFlags = BindFlags.None;
			Desc.CpuAccessFlags = CpuAccessFlags.Read;
			Desc.OptionFlags = ResourceOptionFlags.None;
			Desc.Usage = ResourceUsage.Staging;
			Desc.Format = SharpDX.DXGI.Format.R32G32B32A32_Float;
			Desc.ArraySize = 6;
			Desc.Width = m_CubeMapSize;
			Desc.Height = m_CubeMapSize;
			Desc.MipLevels = 1;
			Desc.SampleDescription = new SharpDX.DXGI.SampleDescription( 1, 0 );
			m_StagingEnvironmentCubeMapGeometry = new Texture2D( m_Device.DirectXDevice, Desc );
			m_StagingEnvironmentCubeMapMaterial = new Texture2D( m_Device.DirectXDevice, Desc );
		}

		public float BeginRenderCubeMap()
		{
			if ( m_EnvironmentArrayGeometry == null )
				throw new NException( this, "You must first call \"BeginEnvironmentRendering()\" prior using that method !" );

			// Clear normals to 0 and depth to "infinity"
			m_Device.ClearRenderTarget( m_EnvironmentArrayGeometry, new Vector4( 0.0f, 0.0f, DEPTH_BUFFER_INFINITY, 0.0f ) );

			// Clear materials
			m_Device.ClearRenderTarget( m_EnvironmentArrayMaterial, new Vector4( 0.0f, 0.0f, 0.0f, 0.0f ) );

			// Override targets setup to render to our buffers instead
			m_PipelineDepth.RenderingStart += new Pipeline.PipelineRenderingEventHandler( DepthPipeline_RenderingStart );
			m_PipelineMaterial.RenderingStart += new Pipeline.PipelineRenderingEventHandler( MainPipeline_RenderingStart );

			return DEPTH_BUFFER_INFINITY;
		}

		public void RenderCubeMapFace( Camera _Camera, int _CubeMapFaceIndex )
		{
			m_CubeMapFaceIndex = _CubeMapFaceIndex;
			m_Camera2WorldMatrix = _Camera.Camera2World;

			// Render the cube face
			m_EnvMapRenderer.Render();
		}

		public void EndRenderCubeMap()
		{
			m_PipelineMaterial.RenderingStart -= new Pipeline.PipelineRenderingEventHandler( MainPipeline_RenderingStart );
			m_PipelineDepth.RenderingStart -= new Pipeline.PipelineRenderingEventHandler( DepthPipeline_RenderingStart );

			// Copy cube maps for CPU read
			m_Device.DirectXDevice.CopyResource( m_EnvironmentArrayGeometry.Texture, m_StagingEnvironmentCubeMapGeometry );
			m_Device.DirectXDevice.CopyResource( m_EnvironmentArrayMaterial.Texture, m_StagingEnvironmentCubeMapMaterial );
		}

		protected DataRectangle		m_RectGeometry;
		protected DataRectangle		m_RectMaterial;
		protected DataStream		m_StreamGeometry = null;
		protected DataStream		m_StreamMaterial = null;
		public void BeginReadCubeMapFace( int _FaceIndex )
		{
			m_RectGeometry = m_StagingEnvironmentCubeMapGeometry.Map( _FaceIndex, MapMode.Read, MapFlags.None );
			m_StreamGeometry = new DataStream( m_RectGeometry.DataPointer, m_CubeMapSize*m_CubeMapSize*16, true, false );
			m_RectMaterial = m_StagingEnvironmentCubeMapMaterial.Map( _FaceIndex, MapMode.Read, MapFlags.None );
			m_StreamMaterial = new DataStream( m_RectMaterial.DataPointer, m_CubeMapSize*m_CubeMapSize*16, true, false );
		}

		public void ReadPixel( ref Vector3 _Albedo, ref Vector3 _WorldNormal, ref float _Depth )
		{
			Vector4	Albedo = m_StreamMaterial.Read<Vector4>();
			Vector4	Geometry = m_StreamGeometry.Read<Vector4>();

			// Copy simple data
			_Albedo.X = Albedo.X;
			_Albedo.Y = Albedo.Y;
			_Albedo.Z = Albedo.Z;

			_Depth = Geometry.Z;

			// Transform camera normal to world normal
			Vector3	CameraNormal = new Vector3();
			CameraNormal.Z = (float) Math.Sqrt( 1.0f - CameraNormal.X*CameraNormal.X - CameraNormal.Y*CameraNormal.Y );
			CameraNormal.X = 2.0f * CameraNormal.Z * CameraNormal.X;
			CameraNormal.Y = 2.0f * CameraNormal.Z * CameraNormal.Y;
			CameraNormal.Z = 1.0f - 2.0f * CameraNormal.Z * CameraNormal.Z;

			_WorldNormal = Vector3.TransformNormal( CameraNormal, m_Camera2WorldMatrix );
		}

		public void EndReadCubeMapFace( int _FaceIndex )
		{
			m_StreamGeometry.Dispose();
			m_StagingEnvironmentCubeMapGeometry.Unmap( _FaceIndex );
			m_StreamMaterial.Dispose();
			m_StagingEnvironmentCubeMapMaterial.Unmap( _FaceIndex );
		}

		public void EndRender()
		{
			//////////////////////////////////////////////////////////////////////////
			// Delete render targets
			if ( m_EnvironmentArrayMaterial != null )
				m_EnvironmentArrayMaterial.Dispose();
			m_EnvironmentArrayMaterial = null;

			if ( m_EnvironmentArrayGeometry != null )
				m_EnvironmentArrayGeometry.Dispose();
			m_EnvironmentArrayGeometry = null;

			if ( m_EnvironmentDepth != null )
				m_EnvironmentDepth.Dispose();
			m_EnvironmentDepth = null;

			//////////////////////////////////////////////////////////////////////////
			// Delete staging resources
			if ( m_StagingEnvironmentCubeMapGeometry != null )
				m_StagingEnvironmentCubeMapGeometry.Dispose();
			m_StagingEnvironmentCubeMapGeometry= null;

			if ( m_StagingEnvironmentCubeMapMaterial != null )
				m_StagingEnvironmentCubeMapMaterial.Dispose();
			m_StagingEnvironmentCubeMapMaterial = null;

			//////////////////////////////////////////////////////////////////////////
			// Remove pipelines
			m_EnvMapRenderer.RemovePipeline( m_PipelineDepth );
			m_EnvMapRenderer.RemovePipeline( m_PipelineMaterial );
			m_PipelineDepth = null;
			m_PipelineMaterial = null;
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		protected void DepthPipeline_RenderingStart( Pipeline _Sender )
		{
			// Setup only a depth stencil (no render target) and clear it
			m_Device.SetRenderTarget( null as IRenderTarget, m_EnvironmentDepth );
			m_Device.SetViewport( 0, 0, m_EnvironmentDepth.Width, m_EnvironmentDepth.Height, 0.0f, 1.0f );
			m_Device.ClearDepthStencil( m_EnvironmentDepth, DepthStencilClearFlags.Depth, 1.0f, 0 );
		}

		protected void MainPipeline_RenderingStart( Pipeline _Sender )
		{
			// Setup our multiple render targets (1 for material albedo, 1 for normals & depth)
			m_Device.SetMultipleRenderTargets( new RenderTargetView[]
				{
					m_EnvironmentArrayMaterial.GetSingleRenderTargetView( 0, m_CubeMapFaceIndex ),
					m_EnvironmentArrayGeometry.GetSingleRenderTargetView( 0, m_CubeMapFaceIndex )
				}, m_EnvironmentDepth.DepthStencilView );
			m_Device.SetViewport( 0, 0, m_EnvironmentArrayGeometry.Width, m_EnvironmentArrayGeometry.Height, 0.0f, 1.0f );
		}

		#endregion
	}
}
