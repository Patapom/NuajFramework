//#define EFFECT_DEBUG

#define USE_GENERIC
#define USE_STREAK
#define USE_IRIS
#define USE_SHIMMER
#define USE_SPIKEBALL
#define USE_GLINT

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
	/// Lens-flare post process utility.
	/// 
	/// This technique mimics the "Optical Flares" rendering of lens flares (http://www.videocopilot.net/products/opticalflares/).
	/// It allows to create and render multiple lens-flares that can then be driven by lights.
	/// 
	/// All in all, almost every features of the original package are supported. Also, I tried to duplicate the effects
	///  rendering as well as I could, not all formulas are exactly identical and some are pretty nuts just to achieve
	///  the same aspect so don't expect all lens-flares to behave in the exact same manner !
	/// 
	/// Noticeable artefacts :
	/// _ Glows are not as bright
	/// _ Some intensities in spike balls and shimmers are not quite right
	/// _ Random values are (obviously) not identical
	/// 
	/// Unsupported feature :
	/// _ Matte Box => These are made to automatically obscure the lens-flare when going off-screen but I decided to leave that to the user
	/// _ All "1 Way" flags in dynamic triggering (as I didn't understand their purpose)
	/// _ All flags in "Advanced Settings" (as I couldn't get their purpose either)
	/// _ "Convex Roundness" in "Advanced Settings" for irises
	/// </summary>
	public class RenderTechniquePostProcessLensFlares<PF> : RenderTechnique where PF:struct,IPixelFormat
	{
		#region NESTED TYPES

		public class	Light
		{
			#region NESTED TYPES

			public delegate void	LightProjectingEventHandler( Light _Light );

			#endregion

			#region FIELDS

			protected PointLight	m_Light = null;
			protected ITexture2D	m_LightIntensityScale = null;

			protected Vector2		m_FlarePosition;
			protected float			m_FlareSize = 1.0f;
			protected float			m_ManualTrigger = 0.0f;
			protected bool			m_bUseOnlyManualTrigger = false;

			#endregion

			#region PROPERTIES

			public PointLight		SourceLight			{ get { return m_Light; } }
			public ITexture2D		LightIntensityScale	{ get { return m_LightIntensityScale; } }

			/// <summary>
			/// Gets the light's position in FLARE space after projection
			/// </summary>
			public Vector2			FlarePosition		{ get { return m_FlarePosition; } }

			/// <summary>
			/// Gets or sets the light's global size factor
			/// </summary>
			public float			FlareSize			{ get { return m_FlareSize; } set { m_FlareSize = value; } }

			/// <summary>
			/// Gets or sets the manual trigger value (in [0,1])
			/// The dynamic trigger is usually computed by the lens flare shaders but you can activate it manually
			/// This allows you to do some neat effects like modifying the lens flare's aspect based on the phase between the light and the camera for example,
			///  like with a torch light that behaves differently when it's pointed at the camera
			/// </summary>
			public float			ManualTrigger		{ get { return m_ManualTrigger; } set { m_ManualTrigger = value; } }

			/// <summary>
			/// Gets or sets the flag telling if only manual trigger should be used, overriding any other trigger mechanism
			/// </summary>
			public bool				UseOnlyManualTrigger	{ get { return m_bUseOnlyManualTrigger; } set { m_bUseOnlyManualTrigger = value; } }

			/// <summary>
			/// Occurs when the light is being projected to the screen so you have a chance to update the FlareSize and ManualTrigger parameters
			/// </summary>
			public event LightProjectingEventHandler	OnLightProjecting;

			#endregion

			#region METHODS

			/// <param name="_Light">The light that will drive the lens-flare</param>
			/// <param name="_LightIntensityScale">An optional 1x1 source image that can be accessed to dynamically measure the light's intensity (RGB) and scale (Alpha). If none is provided then a white and unit scale light is used.</param>
			public Light( PointLight _Light, ITexture2D _LightIntensityScale )
			{
				m_Light = _Light;
				m_LightIntensityScale = _LightIntensityScale;
			}

			/// <summary>
			/// Projects the light in 2D
			/// </summary>
			/// <param name="_Camera">The camera to project to</param>
			public void				ProjectLight( Camera _Camera )
			{
				m_FlarePosition = _Camera.ProjectPoint( m_Light.Position );
				m_FlarePosition.Y = -m_FlarePosition.Y;
				m_FlarePosition = 50.0f * (Vector2.One + m_FlarePosition);

				// Notify
				if ( OnLightProjecting != null )
					OnLightProjecting( this );
			}

			#endregion
		}

		public class	LensFlareDisplay : Component
		{
			#region FIELDS

			protected RenderTechniquePostProcessLensFlares<PF>	m_Owner = null;
			protected LensFlare				m_Source = null;

			// Global parameters
			protected float					m_GlobalBrightness = 1.0f;		// Manual overall brightness
			protected float					m_GlobalScale = 1.0f;			// Manual overall scale
			protected float					m_GlobalAspectRatio = 1.0f;
			protected Vector3				m_GlobalColor = Vector3.One;
			protected float					m_GlobalSeed = 5000.0f;
			protected bool					m_bScreenMode = false;
			protected bool					m_bUseLensTexture = false;
			protected ITexture2D			m_LensTexture = null;

			// The list of lens-objects for that lens-flare
			internal List<LensFlare.LensObjectGlow>			m_Glows = new List<LensFlare.LensObjectGlow>();
			internal List<LensFlare.LensObjectGlint>		m_Glints = new List<LensFlare.LensObjectGlint>();
			internal List<LensFlare.LensObjectSparkle>		m_Sparkles = new List<LensFlare.LensObjectSparkle>();
			internal List<LensFlare.LensObjectSpikeBall>	m_SpikeBalls = new List<LensFlare.LensObjectSpikeBall>();
			internal List<LensFlare.LensObjectRing>			m_Rings = new List<LensFlare.LensObjectRing>();
			internal List<LensFlare.LensObjectHoop>			m_Hoops = new List<LensFlare.LensObjectHoop>();
			internal List<LensFlare.LensObjectShimmer>		m_Shimmers = new List<LensFlare.LensObjectShimmer>();
			internal List<LensFlare.LensObjectStreak>		m_Streaks = new List<LensFlare.LensObjectStreak>();

				// Irises can have textures
			internal List<LensFlare.LensObjectIris>			m_Irises = new List<LensFlare.LensObjectIris>();
			internal List<ITexture2D>						m_IrisTextures = new List<ITexture2D>();

			// The list of lights that will be rendered with that lens flare
			internal List<Light>			m_Lights = new List<Light>();

// DEBUG
public float		Param0 = 0.0f;
public float		Param1 = 0.0f;
public float		Param2 = 0.0f;
public float		Param3 = 0.0f;
public float		Param4 = 0.0f;
public float		Param5 = 0.0f;
public float		Param6 = 0.0f;
public float		Param7 = 0.0f;
public float		Param8 = 0.0f;
public float		Param9 = 0.0f;
// DEBUG

			#endregion

			#region PROPERTIES

			/// <summary>
			/// Gets the source lens flare this object is mimicing
			/// </summary>
			public LensFlare	Source				{ get { return m_Source; } }

			/// <summary>
			/// Gets or sets the overall brightness
			/// </summary>
			public float		GlobalBrightness	{ get { return m_GlobalBrightness; } set { m_GlobalBrightness = value; } }

			/// <summary>
			/// Gets or sets the overall scale
			/// </summary>
			public float		GlobalScale			{ get { return m_GlobalScale; } set { m_GlobalScale = value; } }

			/// <summary>
			/// Gets or sets the overall aspect ratio
			/// </summary>
			public float		GlobalAspectRatio	{ get { return m_GlobalAspectRatio; } set { m_GlobalAspectRatio = value; } }

			/// <summary>
			/// Gets or sets the global color
			/// </summary>
			public Vector3		GlobalColor			{ get { return m_GlobalColor; } set { m_GlobalColor = value; } }

			/// <summary>
			/// Gets or sets the global seed
			/// </summary>
			public float		GlobalSeed			{ get { return m_GlobalSeed; } set { m_GlobalSeed = value; } }

			#endregion

			#region METHODS

			/// <summary>
			/// Builds the lens-flare display from a lens-flare source
			/// </summary>
			/// <param name="_Source"></param>
			public LensFlareDisplay( RenderTechniquePostProcessLensFlares<PF> _Owner, string _Name, LensFlare _Source ) : base( _Owner.Device, _Name )
			{
				m_Owner = _Owner;
				m_Source = _Source;

				// 1] Store global parameters
				m_GlobalScale = 0.01f * _Source.Parameters.GlobalScale;
				m_GlobalAspectRatio = _Source.Parameters.AspectRatio;
				m_GlobalSeed = _Source.Parameters.GlobalSeed;
				m_GlobalColor = new Vector3( _Source.Parameters.GlobalColor.R, _Source.Parameters.GlobalColor.G, _Source.Parameters.GlobalColor.B ) / 255.0f;
				m_bScreenMode = _Source.Parameters.BlendMode == LensFlare.GlobalParameters.BLEND_MODE.SCREEN;

				m_bUseLensTexture = m_Source.Parameters.TextureImage.Value > 0;
				if ( m_bUseLensTexture )
					m_LensTexture = m_Owner.QueryTexture( false, m_Source.Parameters.TextureImage.Name );

				// 2] Add lens-objects
				bool	bIsSoloMode = false;
				foreach ( LensFlare.LensObject LO in _Source.LensObjects )
					if ( LO.Solo )
					{
						bIsSoloMode = true;
						break;
					}

				foreach ( LensFlare.LensObject LO in _Source.LensObjects )
					if ( !LO.Hide && (!bIsSoloMode || LO.Solo) )
					{
						if ( LO is LensFlare.LensObjectGlow )
						{
							m_Glows.Add( LO as LensFlare.LensObjectGlow );
#if EFFECT_DEBUG
// To test glow parameters
_LensFlare.Param0 = LO.Gamma;
_LensFlare.Param1 = LO.Scale;
#endif
						}
						else if ( LO is LensFlare.LensObjectRing )
						{
							m_Rings.Add( LO as LensFlare.LensObjectRing );
						}
						else if ( LO is LensFlare.LensObjectHoop )
						{
							m_Hoops.Add( LO as LensFlare.LensObjectHoop );

#if EFFECT_DEBUG
// To test Hoop shape parameters
_LensFlare.Param0 = LO.Complexity;
_LensFlare.Param1 = LO.Detail;
_LensFlare.Param2 = LO.Length;
_LensFlare.Param3 = LO.Completion;
_LensFlare.Param4 = LO.CompletionFeathering;
_LensFlare.Param5 = LO.CompletionRotation;
_LensFlare.Param6 = LO.Distance;
#endif
						}
						else if ( LO is LensFlare.LensObjectSparkle )
						{
							m_Sparkles.Add( LO as LensFlare.LensObjectSparkle );
#if EFFECT_DEBUG
// To test sparkle parameters
_LensFlare.Param0 = LO.Complexity;
_LensFlare.Param1 = LO.Length;
_LensFlare.Param2 = LO.LengthRandom;
_LensFlare.Param3 = LO.Thickness;
_LensFlare.Param4 = LO.ThicknessRandom;
_LensFlare.Param5 = LO.BrightnessRandom;
_LensFlare.Param6 = LO.Spread;
_LensFlare.Param7 = LO.SpreadRandom;
_LensFlare.Param8 = LO.SpacingRandom;
_LensFlare.Param9 = LO.ShapeOrientation;
#endif
						}
						else if ( LO is LensFlare.LensObjectShimmer )
						{
							m_Shimmers.Add( LO as LensFlare.LensObjectShimmer );

#if EFFECT_DEBUG
// To test shimmer parameters
_LensFlare.Param0 = LO.Complexity;
_LensFlare.Param1 = LO.Detail;
_LensFlare.Param2 = LO.ShapeOrientation;
_LensFlare.Param3 = LO.Completion;
_LensFlare.Param4 = LO.CompletionFeathering;
_LensFlare.Param5 = LO.CompletionRotation;
_LensFlare.Param6 = LO.Scale;
#endif
						}
						else if ( LO is LensFlare.LensObjectSpikeBall )
						{
							m_SpikeBalls.Add( LO as LensFlare.LensObjectSpikeBall );

#if EFFECT_DEBUG
// To test spike ball parameters
_LensFlare.Param0 = LO.Complexity;
_LensFlare.Param1 = LO.Length;
_LensFlare.Param2 = LO.LengthRandom;
_LensFlare.Param3 = LO.Thickness;
_LensFlare.Param4 = LO.ThicknessRandom;
_LensFlare.Param5 = LO.BrightnessRandom;
_LensFlare.Param6 = LO.SpacingRandom;
_LensFlare.Param7 = LO.ShapeOrientation;
_LensFlare.Param8 = LO.Scale;
#endif
						}
						else if ( LO is LensFlare.LensObjectGlint )
						{
							m_Glints.Add( LO as LensFlare.LensObjectGlint );

#if EFFECT_DEBUG
// To test glint parameters
_LensFlare.Param0 = LO.Complexity;
_LensFlare.Param1 = LO.Length;
_LensFlare.Param2 = LO.LengthRandom;
_LensFlare.Param3 = LO.Thickness;
_LensFlare.Param4 = LO.SpacingRandom;
_LensFlare.Param5 = LO.ShapeOrientation;
_LensFlare.Param6 = LO.Completion;
_LensFlare.Param7 = LO.CompletionFeathering;
_LensFlare.Param8 = LO.CompletionRotation;
_LensFlare.Param9 = LO.Scale;
#endif
						}
						else if ( LO is LensFlare.LensObjectIris )
						{
							LensFlare.LensObjectIris	Iris = LO as LensFlare.LensObjectIris;
							m_Irises.Add( Iris );
							m_IrisTextures.Add( Iris.ShapeType == LensFlare.LensObject.SHAPE_TYPE.TEXTURE ? m_Owner.QueryTexture( true, Iris.TextureName.Name ) : null );

#if EFFECT_DEBUG
// To test Multi-Iris shape parameters
_LensFlare.Param1 = LO.PolygonSides;
_LensFlare.Param2 = LO.PolygonRoundness;
_LensFlare.Param3 = LO.BladeNotching;
_LensFlare.Param4 = LO.Smoothness;
_LensFlare.Param5 = LO.OutlineIntensity;
_LensFlare.Param6 = LO.OutlineThickness;
_LensFlare.Param7 = LO.OutlineFeathering;
_LensFlare.Param8 = LO.Completion;
_LensFlare.Param9 = LO.CompletionFeathering;
#endif
						}
						else if ( LO is LensFlare.LensObjectStreak )
						{
							m_Streaks.Add( LO as LensFlare.LensObjectStreak );

#if EFFECT_DEBUG
// To test streak shape parameters
_LensFlare.Param0 = LO.Length;
_LensFlare.Param1 = LO.Thickness;
_LensFlare.Param2 = LO.CoreIntensity;
_LensFlare.Param3 = LO.Symmetry;
_LensFlare.Param4 = LO.FanEnds;
_LensFlare.Param5 = LO.FanFeathering;
_LensFlare.Param6 = LO.ReplicatorCopies;
_LensFlare.Param7 = LO.ReplicatorAngle;
_LensFlare.Param8 = LO.ScaleRandom;
_LensFlare.Param9 = LO.SpacingRandom;
// Streak.ColorSource = LensFlare.LensObject.COLOR_SOURCE.SPECTRUM;
#endif
						}
					}
			}

			public override string ToString()
			{
				return Name;
			}

			/// <summary>
			/// Attaches a light to the lens-flare
			/// </summary>
			/// <param name="_Light"></param>
			public Light	AttachLight( PointLight _Light )
			{
				return AttachLight( _Light, null );
			}

			/// <summary>
			/// Attaches a lens-flare to a light
			/// </summary>
			/// <param name="_Light">The light that will drive the lens-flare</param>
			/// <param name="_LightIntensityScale">An optional 1x1 source image that can be accessed to dynamically measure the light's intensity (RGB) and scale (Alpha). If none is provided then a white and unit scale light is used.</param>
			public Light	AttachLight( PointLight _Light, ITexture2D _LightIntensityScale )
			{
				Light	L = new Light( _Light, _LightIntensityScale );
				m_Lights.Add( L );

				return L;
			}

			/// <summary>
			/// Detaches a light from the lens-flare
			/// </summary>
			/// <param name="_Light"></param>
			public void		DetachLight( Light _Light )
			{
				m_Lights.Remove( _Light );
			}

			public void		Render()
			{
				if ( m_Lights.Count == 0 )
					return;	// No need to bother...

				// Project lights
				foreach ( Light L in m_Lights )
					L.ProjectLight( m_Owner.Camera );

#if DEBUG
				// Add profiling task
				if ( m_Device.HasProfilingStarted )
					m_Device.AddProfileTask( this, "Render", m_Lights.Count + " Lights" );
#endif

				// Render in the temp target
				m_Device.SetRenderTarget( m_Owner.m_LensFlareTarget );
				m_Device.SetViewport( 0, 0, m_Owner.m_LensFlareTarget.Width, m_Owner.m_LensFlareTarget.Height, 0.0f, 1.0f );
				if ( m_bScreenMode )
					m_Device.ClearRenderTarget( m_Owner.m_LensFlareTarget, new Vector4( 1.0f, 1.0f, 1.0f, 0.0f ) );
				else
					m_Device.ClearRenderTarget( m_Owner.m_LensFlareTarget, Vector4.Zero );

				// Lens objects all use point lists
				m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
				m_Owner.m_PointVB.Use();

				//////////////////////////////////////////////////////////////////////////
				// Render effects
				if ( m_bScreenMode )
					m_Device.OutputMerger.SetBlendState( m_Owner.m_BlendScreenRGB, new Color4( 0.0f ), -1 );
				else
					m_Device.OutputMerger.SetBlendState( m_Owner.m_BlendAdditiveRGB, new Color4( 0.0f ), -1 );

#if USE_GENERIC
				m_Owner.m_MaterialGeneric.Render( this );
#endif
#if USE_IRIS
				m_Owner.m_MaterialIris.Render( this );
#endif
#if USE_STREAK
				m_Owner.m_MaterialStreak.Render( this );
#endif
#if USE_SHIMMER
				m_Owner.m_MaterialShimmer.Render( this );
#endif
#if USE_SPIKEBALL
				m_Owner.m_MaterialSpikeBall.Render( this );
#endif
#if USE_GLINT
				m_Owner.m_MaterialGlint.Render( this );
#endif

				//////////////////////////////////////////////////////////////////////////
				// Blend with previous target
				using ( m_Owner.m_MaterialBlend.UseLock() )
				{
					m_Owner.m_vSourceTexture.SetResource( m_Owner.m_LensFlareTarget );

					m_Owner.m_vScreenMode.Set( m_bScreenMode );
					m_Owner.m_vAberrationType.Set( (int) m_Source.Parameters.AberrationType );
					m_Owner.m_vAberrationIntensity.Set( 0.01f * m_Source.Parameters.AberrationIntensity );
					m_Owner.m_vAberrationSpread.Set( 0.01f * m_Source.Parameters.AberrationSpread );
					m_Owner.m_vCCBrightness.Set( 0.01f * m_Source.Parameters.CorrectionBrightness );
					m_Owner.m_vCCContrast.Set( 0.01f * m_Source.Parameters.CorrectionContrast );
					m_Owner.m_vCCSaturation.Set( 0.01f * m_Source.Parameters.CorrectionSaturation );

					// Render lights in alpha for lens texture mixing
					m_Owner.m_vUseLensTexture.Set( m_bUseLensTexture );
					if ( m_bUseLensTexture )
					{
						m_Owner.m_MaterialBlend.CurrentTechnique = m_Owner.m_MaterialBlend.GetTechniqueByName( "DisplayLightsAlpha" );
						m_Device.OutputMerger.SetBlendState( m_Owner.m_BlendAdditiveAlpha, new Color4( 0.0f ), -1 );

						m_Owner.m_vLensTexture.SetResource( m_LensTexture );
						m_Owner.m_vIlluminationRadius.Set( m_Source.Parameters.TextureIlluminationRadius );
						m_Owner.m_vFallOff.Set( m_Source.Parameters.TextureFallOff );
						m_Owner.m_vBrightness.Set( m_GlobalBrightness * (0.01f * m_Source.Parameters.TextureBrightness) );
						m_Owner.m_vScale.Set( new Vector2( 0.01f * m_Source.Parameters.TextureScale.X, 0.01f * m_Source.Parameters.TextureScale.Y ) );
						m_Owner.m_vOffset.Set( new Vector2( m_Source.Parameters.TextureOffset.X,  m_Source.Parameters.TextureOffset.Y ) );

						foreach ( Light L in m_Lights )
						{
							m_Owner.m_vLightPosition.Set( L.FlarePosition );
							m_Owner.m_MaterialBlend.ApplyPass( 0 );
							m_Owner.DrawFullscreenQuad();
						}
					}

					// Blend lens flares with background
					m_Owner.m_MaterialBlend.CurrentTechnique = m_Owner.m_MaterialBlend.GetTechniqueByName( "DisplayBlend" );

					if ( m_Owner.SetRenderTarget != null )
						m_Owner.SetRenderTarget();
					else
					{
						m_Device.SetRenderTarget( m_Owner.m_TargetImage );
						m_Device.SetViewport( 0, 0, m_Owner.m_TargetImage.Width, m_Owner.m_TargetImage.Height, 0.0f, 1.0f );
					}

					m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE );
					m_Owner.m_MaterialBlend.ApplyPass( 0 );
					m_Owner.DrawFullscreenQuad();
				}
			}

			#endregion
		}

		protected abstract class	LensObjectsMaterial : Component
		{
			#region FIELDS

			protected RenderTechniquePostProcessLensFlares<PF>	m_Owner = null;

			// Global variables
			protected VariableScalar		m_vTime = null;
			protected VariableVector		m_vInvSourceSize = null;
			protected VariableScalar		m_vScreenAspectRatio = null;
			protected VariableScalar		m_vRandomSeed = null;
			protected VariableScalar		m_vScreenMode = null;
			protected VariableScalar		m_vBrightness = null;
			protected VariableScalar		m_vGlobalBrightnessFactor = null;
			protected VariableVector		m_vGlobalColor = null;
			protected VariableScalar		m_vGlobalSeed = null;

			// Per light variables
			protected VariableVector		m_vLightPosition2D = null;
			protected VariableScalar		m_vLightSize = null;
			protected VariableScalar		m_vLightManualTrigger = null;
			protected VariableResource		m_vLightIntensityScale = null;

			// Transform
			protected VariableScalar		m_vScale = null;
			protected VariableVector		m_vStretch = null;
			protected VariableVector		m_vDistance = null;
			protected VariableScalar		m_vRotation = null;
			protected VariableScalar		m_vAutoRotateMode = null;
			protected VariableVector		m_vOffset = null;
			protected VariableScalar		m_vTranslationMode = null;
			protected VariableVector		m_vCustomTranslation = null;
			protected VariableScalar		m_vAspectRatio = null;

			// Colorize
			protected VariableScalar		m_vColorSource = null;
			protected VariableVector		m_vColor1 = null;
			protected VariableVector		m_vColor2 = null;
			protected VariableScalar		m_vGradientLoops = null;
			protected VariableScalar		m_vGradientOffset = null;
			protected VariableScalar		m_vReverseGradient = null;

			// Dynamic triggering
			protected VariableScalar		m_vEnableTrigger = null;
			protected VariableScalar		m_vBrightnessOffset = null;
			protected VariableScalar		m_vScaleOffset = null;
			protected VariableVector		m_vStretchOffset = null;
			protected VariableScalar		m_vRotationOffset = null;
			protected VariableScalar		m_vCompletionOffset = null;
			protected VariableVector		m_vColorShift = null;
			protected VariableScalar		m_vTriggerType = null;
			protected VariableScalar		m_vTriggerMode = null;
			protected VariableScalar		m_vInvertTrigger = null;
			protected VariableScalar		m_vBorderWidth = null;
			protected VariableScalar		m_vExpansion = null;
			protected VariableScalar		m_vInnerFallOffRange = null;
			protected VariableScalar		m_vOuterFallOffRange = null;
			protected VariableScalar		m_vFallOffType = null;
			protected VariableVector		m_vTriggerStretch = null;
			protected VariableVector		m_vTriggerOffset = null;

			// Circular completion
			protected VariableScalar		m_vCompletion = null;
			protected VariableScalar		m_vCompletionFeathering = null;
			protected VariableScalar		m_vCompletionRotation = null;
			protected VariableScalar		m_vAutoRotateModeCompletion = null;

			// Animation
			protected VariableScalar		m_vAnimSpeed = null;
			protected VariableScalar		m_vAnimAmount = null;

			#endregion

			#region PROPERTIES

			protected abstract IMaterial	CurrentMaterial	{ get; }

			#endregion

			#region METHODS

			public	LensObjectsMaterial( RenderTechniquePostProcessLensFlares<PF> _Owner ) : base( _Owner.Device, "LensObjectMaterial" )
			{
				m_Owner = _Owner;
			}

			/// <summary>
			/// Renders a single lens-flare
			/// </summary>
			public abstract void	Render( LensFlareDisplay _LensFlare );

			protected virtual void	ApplyPerMaterialVariables( LensFlareDisplay _LensFlare )
			{
				m_vTime.Set( m_Owner.Time );
				m_vInvSourceSize.Set( m_Owner.m_LensFlareTarget.InvSize2 );
				m_vScreenAspectRatio.Set( m_Owner.m_LensFlareTarget.AspectRatio );
				m_vGlobalColor.Set( _LensFlare.GlobalColor );
				m_vGlobalSeed.Set( _LensFlare.GlobalSeed );
			}

			protected virtual void	ApplyPerLightVariables( Light _Light )
			{
				m_vLightPosition2D.Set( _Light.FlarePosition );
				m_vLightSize.Set( _Light.FlareSize );
				m_vLightManualTrigger.Set( (_Light.UseOnlyManualTrigger ? -1.0f : 1.0f ) * _Light.ManualTrigger );
				m_vLightIntensityScale.SetResource( _Light.LightIntensityScale != null ? _Light.LightIntensityScale : m_Owner.m_DefaultLightIntensityScale );
			}

			/// <summary>
			/// Applies the common lens-object variables
			/// </summary>
			/// <param name="_LO"></param>
			protected void			ApplyPerObjectVariablesCommon( LensFlareDisplay _LensFlare, LensFlare.LensObject _LO )
			{
				// Common settings
#if !EFFECT_DEBUG
				m_vBrightness.Set( 0.01f * _LO.Brightness );
#else
				m_vBrightness.Set( 1.0f );
#endif
				m_vGlobalBrightnessFactor.Set( _LensFlare.GlobalBrightness );
				m_vScale.Set( 0.01f * Math.Max( 1.0f, _LO.Scale ) * _LensFlare.GlobalScale );
				m_vStretch.Set( new Vector2( 0.01f * Math.Max( 0.1f, _LO.Stretch.X ), 0.01f * Math.Max( 0.1f, _LO.Stretch.Y ) ) );
				m_vDistance.Set( new Vector2( 0.01f * _LO.Distance.X, 0.01f * _LO.Distance.Y ) );
				m_vRotation.Set( ToRadians( _LO.Rotation ) );
				m_vAutoRotateMode.Set( (int) _LO.AutoRotate );
				m_vOffset.Set( new Vector2( _LO.Offset.X, _LO.Offset.Y ) );
				m_vTranslationMode.Set( (int) _LO.Translation );
				m_vCustomTranslation.Set( new Vector2( 0.01f * _LO.CustomTranslation.X, 0.01f * _LO.CustomTranslation.Y ) );
				m_vAspectRatio.Set( _LO.AspectRatio * _LensFlare.GlobalAspectRatio );

				// Colorize
				m_vColorSource.Set( (int) _LO.ColorSource );
				m_vColor1.Set( new Vector3( _LO.Color1.R, _LO.Color1.G, _LO.Color1.B ) / 255.0f );
				m_vColor2.Set( new Vector3( _LO.Color2.R, _LO.Color2.G, _LO.Color2.B ) / 255.0f );
				m_vGradientLoops.Set( _LO.GradientLoops );
				m_vGradientOffset.Set( ToRadians( _LO.GradientOffset ) );
				m_vReverseGradient.Set( _LO.ReverseGradient );

				// Dynamic triggering
				m_vEnableTrigger.Set( _LO.EnableTrigger );
				if ( !_LO.EnableTrigger )
					return;

				m_vBrightnessOffset.Set( 0.01f * _LO.BrightnessOffset );
				m_vScaleOffset.Set( 0.01f * _LO.ScaleOffset );
				m_vStretchOffset.Set( new Vector2( 0.01f * _LO.StretchOffset.X, 0.01f * _LO.StretchOffset.Y ) );
				m_vRotationOffset.Set( ToRadians( _LO.RotationOffset ) );
				m_vColorShift.Set( !_LO.ColorShift.Bool ? Vector3.One : new Vector3( _LO.ColorShift.Color.R, _LO.ColorShift.Color.G, _LO.ColorShift.Color.B ) / 255.0f );
				m_vTriggerType.Set( (int) _LO.TriggerType );
				m_vTriggerMode.Set( (int) _LO.TriggerMode );
				m_vInvertTrigger.Set( _LO.InvertTrigger );
				m_vBorderWidth.Set( _LO.BorderWidth );
				m_vExpansion.Set( _LO.Expansion );
				m_vInnerFallOffRange.Set( _LO.InnerFalloffRange );
				m_vOuterFallOffRange.Set( _LO.OuterFalloffRange );
				m_vFallOffType.Set( (int) _LO.FalloffType );
				m_vTriggerStretch.Set( new Vector2( 0.01f * Math.Max( 0.01f, _LO.TriggerStretch.X ), 0.01f * Math.Max( 0.01f, _LO.TriggerStretch.Y ) ) );
				m_vTriggerOffset.Set( new Vector2( _LO.OffsetTriggerLocation.X, _LO.OffsetTriggerLocation.Y ) );
			}

			protected virtual void	GetShaderVariables()
			{
				// Global variables
				m_vTime = CurrentMaterial.GetVariableByName( "Time" ).AsScalar;
				m_vInvSourceSize = CurrentMaterial.GetVariableByName( "InvSourceSize" ).AsVector;
				m_vScreenAspectRatio = CurrentMaterial.GetVariableByName( "ScreenAspectRatio" ).AsScalar;
				m_vRandomSeed = CurrentMaterial.GetVariableByName( "RandomSeed" ).AsScalar;
				m_vBrightness = CurrentMaterial.GetVariableByName( "Brightness" ).AsScalar;
				m_vGlobalBrightnessFactor = CurrentMaterial.GetVariableByName( "GlobalBrightnessFactor" ).AsScalar;
				m_vGlobalColor = CurrentMaterial.GetVariableByName( "GlobalColor" ).AsVector;
				m_vGlobalSeed = CurrentMaterial.GetVariableByName( "GlobalSeed" ).AsScalar;

				// Per light variables
				m_vLightPosition2D = CurrentMaterial.GetVariableByName( "LightPosition" ).AsVector;
				m_vLightSize = CurrentMaterial.GetVariableByName( "LightSize" ).AsScalar;
				m_vLightManualTrigger = CurrentMaterial.GetVariableByName( "LightManualTrigger" ).AsScalar;
				m_vLightIntensityScale = CurrentMaterial.GetVariableByName( "LightIntensityScale" ).AsResource;

				// Common Settings
				m_vScale = CurrentMaterial.GetVariableByName( "Scale" ).AsScalar;
				m_vStretch = CurrentMaterial.GetVariableByName( "Stretch" ).AsVector;
				m_vDistance = CurrentMaterial.GetVariableByName( "Distance" ).AsVector;
				m_vRotation = CurrentMaterial.GetVariableByName( "Rotation" ).AsScalar;
				m_vAutoRotateMode = CurrentMaterial.GetVariableByName( "AutoRotateMode" ).AsScalar;
				m_vOffset = CurrentMaterial.GetVariableByName( "Offset" ).AsVector;
				m_vTranslationMode = CurrentMaterial.GetVariableByName( "TranslationMode" ).AsScalar;
				m_vCustomTranslation = CurrentMaterial.GetVariableByName( "CustomTranslation" ).AsVector;
				m_vAspectRatio = CurrentMaterial.GetVariableByName( "AspectRatio" ).AsScalar;

				// Colorize
				m_vColorSource = CurrentMaterial.GetVariableByName( "ColorSource" ).AsScalar;
				m_vColor1 = CurrentMaterial.GetVariableByName( "Color1" ).AsVector;
				m_vColor2 = CurrentMaterial.GetVariableByName( "Color2" ).AsVector;
				m_vGradientLoops = CurrentMaterial.GetVariableByName( "GradientLoops" ).AsScalar;
				m_vGradientOffset = CurrentMaterial.GetVariableByName( "GradientOffset" ).AsScalar;
				m_vReverseGradient = CurrentMaterial.GetVariableByName( "bReverseGradient" ).AsScalar;

				// Dynamic triggering
				m_vEnableTrigger = CurrentMaterial.GetVariableByName( "bEnableTrigger" ).AsScalar;
				m_vBrightnessOffset = CurrentMaterial.GetVariableByName( "BrightnessOffset" ).AsScalar;
				m_vScaleOffset = CurrentMaterial.GetVariableByName( "ScaleOffset" ).AsScalar;
				m_vStretchOffset = CurrentMaterial.GetVariableByName( "StretchOffset" ).AsVector;
				m_vRotationOffset = CurrentMaterial.GetVariableByName( "RotationOffset" ).AsScalar;
				m_vCompletionOffset = CurrentMaterial.GetVariableByName( "CompletionOffset" ).AsScalar;
				m_vColorShift = CurrentMaterial.GetVariableByName( "ColorShift" ).AsVector;
				m_vTriggerType = CurrentMaterial.GetVariableByName( "TriggerType" ).AsScalar;
				m_vTriggerMode = CurrentMaterial.GetVariableByName( "TriggerMode" ).AsScalar;
				m_vInvertTrigger = CurrentMaterial.GetVariableByName( "bInvertTrigger" ).AsScalar;
				m_vBorderWidth = CurrentMaterial.GetVariableByName( "BorderWidth" ).AsScalar;
				m_vExpansion = CurrentMaterial.GetVariableByName( "Expansion" ).AsScalar;
				m_vInnerFallOffRange = CurrentMaterial.GetVariableByName( "InnerFallOffRange" ).AsScalar;
				m_vOuterFallOffRange = CurrentMaterial.GetVariableByName( "OuterFallOffRange" ).AsScalar;
				m_vFallOffType = CurrentMaterial.GetVariableByName( "FallOffType" ).AsScalar;
				m_vTriggerStretch = CurrentMaterial.GetVariableByName( "TriggerStretch" ).AsVector;
				m_vTriggerOffset = CurrentMaterial.GetVariableByName( "TriggerOffset" ).AsVector;

				// Circular completion
				m_vCompletion = CurrentMaterial.GetVariableByName( "Completion" ).AsScalar;
				m_vCompletionFeathering = CurrentMaterial.GetVariableByName( "CompletionFeathering" ).AsScalar;
				m_vCompletionRotation = CurrentMaterial.GetVariableByName( "CompletionRotation" ).AsScalar;
				m_vAutoRotateModeCompletion = CurrentMaterial.GetVariableByName( "AutoRotateModeCompletion" ).AsScalar;

				// Animation
				m_vAnimSpeed = CurrentMaterial.GetVariableByName( "AnimSpeed" ).AsScalar;
				m_vAnimAmount = CurrentMaterial.GetVariableByName( "AnimAmount" ).AsScalar;

				// Setup global constants
				CurrentMaterial.GetVariableByName( "TextureRandom" ).AsResource.SetResource( m_Owner.m_TextureRandom );
			}

			#region Helpers

			protected float		ToRadians( float _Degrees )
			{
				return _Degrees * (float) Math.PI / 180.0f;
			}

			protected float		Lerp( float a, float b, float t )
			{
				return a + (b-a) * t;
			}

			protected static Vector3	RGB2HSL( Vector3 c )
			{ 
				Vector3 hsl = new Vector3(); 
          
				float Max, Min, Diff, Sum;

				//	Of our RGB values, assign the highest value to Max, and the Smallest to Min
				if ( c.X > c.Y )	{ Max = c.X; Min = c.Y; }
				else				{ Max = c.Y; Min = c.X; }
				if ( c.Z > Max )	  Max = c.Z;
				else if ( c.Z < Min ) Min = c.Z;

				Diff = Max - Min;
				Sum = Max + Min;

				//	Luminance - a.k.a. Brightness - Adobe photoshop uses the logic that the
				//	site VBspeed regards (regarded) as too primitive = superior decides the 
				//	level of brightness.
				hsl.Z = Max;
	//			hsl.Z = 0.5f * (Min + Max);

				//	Saturation
				if ( Max == 0 ) hsl.Y = 0;		//	Protecting from the impossible operation of division by zero.
				else hsl.Y = Diff/Max;			//	The logic of Adobe Photoshops is this simple.

				//	Hue		R is situated at the angel of 360 eller noll degrees; 
				//			G vid 120 degrees
				//			B vid 240 degrees
				float q;
				if ( Diff == 0 ) q = 0; // Protecting from the impossible operation of division by zero.
				else q = (float)60/Diff;
			
				if ( Max == c.X )
				{
					if ( c.Y < c.Z )	hsl.X = (360 + q * (c.Y - c.Z))/360.0f;
					else				hsl.X = (q * (c.Y - c.Z))/360.0f;
				}
				else if ( Max == c.Y )	hsl.X = (120 + q * (c.Z - c.X))/360.0f;
				else if ( Max == c.Z )	hsl.X = (240 + q * (c.X - c.Y))/360.0f;
				else					hsl.X = 0.0f;

				return hsl; 
			} 

			protected static Vector3	HSL2RGB( Vector3 hsl )
			{
				float	Max, Mid, Min;
				double q;

				Max = (float) hsl.Z;
				Min = (float) ((1.0 - hsl.Y) * hsl.Z);
				q   = (double)(Max - Min);

				if ( hsl.X >= 0 && hsl.X <= 1.0/6.0 )
				{
					Mid = (float) (((hsl.X - 0) * q) * 6 + Min);
					return new Vector3( Max,Mid,Min );
				}
				else if ( hsl.X <= 1.0/3.0 )
				{
					Mid = (float) (-((hsl.X - 1.0/6.0) * q) * 6 + Max);
					return new Vector3( Mid,Max,Min);
				}
				else if ( hsl.X <= 0.5 )
				{
					Mid = (float) (((hsl.X - 1.0/3.0) * q) * 6 + Min);
					return new Vector3( Min,Max,Mid);
				}
				else if ( hsl.X <= 2.0/3.0 )
				{
					Mid = (float) (-((hsl.X - 0.5) * q) * 6 + Max);
					return new Vector3( Min,Mid,Max);
				}
				else if ( hsl.X <= 5.0/6.0 )
				{
					Mid = (float) (((hsl.X - 2.0/3.0) * q) * 6 + Min);
					return new Vector3( Mid,Min,Max);
				}
				else if ( hsl.X <= 1.0 )
				{
					Mid = (float) (-((hsl.X - 5.0/6.0) * q) * 6 + Max);
					return new Vector3( Max,Min,Mid);
				}
				else
					return new Vector3( 0,0,0);
			} 

			#endregion

			#endregion
		}

		/// <summary>
		/// Generic effects (i.e. rings, hoops, glow, sparkle)
		/// </summary>
		protected class		LensObjectsMaterial_Generic : LensObjectsMaterial
		{
			#region FIELDS

			protected Material<VS_T2>					m_Material = null;
			protected EffectTechnique					m_TechniqueRing = null;
			protected EffectPass						m_PassRing = null;
			protected EffectTechnique					m_TechniqueHoop = null;
			protected EffectPass						m_PassHoop = null;
			protected EffectTechnique					m_TechniqueGlow = null;
			protected EffectPass						m_PassGlow = null;
			protected EffectTechnique					m_TechniqueSparkle = null;
			protected EffectPass						m_PassSparkle = null;

			// Effect-specific variables
				// For rings
			protected VariableScalar					m_vThickness = null;
			protected VariableScalar					m_vInsideFeathering = null;
			protected VariableScalar					m_vOutsideFeathering = null;
			protected VariableScalar					m_vThicknessOffset = null;

				// For hoops
			protected VariableScalar					m_vComplexity = null;
			protected VariableScalar					m_vDetail = null;
			protected VariableScalar					m_vLength = null;

				// For glows
			protected VariableScalar					m_vGamma = null;

				// For glints
			protected VariableScalar					m_vLengthRandom = null;
			protected VariableScalar					m_vSpacingRandom = null;
			protected VariableScalar					m_vShapeOrientation = null;

				// For sparkles
			protected VariableScalar					m_vThicknessRandom = null;
			protected VariableScalar					m_vBrightnessRandom = null;
			protected VariableScalar					m_vSpread = null;
			protected VariableScalar					m_vSpreadRandom = null;
			protected VariableScalar					m_vSpreadOffset = null;

			#endregion

			#region PROPERTIES

			protected override IMaterial CurrentMaterial
			{
				get { return m_Material; }
			}

			#endregion

			#region METHODS

			public LensObjectsMaterial_Generic( RenderTechniquePostProcessLensFlares<PF> _Owner ) : base( _Owner )
			{
				m_Material = ToDispose( new Material<VS_T2>( m_Device, "Lens-Flare Generic", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Utility/LensFlares/PostProcessLensFlareGeneric.fx" ) ) );
				m_Material.EffectRecompiled += new EventHandler( Material_EffectRecompiled );

				GetShaderVariables();
			}

			public override void Render( LensFlareDisplay _LensFlare )
			{
				if ( _LensFlare.m_Rings.Count == 0 && _LensFlare.m_Hoops.Count == 0 && _LensFlare.m_Glows.Count == 0 && _LensFlare.m_Sparkles.Count == 0 )
					return;

				using ( m_Material.UseLock() )
				{
					ApplyPerMaterialVariables( _LensFlare );

					// Render rings
					if ( _LensFlare.m_Rings.Count > 0 )
					{
						m_Material.CurrentTechnique = m_TechniqueRing;
						foreach ( LensFlare.LensObjectRing LO in _LensFlare.m_Rings )
						{
							ApplyPerObjectVariablesCommon( _LensFlare, LO );
							ApplyPerObjectVariablesRing( LO );

							foreach ( Light L in _LensFlare.m_Lights )
							{
								ApplyPerLightVariables( L );
								m_PassRing.Apply();
								m_Owner.DrawPoints( 1 );
							}
						}
					}

					// Render hoops
					if ( _LensFlare.m_Hoops.Count > 0 )
					{
						m_Material.CurrentTechnique = m_TechniqueHoop;
						foreach ( LensFlare.LensObjectHoop LO in _LensFlare.m_Hoops )
						{
#if EFFECT_DEBUG
LO.Complexity = _LensFlare.Param0;
LO.Detail = _LensFlare.Param1;
LO.Length = _LensFlare.Param2;
LO.Completion = _LensFlare.Param3;
LO.CompletionFeathering = _LensFlare.Param4;
LO.CompletionRotation = _LensFlare.Param5;
LO.Distance = _LensFlare.Param6;
#endif
							ApplyPerObjectVariablesCommon( _LensFlare, LO );
							ApplyPerObjectVariablesHoop( LO );
							foreach ( Light L in _LensFlare.m_Lights )
							{
								ApplyPerLightVariables( L );
								m_PassHoop.Apply();
								m_Owner.DrawPoints( 1 );
							}
						}
					}

					// Render glows
					if ( _LensFlare.m_Glows.Count > 0 )
					{
						m_Material.CurrentTechnique = m_TechniqueGlow;
						foreach ( LensFlare.LensObjectGlow LO in _LensFlare.m_Glows )
						{
#if EFFECT_DEBUG
LO.Gamma = _LensFlare.Param0;
LO.Scale = _LensFlare.Param1;
#endif
							ApplyPerObjectVariablesCommon( _LensFlare, LO );
							ApplyPerObjectVariablesGlow( LO );
							foreach ( Light L in _LensFlare.m_Lights )
							{
								ApplyPerLightVariables( L );
								m_PassGlow.Apply();
								m_Owner.DrawPoints( 1 );
							}
						}
					}

					// Render sparkles
					if ( _LensFlare.m_Sparkles.Count > 0 )
					{
						m_Material.CurrentTechnique = m_TechniqueSparkle;
						foreach ( LensFlare.LensObjectSparkle LO in _LensFlare.m_Sparkles )
						{

#if EFFECT_DEBUG
LO.Complexity = _LensFlare.Param0;
LO.Length = _LensFlare.Param1;
LO.LengthRandom = _LensFlare.Param2;
LO.Thickness = _LensFlare.Param3;
LO.ThicknessRandom = _LensFlare.Param4;
LO.BrightnessRandom = _LensFlare.Param5;
LO.Spread = _LensFlare.Param6;
LO.SpreadRandom = _LensFlare.Param7;
LO.SpacingRandom = _LensFlare.Param8;
LO.ShapeOrientation = _LensFlare.Param9;
#endif
							ApplyPerObjectVariablesCommon( _LensFlare, LO );
							ApplyPerObjectVariablesSparkle( LO );
							foreach ( Light L in _LensFlare.m_Lights )
							{
								ApplyPerLightVariables( L );
								m_PassSparkle.Apply();
								m_Owner.DrawPoints( 1 );
							}
						}
					}
				}
			}

			protected void	ApplyPerObjectVariablesRing( LensFlare.LensObjectRing _LO )
			{
				// Object shape
				m_vThickness.Set( 0.01f * _LO.Thickness );
				m_vInsideFeathering.Set( 0.01f * _LO.InsideFeathering );
				m_vOutsideFeathering.Set( 0.01f * _LO.OutsideFeathering );

				// Circular completion
				m_vCompletion.Set( ToRadians( _LO.Completion ) );
				m_vCompletionFeathering.Set( 0.01f * _LO.CompletionFeathering );
				m_vCompletionRotation.Set( ToRadians( _LO.CompletionRotation ) );
				m_vAutoRotateModeCompletion.Set( (int) _LO.AutoRotateCompletion );

				// Dynamic triggering
				m_vCompletionOffset.Set( ToRadians( _LO.CompletionOffset ) );
				m_vThicknessOffset.Set( 0.01f * _LO.ThicknessOffset );
			}

			protected void	ApplyPerObjectVariablesHoop( LensFlare.LensObjectHoop _LO )
			{
				// Distance override
				m_vDistance.Set( new Vector2( 0.01f * _LO.Distance, 0.01f * _LO.Distance ) );
				m_vRandomSeed.Set( _LO.RandomSeed );

				// Object shape
				m_vComplexity.Set( (int) _LO.Complexity );
				m_vDetail.Set( 0.01f * _LO.Detail );
				m_vLength.Set( 0.01f * _LO.Length );

				// Animation
				m_vAnimSpeed.Set( 0.01f * _LO.AnimationSpeed );
				m_vAnimAmount.Set( _LO.EnableAnimation ? 0.01f * _LO.AnimationAmount : 0.0f );

				// Circular completion
				m_vCompletion.Set( ToRadians( _LO.Completion ) );
				m_vCompletionFeathering.Set( 0.01f * _LO.CompletionFeathering );
				m_vCompletionRotation.Set( ToRadians( _LO.CompletionRotation ) );

				// Dynamic triggering
				m_vCompletionOffset.Set( ToRadians( _LO.CompletionOffset ) );
			}

			protected void	ApplyPerObjectVariablesGlow( LensFlare.LensObjectGlow _LO )
			{
				m_vGamma.Set( _LO.Gamma );
			}

			protected void	ApplyPerObjectVariablesSparkle( LensFlare.LensObjectSparkle _LO )
			{
				// Sparkle parameters
				m_vComplexity.Set( _LO.Complexity );
				m_vLength.Set( _LO.Length );
				m_vLengthRandom.Set( 0.01f * _LO.LengthRandom );
				m_vThickness.Set( 0.01f * _LO.Thickness );
				m_vThicknessRandom.Set( 0.01f * _LO.ThicknessRandom );
				m_vBrightnessRandom.Set( 0.01f * _LO.BrightnessRandom );
				m_vSpread.Set( _LO.Spread );
				m_vSpreadRandom.Set( 0.01f * _LO.SpreadRandom );
				m_vSpacingRandom.Set( 0.01f * _LO.SpacingRandom );
				m_vShapeOrientation.Set( ToRadians( _LO.ShapeOrientation ) );

				// Animation
				m_vAnimSpeed.Set( 0.01f * _LO.AnimationSpeed );
				m_vAnimAmount.Set( _LO.EnableAnimation ? 0.01f * _LO.AnimationAmount : 0.0f );

				// Circular completion
				m_vCompletion.Set( ToRadians( _LO.Completion ) );
				m_vCompletionFeathering.Set( 0.01f * _LO.CompletionFeathering );
				m_vCompletionRotation.Set( ToRadians( _LO.CompletionRotation ) );
				m_vAutoRotateModeCompletion.Set( (int) _LO.AutoRotateCompletion );

				// Dynamic triggering
				m_vCompletionOffset.Set( ToRadians( _LO.CompletionOffset ) );
				m_vSpreadOffset.Set( _LO.SpreadOffset );
			}

			protected override void GetShaderVariables()
			{
				base.GetShaderVariables();

				// For rings
				m_vThickness = m_Material.GetVariableByName( "Thickness" ).AsScalar;
				m_vInsideFeathering = m_Material.GetVariableByName( "InsideFeathering" ).AsScalar;
				m_vOutsideFeathering = m_Material.GetVariableByName( "OutsideFeathering" ).AsScalar;
				m_vThicknessOffset = m_Material.GetVariableByName( "ThicknessOffset" ).AsScalar;

				// For hoops
				m_vComplexity = m_Material.GetVariableByName( "Complexity" ).AsScalar;
				m_vDetail = m_Material.GetVariableByName( "Detail" ).AsScalar;
				m_vLength = m_Material.GetVariableByName( "Length" ).AsScalar;

				// For glows
				m_vGamma = m_Material.GetVariableByName( "Gamma" ).AsScalar;

				// For sparkles
				m_vLengthRandom = m_Material.GetVariableByName( "LengthRandom" ).AsScalar;
				m_vThicknessRandom = m_Material.GetVariableByName( "ThicknessRandom" ).AsScalar;
				m_vBrightnessRandom = m_Material.GetVariableByName( "BrightnessRandom" ).AsScalar;
				m_vSpacingRandom = m_Material.GetVariableByName( "SpacingRandom" ).AsScalar;
				m_vSpread = m_Material.GetVariableByName( "Spread" ).AsScalar;
				m_vSpreadRandom = m_Material.GetVariableByName( "SpreadRandom" ).AsScalar;
				m_vSpreadOffset = m_Material.GetVariableByName( "SpreadOffset" ).AsScalar;
				m_vShapeOrientation = m_Material.GetVariableByName( "ShapeOrientation" ).AsScalar;


				// Get techniques
				m_TechniqueRing = m_Material.GetTechniqueByName( "DisplayRing" );
				m_PassRing = m_TechniqueRing.GetPassByIndex( 0 );
				m_TechniqueHoop = m_Material.GetTechniqueByName( "DisplayHoop" );
				m_PassHoop = m_TechniqueHoop.GetPassByIndex( 0 );
				m_TechniqueGlow = m_Material.GetTechniqueByName( "DisplayGlow" );
				m_PassGlow = m_TechniqueGlow.GetPassByIndex( 0 );
				m_TechniqueSparkle = m_Material.GetTechniqueByName( "DisplaySparkle" );
				m_PassSparkle = m_TechniqueSparkle.GetPassByIndex( 0 );
			}

			#endregion

			#region EVENT HANDLERS

			void Material_EffectRecompiled( object sender, EventArgs e )
			{
				GetShaderVariables();
			}

			#endregion
		}

		/// <summary>
		/// Streak effects
		/// </summary>
		protected class		LensObjectsMaterial_Streak : LensObjectsMaterial
		{
			#region FIELDS

			protected Material<VS_T2>					m_Material = null;
			protected EffectTechnique					m_TechniqueStreak = null;
			protected EffectPass						m_PassStreak = null;

			// Effect-specific variables
			protected VariableScalar					m_vThickness = null;
			protected VariableScalar					m_vLength = null;
			protected VariableScalar					m_vCoreIntensity = null;
			protected VariableScalar					m_vSymmetry = null;
			protected VariableScalar					m_vFanEnds = null;
			protected VariableScalar					m_vFanFeathering = null;
			protected VariableScalar					m_vReplicatorCopies = null;
			protected VariableScalar					m_vReplicatorAngle = null;
			protected VariableScalar					m_vScaleRandom = null;
			protected VariableScalar					m_vSpacingRandom = null;

			#endregion

			#region PROPERTIES

			protected override IMaterial CurrentMaterial
			{
				get { return m_Material; }
			}

			#endregion

			#region METHODS

			public LensObjectsMaterial_Streak( RenderTechniquePostProcessLensFlares<PF> _Owner ) : base( _Owner )
			{
				m_Material = ToDispose( new Material<VS_T2>( m_Device, "Lens-Flare Streak", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Utility/LensFlares/PostProcessLensFlareStreak.fx" ) ) );
				m_Material.EffectRecompiled += new EventHandler( Material_EffectRecompiled );

				GetShaderVariables();
			}

			public override void Render( LensFlareDisplay _LensFlare )
			{
				if ( _LensFlare.m_Streaks.Count == 0 )
					return;

				using ( m_Material.UseLock() )
				{
					ApplyPerMaterialVariables( _LensFlare );

					// Render streaks
					foreach ( LensFlare.LensObjectStreak LO in _LensFlare.m_Streaks )
					{

#if EFFECT_DEBUG
LO.Length = _LensFlare.Param0;
LO.Thickness = _LensFlare.Param1;
LO.CoreIntensity = _LensFlare.Param2;
LO.Symmetry = _LensFlare.Param3;
LO.FanEnds = _LensFlare.Param4;
LO.FanFeathering = _LensFlare.Param5;
LO.ReplicatorCopies = _LensFlare.Param6;
LO.ReplicatorAngle = _LensFlare.Param7;
LO.ScaleRandom = _LensFlare.Param8;
LO.SpacingRandom = _LensFlare.Param9;
#endif

						ApplyPerObjectVariablesCommon( _LensFlare, LO );
						ApplyPerObjectVariablesStreak( LO );

		 				int	StreaksCount = (int) Math.Floor( LO.ReplicatorCopies );
						if ( StreaksCount == 0 )
							return;

						foreach ( Light L in _LensFlare.m_Lights )
						{
							ApplyPerLightVariables( L );
							m_PassStreak.Apply();
							m_Owner.DrawPoints( StreaksCount );
						}
					}
				}
			}

			protected void	ApplyPerObjectVariablesStreak( LensFlare.LensObjectStreak _LO )
			{
				// Streak control
				m_vLength.Set( _LO.Length );
				m_vThickness.Set( 0.01f * _LO.Thickness );
				m_vCoreIntensity.Set( 0.01f * _LO.CoreIntensity );
				m_vSymmetry.Set( 0.01f * _LO.Symmetry );
				m_vFanEnds.Set( 0.01f * _LO.FanEnds );
				m_vFanFeathering.Set( 0.01f * _LO.FanFeathering );
				m_vReplicatorCopies.Set( (int) Math.Floor( _LO.ReplicatorCopies ) );
				m_vReplicatorAngle.Set( ToRadians( _LO.ReplicatorAngle ) );
				m_vScaleRandom.Set( 0.01f * _LO.ScaleRandom );
				m_vSpacingRandom.Set( 0.01f * _LO.SpacingRandom );
			}

			protected override void GetShaderVariables()
			{
				base.GetShaderVariables();

				// For streaks
				m_vLength = m_Material.GetVariableByName( "Length" ).AsScalar;
				m_vThickness = m_Material.GetVariableByName( "Thickness" ).AsScalar;
				m_vCoreIntensity = m_Material.GetVariableByName( "CoreIntensity" ).AsScalar;
				m_vSymmetry = m_Material.GetVariableByName( "Symmetry" ).AsScalar;
				m_vFanEnds = m_Material.GetVariableByName( "FanEnds" ).AsScalar;
				m_vFanFeathering = m_Material.GetVariableByName( "FanFeathering" ).AsScalar;
				m_vReplicatorCopies = m_Material.GetVariableByName( "ReplicatorCopies" ).AsScalar;
				m_vReplicatorAngle = m_Material.GetVariableByName( "ReplicatorAngle" ).AsScalar;
				m_vScaleRandom = m_Material.GetVariableByName( "ScaleRandom" ).AsScalar;
				m_vSpacingRandom = m_Material.GetVariableByName( "SpacingRandom" ).AsScalar;

				// Get techniques
				m_TechniqueStreak = m_Material.GetTechniqueByName( "DisplayStreak" );
				m_PassStreak = m_TechniqueStreak.GetPassByIndex( 0 );
			}

			#endregion

			#region EVENT HANDLERS

			void Material_EffectRecompiled( object sender, EventArgs e )
			{
				GetShaderVariables();
			}

			#endregion
		}

		/// <summary>
		/// Iris effects
		/// </summary>
		protected class		LensObjectsMaterial_Iris : LensObjectsMaterial
		{
			#region FIELDS

			protected Material<VS_T2>					m_Material = null;
			protected EffectTechnique					m_TechniqueIris = null;
			protected EffectPass						m_PassIris = null;

// 			protected List<LensFlare.LensObjectIris>	m_Irises = new List<LensFlare.LensObjectIris>();
//			protected List<ITexture2D>					m_IrisTextures = new List<ITexture2D>();

			// Effect-specific variables
			protected VariableScalar					m_vColorRandom = null;
			protected VariableScalar					m_vSpread = null;
			protected VariableScalar					m_vSpreadRandom = null;
			protected VariableScalar					m_vIrisesCount = null;
			protected VariableScalar					m_vScaleRandom = null;
			protected VariableScalar					m_vBrightnessRandom = null;
			protected VariableScalar					m_vRotationRandom = null;
			protected VariableScalar					m_vOffsetRandom = null;
			protected VariableScalar					m_vShapeOrientation = null;
			protected VariableScalar					m_vOrientationRandom = null;
			protected VariableScalar					m_vCompletionRotationRandom = null;

			protected VariableScalar					m_vShapeType = null;
			protected VariableResource					m_vTextureImage = null;
			protected VariableScalar					m_vPolygonSides = null;
			protected VariableScalar					m_vPolygonRoundness = null;
			protected VariableScalar					m_vBladeNotching = null;
			protected VariableScalar					m_vSmoothness = null;
			protected VariableScalar					m_vSmoothnessRandom = null;
			protected VariableScalar					m_vOutlineIntensity = null;
			protected VariableScalar					m_vOutlineThickness = null;
			protected VariableScalar					m_vOutlineFeathering = null;
			protected VariableScalar					m_vOutlineIntensityOffset = null;

			#endregion

			#region PROPERTIES

			protected override IMaterial CurrentMaterial
			{
				get { return m_Material; }
			}

			#endregion

			#region METHODS

			public LensObjectsMaterial_Iris( RenderTechniquePostProcessLensFlares<PF> _Owner ) : base( _Owner )
			{
				m_Material = ToDispose( new Material<VS_T2>( m_Device, "Lens-Flare Iris", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Utility/LensFlares/PostProcessLensFlareIris.fx" ) ) );
				m_Material.EffectRecompiled += new EventHandler( Material_EffectRecompiled );

				GetShaderVariables();
			}

			public override void Render( LensFlareDisplay _LensFlare )
			{
				if ( _LensFlare.m_Irises.Count == 0 )
					return;

				using ( m_Material.UseLock() )
				{
					ApplyPerMaterialVariables( _LensFlare );

					// Render irises
					if ( _LensFlare.m_Irises.Count > 0 )
					{
						for ( int IrisIndex=0; IrisIndex < _LensFlare.m_Irises.Count; IrisIndex++ )
						{
							LensFlare.LensObjectIris	LO = _LensFlare.m_Irises[IrisIndex];
							ITexture2D					IrisTexture = _LensFlare.m_IrisTextures[IrisIndex];
#if EFFECT_DEBUG
LO.ShapeOrientation = _LensFlare.Param0;
LO.PolygonSides = _LensFlare.Param1;
LO.PolygonRoundness = _LensFlare.Param2;
LO.BladeNotching = _LensFlare.Param3;
LO.Smoothness = _LensFlare.Param4;
LO.OutlineIntensity = _LensFlare.Param5;
LO.OutlineThickness = _LensFlare.Param6;
LO.OutlineFeathering = _LensFlare.Param7;

// LO.Completion = _LensFlare.Param8;
// LO.CompletionFeathering = _LensFlare.Param9;
// LO.AutoRotateCompletion = LensFlare.LensObject.AUTO_ROTATE_COMPLETION_MODE.TO_CENTER;
// 
//LO.BrightnessOffset = -100.0f;
//LO.ScaleOffset = 100.0f;
//LO.StretchOffset.X = 100.0f;
//LO.CompletionOffset = -180.0f;
//LO.OutlineIntensityOffset = -100.0f;
//LO.EnableTrigger = true;
//LO.TriggerType = LensFlare.LensObject.TRIGGER_TYPE.FROM_CENTER;
//
//LO.ColorSource = LensFlare.LensObject.COLOR_SOURCE.SPECTRUM;
#endif

							ApplyPerObjectVariablesCommon( _LensFlare, LO );
							ApplyPerObjectVariablesIris( LO, IrisTexture );

			 				int	IrisesCount = (int) Math.Floor( LO.NumberOfObjects );
							if ( IrisesCount == 0 )
								return;

							foreach ( Light L in _LensFlare.m_Lights )
							{
								ApplyPerLightVariables( L );
								m_PassIris.Apply();
								m_Owner.DrawPoints( IrisesCount );
							}
						}
					}
				}
			}

			protected void	ApplyPerObjectVariablesIris( LensFlare.LensObjectIris _LO, ITexture2D _IrisTexture )
			{
				// Multi-iris control
				m_vColorRandom.Set( 0.01f * _LO.ColorRandom );
				m_vSpread.Set( 0.01f * _LO.Spread );
				m_vSpreadRandom.Set( 0.01f * _LO.SpreadRandom );
				m_vIrisesCount.Set( _LO.IsMultiIris ? (int) Math.Floor( _LO.NumberOfObjects ) : 1 );
				m_vScaleRandom.Set( 0.01f * _LO.ScaleRandom );
				m_vBrightnessRandom.Set( 0.01f * _LO.BrightnessRandom );
				m_vRotationRandom.Set( 0.01f * _LO.RotationRandom );
				m_vOffsetRandom.Set( 0.01f * _LO.OffsetRandom );
				m_vShapeOrientation.Set( ToRadians( _LO.ShapeOrientation ) );
				m_vOrientationRandom.Set( 0.01f * _LO.OrientationRandom );
				m_vCompletionRotationRandom.Set( 0.01f * _LO.CompletionRotationRandom );

				// Object shape
				m_vShapeType.Set( (int) _LO.ShapeType );
				if ( _LO.ShapeType == LensFlare.LensObject.SHAPE_TYPE.TEXTURE )
					m_vTextureImage.SetResource( _IrisTexture );
				m_vPolygonSides.Set( (int) Math.Floor( _LO.PolygonSides ) );
				m_vPolygonRoundness.Set( 0.01f * _LO.PolygonRoundness );
				m_vBladeNotching.Set( 0.01f * _LO.BladeNotching );
				m_vSmoothness.Set( 0.01f * _LO.Smoothness );
				m_vSmoothnessRandom.Set( 0.01f * _LO.SmoothnessRandom );
				m_vOutlineIntensity.Set( 0.01f * _LO.OutlineIntensity );
				m_vOutlineThickness.Set( 0.01f * _LO.OutlineThickness );
				m_vOutlineFeathering.Set( 0.01f * _LO.OutlineFeathering );

				// Circular completion
				m_vCompletion.Set( ToRadians( _LO.Completion ) );
				m_vCompletionFeathering.Set( 0.01f * _LO.CompletionFeathering );
				m_vAutoRotateModeCompletion.Set( (int) _LO.AutoRotateCompletion );

				// Dynamic triggering
				m_vCompletionOffset.Set( ToRadians( _LO.CompletionOffset ) );
				m_vOutlineIntensityOffset.Set( 0.01f * _LO.OutlineIntensityOffset );
			}

			protected override void GetShaderVariables()
			{
				base.GetShaderVariables();

				// For irises
				m_vColorRandom = m_Material.GetVariableByName( "ColorRandom" ).AsScalar;
				m_vSpread = m_Material.GetVariableByName( "Spread" ).AsScalar;
				m_vSpreadRandom = m_Material.GetVariableByName( "SpreadRandom" ).AsScalar;
				m_vIrisesCount = m_Material.GetVariableByName( "IrisesCount" ).AsScalar;
				m_vScaleRandom = m_Material.GetVariableByName( "ScaleRandom" ).AsScalar;
				m_vBrightnessRandom = m_Material.GetVariableByName( "BrightnessRandom" ).AsScalar;
				m_vRotationRandom = m_Material.GetVariableByName( "RotationRandom" ).AsScalar;
				m_vOffsetRandom = m_Material.GetVariableByName( "OffsetRandom" ).AsScalar;
				m_vShapeOrientation = m_Material.GetVariableByName( "ShapeOrientation" ).AsScalar;
				m_vOrientationRandom = m_Material.GetVariableByName( "OrientationRandom" ).AsScalar;
				m_vCompletionRotationRandom = m_Material.GetVariableByName( "CompletionRotationRandom" ).AsScalar;

				m_vShapeType = m_Material.GetVariableByName( "ShapeType" ).AsScalar;
				m_vTextureImage = m_Material.GetVariableByName( "IrisTexture" ).AsResource;
				m_vPolygonSides = m_Material.GetVariableByName( "PolygonSides" ).AsScalar;
				m_vPolygonRoundness = m_Material.GetVariableByName( "PolygonRoundness" ).AsScalar;
				m_vBladeNotching = m_Material.GetVariableByName( "BladeNotching" ).AsScalar;
				m_vSmoothness = m_Material.GetVariableByName( "Smoothness" ).AsScalar;
				m_vSmoothnessRandom = m_Material.GetVariableByName( "SmoothnessRandom" ).AsScalar;
				m_vOutlineIntensity = m_Material.GetVariableByName( "OutlineIntensity" ).AsScalar;
				m_vOutlineThickness = m_Material.GetVariableByName( "OutlineThickness" ).AsScalar;
				m_vOutlineFeathering = m_Material.GetVariableByName( "OutlineFeathering" ).AsScalar;
				m_vOutlineIntensityOffset = m_Material.GetVariableByName( "OutlineIntensityOffset" ).AsScalar;

				// Get techniques
				m_TechniqueIris = m_Material.GetTechniqueByName( "DisplayIris" );
				m_PassIris = m_TechniqueIris.GetPassByIndex( 0 );
			}

			#endregion

			#region EVENT HANDLERS

			void Material_EffectRecompiled( object sender, EventArgs e )
			{
				GetShaderVariables();
			}

			#endregion
		}

		/// <summary>
		/// Shimmer effects
		/// </summary>
		protected class		LensObjectsMaterial_Shimmer : LensObjectsMaterial
		{
			#region FIELDS

			protected Material<VS_T2>					m_Material = null;
			protected EffectTechnique					m_Technique = null;
			protected EffectPass						m_Pass = null;

//			protected List<LensFlare.LensObjectShimmer>	m_Shimmers = new List<LensFlare.LensObjectShimmer>();

			// Effect-specific variables
			protected VariableScalar					m_vComplexity = null;
			protected VariableScalar					m_vDetail = null;
			protected VariableScalar					m_vShapeOrientation = null;

			#endregion

			#region PROPERTIES

			protected override IMaterial CurrentMaterial
			{
				get { return m_Material; }
			}

			#endregion

			#region METHODS

			public LensObjectsMaterial_Shimmer( RenderTechniquePostProcessLensFlares<PF> _Owner ) : base( _Owner )
			{
				m_Material = ToDispose( new Material<VS_T2>( m_Device, "Lens-Flare Shimmer", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Utility/LensFlares/PostProcessLensFlareShimmer.fx" ) ) );
				m_Material.EffectRecompiled += new EventHandler( Material_EffectRecompiled );

				GetShaderVariables();
			}

			public override void Render( LensFlareDisplay _LensFlare )
			{
				if ( _LensFlare.m_Shimmers.Count == 0 )
					return;

				using ( m_Material.UseLock() )
				{
					ApplyPerMaterialVariables( _LensFlare );

					// Render shimmers
					foreach ( LensFlare.LensObjectShimmer LO in _LensFlare.m_Shimmers )
					{

#if EFFECT_DEBUG
LO.Complexity = _LensFlare.Param0;
LO.Detail = _LensFlare.Param1;
LO.ShapeOrientation = _LensFlare.Param2;
LO.Completion = _LensFlare.Param3;
LO.CompletionFeathering = _LensFlare.Param4;
LO.CompletionRotation = _LensFlare.Param5;
LO.Scale = _LensFlare.Param6;
#endif

						ApplyPerObjectVariablesCommon( _LensFlare, LO );
						ApplyPerObjectVariablesShimmer( LO );

						int	SidesCount = (int) Math.Floor( LO.Complexity ) - 1;
						if ( SidesCount <= 0 )
							return;

						foreach ( Light L in _LensFlare.m_Lights )
						{
							ApplyPerLightVariables( L );
							m_Pass.Apply();
							m_Owner.DrawPoints( 3*SidesCount );
						}
					}
				}
			}

			protected void	ApplyPerObjectVariablesShimmer( LensFlare.LensObjectShimmer _LO )
			{
				// Shimmer control
				m_vComplexity.Set( (int) Math.Floor( _LO.Complexity ) - 1 );	// Here we use Complexity-1 as it's the actual number of sides in the shimmer polygons
				m_vDetail.Set( 0.01f * _LO.Detail );
				m_vShapeOrientation.Set( ToRadians( _LO.ShapeOrientation ) );

				// Animation
				m_vAnimSpeed.Set( 0.01f * _LO.AnimationSpeed );
				m_vAnimAmount.Set( _LO.EnableAnimation ? 0.01f * _LO.AnimationAmount : 0.0f );

				// Circular completion
				m_vCompletion.Set( ToRadians( _LO.Completion ) );
				m_vCompletionFeathering.Set( 0.01f * _LO.CompletionFeathering );
				m_vCompletionRotation.Set( ToRadians( _LO.CompletionRotation ) );
				m_vAutoRotateModeCompletion.Set( (int) _LO.AutoRotateCompletion );

				// Dynamic triggering
				m_vCompletionOffset.Set( ToRadians( _LO.CompletionOffset ) );
			}

			protected override void GetShaderVariables()
			{
				base.GetShaderVariables();

				// Shimmer variables
				m_vComplexity = m_Material.GetVariableByName( "Complexity" ).AsScalar;
				m_vDetail = m_Material.GetVariableByName( "Detail" ).AsScalar;
				m_vShapeOrientation = m_Material.GetVariableByName( "ShapeOrientation" ).AsScalar;

				// Get techniques
				m_Technique = m_Material.GetTechniqueByName( "DisplayShimmer" );
				m_Pass = m_Technique.GetPassByIndex( 0 );
			}

			#endregion

			#region EVENT HANDLERS

			void Material_EffectRecompiled( object sender, EventArgs e )
			{
				GetShaderVariables();
			}

			#endregion
		}

		/// <summary>
		/// SpikeBall effects
		/// </summary>
		protected class		LensObjectsMaterial_SpikeBall : LensObjectsMaterial
		{
			#region FIELDS

			protected Material<VS_T2>					m_Material = null;
			protected EffectTechnique					m_Technique = null;
			protected EffectPass						m_Pass = null;

//			protected List<LensFlare.LensObjectSpikeBall>	m_SpikeBalls = new List<LensFlare.LensObjectSpikeBall>();

			// Effect-specific variables
			protected VariableScalar					m_vComplexity = null;
			protected VariableScalar					m_vLength = null;
			protected VariableScalar					m_vLengthRandom = null;
			protected VariableScalar					m_vThickness = null;
			protected VariableScalar					m_vThicknessRandom = null;
			protected VariableScalar					m_vBrightnessRandom = null;
			protected VariableScalar					m_vSpacingRandom = null;
			protected VariableScalar					m_vShapeOrientation = null;

			#endregion

			#region PROPERTIES

			protected override IMaterial CurrentMaterial
			{
				get { return m_Material; }
			}

			#endregion

			#region METHODS

			public LensObjectsMaterial_SpikeBall( RenderTechniquePostProcessLensFlares<PF> _Owner ) : base( _Owner )
			{
				m_Material = ToDispose( new Material<VS_T2>( m_Device, "Lens-Flare SpikeBall", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Utility/LensFlares/PostProcessLensFlareSpikeBall.fx" ) ) );
				m_Material.EffectRecompiled += new EventHandler( Material_EffectRecompiled );

				GetShaderVariables();
			}

			public override void Render( LensFlareDisplay _LensFlare )
			{
				if ( _LensFlare.m_SpikeBalls.Count == 0 )
					return;

				using ( m_Material.UseLock() )
				{
					ApplyPerMaterialVariables( _LensFlare );

					// Render shimmers
					foreach ( LensFlare.LensObjectSpikeBall LO in _LensFlare.m_SpikeBalls )
					{

#if EFFECT_DEBUG
LO.Complexity = _LensFlare.Param0;
LO.Length = _LensFlare.Param1;
LO.LengthRandom = _LensFlare.Param2;
LO.Thickness = _LensFlare.Param3;
LO.ThicknessRandom = _LensFlare.Param4;
LO.BrightnessRandom = _LensFlare.Param5;
LO.SpacingRandom = _LensFlare.Param6;
LO.ShapeOrientation = _LensFlare.Param7;
LO.Scale = _LensFlare.Param8;
#endif

						ApplyPerObjectVariablesCommon( _LensFlare, LO );
						ApplyPerObjectVariablesSpikeBall( LO );

						int	SpikesCount = (int) Math.Floor( LO.Complexity );
						if ( SpikesCount == 0 )
							return;

						foreach ( Light L in _LensFlare.m_Lights )
						{
							ApplyPerLightVariables( L );
							m_Pass.Apply();
							m_Owner.DrawPoints( SpikesCount );
						}
					}
				}
			}

			protected void	ApplyPerObjectVariablesSpikeBall( LensFlare.LensObjectSpikeBall _LO )
			{
				// SpikeBall control
				m_vComplexity.Set( (int) Math.Floor( _LO.Complexity ) );
				m_vLength.Set( _LO.Length );
				m_vLengthRandom.Set( 0.01f * _LO.LengthRandom );
				m_vThickness.Set( 0.01f * _LO.Thickness );
				m_vThicknessRandom.Set( 0.01f * _LO.ThicknessRandom );
				m_vBrightnessRandom.Set( 0.01f * _LO.BrightnessRandom );
				m_vSpacingRandom.Set( 0.01f * _LO.SpacingRandom );
				m_vShapeOrientation.Set( ToRadians( _LO.ShapeOrientation ) );

				// Animation
				m_vAnimSpeed.Set( 0.01f * _LO.AnimationSpeed );
				m_vAnimAmount.Set( _LO.EnableAnimation ? 0.01f * _LO.AnimationAmount : 0.0f );
			}

			protected override void GetShaderVariables()
			{
				base.GetShaderVariables();

				// SpikeBall variables
				m_vComplexity = m_Material.GetVariableByName( "Complexity" ).AsScalar;
				m_vLength = m_Material.GetVariableByName( "Length" ).AsScalar;
				m_vLengthRandom = m_Material.GetVariableByName( "LengthRandom" ).AsScalar;
				m_vThickness = m_Material.GetVariableByName( "Thickness" ).AsScalar;
				m_vThicknessRandom = m_Material.GetVariableByName( "ThicknessRandom" ).AsScalar;
				m_vBrightnessRandom = m_Material.GetVariableByName( "BrightnessRandom" ).AsScalar;
				m_vSpacingRandom = m_Material.GetVariableByName( "SpacingRandom" ).AsScalar;
				m_vShapeOrientation = m_Material.GetVariableByName( "ShapeOrientation" ).AsScalar;

				// Get techniques
				m_Technique = m_Material.GetTechniqueByName( "DisplaySpikeBall" );
				m_Pass = m_Technique.GetPassByIndex( 0 );
			}

			#endregion

			#region EVENT HANDLERS

			void Material_EffectRecompiled( object sender, EventArgs e )
			{
				GetShaderVariables();
			}

			#endregion
		}

		/// <summary>
		/// Glint effects
		/// </summary>
		protected class		LensObjectsMaterial_Glint : LensObjectsMaterial
		{
			#region FIELDS

			protected Material<VS_T2>					m_Material = null;
			protected EffectTechnique					m_Technique = null;
			protected EffectPass						m_Pass = null;

//			protected List<LensFlare.LensObjectGlint>	m_Glints = new List<LensFlare.LensObjectGlint>();

			// Effect-specific variables
			protected VariableScalar					m_vComplexity = null;
			protected VariableScalar					m_vLength = null;
			protected VariableScalar					m_vLengthRandom = null;
			protected VariableScalar					m_vThickness = null;
			protected VariableScalar					m_vSpacingRandom = null;
			protected VariableScalar					m_vShapeOrientation = null;

			#endregion

			#region PROPERTIES

			protected override IMaterial CurrentMaterial
			{
				get { return m_Material; }
			}

			#endregion

			#region METHODS

			public LensObjectsMaterial_Glint( RenderTechniquePostProcessLensFlares<PF> _Owner ) : base( _Owner )
			{
				m_Material = ToDispose( new Material<VS_T2>( m_Device, "Lens-Flare Glint", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Utility/LensFlares/PostProcessLensFlareGlint.fx" ) ) );
				m_Material.EffectRecompiled += new EventHandler( Material_EffectRecompiled );

				GetShaderVariables();
			}

			public override void Render( LensFlareDisplay _LensFlare )
			{
				if ( _LensFlare.m_Glints.Count == 0 )
					return;

				using ( m_Material.UseLock() )
				{
					ApplyPerMaterialVariables( _LensFlare );

					// Render shimmers
					foreach ( LensFlare.LensObjectGlint LO in _LensFlare.m_Glints )
					{

#if EFFECT_DEBUG
LO.Complexity = _LensFlare.Param0;
LO.Length = _LensFlare.Param1;
LO.LengthRandom = _LensFlare.Param2;
LO.Thickness = _LensFlare.Param3;
LO.SpacingRandom = _LensFlare.Param4;
LO.ShapeOrientation = _LensFlare.Param5;
LO.Completion = _LensFlare.Param6;
LO.CompletionFeathering = _LensFlare.Param7;
LO.CompletionRotation = _LensFlare.Param8;
LO.Scale = _LensFlare.Param9;
#endif

						ApplyPerObjectVariablesCommon( _LensFlare, LO );
						ApplyPerObjectVariablesGlint( LO );

						int	SpikesCount = (int) Math.Floor( LO.Complexity );
						if ( SpikesCount == 0 )
							return;

						foreach ( Light L in _LensFlare.m_Lights )
						{
							ApplyPerLightVariables( L );
							m_Pass.Apply();
							m_Owner.DrawPoints( SpikesCount );
						}
					}
				}
			}

			protected void	ApplyPerObjectVariablesGlint( LensFlare.LensObjectGlint _LO )
			{
				// Glint control
				m_vComplexity.Set( (int) Math.Floor( _LO.Complexity ) );
				m_vLength.Set( _LO.Length );
				m_vLengthRandom.Set( 0.01f * _LO.LengthRandom );
				m_vThickness.Set( 0.01f * _LO.Thickness );
				m_vSpacingRandom.Set( 0.01f * _LO.SpacingRandom );
				m_vShapeOrientation.Set( ToRadians( _LO.ShapeOrientation ) );

				// Animation
				m_vAnimSpeed.Set( 0.01f * _LO.AnimationSpeed );
				m_vAnimAmount.Set( _LO.EnableAnimation ? 0.01f * _LO.AnimationAmount : 0.0f );

				// Circular completion
				m_vCompletion.Set( ToRadians( _LO.Completion ) );
				m_vCompletionFeathering.Set( 0.01f * _LO.CompletionFeathering );
				m_vCompletionRotation.Set( ToRadians( _LO.CompletionRotation ) );
				m_vAutoRotateModeCompletion.Set( (int) _LO.AutoRotateCompletion );

				// Dynamic triggering
				m_vCompletionOffset.Set( ToRadians( _LO.CompletionOffset ) );
			}

			protected override void GetShaderVariables()
			{
				base.GetShaderVariables();

				// Glint variables
				m_vComplexity = m_Material.GetVariableByName( "Complexity" ).AsScalar;
				m_vLength = m_Material.GetVariableByName( "Length" ).AsScalar;
				m_vLengthRandom = m_Material.GetVariableByName( "LengthRandom" ).AsScalar;
				m_vThickness = m_Material.GetVariableByName( "Thickness" ).AsScalar;
				m_vSpacingRandom = m_Material.GetVariableByName( "SpacingRandom" ).AsScalar;
				m_vShapeOrientation = m_Material.GetVariableByName( "ShapeOrientation" ).AsScalar;

				// Get techniques
				m_Technique = m_Material.GetTechniqueByName( "DisplayGlint" );
				m_Pass = m_Technique.GetPassByIndex( 0 );
			}

			#endregion

			#region EVENT HANDLERS

			void Material_EffectRecompiled( object sender, EventArgs e )
			{
				GetShaderVariables();
			}

			#endregion
		}

		public delegate void			SetRenderTargetEventHandler();

		#endregion

		#region FIELDS

		protected Camera				m_Camera = null;
		protected float					m_Time = 0.0f;

		protected float					m_SmoothRadius = 0.0f;

		// Lens objects materials
		protected LensObjectsMaterial_Generic	m_MaterialGeneric = null;
		protected LensObjectsMaterial_Iris		m_MaterialIris = null;
		protected LensObjectsMaterial_Streak	m_MaterialStreak = null;
		protected LensObjectsMaterial_Shimmer	m_MaterialShimmer = null;
		protected LensObjectsMaterial_SpikeBall	m_MaterialSpikeBall = null;
		protected LensObjectsMaterial_Glint		m_MaterialGlint = null;

		// The global blending material
		protected Material<VS_Pt4>		m_MaterialBlend = null;

		// Shader variables
		protected VariableVector		m_vInvSourceSize = null;
		protected VariableResource		m_vSourceTexture = null;
		protected VariableVector		m_vLightPosition = null;
		protected VariableScalar		m_vScreenMode = null;
		protected VariableScalar		m_vUseLensTexture = null;
		protected VariableResource		m_vLensTexture = null;
		protected VariableScalar		m_vIlluminationRadius = null;
		protected VariableScalar		m_vFallOff = null;
		protected VariableScalar		m_vBrightness = null;
		protected VariableVector		m_vScale = null;
		protected VariableVector		m_vOffset = null;
		protected VariableScalar		m_vAberrationType = null;
		protected VariableScalar		m_vAberrationIntensity = null;
		protected VariableScalar		m_vAberrationSpread = null;
		protected VariableScalar		m_vCCBrightness = null;
		protected VariableScalar		m_vCCContrast = null;
		protected VariableScalar		m_vCCSaturation = null;

		// Blend states (additive/screen)
		protected BlendState			m_BlendAdditiveRGB = null;
		protected BlendState			m_BlendScreenRGB = null;
		protected BlendState			m_BlendAdditiveAlpha = null;

		// The list of created lens-flare display objects
		protected List<LensFlareDisplay>	m_LensFlares = new List<LensFlareDisplay>();

		// Geometry
		protected VertexBuffer<VS_T2>	m_PointVB = null;
		protected Helpers.ScreenQuad	m_Quad = null;

		// The textures loader and the already loaded lens textures
		protected TextureLoader			m_TextureLoader = null;
		protected Dictionary<string,ITexture2D>	m_Name2Texture = new Dictionary<string,ITexture2D>();

		// Textures
		protected Texture2D<PF_RGBA8>	m_DefaultLightIntensityScale = null;
		protected Texture2D<PF_RGBA8>	m_TextureRandom = null;

		// Render targets
		protected RenderTarget<PF>		m_LensFlareTarget = null;
 		protected IRenderTarget			m_TargetImage = null;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the camera used to visualize the lens-flares
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Camera				Camera					{ get { return m_Camera; } set { m_Camera = value; } }

		/// <summary>
		/// Gets or sets the time for lens-flares animation
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public float				Time					{ get { return m_Time; } set { m_Time = value; } }

		/// <summary>
		/// Gets or sets the target image to render to
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public IRenderTarget		TargetImage				{ get { return m_TargetImage; } set { m_TargetImage = value; } }

		/// <summary>
		/// Occurs when the post-process is rendering to setup the render target it should render to
		/// This event, if set, takes precedence over the "TargetImage" property
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public event SetRenderTargetEventHandler			SetRenderTarget;

// 		/// <summary>
// 		/// Gets or sets the smooth radius to smooth out the lens-flares a bit
// 		/// </summary>
// 		public float				SmoothRadius			{ get { return m_SmoothRadius; } set { m_SmoothRadius = value; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates the render technique
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Width">The width of the render target where to render the lens-flares</param>
		/// <param name="_Height">The height of the render target where to render the lens-flares</param>
		/// <param name="_TextureLoader">An abstract texture loader</param>
		/// <param name="_Factory">An abstract render target factory to create the render targets required by that technique</param>
		public RenderTechniquePostProcessLensFlares( Device _Device, string _Name, int _Width, int _Height, TextureLoader _TextureLoader, IRenderTargetFactory _Factory ) : base( _Device, _Name )
		{
			m_TextureLoader = _TextureLoader;

			//////////////////////////////////////////////////////////////////////////
			// Create the temp render target
			m_LensFlareTarget = _Factory.QueryRenderTarget<PF>( this, RENDER_TARGET_USAGE.DISCARD, "Lens-Flares Temp Target", _Width, _Height, 1 );

			//////////////////////////////////////////////////////////////////////////
			// Create the random texture
			Random	RNG = new Random( 1 );
			using ( Image<PF_RGBA8> I = new Image<PF_RGBA8>( m_Device, "Random Image", 256, 1,
				( int _X, int _Y, ref Vector4 _Color ) =>
				{
					_Color.X = (float) RNG.NextDouble();
					_Color.Y = (float) RNG.NextDouble();
					_Color.Z = (float) RNG.NextDouble();
					_Color.W = (float) RNG.NextDouble();
				}, 1 ) )
				m_TextureRandom = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Random", I ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the default light intensity texture (Itensity=White, Scale=1)
			using ( Image<PF_RGBA8> I = new Image<PF_RGBA8>( m_Device, "Default Light Intensity Image", 1, 1, ( int _X, int _Y, ref Vector4 _Color ) => { _Color = Vector4.One; } , 1 ) )
				m_DefaultLightIntensityScale = ToDispose( new Texture2D<PF_RGBA8>( m_Device, "Default Light Intensity", I ) );

			//////////////////////////////////////////////////////////////////////////
			// Create geometry that goes with the material : a single point
			VS_T2[]	PointVertices = new VS_T2[]
			{
				new VS_T2() { UV = new Vector2( 0.0f, 0.0f ) }
			};

			m_PointVB = ToDispose( new VertexBuffer<VS_T2>( m_Device, "Lens-Flare Individual Point", PointVertices ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the blending screen quad
			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "LensFlare Compositing Quad" ) );

			//////////////////////////////////////////////////////////////////////////
			// Build the lens-flare materials
#if USE_GENERIC
			m_MaterialGeneric = ToDispose( new LensObjectsMaterial_Generic( this ) );
#endif
#if USE_STREAK
			m_MaterialStreak = ToDispose( new LensObjectsMaterial_Streak( this ) );
#endif
#if USE_IRIS
			m_MaterialIris = ToDispose( new LensObjectsMaterial_Iris( this ) );
#endif
#if USE_SHIMMER
			m_MaterialShimmer = ToDispose( new LensObjectsMaterial_Shimmer( this ) );
#endif
#if USE_SPIKEBALL
			m_MaterialSpikeBall = ToDispose( new LensObjectsMaterial_SpikeBall( this ) );
#endif
#if USE_GLINT
			m_MaterialGlint = ToDispose( new LensObjectsMaterial_Glint( this ) );
#endif

			// Build the blend material
			m_MaterialBlend = ToDispose( new Material<VS_Pt4>( m_Device, "Lens-Flare Blend Material", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/Utility/LensFlares/PostProcessLensFlareBlend.fx" ) ) );
			m_MaterialBlend.EffectRecompiled += new EventHandler( MaterialBlend_EffectRecompiled );
			MaterialBlend_EffectRecompiled( m_MaterialBlend, EventArgs.Empty );

			// Create the additive Alpha-write and Additive-RGB blend states
			BlendStateDescription	Desc = m_Device.GetStockBlendState( Device.HELPER_BLEND_STATES.ADDITIVE ).Description;
			Desc.RenderTargetWriteMask[0] = ColorWriteMaskFlags.Alpha;	// Only write alpha
			m_BlendAdditiveAlpha = ToDispose( new BlendState( m_Device.DirectXDevice, Desc ) );
			Desc.RenderTargetWriteMask[0] = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue;	// Only write RGB
			m_BlendAdditiveRGB = ToDispose( new BlendState( m_Device.DirectXDevice, Desc ) );

			// 5] Create the Screen-RGB blend state
			// Here's a quick description of how I achieved the SCREEN blend mode as it's not that easy :
			// Screen mode's equation is : Dst = 1-(1-Src)*(1-Dst)
			//
			// We quickly see that it's possible to perform Dst = Src*(1-Dst) using standard blend operations
			//  and Src can easily be complemented in the shader by returning 1-Result instead of Result, thus
			//	achieving the (1-Src)*(1-Dst) operation, but there's no way we can perform the complement of
			//	that operation to obtain the exact formula !
			//
			// If you examine what you obtain by using the reduced formula Dst=(1-Src)*(1-Dst) you get, for the first 2 steps :
			//
			//	Dst_1 = (1-Src_0)*(1-Dst_0)  --> Dst_0 is the original background here
			//	Dst_2 = (1-Src_1)*(1-Dst_1) = (1-Src_1)*(1-(1-Src_0)*(1-Dst_0))
			//
			// But we expected Dst_1 to be equal to Dst_1 = 1-(1-Src_0)*(1-Dst_0) in the first place so Dst_2 should really be :
			//
			//	Dst_2 = (1-Src_1)*(1-(1-(1-Src_0)*(1-Dst_0))) = (1-Src_1)*((1-Src_0)*(1-Dst_0)) = (1-Src_1)*Dst_1
			//
			// This means that using the blend operation (1-Src)*Dst is actually right on the second step.
			// Recursively, Dst_2 is also wrong because not complemented (i.e. we get Dst_2 instead of 1-Dst_2), but re-applying
			//	
			//	Dst_3 = (1-Src_2)*Dst_2
			//
			// Will actually complement Dst_2 in that stage. So we can deduce that, except for the first stage and the last that
			//	need to be complemented manually, all intermediate stages are ok using the (1-Src)*Dst blend operation.
			//
			// To sum up, to obtain a correct SCREEN mode we need to do :
			//
			//	1) Dst_0' = 1 - Dst_0		<-- First complement of the original background
			//	2) N passes of rendering using the  Dst_i+1 = (1-Src_i)*Dst_i  blend mode
			//	3) A final complement of the last Dst_N buffer
			//
			// So here you go : SCREEN blend mode for you
			//
			Desc.DestinationBlend = BlendOption.InverseSourceColor;
			Desc.SourceBlend = BlendOption.Zero;
			Desc.BlendOperation = BlendOperation.Add;
			m_BlendScreenRGB = ToDispose( new BlendState( m_Device.DirectXDevice, Desc ) );	// Dst' = 0 + (1-Src)*Dst
		}

		public override void Dispose()
		{
			base.Dispose();

			// Dispose of lens-flares
			while ( m_LensFlares.Count > 0 )
				RemoveLensFlare( m_LensFlares[0] );
		}

		/// <summary>
		/// Creates a lens-flare from a LensFlare source
		/// </summary>
		/// <param name="_Name">The name to give to the lens flare</param>
		/// <param name="_Source"></param>
		/// <returns></returns>
		public LensFlareDisplay	CreateLensFlare( string _Name, LensFlare _Source )
		{
			if ( _Source == null )
				throw new NException( this, "Invalid lens-flare source to build from !" );

			LensFlareDisplay	Result = new LensFlareDisplay( this, _Name, _Source );
			m_LensFlares.Add( Result );

			return Result;
		}

		/// <summary>
		/// Removes an existing lens-flare
		/// </summary>
		/// <param name="_LensFlare"></param>
		public void				RemoveLensFlare( LensFlareDisplay _LensFlare )
		{
			if ( !m_LensFlares.Contains( _LensFlare ) )
				return;

			_LensFlare.Dispose();
			m_LensFlares.Remove( _LensFlare );
		}

		/// <summary>
		/// Finds a lens-flare by name
		/// </summary>
		/// <param name="_Name"></param>
		/// <returns></returns>
		public LensFlareDisplay	FindLensFlare( string _Name )
		{
			foreach ( LensFlareDisplay LFD in m_LensFlares )
				if ( LFD.Name == _Name )
					return LFD;

			return null;
		}

		public override void	Render( int _FrameToken )
		{
			if ( m_TargetImage == null && SetRenderTarget == null )
				throw new NException( this, "Target image to render to is not set !" );

			if ( m_Camera == null )
				throw new Exception( "Invalid camera for lights projection ! Did you forget to assign a camera to the technique ?" );

			// Start profiling
			if ( m_Device.HasProfilingStarted )
				m_Device.AddProfileTask( this, "Lens Flare", "<START>" );


			//////////////////////////////////////////////////////////////////////////
			// Display in the temp target
			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );

			// Start drawing
			foreach ( LensFlareDisplay LensFlare in m_LensFlares )
				LensFlare.Render();

			// Stop profiling
			if ( m_Device.HasProfilingStarted )
				m_Device.AddProfileTask( this, "Lens Flare", "<END>" );
		}

		/// <summary>
		/// The only vertex buffer we need to draw the lens-objects
		/// </summary>
		/// <param name="_Count"></param>
		protected void	DrawPoints( int _Count )
		{
			m_PointVB.DrawInstanced( _Count );
		}

		protected void	DrawFullscreenQuad()
		{
			m_Quad.Render();
		}

		/// <summary>
		/// Queries a lens texture by name
		/// </summary>
		/// <param name="_bElement">True if the texture is an element texture (for irises), false if it's a lens texture</param>
		/// <param name="_Name">The name of the texture</param>
		/// <returns></returns>
		protected ITexture2D	QueryTexture( bool _bElement, string _Name )
		{
			if ( m_Name2Texture.ContainsKey( _Name ) )
				return m_Name2Texture[_Name];

			System.IO.FileInfo	TextureFileName = new System.IO.FileInfo( "./Media/LensFlares/Optical Flares Textures/" + (_bElement ? "Elements" : "Glass") + "/" + _Name + ".png" );

			// Load and register the texture
			ITexture2D	Result = m_TextureLoader.LoadTexture<PF_RGBA8>( _Name, 2.2f, TextureFileName );
			m_Name2Texture[_Name] = Result;

			return Result;
		}

		#endregion

		#region EVENT HANDLERS

		void MaterialBlend_EffectRecompiled( object sender, EventArgs e )
		{
			// Global parameters
			m_MaterialBlend.GetVariableByName( "InvSourceSize" ).AsVector.Set( m_LensFlareTarget.InvSize2 );
			m_vSourceTexture = m_MaterialBlend.GetVariableByName( "SourceTexture" ).AsResource;
			m_vLightPosition = m_MaterialBlend.GetVariableByName( "LightPosition" ).AsVector;
			m_vScreenMode = m_MaterialBlend.GetVariableByName( "bScreenMode" ).AsScalar;

			// Lens texture
			m_vUseLensTexture = m_MaterialBlend.GetVariableByName( "bUseLensTexture" ).AsScalar;
			m_vLensTexture = m_MaterialBlend.GetVariableByName( "LensTexture" ).AsResource;
			m_vIlluminationRadius = m_MaterialBlend.GetVariableByName( "IlluminationRadius" ).AsScalar;
			m_vFallOff = m_MaterialBlend.GetVariableByName( "LensTextureFallOff" ).AsScalar;
			m_vBrightness = m_MaterialBlend.GetVariableByName( "LensTextureBrightness" ).AsScalar;
			m_vScale = m_MaterialBlend.GetVariableByName( "LensTextureScale" ).AsVector;
			m_vOffset = m_MaterialBlend.GetVariableByName( "LensTextureOffset" ).AsVector;

			// Chromatic abberation
			m_vAberrationType = m_MaterialBlend.GetVariableByName( "AberrationType" ).AsScalar;
			m_vAberrationIntensity = m_MaterialBlend.GetVariableByName( "AberrationIntensity" ).AsScalar;
			m_vAberrationSpread = m_MaterialBlend.GetVariableByName( "AberrationSpread" ).AsScalar;

			// Color correction
			m_vCCBrightness = m_MaterialBlend.GetVariableByName( "CCBrightness" ).AsScalar;
			m_vCCContrast = m_MaterialBlend.GetVariableByName( "CCContrast" ).AsScalar;
			m_vCCSaturation = m_MaterialBlend.GetVariableByName( "CCSaturation" ).AsScalar;
		}

		#endregion
	}
}
