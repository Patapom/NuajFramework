using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// Filmic tone-mapping post process utility.
	/// 
	/// This technique applies "filmic curve" tone mapping as described by John Hable in http://filmicgames.com/archives/75#more-75
	/// or in his GDC talk about tone mapping in Uncharted 2 (http://www.gdcvault.com/play/1012459/Uncharted_2__HDR_Lighting)
	/// The filmic curve is a S-shaped curve that has been used for decades by the film industry (i.e. Kodak or Fuji, not Holywood)
	///  as the "film impression" response curve.
	/// For example, consult : http://i217.photobucket.com/albums/cc75/nikonf2/scurve.jpg
	///
	/// -------------------------------------------------------------------------------------
	/// To correctly use that utility technique, you must provide :
	///	_ A SourceImage render target that was created with all its mip-map levels, down to the 1x1 size
	///	_ A TargetImage that the render technique will render to
	/// </summary>
	public class RenderTechniquePostProcessToneMappingFilmic : RenderTechnique, IShaderInterfaceProvider
	{
		#region NESTED TYPES

		public delegate IRenderTarget	QuerySourceBufferEventHandler();
		public delegate void			SetRenderTargetEventHandler();

		public class	IToneMappingSupport : ShaderInterfaceBase
		{
			[Semantic( "IMAGE_LUMINANCE" )]
			public ITexture2D			ImageLuminance	{ set { SetResource( "IMAGE_LUMINANCE", value ); } }
		}

		public enum DEBUG_TYPE
		{
			DISABLED,
			LUMINANCE_NORMALIZED,		// Display luminance as a color gradient. Gradient extremes are exactly Min and Max luminance
			LUMINANCE_CUSTOM,			// Display luminance as a color gradient. Gradient extremes are specified manually
			GRADIENTS_FULLSCREEN,		// Display a gradient table. The gradient is tone mapped
			GRADIENTS_INSET,			// Display a gradient table as an inset in the lower left corner of the screen. The gradient is tone mapped
		}

		#endregion

		#region FIELDS

		protected Material<VS_Pt4V3T2>	m_Material = null;

		protected Helpers.ScreenQuad	m_Quad = null;
		protected IRenderTarget			m_SourceImage = null;
		protected IRenderTarget			m_TargetImage = null;

		// Two 1x1 render targets to perform temporal adaptation
		protected RenderTarget<PF_RGBA16F>[]	m_TemporalAdaptationTargets = new RenderTarget<PF_RGBA16F>[2];

		// Three 1x1 staging targets for CPU access to luminance
		// cf. "Copying and Accessing Resource Data" in the DirectX 10 documentation for an explanation about copying and mapping resources.
		//
		// Basically, at frame :
		//	#F, we ask for a copy the current luminance level (i.e. m_TemporalAdaptationTargets[1]) into m_CPULuminanceAccess[0].
		//	#F+1, the copy happens but the result is not ready yet.
		//	#F+2, the copy result is finally available and we can map it to CPU memory without stalling the GPU
		//
		protected bool					m_bCPUAccess = false;
		protected Texture2DCPU<PF_RGBA16F>[]	m_CPULuminanceAccess = new Texture2DCPU<PF_RGBA16F>[3];
		protected float					m_LuminanceAverage = 0.0f;
		protected float					m_LuminanceMin = 0.0f;
		protected float					m_LuminanceMax = 0.0f;

		// Debug textures
		protected Texture2D<PF_RGBA8_sRGB>	m_DebugTexture_FalseColors = null;
		protected Texture2D<PF_RGBA8>		m_DebugTexture_RGBRamps = null;


		//////////////////////////////////////////////////////////////////////////
		// Parameters

		protected bool					m_bEnableToneMapping = true;
		protected float					m_DeltaTime = 1.0f / 30.0f;	// Default to 30 fps
		protected float					m_AverageOrMax = 0.0f;
		protected float					m_SamplingOffset = 2.0f;
		protected float					m_TemporalAdaptationSpeed = 0.7f;
		protected float					m_Gamma = 1.0f;
		protected Vector2				m_AdaptationLevels = new Vector2( 5.0f, 200.0f );

		// Debug parameters
		protected DEBUG_TYPE			m_DebugType = DEBUG_TYPE.DISABLED;
		protected float					m_DebugLuminanceMin = 0.0f;
		protected float					m_DebugLuminanceMax = 100.0f;
		protected float					m_DebugLuminanceMarker = 50.0f;
		protected float					m_DebugLuminanceMarkerTolerance = 0.5f;	// Show luminance +/- this tolerance

		// Filmic operator parameters
		protected float					m_ExposureBias = 1.0f;
		protected float					m_HDRWhitePointValue = 15.0f;
		protected float					m_LDRWhitePointValue = 1.0f;

		protected float					m_A = 0.15f;
		protected float					m_B = 0.50f;
		protected float					m_C = 0.10f;
		protected float					m_D = 0.80f;//0.20f;
		protected float					m_E = 0.02f;
		protected float					m_F = 0.30f;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the source image to tone map
		/// The source image MUST have been created with all mip levels down to the 1x1 size !
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public IRenderTarget		SourceImage				{ get { return m_SourceImage; } set { m_SourceImage = value; } }

		/// <summary>
		/// Gets or sets the target image to render to
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public IRenderTarget		TargetImage				{ get { return m_TargetImage; } set { m_TargetImage = value; } }

		/// <summary>
		/// Occurs when the post-process is rendering to query which image it should sample from
		/// This event, if set, takes precedence over the "SourceImage" property
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public event QuerySourceBufferEventHandler			QuerySourceBuffer;

		/// <summary>
		/// Occurs when the post-process is rendering to setup the render target it should render to
		/// This event, if set, takes precedence over the "TargetImage" property
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public event SetRenderTargetEventHandler			SetRenderTarget;


		public bool					EnableToneMapping		{ get { return m_bEnableToneMapping; } set { m_bEnableToneMapping = value; } }
		public float				SubPixelSamplingOffset	{ get { return m_SamplingOffset; } set { m_SamplingOffset = value; } }

		/// <summary>
		/// Sets the delta time to use for temporal adaptation
		/// </summary>
		public float				DeltaTime				{ set { m_DeltaTime = value; } }

		[System.ComponentModel.Description( "The interpolant used to decide if we should use the average (0) or max (1) luminance value to tone map the image" )]
		public float				AverageOrMax			{ get { return m_AverageOrMax; } set { m_AverageOrMax = value; } }

		[System.ComponentModel.Description( "The speed at which the camera adapts to luminance" )]
		public float				TemporalAdaptationSpeed	{ get { return m_TemporalAdaptationSpeed; } set { m_TemporalAdaptationSpeed = value; } }
		[System.ComponentModel.Description( "The gamma correction" )]
		public float				Gamma					{ get { return m_Gamma; } set { m_Gamma = value; } }
		[System.ComponentModel.Description( "The minimal adaptable luminance" )]
		public float				AdaptationLevelMin		{ get { return m_AdaptationLevels.X; } set { m_AdaptationLevels.X = value; } }
		[System.ComponentModel.Description( "The maximal adaptable luminance" )]
		public float				AdaptationLevelMax		{ get { return m_AdaptationLevels.Y; } set { m_AdaptationLevels.Y = value; } }

		//////////////////////////////////////////////////////////////////////////
		// Tone Mapping Operator Parameters
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "The exposure bias" )]
		public float				ExposureBias			{ get { return m_ExposureBias; } set { m_ExposureBias = value; } }

		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(W) The HDR white point value" )]
		public float				HDRWhitePointLuminance		{ get { return m_HDRWhitePointValue; } set { m_HDRWhitePointValue = value; } }

		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "The LDR white point value" )]
		public float				LDRWhitePointLuminance	{ get { return m_LDRWhitePointValue; } set { m_LDRWhitePointValue = value; } }

		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(A) Shoulder strength" )]
		public float				A		{ get { return m_A; } set { m_A = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(B) Linear Strength" )]
		public float				B		{ get { return m_B; } set { m_B = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(C) Linear Angle" )]
		public float				C		{ get { return m_C; } set { m_C = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(D) Toe Strength" )]
		public float				D		{ get { return m_D; } set { m_D = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(E) Toe Numerator" )]
		public float				E		{ get { return m_E; } set { m_E = value; } }
		[System.ComponentModel.Category( "Filmic Operator" )]
		[System.ComponentModel.Description( "(F) Toe Denominator" )]
		public float				F		{ get { return m_F; } set { m_F = value; } }
		//
		//////////////////////////////////////////////////////////////////////////


		//////////////////////////////////////////////////////////////////////////
		// DEBUG
		[System.ComponentModel.Category( "DEBUG" )]
		[System.ComponentModel.Description( "Debug Type" )]
		public DEBUG_TYPE			DebugType	{ get { return m_DebugType; } set { m_DebugType = value; } }
		[System.ComponentModel.Category( "DEBUG" )]
		[System.ComponentModel.Description( "Minimum luminance for custom display" )]
		public float				DebugLuminanceMin	{ get { return m_DebugLuminanceMin; } set { m_DebugLuminanceMin = value; } }
		[System.ComponentModel.Category( "DEBUG" )]
		[System.ComponentModel.Description( "Maximum luminance for custom display" )]
		public float				DebugLuminanceMax	{ get { return m_DebugLuminanceMax; } set { m_DebugLuminanceMax = value; } }
		[System.ComponentModel.Description( "Luminance marker" )]
		public float				DebugLuminanceMarker	{ get { return m_DebugLuminanceMarker; } set { m_DebugLuminanceMarker = value; } }
		[System.ComponentModel.Description( "Marker tolerance (marker will show marker luminance +/- this tolerance)" )]
		public float				DebugLuminanceMarkerTolerance	{ get { return m_DebugLuminanceMarkerTolerance; } set { m_DebugLuminanceMarkerTolerance = value; } }
		// DEBUG
		//////////////////////////////////////////////////////////////////////////


		/// <summary>
		/// Gets the adapted AVERAGE luminance level from 2 frames ago
		/// </summary>
		/// <remarks>This value is available only if the post process was created with the ReadableLuminance flag !
		/// Note that it involves copying the luminance information from GPU to CPU and can cause framerate degradation !</remarks>
		[System.ComponentModel.Browsable( false )]
		public float				AverageLuminance	{ get { return m_LuminanceAverage; } }

		/// <summary>
		/// Gets the adapted MIN luminance level from 2 frames ago
		/// </summary>
		/// <remarks>This value is available only if the post process was created with the ReadableLuminance flag !
		/// Note that it involves copying the luminance information from GPU to CPU and can cause framerate degradation !</remarks>
		[System.ComponentModel.Browsable( false )]
		public float				MinLuminance		{ get { return m_LuminanceMin; } }

		/// <summary>
		/// Gets the adapted MIN luminance level from 2 frames ago
		/// </summary>
		/// <remarks>This value is available only if the post process was created with the ReadableLuminance flag !
		/// Note that it involves copying the luminance information from GPU to CPU and can cause framerate degradation !</remarks>
		[System.ComponentModel.Browsable( false )]
		public float				MaxLuminance		{ get { return m_LuminanceMax; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates the render technique
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MaterialLoader">A loader for our material</param>
		/// <param name="_bReadableLuminance">True if the average luminance should be readable by the CPU.
		/// Note that setting this parameter to "true" involves copying the luminance information to system memory and can cause framerate degradation !</param>
		public RenderTechniquePostProcessToneMappingFilmic( Device _Device, string _Name, IMaterialLoader _MaterialLoader, bool _bReadableLuminance ) : base( _Device, _Name )
		{
			// Create our main materials
			m_Material = _MaterialLoader.LoadMaterial<VS_Pt4V3T2>( "ToneMapping Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Utility/PostProcessToneMapping.fx" ) );

			// Create our 2 1x1 temporal adaptation targets
			m_TemporalAdaptationTargets[0] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "TemporalTarget0", 1, 1, 1 ) );
			m_TemporalAdaptationTargets[1] = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "TemporalTarget1", 1, 1, 1 ) );
			m_Device.ClearRenderTarget( m_TemporalAdaptationTargets[0], new Color4( 1.0f, 1.0f, 1.0f, 1.0f ) );
			m_Device.ClearRenderTarget( m_TemporalAdaptationTargets[1], new Color4( 1.0f, 1.0f, 1.0f, 1.0f ) );

			// Create our post-process quad
			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "Quad" ) );

			// Load our debug textures
			System.IO.FileInfo	DebugTextureFileName = new System.IO.FileInfo( "./Media/FalseColors+IsoContours_sRGB.png" );
			if ( DebugTextureFileName.Exists )
				m_DebugTexture_FalseColors = Texture2D<PF_RGBA8_sRGB>.CreateFromBitmapFile( m_Device, "DebugFalseColorsSpectrum", DebugTextureFileName, 0, 1.0f );
			DebugTextureFileName = new System.IO.FileInfo( "./Media/RGBRamps_Linear.png" );
			if ( DebugTextureFileName.Exists )
				m_DebugTexture_RGBRamps = Texture2D<PF_RGBA8>.CreateFromBitmapFile( m_Device, "DebugRGBRamps", DebugTextureFileName, 0, 1.0f );


			// CPU luminance access
			m_bCPUAccess = _bReadableLuminance;
			if ( !m_bCPUAccess )
				return;

			m_CPULuminanceAccess[0] = ToDispose( new Texture2DCPU<PF_RGBA16F>( m_Device, "CPUAccess #0", 1, 1, 1, 1, true ) );
			m_CPULuminanceAccess[1] = ToDispose( new Texture2DCPU<PF_RGBA16F>( m_Device, "CPUAccess #1", 1, 1, 1, 1, true ) );
			m_CPULuminanceAccess[2] = ToDispose( new Texture2DCPU<PF_RGBA16F>( m_Device, "CPUAccess #2", 1, 1, 1, 1, true ) );

			// Declare and provide the IToneMappingSupport interface
			m_Device.DeclareShaderInterface( typeof(IToneMappingSupport) );
			m_Device.RegisterShaderInterfaceProvider( typeof(IToneMappingSupport), this );
		}

		public override void	Render( int _FrameToken )
		{
			IRenderTarget	SourceImage = m_SourceImage;
			if ( QuerySourceBuffer != null )
				SourceImage = QuerySourceBuffer();
			if ( SourceImage == null )
				throw new NException( this, "Source image to tone map is not set !" );

			if ( m_TargetImage == null && SetRenderTarget == null )
				throw new NException( this, "Target image to render to is not set !" );

			using ( m_Material.UseLock() )
			{
 				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				VariableVector		vSourceInfos = CurrentMaterial.GetVariableByName( "_SourceInfos" ).AsVector;
				VariableResource	vSourceTexture = CurrentMaterial.GetVariableByName( "_SourceTexture" ).AsResource;
				VariableResource	vAverageLuminanceTexture = CurrentMaterial.GetVariableByName( "_AverageLuminanceTexture" ).AsResource;

				CurrentMaterial.GetVariableByName( "_Params" ).AsVector.Set( new Vector4( m_SamplingOffset, m_TemporalAdaptationSpeed, 1.0f / m_Gamma, 0.0f ) );

				if ( m_bEnableToneMapping )
				{
					//////////////////////////////////////////////////////////////////////////
					// First, we need to downsample the source image
					// We __know__ the source image has been created with mip-maps so we simply build those mip-maps...
					//
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DownSampleFirstStage" );	// First stage takes the log( luminance )
					EffectPass	P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );

					int	PreviousWith, PreviousHeight;
					int	CurrentWidth = SourceImage.Width, CurrentHeight = SourceImage.Height;

					int	DownSampleStepsCount = SourceImage.MipLevelsCount;
					for ( int DownSampleStepIndex=1; DownSampleStepIndex < DownSampleStepsCount; DownSampleStepIndex++ )
					{
						// Setup source and target data
						PreviousWith = CurrentWidth;
						PreviousHeight = CurrentHeight;
						CurrentWidth = Math.Max( 1, CurrentWidth >> 1 );
						CurrentHeight = Math.Max( 1, CurrentHeight >> 1 );

						m_Device.SetRenderTarget( SourceImage.GetSingleRenderTargetView( DownSampleStepIndex, 0 ) );
						m_Device.SetViewport( 0, 0, CurrentWidth, CurrentHeight, 0.0f, 1.0f );

						vSourceInfos.Set( new Vector2( 1.0f / PreviousWith, 1.0f / PreviousHeight ) );
						vSourceTexture.SetResource( SourceImage.GetSingleTextureView( DownSampleStepIndex-1, 0 ) );
						P.Apply();
						m_Quad.Render();

						if ( DownSampleStepIndex == 1 )
						{	// Swap to normal downscale (i.e. no log)
							CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DownSample" );
							P = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
						}
					}

					//////////////////////////////////////////////////////////////////////////
					// Perform temporal adaptation
					RenderTarget<PF_RGBA16F>	Temp = m_TemporalAdaptationTargets[0];
					m_TemporalAdaptationTargets[0] = m_TemporalAdaptationTargets[1];
					m_TemporalAdaptationTargets[1] = Temp;

					m_Device.SetRenderTarget( m_TemporalAdaptationTargets[1] );
					m_Device.SetViewport( 0, 0, 1, 1, 0.0f, 1.0f );

					// Use our 1x1 mip level (global average luminance) to adapt with previous frame's average luminance
					vSourceTexture.SetResource( SourceImage.GetSingleTextureView( DownSampleStepsCount-1, 0 ) );

					// And feedback our currently adapted luminance from last frame
					vAverageLuminanceTexture.SetResource( m_TemporalAdaptationTargets[0] );

					CurrentMaterial.GetVariableByName( "_DeltaTime" ).AsScalar.Set( m_DeltaTime );
					CurrentMaterial.GetVariableByName( "_AverageOrMax" ).AsScalar.Set( m_AverageOrMax );
					CurrentMaterial.GetVariableByName( "_AdaptationLevels" ).AsVector.Set( m_AdaptationLevels );

					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "TemporalAdaptation" );
					CurrentMaterial.ApplyPass( 0 );
					m_Quad.Render();
				}

				//////////////////////////////////////////////////////////////////////////
				// Display the tone mapped result
				if ( SetRenderTarget != null )
					SetRenderTarget();
				else
				{
					m_Device.SetRenderTarget( m_TargetImage );
					m_Device.SetViewport( 0, 0, m_TargetImage.Width, m_TargetImage.Height, 0.0f, 1.0f );
				}

				vSourceTexture.SetResource( SourceImage.TextureView );
				vSourceInfos.Set( SourceImage.InvSize3 );

				vAverageLuminanceTexture.SetResource( m_TemporalAdaptationTargets[1] );

				CurrentMaterial.GetVariableByName( "A" ).AsScalar.Set( m_A );
				CurrentMaterial.GetVariableByName( "B" ).AsScalar.Set( m_B );
				CurrentMaterial.GetVariableByName( "C" ).AsScalar.Set( m_C );
				CurrentMaterial.GetVariableByName( "D" ).AsScalar.Set( m_D );
				CurrentMaterial.GetVariableByName( "E" ).AsScalar.Set( m_E );
				CurrentMaterial.GetVariableByName( "F" ).AsScalar.Set( m_F );

				CurrentMaterial.GetVariableByName( "_ExposureBias" ).AsScalar.Set( m_ExposureBias );
				CurrentMaterial.GetVariableByName( "_HDRWhitePoint" ).AsScalar.Set( m_HDRWhitePointValue );
				CurrentMaterial.GetVariableByName( "_LDRWhitePoint" ).AsScalar.Set( m_LDRWhitePointValue );

				if ( !m_bEnableToneMapping )
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ToneMap_PassThrough" );
				else if ( m_DebugType != DEBUG_TYPE.DISABLED )
				{
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ToneMap_Debug" );
					CurrentMaterial.GetVariableByName( "_DEBUG_Type" ).AsScalar.Set( (int) m_DebugType );
					CurrentMaterial.GetVariableByName( "_DEBUG_LuminanceMinMaxMarker" ).AsVector.Set( new Vector4( m_DebugLuminanceMin, m_DebugLuminanceMax, m_DebugLuminanceMarker, m_DebugLuminanceMarkerTolerance ) );
					if ( m_DebugTexture_FalseColors != null )
						CurrentMaterial.GetVariableByName( "_DEBUG_FalseColors" ).AsResource.SetResource( m_DebugTexture_FalseColors );
					if ( m_DebugTexture_RGBRamps != null )
						CurrentMaterial.GetVariableByName( "_DEBUG_RGBRamps" ).AsResource.SetResource( m_DebugTexture_RGBRamps );
				}
				else
					CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ToneMap" );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();
			}

			if ( !m_bCPUAccess )
				return;

			//////////////////////////////////////////////////////////////////////////
			// Copy for CPU access
			// Basically, at frame:
			//	#F, we ask for a copy the current luminance level (i.e. m_TemporalAdaptationTargets[1]) into m_CPULuminanceAccess[0].
			//	#F+1, the copy happens but the result is not ready yet.
			//	#F+2, the copy result is finally available
			//

			// Map texture #0 which should be ready as its copy was ordered 2 frames ago
			DataRectangle	Data = m_CPULuminanceAccess[0].Map( 0 );

			Half4	TempLum = new Half4( (Half) 0.0f );
			Utilities.Read<Half4>( Data.DataPointer, ref TempLum );

			m_LuminanceAverage = TempLum.X;
			m_LuminanceMin = TempLum.Y;
			m_LuminanceMax = TempLum.Z;

			m_CPULuminanceAccess[0].UnMap( 0 );

			// Copy current luminance level to CPU
			// The copy is posted, it will happen next frame and the result will be available 2 frames from now
			m_TemporalAdaptationTargets[1].CopyTo( m_CPULuminanceAccess[0] );

			// Scroll
			Texture2DCPU<PF_RGBA16F>	Temp2 = m_CPULuminanceAccess[0];
			m_CPULuminanceAccess[0] = m_CPULuminanceAccess[1];
			m_CPULuminanceAccess[1] = m_CPULuminanceAccess[2];
			m_CPULuminanceAccess[2] = Temp2;
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			IToneMappingSupport	I = _Interface as IToneMappingSupport;
			I.ImageLuminance = m_TemporalAdaptationTargets[1];
		}

		#endregion

		#endregion
	}
}
