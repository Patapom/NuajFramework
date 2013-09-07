using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This extends the basic renderer with and additional shadow map technique
	/// </summary>
	public class RendererSetupShadowMap : RendererSetupBasic
	{
		#region NESTED TYPES

		public class	InitParams : BasicInitParams
		{
			public int	ShadowMapSlicesCount;	// The amount of slices used for the cascaded shadow maps technique
			public int	ShadowMapSize;			// The size of the shadow map texture
		}

		#endregion

		#region FIELDS

		protected RenderTechniqueShadowMap	m_ShadowMapTechnique = null;

		#endregion

		#region PROPERTIES

		public RenderTechniqueShadowMap	ShadowMapTechnique	{ get { return m_ShadowMapTechnique; } }

		// These parameters are proxies for the shadow map technique
		public float					LambdaCorrection			{ get { return m_ShadowMapTechnique.LambdaCorrection; } set { m_ShadowMapTechnique.LambdaCorrection = value; } }
		public bool						UseCameraNearFarOverride	{ get { return m_ShadowMapTechnique.UseCameraNearFarOverride; } set { m_ShadowMapTechnique.UseCameraNearFarOverride = value; } }
		public float					CameraNearOverride			{ get { return m_ShadowMapTechnique.CameraNearOverride; } set { m_ShadowMapTechnique.CameraNearOverride = value; } }
		public float					CameraFarOverride			{ get { return m_ShadowMapTechnique.CameraFarOverride; } set { m_ShadowMapTechnique.CameraFarOverride = value; } }
		public Vector3					ShadowBias					{ get { return m_ShadowMapTechnique.ShadowBias; } set { m_ShadowMapTechnique.ShadowBias = value; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Setups a default renderer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	RendererSetupShadowMap( Device _Device, string _Name, InitParams _Params ) : base( _Device, _Name, _Params )
		{
		}

		protected override void  Init( BasicInitParams _Params )
		{
			InitParams	Params = _Params as InitParams;

			// Create the shadow render technique
			RenderTechniqueShadowMap.InitParams	ShadowParams = new RenderTechniqueShadowMap.InitParams();
			ShadowParams.SlicesCount = Params.ShadowMapSlicesCount;
			ShadowParams.ShadowMapWidth = ShadowParams.ShadowMapHeight = Params.ShadowMapSize;
			m_ShadowMapTechnique = ToDispose( new RenderTechniqueShadowMap( m_Device, "ShadowMap Technique", ShadowParams ) );

			// Create the main render technique
			m_DefaultTechnique = ToDispose( new RenderTechniqueDefaultWithShadows( m_Device, "Default Technique with Shadows", _Params.bUseAlphaToCoverage ) );

			// Create the shadow map pipeline
			Pipeline	Shadow = ToDispose( new Pipeline( m_Device, "Shadow Pipeline", Pipeline.TYPE.SHADOW_MAPPING ) );
			m_Renderer.AddPipeline( Shadow );	// Add before MAIN
			Shadow.AddTechnique( m_ShadowMapTechnique );

			// Create the main pipeline
			Pipeline	Main = ToDispose( new Pipeline( m_Device, "Main Pipeline", Pipeline.TYPE.MAIN_RENDERING ) );
			m_Renderer.AddPipeline( Main );
			Main.AddTechnique( m_DefaultTechnique );

			// Register the ILinearToneMapping interface
			m_Device.RegisterShaderInterfaceProvider( typeof(ILinearToneMapping), this );

			CreateDefaultCameraAndLights( _Params );

			// Initialize with default camera and light
			m_ShadowMapTechnique.Camera = Camera;
			m_ShadowMapTechnique.Light = m_MainLight;
		}

		#endregion
	}
}
