//#define DOWNSCALE_VELOCITY_BUFFER

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
	/// Motion blur
	/// </example>
	public class PostProcessMotionBlur : RenderTechniqueBase
	{
		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
 		protected Material<VS_Pt4V3T2>		m_MaterialPostProcess = null;

		protected Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>	m_ImageScaler = null;

		//////////////////////////////////////////////////////////////////////////
		// Objects

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets

		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected float						m_BlurSize = 2.0f;
		protected float						m_BlurWeightFactor = 1.0f;

		protected Matrix					m_PreviousCameraMatrix = Matrix.Identity;

		public int		m_RecordedCameraFrameIndex = 0;//0x00144; //0x0059;
		public bool		m_bUseReverse = false;

		#endregion

		#region PROPERTIES

		public float						BlurSize			{ get { return m_BlurSize; } set { m_BlurSize = value; } }
		public float						BlurWeightFactor	{ get { return m_BlurWeightFactor; } set { m_BlurWeightFactor = value; } }

		#endregion

		#region METHODS

		public	PostProcessMotionBlur( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
//m_bEnabled = false;

			// Create our main materials
 			m_MaterialPostProcess = m_Renderer.LoadMaterial<VS_Pt4V3T2>( "Post-Process Motion Blur Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/PostProcessMotionBlur.fx" ) );

			// Build the precise image scaler
			m_ImageScaler = ToDispose( new Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>( m_Device, "ImageScaler",
					Nuaj.Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>.QUALITY.DEFAULT,
					Helpers.TextureScaler<PF_RGBA16F,PF_RGBA16F>.METHOD.MAX ) );
		}

		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled )
				return;

			//////////////////////////////////////////////////////////////////////////
			// Compute current camera position and rotation
			Matrix		Camera2World = m_Renderer.Camera.Camera2World;

// REPLAY
if ( m_Renderer.RecordedCameraMatricesCount > 0 )
{
	m_PreviousCameraMatrix = m_Renderer.RecordedCameraMatrices[(m_RecordedCameraFrameIndex + m_Renderer.RecordedCameraMatricesCount - 1) % m_Renderer.RecordedCameraMatricesCount];
	Camera2World = m_Renderer.RecordedCameraMatrices[m_RecordedCameraFrameIndex % m_Renderer.RecordedCameraMatricesCount];
}
// REPLAY

			Matrix	Temp0 = m_PreviousCameraMatrix;
			Temp0.Row3 = -Temp0.Row3;
			Matrix	Temp1 = Camera2World;
			Temp1.Row3 = -Temp1.Row3;

			Vector3		DeltaPosition, DeltaPivot;
			Quaternion	DeltaRotation;
			Scene.ComputeObjectDeltaPositionRotation( ref Temp0, ref Temp1, out DeltaPosition, out DeltaRotation, out DeltaPivot );

			using ( m_MaterialPostProcess.UseLock() )
			{
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

				VariableResource	vSourceTexture = CurrentMaterial.GetVariableByName( "SourceVelocityTexture" ).AsResource;
				VariableVector		vSourceTextureInvSize = CurrentMaterial.GetVariableByName( "SourceVelocityTextureInvSize" ).AsVector;

				//////////////////////////////////////////////////////////////////////////
				// Project velocities
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ProjectVelocities" );

				m_Renderer.SetVelocityRenderTarget();	// Should render in VelocityBuffer2
				vSourceTexture.SetResource( m_Renderer.VelocityBuffer );
				vSourceTextureInvSize.Set( new Vector3( 1.0f / m_Renderer.VelocityBuffer.Width, 1.0f / m_Renderer.VelocityBuffer.Height, 0.0f ) );

				CurrentMaterial.ApplyPass( 0 );
				m_Renderer.RenderPostProcessQuad();

				m_Renderer.SwapVelocityRenderTarget();

				//////////////////////////////////////////////////////////////////////////
				// Downscale velocity buffer
#if DOWNSCALE_VELOCITY_BUFFER
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DownSampleVelocities" );

				float	fImageFactor = 0.25f;
				int		TargetWidth = (int) Math.Ceiling( fImageFactor * m_Renderer.VelocityBuffer.Width );
				int		TargetHeight = (int) Math.Ceiling( fImageFactor * m_Renderer.VelocityBuffer.Height );

				m_Renderer.SetVelocityRenderTarget();	// Should render in VelocityBuffer2
				vSourceTexture.SetResource( m_Renderer.VelocityBuffer );
				m_Device.SetViewport( 0, 0, TargetWidth, TargetHeight, 0.0f, 1.0f );

// 				m_ImageScaler.SetTexture( m_Renderer.VelocityBuffer );
// 				m_ImageScaler.PreviouslyRenderedRenderTarget = m_Renderer.VelocityBuffer2;
// 				m_ImageScaler.LastRenderedRenderTarget = m_Renderer.VelocityBuffer;
// 				m_ImageScaler.Scale( TargetWidth, TargetHeight, ( RenderTarget<PF_RGBA16F> _RenderTarget, int _Width, int _Height, bool _bLastStage ) =>
// 					{
// 						m_Renderer.SwapVelocityRenderTarget();
// 					} );

				CurrentMaterial.ApplyPass( 0 );
				m_Renderer.RenderPostProcessQuad();

				m_Renderer.SwapVelocityRenderTarget();
#else
				int		TargetWidth = m_Renderer.VelocityBuffer.Width;
				int		TargetHeight = m_Renderer.VelocityBuffer.Height;
#endif
				//////////////////////////////////////////////////////////////////////////
				// Apply motion blur
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "MotionBlur" );

				m_Renderer.SetFinalRenderTarget();	// Should render in MaterialBuffer2
				vSourceTexture.SetResource( m_Renderer.VelocityBuffer );

				CurrentMaterial.GetVariableByName( "VelocityUVScale" ).AsVector.Set( new Vector2( (float) TargetWidth / m_Renderer.VelocityBuffer.Width, (float) TargetHeight / m_Renderer.VelocityBuffer.Height ) );

				CurrentMaterial.GetVariableByName( "DeltaPosition" ).AsVector.Set( DeltaPosition );
				CurrentMaterial.GetVariableByName( "DeltaRotation" ).AsVector.Set( DeltaRotation );
				CurrentMaterial.GetVariableByName( "DeltaPivot" ).AsVector.Set( DeltaPivot );

				CurrentMaterial.GetVariableByName( "BlurSize" ).AsScalar.Set( m_BlurSize );
				CurrentMaterial.GetVariableByName( "BlurWeightFactor" ).AsScalar.Set( m_BlurWeightFactor );
				CurrentMaterial.GetVariableByName( "SourceTexture" ).AsResource.SetResource( m_Renderer.MaterialBuffer );

				CurrentMaterial.ApplyPass( 0 );
				m_Renderer.RenderPostProcessQuad();
				m_Renderer.SwapFinalRenderTarget();
			}

			// Save camera PR for next frame
			m_PreviousCameraMatrix = Camera2World;
		}
		protected float m_Angle = 0.0f;
		#endregion
	}
}
