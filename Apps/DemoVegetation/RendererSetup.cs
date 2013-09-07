using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;
using Nuaj.Cirrus.Utility;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This setups a deferred renderer with a technique to render trees
	/// </summary>
	public class RendererSetup : Component, IShaderInterfaceProvider, IMaterialLoader
	{
		#region CONSTANTS

		protected const float	DEPTH_BUFFER_INFINITY = 10000.0f;

		#endregion

		#region NESTED TYPES

		/// <summary>
		/// Deferred Shading Support
		/// </summary>
		public class	IDeferredRendering : ShaderInterfaceBase
		{
			[Semantic( "GBUFFER_TEX0" )]
			public RenderTarget<PF_RGBA16F>	GBuffer0	{ set { SetResource( "GBUFFER_TEX0", value ); } }
			[Semantic( "GBUFFER_TEX1" )]
			public RenderTarget<PF_RGBA16F>	GBuffer1	{ set { SetResource( "GBUFFER_TEX1", value ); } }
			[Semantic( "GBUFFER_TEX2" )]
			public RenderTarget<PF_RGBA16F>	GBuffer2	{ set { SetResource( "GBUFFER_TEX2", value ); } }
			[Semantic( "LIGHTBUFFER_TEX" )]
			public RenderTarget<PF_RGBA16F>	LightBuffer	{ set { SetResource( "LIGHTBUFFER_TEX", value ); } }
		}

		[Flags()]
		public enum		RENDER_TARGET_TYPES
		{
			MATERIAL = 1,
			GEOMETRY = 2,
			EMISSIVE = 4,
		}

		#endregion

		#region FIELDS

		// Main renderer & techniques
		protected Renderer							m_Renderer = null;
 		protected RenderTechniqueVegetation			m_Vegetation = null;
 		protected RenderTechniqueTerrain			m_Terrain = null;
 		protected Demo.RenderTechniqueCloudLayer	m_CloudLayer = null;
 		protected RenderTechniqueSky				m_Sky = null;

		// Post-processes
		protected RenderTechniquePostProcessFinalCompositing	m_PostProcessFinalCompositing = null;
		protected RenderTechniquePostProcessToneMappingFilmic	m_PostProcessToneMapping = null;

		// Attributes
		protected Camera							m_Camera = null;
		protected DirectionalLight					m_Sun = null;
		protected float								m_Time = 0.0f;

		// GBuffers at normal resolution
		protected RenderTarget<PF_RGBA16F>			m_GeometryBuffer = null;	// The geometry buffer that will store the normals, depth and surface roughness
		protected RenderTarget<PF_RGBA16F>			m_MaterialBuffer = null;	// The material buffer that will diffuse & specular albedos of materials
		protected RenderTarget<PF_RGBA16F>[]		m_EmissiveBuffers = new RenderTarget<PF_RGBA16F>[2];	// The emissive buffers that will store the emissive/unlit object colors and the global extinction factor

		#endregion

		#region PROPERTIES

		public Renderer						Renderer					{ get { return m_Renderer; } }
 		public RenderTechniqueSky			Sky							{ get { return m_Sky; } }
 		public RenderTechniqueVegetation	Vegetation					{ get { return m_Vegetation; } }
 		public RenderTechniqueTerrain		Terrain						{ get { return m_Terrain; } }
 		public Demo.RenderTechniqueCloudLayer	CloudLayer					{ get { return m_CloudLayer; } }

		public Camera						Camera						{ get { return m_Camera; } }
		public DirectionalLight				Sun							{ get { return m_Sun; } }
		public float						Time						{ get { return m_Time; } set { m_Time = value; } }

		public RenderTarget<PF_RGBA16F>		GeometryBuffer				{ get { return m_GeometryBuffer; } }
		public RenderTarget<PF_RGBA16F>		MaterialBuffer				{ get { return m_MaterialBuffer; } }
		public RenderTarget<PF_RGBA16F>[]	EmissiveBuffers				{ get { return m_EmissiveBuffers; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Setups a default renderer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_bUseAlphaToCoverage">True to use alpha to coverage instead of alpha blending</param>
		/// <param name="_ShadowMapSize"></param>
		public	RendererSetup( Device _Device, string _Name, bool _bUseAlphaToCoverage, float _CameraFOV, float _CameraAspectRatio, float _CameraNear, float _CameraFar ) : base( _Device, _Name )
		{
			m_Renderer = ToDispose( new Renderer( m_Device, m_Name ) );

			//////////////////////////////////////////////////////////////////////////
			// Register shader interfaces
			m_Device.DeclareShaderInterface( typeof(IDeferredRendering) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IDeferredRendering), this );	// Register the IDeferredRendering interface

			//////////////////////////////////////////////////////////////////////////
			// Create rendering buffers
			int	DefaultWidth = m_Device.DefaultRenderTarget.Width;
			int	DefaultHeight = m_Device.DefaultRenderTarget.Height;

			// Build screen resolution targets
			m_GeometryBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "GeometryBuffer", DefaultWidth, DefaultHeight, 1 ) );
			m_MaterialBuffer = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "MaterialBuffer", DefaultWidth, DefaultHeight, 1 ) );
			m_EmissiveBuffers[0] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "EmissiveBuffer0", DefaultWidth, DefaultHeight, 1 ) );
			m_EmissiveBuffers[1] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "EmissiveBuffer1", DefaultWidth, DefaultHeight, 1 ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the pipelines
			Pipeline	Depth = ToDispose( new Pipeline( m_Device, "Depth Pass Pipeline", Pipeline.TYPE.DEPTH_PASS ) );
			m_Renderer.AddPipeline( Depth );
			Depth.RenderingStart += new Pipeline.PipelineRenderingEventHandler( DepthPipeline_RenderingStart );

// 			Pipeline	Shadow = ToDispose( new Pipeline( m_Device, "Shadow Pipeline", Pipeline.TYPE.SHADOW_MAPPING ) );
// 			m_Renderer.AddPipeline( Shadow );

			Pipeline	Main = ToDispose( new Pipeline( m_Device, "Main Pipeline", Pipeline.TYPE.MAIN_RENDERING ) );
			m_Renderer.AddPipeline( Main );
			Main.RenderingStart += new Pipeline.PipelineRenderingEventHandler( MainPipeline_RenderingStart );

			Pipeline	DeferredLighting = ToDispose( new Pipeline( m_Device, "Deferred Lighting Pipeline", Pipeline.TYPE.DEFERRED_LIGHTING ) );
			m_Renderer.AddPipeline( DeferredLighting );

			Pipeline	Emissive = ToDispose( new Pipeline( m_Device, "Emissive Pipeline", Pipeline.TYPE.EMISSIVE_UNLIT ) );
			m_Renderer.AddPipeline( Emissive );
			Emissive.RenderingStart += new Pipeline.PipelineRenderingEventHandler( EmissivePipeline_RenderingStart );

			Pipeline	PostProcessing = ToDispose( new Pipeline( m_Device, "Post-Processing Pipeline", Pipeline.TYPE.POST_PROCESSING ) );
			m_Renderer.AddPipeline( PostProcessing );


			//////////////////////////////////////////////////////////////////////////
			// Create the sky technique
			m_Sky = ToDispose( new RenderTechniqueSky( this, "Sky" ) );
			Emissive.AddTechnique( m_Sky );

			//////////////////////////////////////////////////////////////////////////
			// Create the cloud layer technique
			m_CloudLayer = ToDispose( new Demo.RenderTechniqueCloudLayer( this, "Cloud Layer" ) );
			Emissive.AddTechnique( m_CloudLayer );

			//////////////////////////////////////////////////////////////////////////
			// Create the Terrain technique
// 			m_Terrain = ToDispose( new RenderTechniqueTerrain( this, "Terrain Technique" ) );
// 			Main.AddTechnique( m_Terrain );

			//////////////////////////////////////////////////////////////////////////
 			// Create the Vegetation technique
// 			m_Vegetation = ToDispose( new RenderTechniqueVegetation( this, "Vegetation Technique" ) );
// 			Main.AddTechnique( m_Vegetation );

			//////////////////////////////////////////////////////////////////////////
			// Create the depth pass render technique
// 			m_DepthPass = ToDispose( new RenderTechniqueDepthPass( m_Renderer, "Depth Pass" ) );
// 			Depth.AddTechnique( m_DepthPass );


// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the shadow mapping technique
// 			m_ShadowMapping = ToDispose( new RenderTechniqueShadowMapping( m_Renderer, "Shadow Mapping" ) );
// //			Shadow.AddTechnique( m_ShadowMapping );
// 
// 			//////////////////////////////////////////////////////////////////////////
// 			// Create the render techniques for drawing a scene into the deferred render targets
// 			m_DeferredScene = ToDispose( new DeferredRenderingScene( m_Renderer, "Scene Rendering", true ) );
// 			Main.AddTechnique( m_DeferredScene );
// 			m_DepthPass.AddRenderable( m_DeferredScene );
// 			m_ShadowMapping.AddRenderable( m_DeferredScene );
// 
// 			m_DeferredGrass = ToDispose( new DeferredRenderingGrass( m_Renderer, "Grass" ) );
// //			Main.AddTechnique( m_DeferredGrass );
// //			m_DepthPass.AddRenderable( m_DeferredGrass ); NOT DEPTH RENDERABLE !
// 
// 			m_DeferredTerrain = ToDispose( new DeferredRenderingTerrain( m_Renderer, "Terrain" ) );
// 			Main.AddTechnique( m_DeferredTerrain );
// 			m_DepthPass.AddRenderable( m_DeferredTerrain );
// 			m_ShadowMapping.AddRenderable( m_DeferredTerrain );
// 			m_DeferredTerrain.DepthPassDepthStencil = m_Device.DefaultDepthStencil;
// 
// 			m_EmissiveSky = ToDispose( new EmissiveRenderingSky( m_Renderer, "Sky" ) );
// 			Emissive.AddTechnique( m_EmissiveSky );
// //			m_DepthPass.AddRenderable( m_EmissiveSky ); NOT DEPTH RENDERABLE !
// 
// 			// TODO: Add others like skin, trees, etc.


			//////////////////////////////////////////////////////////////////////////
			// Create the post-process render techniques

			// Final image compositing that creates the final image to further tone map and post-process
			m_PostProcessFinalCompositing = ToDispose( new RenderTechniquePostProcessFinalCompositing( this, "FinalCompositing" ) );
			PostProcessing.AddTechnique( m_PostProcessFinalCompositing );

			// Tone-mapping operator to map the HDR image into a LDR one
			m_PostProcessToneMapping = ToDispose( new RenderTechniquePostProcessToneMappingFilmic( m_Device, "ToneMapper", this, false ) );
 			PostProcessing.AddTechnique( m_PostProcessToneMapping );

			m_PostProcessToneMapping.SourceImage = m_PostProcessFinalCompositing.CompositedImage;
			m_PostProcessToneMapping.TargetImage = m_Device.DefaultRenderTarget;
			m_PostProcessToneMapping.AdaptationLevelMin = 0.1f;
//m_PostProcessToneMapping.EnableToneMapping = false;


			//////////////////////////////////////////////////////////////////////////
			// Additional data

			// Create a perspective camera
			m_Camera = ToDispose( new Camera( m_Device, "Default Camera" ) );
			m_Camera.CreatePerspectiveCamera( _CameraFOV, _CameraAspectRatio, _CameraNear, _CameraFar );
			m_Camera.Activate();

			// Create the Sun directional lights
			m_Sun = ToDispose( new DirectionalLight( m_Device, "Sun", true ) );
			m_Sun.Activate();
		}

		/// <summary>
		/// Renders the objects registered to our renderer
		/// </summary>
		public void	Render()
		{
			//////////////////////////////////////////////////////////////////////////
			// Clear stuff
			m_Device.AddProfileTask( this, "Prepare Rendering", "Clear Deferred Targets" );

			// Clear normals to 0 and depth to "infinity"
			m_Device.ClearRenderTarget( m_GeometryBuffer, new Color4( 0.0f, 0.0f, 0.0f, DEPTH_BUFFER_INFINITY ) );

			// Clear materials
			m_Device.ClearRenderTarget( m_MaterialBuffer, new Color4( 0.0f, 0.0f, 0.0f, 0.0f ) );

			// Clear emissive to black and no extinction
			m_Device.ClearRenderTarget( m_EmissiveBuffers[0], new Color4( 1.0f, 0.0f, 0.0f, 0.0f ) );


			//////////////////////////////////////////////////////////////////////////
			// Update stuff
			m_Sun.Direction = m_Sky.SunDirection;


			//////////////////////////////////////////////////////////////////////////
			// Render
			m_Device.AddProfileTask( this, "Rendering", "<START>" );
			m_Renderer.Render();
			m_Device.AddProfileTask( this, "Rendering", "<END>" );
		}

		/// <summary>
		/// Setups multiple render targets according to provided flags
		/// The order of the targets is always Material, Geometry and Emissive
		/// </summary>
		/// <param name="_TargetTypes"></param>
		public void		SetRenderTargets( RENDER_TARGET_TYPES _TargetTypes )
		{
			int					TargetsCount = 0;
			RenderTargetView	MaterialTarget = null;
			if ( (_TargetTypes & RENDER_TARGET_TYPES.MATERIAL) != 0 )
			{
				MaterialTarget = m_MaterialBuffer.RenderTargetView;
				TargetsCount++;
			}
			RenderTargetView	GeometryTarget = null;
			if ( (_TargetTypes & RENDER_TARGET_TYPES.GEOMETRY) != 0 )
			{
				GeometryTarget = m_GeometryBuffer.RenderTargetView;
				TargetsCount++;
			}
			RenderTargetView	EmissiveTarget = null;
			if ( (_TargetTypes & RENDER_TARGET_TYPES.EMISSIVE) != 0 )
			{
				EmissiveTarget = m_EmissiveBuffers[1].RenderTargetView;
				TargetsCount++;
			}

			RenderTargetView[]	Targets = new RenderTargetView[TargetsCount];
			TargetsCount = 0;
			if ( MaterialTarget != null )
				Targets[TargetsCount++] = MaterialTarget;
			if ( GeometryTarget != null )
				Targets[TargetsCount++] = GeometryTarget;
			if ( EmissiveTarget != null )
				Targets[TargetsCount++] = EmissiveTarget;

			m_Device.SetMultipleRenderTargets( Targets, m_Device.DefaultDepthStencil.DepthStencilView );
			m_Device.SetViewport( 0, 0, m_GeometryBuffer.Width, m_GeometryBuffer.Height, 0.0f, 1.0f );
		}

		public void		SwapEmissiveBuffers()
		{
			RenderTarget<PF_RGBA16F>	Temp = m_EmissiveBuffers[0];
			m_EmissiveBuffers[0] = m_EmissiveBuffers[1];
			m_EmissiveBuffers[1] = Temp;
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			// Provide deferred rendering interface data
			IDeferredRendering	I = _Interface as IDeferredRendering;
			if ( I != null )
			{
				I.GBuffer0 = m_MaterialBuffer;
				I.GBuffer1 = m_GeometryBuffer;
				I.GBuffer2 = m_EmissiveBuffers[0];
//				I.LightBuffer = m_InferredLighting.LightBuffer;
				return;
			}
		}

		#endregion

		#region IMaterialLoader Members

		public Material<VS> LoadMaterial<VS>( string _Name, ShaderModel _SM, System.IO.FileInfo _FileName ) where VS : struct
		{
			return ToDispose( new Material<VS>( m_Device, _Name, _SM, _FileName ) );
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		protected void DepthPipeline_RenderingStart( Pipeline _Sender )
		{
			// Setup only a depth stencil (no render target) and clear it
			m_Device.SetRenderTarget( null as IRenderTarget, m_Device.DefaultDepthStencil );
			m_Device.SetViewport( 0, 0, m_Device.DefaultDepthStencil.Width, m_Device.DefaultDepthStencil.Height, 0.0f, 1.0f );
			m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );
		}

		protected void MainPipeline_RenderingStart( Pipeline _Sender )
		{
			// Setup our multiple render targets
			m_Device.SetMultipleRenderTargets( new RenderTarget<PF_RGBA16F>[] { m_MaterialBuffer, m_GeometryBuffer }, m_Device.DefaultDepthStencil );
			m_Device.SetViewport( 0, 0, m_MaterialBuffer.Width, m_MaterialBuffer.Height, 0.0f, 1.0f );
		}

		protected void EmissivePipeline_RenderingStart( Pipeline _Sender )
		{
			// Setup the emissive render target and clear it
//			m_Device.SetRenderTarget( m_EmissiveBuffer, m_Device.DefaultDepthStencil );
			m_Device.SetRenderTarget( m_EmissiveBuffers[1], null );
			m_Device.SetViewport( 0, 0, m_EmissiveBuffers[1].Width, m_EmissiveBuffers[1].Height, 0.0f, 1.0f );
		}

		#endregion
	}
}
