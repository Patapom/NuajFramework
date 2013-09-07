using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This setups and encapsulates a basic renderer with only one pipeline and one default render technique
	/// It also contains a camera and 2 main directional lights
	/// </summary>
	public class RendererSetupBasic : Component, IShaderInterfaceProvider
	{
		#region NESTED TYPES

		/// <summary>
		/// Default initialization parameters for the renderer
		/// </summary>
		public class	BasicInitParams
		{
			public float	CameraFOV;				// Default FOV for camera
			public float	CameraAspectRatio;		// Camera aspect ratio (if 0, the default render target's aspect ratio is used)
			public float	CameraClipNear;			// Default camera near clip
			public float	CameraClipFar;			// Default camera far clip
			public bool		bUseAlphaToCoverage;	// True to use alpha to coverage instead of alpha blending
		}

		#endregion

		#region FIELDS

		protected Renderer					m_Renderer = null;
		protected RenderTechniqueDefault	m_DefaultTechnique = null;
		protected Camera					m_Camera = null;
		protected DirectionalLight			m_MainLight = null;
		protected DirectionalLight			m_FillLight = null;

		protected float						m_LastTime = 0.0f;
		protected float						m_DeltaTime = 0.0f;
		protected float						m_Time = 0.0f;
		protected float						m_ToneMappingFactor = 0.5f;
		protected float						m_Gamma = 2.2f;

		#endregion

		#region PROPERTIES

		public Renderer					Renderer			{ get { return m_Renderer; } }
		public RenderTechniqueDefault	DefaultTechnique	{ get { return m_DefaultTechnique; } }
		public Camera					Camera				{ get { return m_Camera; } }
		public DirectionalLight			MainLight			{ get { return m_MainLight; } }
		public DirectionalLight			FillLight			{ get { return m_FillLight; } }
		public float					Time				{ get { return m_Time; } set { m_Time = value; } }
		public float					DeltaTime			{ get { return m_DeltaTime; } }
		public float					ToneMappingFactor	{ get { return m_ToneMappingFactor; } set { m_ToneMappingFactor = value; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Setups a default renderer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Params"></param>
		public	RendererSetupBasic( Device _Device, string _Name, BasicInitParams _Params ) : base( _Device, _Name )
		{
			m_Renderer = ToDispose( new Renderer( m_Device, m_Name ) );
			Init( _Params );
		}

		protected virtual void	Init( BasicInitParams _Params )
		{
			// Create the only pipeline
			Pipeline	Main = ToDispose( new Pipeline( m_Device, "Main Pipeline", Pipeline.TYPE.MAIN_RENDERING ) );
			m_Renderer.AddPipeline( Main );

			// Create the only render technique
			m_DefaultTechnique = ToDispose( new RenderTechniqueDefault( m_Device, "Default Technique", _Params.bUseAlphaToCoverage ) );
			Main.AddTechnique( m_DefaultTechnique );

			// Register the ILinearToneMapping interface
			m_Device.RegisterShaderInterfaceProvider( typeof(ILinearToneMapping), this );

			CreateDefaultCameraAndLights( _Params );
		}

		/// <summary>
		/// Renders the objects registered to our renderer
		/// </summary>
		public void	Render()
		{
			m_DeltaTime = m_Time - m_LastTime;
			m_LastTime = m_Time;

			m_Renderer.Render();
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			(_Interface as ILinearToneMapping).ToneMappingFactor = m_ToneMappingFactor;
			(_Interface as ILinearToneMapping).Gamma = m_Gamma;
		}

		#endregion

		protected void	CreateDefaultCameraAndLights( BasicInitParams _Params )
		{
			// Create a perspective camera
			if ( _Params.CameraAspectRatio == 0.0f )
				_Params.CameraAspectRatio = (float) m_Device.DefaultRenderTarget.Width / m_Device.DefaultRenderTarget.Height;
			m_Camera = ToDispose( new Camera( m_Device, "Default Camera" ) );
			m_Camera.CreatePerspectiveCamera( _Params.CameraFOV, _Params.CameraAspectRatio, _Params.CameraClipNear, _Params.CameraClipFar );
			m_Camera.Activate();

			// Create the default directional lights
			m_MainLight = ToDispose( new DirectionalLight( m_Device, "Main Light", true ) );
			m_MainLight.Direction = new Vector3( 1, 1, 1 );
			m_MainLight.Color = new Vector4( 1, 1, 1, 1 );
			m_MainLight.Activate();

			m_FillLight = ToDispose( new DirectionalLight( m_Device, "Fill Light", false ) );
			m_FillLight.Direction = new Vector3( -1, 0.1f, 1 );
			m_FillLight.Color = 0.2f * new Vector4( 1, 1, 1, 1 );
			m_FillLight.Activate();
		}

		#endregion
	}
}
