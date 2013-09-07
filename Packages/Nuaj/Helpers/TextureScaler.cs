using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj.Helpers
{
	/// <summary>
	/// This is a class that helps you rescale a texture to any size
	/// The scaling takes place in multiple stages that are at most a factor of 2 from the last stage (or 0.5 for a downscale)
	/// 
	/// You can provide a delegate to hook to every stage of the scaling or you can collect the final scaled image using the
	///  LastRenderedRenderTarget property (the scaled image will then lie in the rectangle of coordinates (0,0) - (Width-1,Height-1).
	/// 
	/// NOTE :
	/// ------
	/// Scaling is an iterative process that starts from the original texture at its original size and either down- or up-scales the texture
	///  to a new specified size after a call to Scale().
	/// You can then call Scale() again but this time it will start from the last size and scale again from that size to the new target size.
	/// You must call Reset() if you wish to restart scaling from the original texture and the original size again.
	/// </summary>
	/// <remarks>This helper can be used to generate all the mip levels of a texture although they're currently generated in software by the Image class</remarks>
	public class TextureScaler<PF,PF2> : Component where PF:struct,IPixelFormat where PF2:struct,IPixelFormat
	{
		#region NESTED TYPES

		public enum QUALITY
		{
			DEFAULT,	// DEFAULT is MEDIUM
			LOW,		// Uses a single sample per pixel
			MEDIUM,		// Uses 5 samples per pixel
			HIGH		// Uses 9 samples per pixel
		}

		public enum METHOD
		{
			DEFAULT,	// DEFAULT is AVERAGE
			AVERAGE,	// Averages the samples
			MAX,		// Takes the maximum of all samples
			MIN			// Takes the minimum of all samples
		}

		/// <summary>
		/// This delegate helps you hook the different scaling stages of the original texture
		/// </summary>
		/// <param name="_RenderTarget">The render target in which the last rescaling took place</param>
		/// <param name="_Width">The width of the currenlty rescaled rendering</param>
		/// <param name="_Height">The height of the currenlty rescaled rendering</param>
		/// <param name="_bLastStage">True if this is the last rescaling (so you know if it is the actual size you requested in the first place)</param>
		public delegate void	ScaleEventHandler( RenderTarget<PF2> _RenderTarget, int _Width, int _Height, bool _bLastStage );

		#endregion

		#region FIELDS

		protected QUALITY					m_Quality;
		protected METHOD					m_Method;

		protected Texture2D<PF>				m_Texture = null;
		protected bool						m_bFirstCall = true;
		protected int						m_CurrentWidth;
		protected int						m_CurrentHeight;

		protected bool						m_bTargetsHandledInternally = true;
		protected RenderTarget<PF2>			m_TempTarget0 = null;
		protected RenderTarget<PF2>			m_TempTarget1 = null;

		protected Material<VS_Pt4V3T2>		m_ScaleMaterial = null;
		protected VariableResource			m_vSourceTexture = null;
		protected VariableVector			m_vSubPixelUVScale = null;
		protected VariableVector			m_vTextureSubSize = null;
		protected VariableVector			m_vTextureFullSize = null;
		protected ScreenQuad				m_Quad = null;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the previously rendered target whose data are from 1 pass ago
		/// </summary>
		public RenderTarget<PF2>	PreviouslyRenderedRenderTarget
		{
			get { return m_TempTarget1; }
			set
			{
				if ( m_bTargetsHandledInternally )
					throw new Exception( "You cannot set the render target manually as the render targets are handled internally !You must use the second constructor that takes custom render targets to be able to change them dynamically !" );
				m_TempTarget1 = value;
			}
		}

		/// <summary>
		/// Gets or sets the render target where the last rendering occurred (that's where you will find the latest scaling result)
		/// </summary>
		public RenderTarget<PF2>	LastRenderedRenderTarget
		{
			get { return m_TempTarget0; }
			set
			{
				if ( m_bTargetsHandledInternally )
					throw new Exception( "You cannot set the render target manually as the render targets are handled internally !You must use the second constructor that takes custom render targets to be able to change them dynamically !" );
				m_TempTarget0 = value;
			}
		}

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a texture scaler with its own double buffered render targets of specified maximum dimenstions (scaling cannot get larger than this)
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Texture"></param>
		/// <param name="_MaxWidth"></param>
		/// <param name="_MaxHeight"></param>
		public	TextureScaler( Device _Device, string _Name, Texture2D<PF> _Texture, int _MaxWidth, int _MaxHeight, QUALITY _Quality, METHOD _Method ) : base( _Device, _Name )
		{
			SetTexture( _Texture );

			// Build temporary render targets
			m_TempTarget0 = ToDispose( new RenderTarget<PF2>( m_Device, "Temp Render Target #0", _MaxWidth, _MaxHeight, 1 ) );
			m_TempTarget1 = ToDispose( new RenderTarget<PF2>( m_Device, "Temp Render Target #1", _MaxWidth, _MaxHeight, 1 ) );

			Init( _Quality, _Method );
		}

		/// <summary>
		/// Creates a texture scaler with custom render targets (scaling cannot get larger than the dimension of the targets)
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Texture"></param>
		/// <param name="_TempRenderTarget0"></param>
		/// <param name="_TempRenderTarget1"></param>
		public	TextureScaler( Device _Device, string _Name, Texture2D<PF> _Texture, RenderTarget<PF2> _TempRenderTarget0, RenderTarget<PF2> _TempRenderTarget1, QUALITY _Quality, METHOD _Method ) : base( _Device, _Name )
		{
			SetTexture( _Texture );

			m_bTargetsHandledInternally = false;
			m_TempTarget0 = _TempRenderTarget0;
			m_TempTarget1 = _TempRenderTarget1;

			Init( _Quality, _Method );
		}

		/// <summary>
		/// Creates an empty texture scaler, assuming you know how to configure it later (i.e. call SetTexture() + setup appropriate render targets)
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	TextureScaler( Device _Device, string _Name, QUALITY _Quality, METHOD _Method ) : base( _Device, _Name )
		{
			m_bTargetsHandledInternally = false;
			Init( _Quality, _Method );
		}

		protected void	Init( QUALITY _Quality, METHOD _Method )
		{
			m_Quality = _Quality;
			m_Method = _Method;

			// Create the scale material
			m_ScaleMaterial = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "TextureScaler Material", ShaderModel.SM4_0, Properties.Resources.TextureScale ) );
			m_vSourceTexture = m_ScaleMaterial.GetVariableByName( "TexSource" ).AsResource;
			m_vTextureFullSize = m_ScaleMaterial.GetVariableByName( "TextureFullSize" ).AsVector;
			m_vTextureSubSize = m_ScaleMaterial.GetVariableByName( "TextureSubSize" ).AsVector;
			m_vSubPixelUVScale = m_ScaleMaterial.GetVariableByName( "SubPixelUVScale" ).AsVector;

			switch ( _Method )
			{
				case METHOD.DEFAULT:
				case METHOD.AVERAGE:
					switch ( _Quality )
					{
						case QUALITY.LOW:
							m_ScaleMaterial.CurrentTechnique = m_ScaleMaterial.GetTechniqueByName( "PostProcessRender_Scale_LOWQUALITY" );
							break;
						case QUALITY.MEDIUM:
						case QUALITY.DEFAULT:
							m_ScaleMaterial.CurrentTechnique = m_ScaleMaterial.GetTechniqueByName( "PostProcessRender_Scale_MEDIUMQUALITY" );
							break;
						case QUALITY.HIGH:
							m_ScaleMaterial.CurrentTechnique = m_ScaleMaterial.GetTechniqueByName( "PostProcessRender_Scale_HIGHQUALITY" );
							break;
					}
					break;
				case METHOD.MAX:
					switch ( _Quality )
					{
						case QUALITY.LOW:
							m_ScaleMaterial.CurrentTechnique = m_ScaleMaterial.GetTechniqueByName( "PostProcessRender_Scale_LOWQUALITY" );
							break;
						case QUALITY.MEDIUM:
						case QUALITY.DEFAULT:
							m_ScaleMaterial.CurrentTechnique = m_ScaleMaterial.GetTechniqueByName( "PostProcessRender_Scale_MEDIUMQUALITY_MAX" );
							break;
						case QUALITY.HIGH:
							m_ScaleMaterial.CurrentTechnique = m_ScaleMaterial.GetTechniqueByName( "PostProcessRender_Scale_HIGHQUALITY_MAX" );
							break;
					}
					break;
				case METHOD.MIN:
					switch ( _Quality )
					{
						case QUALITY.LOW:
							m_ScaleMaterial.CurrentTechnique = m_ScaleMaterial.GetTechniqueByName( "PostProcessRender_Scale_LOWQUALITY" );
							break;
						case QUALITY.MEDIUM:
						case QUALITY.DEFAULT:
							m_ScaleMaterial.CurrentTechnique = m_ScaleMaterial.GetTechniqueByName( "PostProcessRender_Scale_MEDIUMQUALITY_MIN" );
							break;
						case QUALITY.HIGH:
							m_ScaleMaterial.CurrentTechnique = m_ScaleMaterial.GetTechniqueByName( "PostProcessRender_Scale_HIGHQUALITY_MIN" );
							break;
					}
					break;
			}

			// Create the render quad
			m_Quad = ToDispose( new ScreenQuad( m_Device, "Render Quad" ) );
		}

		/// <summary>
		/// Resets the texture for the scaler
		/// </summary>
		/// <remarks>Existing render targets must be compatible with the texture's size otherwise this method may crash !</remarks>
		/// <param name="_Texture"></param>
		public void SetTexture( Texture2D<PF> _Texture )
		{
			if ( _Texture == null )
				throw new NException( this, "Invalid texture !" );

			m_Texture = _Texture;
			Reset();
		}

		/// <summary>
		/// Restarts scaling from the original texture at its original size
		/// </summary>
		public void	Reset()
		{
			m_bFirstCall = true;
			m_CurrentWidth = m_Texture.Width;
			m_CurrentHeight = m_Texture.Height;
		}

		/// <summary>
		/// Forces the current size of the image to a specific value
		/// </summary>
		/// <param name="_CurrentWidth"></param>
		/// <param name="_CurrentHeight"></param>
		public void	SetCurrentSize( int _CurrentWidth, int _CurrentHeight )
		{
			m_CurrentWidth = _CurrentWidth;
			m_CurrentHeight = _CurrentHeight;
		}

		/// <summary>
		/// Scales the source texture from its current size to the requested size
		/// </summary>
		/// <param name="_TargetWidth"></param>
		/// <param name="_TargetHeight"></param>
		/// <param name="_ScaleDelegate"></param>
		public void	Scale( int _TargetWidth, int _TargetHeight, ScaleEventHandler _ScaleDelegate )
		{
			try
			{
				// Perform multiple scaling by at most a factor of 2
				int		PreviousWidth = 0;
				int		PreviousHeight = 0;

				using ( m_ScaleMaterial.UseLock() )
				{
					while ( m_CurrentWidth != _TargetWidth || m_CurrentHeight != _TargetHeight )
					{
						// Compute new size
						PreviousWidth = m_CurrentWidth;
						PreviousHeight = m_CurrentHeight;

						float fScaleX = 1.0f;
						float fSubPixelScaleX = 1.0f;
						if ( _TargetWidth > m_CurrentWidth )
						{	// Upscale
							fScaleX = Math.Min( 2.0f, (float) _TargetWidth / m_CurrentWidth );
							m_CurrentWidth = Math.Min( _TargetWidth, (int) Math.Ceiling( fScaleX * PreviousWidth ) );
							fSubPixelScaleX = (float) m_CurrentWidth / PreviousWidth - 1.0f;
						}
						else if ( _TargetWidth < m_CurrentWidth )
						{	// Downscale
							fScaleX = Math.Max( 0.5f, (float) _TargetWidth / m_CurrentWidth );
							m_CurrentWidth = Math.Max( _TargetWidth, (int) Math.Floor( fScaleX * PreviousWidth ) );
							fSubPixelScaleX = (float) PreviousWidth / m_CurrentWidth - 1.0f;
						}

						float fScaleY = 1.0f;
						float fSubPixelScaleY = 1.0f;
						if ( _TargetHeight > m_CurrentHeight )
						{	// Upscale
							fScaleY = Math.Min( 2.0f, (float) _TargetHeight / m_CurrentHeight );
							m_CurrentHeight = Math.Min( _TargetHeight, (int) Math.Ceiling( fScaleY * PreviousHeight ) );
							fSubPixelScaleY = (float) m_CurrentHeight / PreviousHeight - 1.0f;
						}
						else if ( _TargetHeight < m_CurrentHeight )
						{	// Downscale
							fScaleY = Math.Max( 0.5f, (float) _TargetHeight / m_CurrentHeight );
							m_CurrentHeight = Math.Max( _TargetHeight, (int) Math.Floor( fScaleY * PreviousHeight ) );
							fSubPixelScaleY = (float) PreviousHeight / m_CurrentHeight - 1.0f;
						}

						if ( m_CurrentWidth > m_TempTarget1.Width || m_CurrentHeight > m_TempTarget1.Height )
							throw new NException( this, "The current scale size is larger than the render target !" );

						// Setup source scaling factors
						m_vSourceTexture.SetResource( m_bFirstCall ? m_Texture.TextureView : m_TempTarget0.TextureView );
						m_vTextureFullSize.Set( m_bFirstCall ? new Vector2( m_Texture.Width, m_Texture.Height ) : new Vector2( m_TempTarget0.Width, m_TempTarget0.Height ) );
						m_vTextureSubSize.Set( new Vector2( PreviousWidth, PreviousHeight ) );
						m_vSubPixelUVScale.Set( new Vector2( fSubPixelScaleX, fSubPixelScaleY ) );

						// Render
						m_Device.SetRenderTarget( m_TempTarget1 );
						m_Device.SetViewport( 0, 0, m_CurrentWidth, m_CurrentHeight, 0.0f, 1.0f );

						m_ScaleMaterial.Render( ( _Sender, _Pass, _PassIndex ) => { m_Quad.Render(); } );

						// Swap targets
						SwapRenderTargets();

						// Notify of the scale
						if ( _ScaleDelegate != null )
							_ScaleDelegate( LastRenderedRenderTarget, m_CurrentWidth, m_CurrentHeight, m_CurrentWidth == _TargetWidth && m_CurrentHeight == _TargetHeight );

						m_bFirstCall = false;
					}
				}
			}
			catch ( Exception _e )
			{
				throw new NException( this, "An error occurred while performing texture scaling !", _e );
			}
		}

		/// <summary>
		/// Swaps the render targets once, the last rendered target becomes the previously rendered target and vice versa
		/// This can be useful if you intend to use the 2 render targets at a given scale stage and apply a material rendering of your own
		/// </summary>
		/// <example>
		/// For example, you can :
		///		1) Downscale a source texture => Last Rendered target is T0, Previously render target is T1
		///		2) Apply a process to the downscaled texture from T0 to T1
		///		3) Call SwapRenderTargets() => Last Rendered target is T1, Previously render target is T0
		///		4) Upscale the texture again => The upscale will correctly start from T1 and continue the scaling process
		/// </example>
		public void	SwapRenderTargets()
		{
			RenderTarget<PF2>	TempTarget = m_TempTarget0;
			m_TempTarget0 = m_TempTarget1;
			m_TempTarget1 = TempTarget;

		}

		#endregion
	}
}
