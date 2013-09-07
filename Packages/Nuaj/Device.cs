using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D10.Device;

namespace Nuaj
{
	/// <summary>
	/// This is a host for the global device
	/// </summary>
	public class	Device : Component, IShaderInterfaceProvider
	{
		#region NESTED TYPES

		public enum HELPER_STATES
		{
			/// <summary>
			/// Backface culling
			/// </summary>
			CULL_BACK,
			/// <summary>
			/// Frontface culling
			/// </summary>
			CULL_FRONT,
			/// <summary>
			/// No culling
			/// </summary>
			NO_CULLING,
			/// <summary>
			/// Backface culling
			/// </summary>
			CULL_BACK_MULTISAMPLING,
			/// <summary>
			/// Frontface culling
			/// </summary>
			CULL_FRONT_MULTISAMPLING,
			/// <summary>
			/// No culling
			/// </summary>
			NO_CULLING_MULTISAMPLING,
		}

		public enum HELPER_DEPTH_STATES
		{
			/// <summary>
			///  Test for closest pixel and write
			/// </summary>
			WRITE_CLOSEST,
			/// <summary>
			/// Test for closest or equal pixel and write (usually used after a depth pass)
			/// </summary>
			WRITE_CLOSEST_OR_EQUAL,
			/// <summary>
			/// Test for farthest pixel and write
			/// </summary>
			WRITE_FARTHEST,
			/// <summary>
			/// Test for farthest or equal pixel and write
			/// </summary>
			WRITE_FARTHEST_OR_EQUAL,
			/// <summary>
			/// Test for equal pixel and write
			/// </summary>
			WRITE_EQUAL,
			/// <summary>
			/// Always write
			/// </summary>
			WRITE_ALWAYS,
			/// <summary>
			/// Test for closest pixel but don't write
			/// </summary>
			NOWRITE_CLOSEST,
			/// <summary>
			/// Test for closest or equal pixel but don't write
			/// </summary>
			NOWRITE_CLOSEST_OR_EQUAL,
			/// <summary>
			/// Test for farthest pixel but don't write
			/// </summary>
			NOWRITE_FARTHEST,
			/// <summary>
			/// Test for farthest or equal pixel but don't write
			/// </summary>
			NOWRITE_FARTHEST_OR_EQUAL,
			/// <summary>
			/// Test for equal pixel but don't write
			/// </summary>
			NOWRITE_EQUAL,
			/// <summary>
			/// Depth disabled
			/// </summary>
			DISABLED,
		}

		public enum HELPER_BLEND_STATES
		{
			/// <summary>
			/// No blending 
			/// </summary>
			DISABLED,
			/// <summary>
			/// Additive blending (Dest += Source)
			/// </summary>
			ADDITIVE,
			/// <summary>
			/// Subtractive blending (Dest -= Source)
			/// </summary>
			SUBTRACTIVE,
			/// <summary>
			/// Standard blending (Dest = AlphaSource*Source + (1-AlphaSource)*Dest)
			/// </summary>
			BLEND,
			/// <summary>
			/// Pre-multiplied alpha (Dest = Source + (1-AlphaSource)*Dest)
			/// </summary>
			PREMULTIPLIED_ALPHA,
			/// <summary>
			/// Minimum pass (Dest = MIN( Source, Dest ))
			/// </summary>
			MIN,
			/// <summary>
			/// Maximum pass (Dest = MAX( Source, Dest ))
			/// </summary>
			MAX,
			/// <summary>
			/// Alpha to coverage enabled
			/// </summary>
			ALPHA2COVERAGE,
		}

		/// <summary>
		/// This class is used to describe one of the semantics that is part of a shader interface
		/// </summary>
		public class	ShaderInterfaceSemantic
		{
			#region FIELDS

			protected string	m_SemanticName = null;
			protected Type		m_SemanticType = null;

			#endregion

			#region PROPERTIES

			public string	SemanticName	{ get { return m_SemanticName; } }
			public Type		SemanticType	{ get { return m_SemanticType; } }

			#endregion

			#region METHODS

			public	ShaderInterfaceSemantic( string _SemanticName, Type _SemanticType )
			{
				m_SemanticName = _SemanticName;
				m_SemanticType = _SemanticType;
			}

			#endregion
		}

		/// <summary>
		/// This class contains a registered shader interface declaration
		/// </summary>
		public class		ShaderInterfaceEntry
		{
			#region FIELDS

			protected Type						m_Type = null;
			protected ShaderInterfaceSemantic[]	m_Semantics = null;

			#endregion

			#region PROPERTIES

			/// <summary>
			/// Gets the interface type
			/// </summary>
			public Type							Type		{ get { return m_Type; } }

			/// <summary>
			/// Gets the list of semantics a shader must provide to be said to implement this interface
			/// </summary>
			public ShaderInterfaceSemantic[]	Semantics	{ get { return m_Semantics; } }

			#endregion

			#region METHODS

			public	ShaderInterfaceEntry( Type _Type, ShaderInterfaceSemantic[] _Semantics )
			{
				m_Type = _Type;
				m_Semantics = _Semantics;
			}

			/// <summary>
			/// Creates an instance of an object "implementing" the shader interface
			/// </summary>
			/// <returns></returns>
			public IShaderInterface	CreateInstance()
			{
				System.Reflection.ConstructorInfo	Const = m_Type.GetConstructor( new Type[0] );
				return Const.Invoke( new Object[0] ) as IShaderInterface;
			}

			#endregion
		}

		public delegate void	ShaderInterfaceEventHandler( IShaderInterfaceProvider _Provider );

		/// <summary>
		/// This class stores profiling informations
		/// </summary>
		public class	ProfileTaskInfos
		{
			#region FIELDS

			protected Device			m_Owner = null;
			protected ProfileTaskInfos	m_Previous = null;
			protected ProfileTaskInfos	m_Next = null;

			protected Component			m_Source = null;
			protected string			m_Category = "";
			protected string			m_InfoString = "";
			protected DateTime			m_ProfileTime;

			#endregion

			#region PROPERTIES

			public ProfileTaskInfos		Previous	{ get { return m_Previous; } internal set { m_Previous = value; } }
			public ProfileTaskInfos		Next		{ get { return m_Next; } internal set { m_Next = value; } }
			public ProfileTaskInfos		Last		{ get { return m_Next != null ? m_Next.Last : this; } }

			/// <summary>
			/// Gets the component that generated this profile information
			/// </summary>
			public Component		Source		{ get { return m_Source; } }

			/// <summary>
			/// Gets the category
			/// </summary>
			public string			Category	{ get { return m_Category; } }

			/// <summary>
			/// Gets the information string
			/// </summary>
			public string			InfoString	{ get { return m_InfoString; } }

			/// <summary>
			/// Gets the time (in milliseconds) this tasks costs
			/// </summary>
			public double			Duration	{ get { return m_Next != null ? (m_Next.m_ProfileTime - m_ProfileTime).TotalMilliseconds : 0.0; } }

			/// <summary>
			/// Gets the cost ratio of this task compared to all other tasks
			/// </summary>
			public double			DurationRatio	{ get { return Duration / m_Owner.ProfileTotalDuration; } }

			#endregion

			#region METHODS

			public ProfileTaskInfos( Device _OWner )
			{
				m_Owner = _OWner;
			}

			public void		Update( Component _Source, string _Category, string _InfoString )
			{
				m_Source = _Source;
				m_Category = _Category;
				m_InfoString = _InfoString;
				m_ProfileTime = DateTime.Now;
			}

			public override string ToString()
			{
				return (m_Source != null ? m_Source.Name : "<ANONYMOUS>") + " - " + m_Category + " : " + m_InfoString;
			}

			#endregion
		}

		/// <summary>
		/// Very useful class that tells if a window is covered by another one
		/// (from http://social.msdn.microsoft.com/Forums/en/netfxbcl/thread/78289886-f3c1-405b-aaa1-722a23690245)
		/// </summary>
		protected static class Window
		{
			/// <summary>
			/// Tells if the specified window is partially covered by another window
			/// </summary>
			/// <param name="_Window">The window to test for (must be a Form !)</param>
			/// <param name="_FullyCovered">True to test if the window is fully covered !</param>
			/// <returns></returns>
			public static bool IsOverlapped( Control _ViewportControl, bool _bFullyCovered )
			{
				Form	ParentForm = _ViewportControl.FindForm();
				IntPtr	hWnd = ParentForm.Handle;
				if ( !IsWindowVisible( hWnd ) || ParentForm.ContainsFocus )
					return false;

				// The set is used to make calling GetWindow in a loop stable by checking if we have already
				//  visited the _Window returned by GetWindow. This avoids the possibility of an infinite loop.
				HashSet<IntPtr> visited = new HashSet<IntPtr> { hWnd };

				// Get viewport rectangle
				RECT thisRect;
				GetWindowRect( _ViewportControl.Handle, out thisRect );

				// Check higher Z-order windows for intersection
				while ( (hWnd = GetWindow(hWnd, GW_HWNDPREV)) != IntPtr.Zero && !visited.Contains(hWnd) )
				{
					visited.Add(hWnd);

					RECT testRect, intersection;
					if ( IsWindowVisible(hWnd) && GetWindowRect(hWnd, out testRect) && IntersectRect(out intersection, ref thisRect, ref testRect) )
					{
						if ( !_bFullyCovered )
							return	true;	// True in case of any intersection...

						// Check if the intersection is the entire rectangle itself
						if ( intersection.left == thisRect.left &&
							 intersection.right == thisRect.right &&
							 intersection.top == thisRect.top &&
							 intersection.bottom == thisRect.bottom )
							return true;	// Only then we have a full intersection...
					}
				}

				return false;
			}

			[System.Runtime.InteropServices.DllImport("user32.dll")]
			private static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
			private static extern bool GetWindowRect(IntPtr hWnd, [System.Runtime.InteropServices.Out] out RECT lpRect);
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
			private static extern bool IntersectRect([System.Runtime.InteropServices.Out] out RECT lprcDst, [System.Runtime.InteropServices.In] ref RECT lprcSrc1, [System.Runtime.InteropServices.In] ref RECT lprcSrc2);
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
			private static extern bool IsWindowVisible(IntPtr hWnd);

			[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
			private struct RECT
			{
				public int left;
				public int top;
				public int right;
				public int bottom;
			}

			private const int GW_HWNDPREV = 3;
		}

		#endregion

		#region FIELDS

		// Direct X data (device + swap chain and default render target + depth stencil)
		protected new SharpDX.Direct3D10.Device1	m_Device = null;
		protected ShaderModel						m_ShaderModel = ShaderModel.Empty;
		protected string							m_EffectCompilationProfile = "";
		protected SwapChain							m_SwapChain = null;
		protected bool								m_bWindowOccluded = false;
		protected RenderTarget<PF_RGBA8>			m_DefaultRenderTarget = null;
		protected DepthStencil<PF_D32>				m_DefaultDepthStencil = null;
		protected int								m_DefaultDepthStencilMultiSamplesCount = 1;
		protected RasterizerState					m_DefaultRasterizerState = null;
		protected DepthStencilState					m_DefaultDepthStencilState = null;
		protected BlendState						m_DefaultBlendState = null;

		// Stock states
		protected RasterizerState					m_RasterizerStateNoCulling = null;
		protected RasterizerState					m_RasterizerStateCullFront = null;
		protected RasterizerState					m_RasterizerStateCullBackMultiSampling = null;
		protected RasterizerState					m_RasterizerStateNoCullingMultiSampling = null;
		protected RasterizerState					m_RasterizerStateCullFrontMultiSampling = null;

		protected DepthStencilState					m_DepthStateWriteTestClosestOrEqual = null;
		protected DepthStencilState					m_DepthStateWriteTestFarthest = null;
		protected DepthStencilState					m_DepthStateWriteTestFarthestOrEqual = null;
		protected DepthStencilState					m_DepthStateWriteTestEqual = null;
		protected DepthStencilState					m_DepthStateWriteAlways = null;
		protected DepthStencilState					m_DepthStateNoWriteTestClosest = null;
		protected DepthStencilState					m_DepthStateNoWriteTestClosestOrEqual = null;
		protected DepthStencilState					m_DepthStateNoWriteTestFarthest = null;
		protected DepthStencilState					m_DepthStateNoWriteTestFarthestOrEqual = null;
		protected DepthStencilState					m_DepthStateNoWriteTestEqual = null;
		protected DepthStencilState					m_DepthStateDisabled = null;

		protected BlendState						m_BlendStateAdditive = null;
		protected BlendState						m_BlendStateSubtractive = null;
		protected BlendState						m_BlendStateBlend = null;
		protected BlendState						m_BlendStatePremultiplyAlpha = null;
		protected BlendState						m_BlendStateMin = null;
		protected BlendState						m_BlendStateMax = null;
		protected BlendState						m_BlendStateAlpha2Coverage = null;

		// Stack of current materials
		protected Stack<IMaterial>					m_CurrentMaterialStack = new Stack<IMaterial>();

		// Current render targets & depth-stencil
		protected IRenderTarget[]					m_CurrentRenderTargets = null;
		protected IRenderTarget3D					m_CurrentRenderTarget3D = null;
		protected IDepthStencil						m_CurrentDepthStencil = null;


		// Default instances
		protected Texture2D<PF_RGBA8>				m_MissingTexture = null;

		// Windows Control used for rendering
		protected Control							m_TargetControl = null;

		// Shader interface data
		protected List<ShaderInterfaceEntry>				m_RegisteredShaderInterfaces = new List<ShaderInterfaceEntry>();
		protected Dictionary<Type,ShaderInterfaceEntry>		m_InterfaceType2ShaderInterface = new Dictionary<Type,ShaderInterfaceEntry>();
		protected Dictionary<string,ShaderInterfaceEntry>	m_Semantic2ShaderInterface = new Dictionary<string,ShaderInterfaceEntry>();

		protected Dictionary<Type,Stack<IShaderInterfaceProvider>>	m_InterfaceType2Providers = new Dictionary<Type,Stack<IShaderInterfaceProvider>>();

		// Profiling data
		protected bool								m_bProfilingStarted = false;
		protected ProfileTaskInfos					m_ProfilingRootUsed = null;
		protected ProfileTaskInfos					m_ProfilingRootFree = null;
		protected DateTime							m_ProfilingStartTime;
		protected DateTime							m_ProfilingEndTime;
		protected bool								m_bFlushOnEveryTask = false;

#if DEBUG
		// Debug Helpers
		protected Component							m_LastUsedVertexBuffer = null;
		protected Component							m_LastUsedIndexBuffer = null;
#endif

		// The singleton device
		protected static Device						ms_Instance = null;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the actual DirectX device
		/// </summary>
		public SharpDX.Direct3D10.Device1	DirectXDevice
		{
			get { return m_Device; }
		}

		/// <summary>
		/// Tells if we're dealing with a 10.1 device
		/// </summary>
		public bool							DirectX10_1_ExtensionAvailable
		{
			get { return m_Device.FeatureLevel == SharpDX.Direct3D10.FeatureLevel.Level_10_1; }
		}

		/// <summary>
		/// Gets the supported shader model major version number (e.g. for shader model 4.1 it will return 4)
		/// </summary>
		public ShaderModel					SupportedShaderModel
		{
			get { return m_ShaderModel; }
		}

		/// <summary>
		/// Gets the effect compilation profile string to use when calling the effect compiler
		/// The string depends on the supported shader model
		/// </summary>
		public string						EffectCompilationProfileString
		{
			get { return m_EffectCompilationProfile; }
		}

		/// <summary>
		/// Gets the swap chain
		/// </summary>
		public SwapChain					SwapChain
		{
			get { return m_SwapChain; }
		}

		/// <summary>
		/// Tells if the window is occluded (no rendering should be done then)
		/// </summary>
		public bool							IsWindowOccluded
		{
			get
			{
				if ( m_bWindowOccluded )
					return true;

				// If we don't have focus, check if our window is overlapped
				if ( !m_TargetControl.Focused )
				{
					if ( Window.IsOverlapped( m_TargetControl, true ) )
						return true;
				}

				return false;
			}
		}

		/// <summary>
		/// Gets the default render target for screen rendering
		/// </summary>
		public RenderTarget<PF_RGBA8>		DefaultRenderTarget
		{
			get { return m_DefaultRenderTarget; }
		}

		/// <summary>
		/// Gets the list of currently assigned render targets (null if none is set or a 3D target is set or if the render targets have been set via RenderTargetViews)
		/// </summary>
		public IRenderTarget[]				CurrentRenderTargets
		{
			get { return m_CurrentRenderTargets; }
			protected set
			{
				m_CurrentRenderTargets = value;
				m_CurrentRenderTarget3D = null;
			}
		}

		protected IRenderTarget				CurrentRenderTarget { set { CurrentRenderTargets = value != null ? new IRenderTarget[] { value } : null; } }

		/// <summary>
		/// Gets the currently assigned render targets (null if none is set or 2D targets are set or if the 3D render target has been set via a RenderTargetView)
		/// </summary>
		public IRenderTarget3D				CurrentRenderTarget3D
		{
			get { return m_CurrentRenderTarget3D; }
			protected set
			{
				m_CurrentRenderTarget3D = value;
				m_CurrentRenderTargets = null;
			}
		}

		/// <summary>
		/// Gets the default depth stencil for screen rendering
		/// </summary>
		public DepthStencil<PF_D32>			DefaultDepthStencil
		{
			get { return m_DefaultDepthStencil; }
		}

		/// <summary>
		/// Gets the currently assigned depth stencil buffer (null if none is set or if one has been set via a DepthStencilView)
		/// </summary>
		public IDepthStencil				CurrentDepthStencil
		{
			get { return m_CurrentDepthStencil; }
			protected set { m_CurrentDepthStencil = value; }
		}

		/// <summary>
		/// Gets the list of registered shader interfaces
		/// </summary>
		public ShaderInterfaceEntry[]		RegisteredShaderInterfaces
		{
			get { return m_RegisteredShaderInterfaces.ToArray(); }
		}

		/// <summary>
		/// Gets the list of registered shader interface providers
		/// </summary>
		public IShaderInterfaceProvider[]	RegisteredShaderInterfaceProviders
		{
			get
			{
				List<IShaderInterfaceProvider>	Result = new List<IShaderInterfaceProvider>();
				foreach ( Stack<IShaderInterfaceProvider> ProvidersStack in m_InterfaceType2Providers.Values )
					if ( ProvidersStack.Count > 0 )
						Result.Add( ProvidersStack.Peek() );

				return Result.ToArray();
			}
		}

		public event ShaderInterfaceEventHandler	ShaderInterfaceProviderAdded;
		public event ShaderInterfaceEventHandler	ShaderInterfaceProviderRemoved;

		/// <summary>
		/// Gets the default "Missing Texture" texture we use for un-initialized texture parameters
		/// </summary>
		public Texture2D<PF_RGBA8>			MissingTexture		{ get { return m_MissingTexture; } }

		/// <summary>
		/// Gets the material currently in use
		/// Can be null if not using a material (i.e. not withing a "UseLock()" loop)
		/// </summary>
		public IMaterial					CurrentMaterial		{ get { return m_CurrentMaterialStack.Count > 0 ? m_CurrentMaterialStack.Peek() : null; } }

		/// <summary>
		/// Notifies that a material effect was recompiled, you can use that to globally monitor compilation errors
		/// </summary>
		public event EventHandler			MaterialEffectRecompiled;

		/// <summary>
		/// Gets the root profiling task
		/// </summary>
		public ProfileTaskInfos				ProfilingRootTask		{ get { return m_ProfilingRootUsed; } }

		/// <summary>
		/// Tells if the profiling has started (use this to know if you should add profiling tasks or not)
		/// </summary>
		public bool							HasProfilingStarted		{ get { return m_bProfilingStarted; } }

		/// <summary>
		/// Gets the total duration (in milliseconds) of the last completed profiling
		/// </summary>
		public double						ProfileTotalDuration	{ get { return (m_ProfilingEndTime - m_ProfilingStartTime).TotalMilliseconds; } }

		/// <summary>
		/// Occurs whenever the profiling started
		/// </summary>
		public event EventHandler			ProfilingStarted;

		/// <summary>
		/// Occurs whenever the profiling ended
		/// </summary>
		public event EventHandler			ProfilingStopped;

		#region Device Wrapping

		public SharpDX.Direct3D10.InputAssemblerStage	InputAssembler
		{
			get { return m_Device.InputAssembler; }
		}

        public SharpDX.Direct3D10.OutputMergerStage		OutputMerger
		{
			get { return m_Device.OutputMerger; }
		}

        public SharpDX.Direct3D10.VertexShaderStage		VertexShader
		{
			get { return m_Device.VertexShader; }
		}

        public SharpDX.Direct3D10.PixelShaderStage		PixelShader
		{
			get { return m_Device.PixelShader; }
		}

        public SharpDX.Direct3D10.GeometryShaderStage	GeometryShader
		{
			get { return m_Device.GeometryShader; }
		}

        public SharpDX.Direct3D10.StreamOutputStage		StreamOutput
		{
			get { return m_Device.StreamOutput; }
		}

        public SharpDX.Direct3D10.RasterizerStage 		Rasterizer
		{
			get { return m_Device.Rasterizer; }
		}

		#endregion

#if DEBUG
		// Debug Helpers
		internal Component					LastUsedVertexBuffer	{ get { return m_LastUsedVertexBuffer; } set { m_LastUsedVertexBuffer = value; } }
		internal Component					LastUsedIndexBuffer		{ get { return m_LastUsedIndexBuffer; } set { m_LastUsedIndexBuffer = value; } }
#endif

		/// <summary>
		/// Gets the singleton device
		/// </summary>
		public static Device				Instance
		{
			get
			{
				if ( ms_Instance == null )
					throw new Exception( "The device was not created ! Use CreateInstance() to create it before using this singleton..." );

				return	ms_Instance;
			}
		}

		#endregion

		#region METHODS

		protected	Device( SharpDX.Direct3D10.Device1 _Device, SwapChain _SwapChain, Control _TargetControl, int _DepthStencilMultiSamplesCount ) : this( _Device, _SwapChain, _TargetControl, _DepthStencilMultiSamplesCount, false )
		{
		}

		protected	Device( SharpDX.Direct3D10.Device1 _Device, SwapChain _SwapChain, Control _TargetControl, int _DepthStencilMultiSamplesCount, bool _bReadableDepthStencil ) : base( null, "Device Singleton" )
		{
			ms_Instance = this;
			m_Device = ToDispose( _Device );
			m_SwapChain = ToDispose( _SwapChain );
			m_TargetControl = _TargetControl;
			m_DefaultDepthStencilMultiSamplesCount = _DepthStencilMultiSamplesCount;

			//////////////////////////////////////////////////////////////////////////
			// Build the supported shader model
			int	Major = 0, Minor = 0;
			switch ( m_Device.FeatureLevel )
			{
				case SharpDX.Direct3D10.FeatureLevel.Level_9_1:
					Major = 2;
					Minor = 0;
					m_EffectCompilationProfile = "fx_2_0";
					break;
				case SharpDX.Direct3D10.FeatureLevel.Level_9_2:
					Major = 2;
					Minor = 1;
					m_EffectCompilationProfile = "fx_2_1";
					break;
				case SharpDX.Direct3D10.FeatureLevel.Level_9_3:
					Major = 3;
					Minor = 0;
					m_EffectCompilationProfile = "fx_3_0";
					break;
				case SharpDX.Direct3D10.FeatureLevel.Level_10_0:
					Major = 4;
					Minor = 0;
					m_EffectCompilationProfile = "fx_4_0";
					break;
				case SharpDX.Direct3D10.FeatureLevel.Level_10_1:
					Major = 4;
					Minor = 1;
					m_EffectCompilationProfile = "fx_4_1";
					break;
			}
			m_ShaderModel = new ShaderModel( Major, Minor );


			//////////////////////////////////////////////////////////////////////////
			// Create the default render target & depth-stencil
			Texture2D			BackBuffer = ToDispose( Texture2D.FromSwapChain<Texture2D>( m_SwapChain, 0 ) );
			RenderTargetView	DefaultRenderTargetView = ToDispose( new RenderTargetView( m_Device, BackBuffer ) );

			// Here we assume the default render target is always RGBA8, waiting for HDR displays...
			m_DefaultRenderTarget = new RenderTarget<PF_RGBA8>( this, "Default Render Target", BackBuffer, DefaultRenderTargetView );
			m_DefaultDepthStencil = ToDispose( new DepthStencil<PF_D32>( this, "Default Depth Stencil", m_DefaultRenderTarget.Width, m_DefaultRenderTarget.Height, m_DefaultDepthStencilMultiSamplesCount, _bReadableDepthStencil ) );

			// Use them
			SetDefaultRenderTarget();

			//////////////////////////////////////////////////////////////////////////
			// Build default rasterizer state
			RasterizerStateDescription	StateDesc = GetDefaultRasterizerStateDescription();

			m_DefaultRasterizerState = ToDispose( new RasterizerState( m_Device, StateDesc ) );
			m_Device.Rasterizer.State = m_DefaultRasterizerState;

			// Create additional useful states
			StateDesc.CullMode = CullMode.None;
			m_RasterizerStateNoCulling = ToDispose( new RasterizerState( m_Device, StateDesc ) );

			StateDesc.CullMode = CullMode.Front;
			m_RasterizerStateCullFront = ToDispose( new RasterizerState( m_Device, StateDesc ) );

				// With multisampling enabled
			StateDesc.IsMultisampleEnabled = true;
			StateDesc.CullMode = CullMode.Back;
			m_RasterizerStateCullBackMultiSampling = ToDispose( new RasterizerState( m_Device, StateDesc ) );

			StateDesc.CullMode = CullMode.None;
			m_RasterizerStateNoCullingMultiSampling = ToDispose( new RasterizerState( m_Device, StateDesc ) );

			StateDesc.CullMode = CullMode.Front;
			m_RasterizerStateCullFrontMultiSampling = ToDispose( new RasterizerState( m_Device, StateDesc ) );

			//////////////////////////////////////////////////////////////////////////
			// Build default depth-stencil state
			DepthStencilStateDescription	DepthStateDesc = GetDefaultDepthStencilStateDescription();

			m_DefaultDepthStencilState = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );
			m_Device.OutputMerger.SetDepthStencilState( m_DefaultDepthStencilState, 0 );

			// Create additional useful states
			DepthStateDesc.DepthComparison = Comparison.LessEqual;
			m_DepthStateWriteTestClosestOrEqual = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );
			DepthStateDesc.DepthComparison = Comparison.Greater;
			m_DepthStateWriteTestFarthest = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );
			DepthStateDesc.DepthComparison = Comparison.GreaterEqual;
			m_DepthStateWriteTestFarthestOrEqual = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );
			DepthStateDesc.DepthComparison = Comparison.Equal;
			m_DepthStateWriteTestEqual = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );
			DepthStateDesc.DepthComparison = Comparison.Always;
			m_DepthStateWriteAlways = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );

			// Non-writing states
			DepthStateDesc.DepthWriteMask = DepthWriteMask.Zero;
			DepthStateDesc.DepthComparison = Comparison.Less;
			m_DepthStateNoWriteTestClosest = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );
			DepthStateDesc.DepthComparison = Comparison.LessEqual;
			m_DepthStateNoWriteTestClosestOrEqual = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );
			DepthStateDesc.DepthComparison = Comparison.Greater;
			m_DepthStateNoWriteTestFarthest = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );
			DepthStateDesc.DepthComparison = Comparison.GreaterEqual;
			m_DepthStateNoWriteTestFarthestOrEqual = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );
			DepthStateDesc.DepthComparison = Comparison.Equal;
			m_DepthStateNoWriteTestEqual = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );

			// Fully disabled (no testing, no writing)
			DepthStateDesc.IsDepthEnabled = false;
			m_DepthStateDisabled = ToDispose( new DepthStencilState( m_Device, DepthStateDesc ) );

			//////////////////////////////////////////////////////////////////////////
			// Build default blend state
			BlendStateDescription	BlendStateDesc = GetDefaultBlendStateDescription();

			m_DefaultBlendState = ToDispose( new BlendState( m_Device, BlendStateDesc ) );
			m_Device.OutputMerger.SetBlendState( m_DefaultBlendState, (Color4) System.Drawing.Color.White, ~0 );

			// Create additional useful states
			BlendStateDesc.IsAlphaToCoverageEnabled = true;
			m_BlendStateAlpha2Coverage = ToDispose( new BlendState( m_Device, BlendStateDesc ) );

			BlendStateDesc.IsAlphaToCoverageEnabled = false;
			BlendStateDesc.BlendOperation = BlendOperation.Add;
			BlendStateDesc.AlphaBlendOperation = BlendOperation.Add;
			BlendStateDesc.SourceBlend = BlendOption.One;
			BlendStateDesc.DestinationBlend = BlendOption.One;
			BlendStateDesc.SourceAlphaBlend = BlendOption.One;
			BlendStateDesc.DestinationAlphaBlend = BlendOption.One;
		    BlendStateDesc.IsBlendEnabled[0] = true;
			m_BlendStateAdditive = ToDispose( new BlendState( m_Device, BlendStateDesc ) );

			BlendStateDesc.BlendOperation = BlendOperation.ReverseSubtract;
			BlendStateDesc.AlphaBlendOperation = BlendOperation.ReverseSubtract;
			m_BlendStateSubtractive = ToDispose( new BlendState( m_Device, BlendStateDesc ) );

			BlendStateDesc.BlendOperation = BlendOperation.Add;
			BlendStateDesc.AlphaBlendOperation = BlendOperation.Add;
			BlendStateDesc.SourceBlend = BlendOption.SourceAlpha;
			BlendStateDesc.DestinationBlend = BlendOption.InverseSourceAlpha;
			BlendStateDesc.SourceAlphaBlend = BlendOption.SourceAlpha;
			BlendStateDesc.DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
			m_BlendStateBlend = ToDispose( new BlendState( m_Device, BlendStateDesc ) );

			BlendStateDesc.SourceBlend = BlendOption.One;
			BlendStateDesc.DestinationBlend = BlendOption.InverseSourceAlpha;
			BlendStateDesc.SourceAlphaBlend = BlendOption.One;
			BlendStateDesc.DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
			m_BlendStatePremultiplyAlpha = ToDispose( new BlendState( m_Device, BlendStateDesc ) );

			BlendStateDesc.SourceBlend = BlendOption.One;
			BlendStateDesc.DestinationBlend = BlendOption.One;
			BlendStateDesc.BlendOperation = BlendOperation.Minimum;
			BlendStateDesc.SourceAlphaBlend = BlendOption.One;
			BlendStateDesc.DestinationAlphaBlend = BlendOption.One;
			BlendStateDesc.AlphaBlendOperation = BlendOperation.Minimum;
			m_BlendStateMin = ToDispose( new BlendState( m_Device, BlendStateDesc ) );

			BlendStateDesc.BlendOperation = BlendOperation.Maximum;
			BlendStateDesc.AlphaBlendOperation = BlendOperation.Maximum;
			m_BlendStateMax = ToDispose( new BlendState( m_Device, BlendStateDesc ) );

			//////////////////////////////////////////////////////////////////////////
			// Declare some basic shader interfaces
			DeclareShaderInterface( typeof(ICamera) );
			DeclareShaderInterface( typeof(IDirectionalLight) );
			DeclareShaderInterface( typeof(IDirectionalLight2) );
			DeclareShaderInterface( typeof(ILinearToneMapping) );
			DeclareShaderInterface( typeof(IReadableZBuffer) );
			if ( _bReadableDepthStencil )
				RegisterShaderInterfaceProvider( typeof(IReadableZBuffer), this );

			//////////////////////////////////////////////////////////////////////////
			// Create the small default "missing texture"
			using ( Image<PF_RGBA8> MissingTextureImage = new Image<PF_RGBA8>( this, "Missing Texture Image", Properties.Resources.MissingTexture, 0, 1.0f ) )
				m_MissingTexture = ToDispose( new Texture2D<PF_RGBA8>( this, "Missing Texture", MissingTextureImage ) );
		}

		/// <summary>
		/// Tells if the device supports the specified shader model
		/// </summary>
		/// <param name="_ShaderModel">The shader model to test for support</param>
		/// <returns>True if the shader model is supported</returns>
		public bool		SupportsShaderModel( ShaderModel _ShaderModel )
		{
			return m_ShaderModel.CanSupport( _ShaderModel );
		}

		/// <summary>
		/// Sets the default render target as output for rendering
		/// </summary>
		public void	SetDefaultRenderTarget()
		{
			SetRenderTarget( m_DefaultRenderTarget, m_DefaultDepthStencil );
			SetViewport( 0, 0, m_DefaultRenderTarget.Width, m_DefaultRenderTarget.Height, 0.0f, 1.0f );
		}

		/// <summary>
		/// This should be called at the beginning of your render loop to skip rendering altogether if the
		///  viewport is completely obstructed by another higher Z-order window.
		/// If the viewport is hidden then the function returns false and sleeps for the specified amount of time.
		/// This is important to avoid the renderer go crazy with its commands stack when switching focus
		///  to another application covering the entire viewport (mainly, the debugger)
		/// </summary>
		/// <param name="_MilliSecondsSleep">The time to sleep if the viewport is obstructed</param>
		/// <returns></returns>
		public bool	CheckCanRender( int _MilliSecondsSleep )
		{
			if ( !IsWindowOccluded )
				return true;	// Can render...

			// Don't render and sleep for a while...
			System.Threading.Thread.Sleep( _MilliSecondsSleep );
			return false;
		}

		/// <summary>
		/// Presents the render target with default presentation flags (usual case)
		/// </summary>
		public void	Present()
		{
			Result	R = m_SwapChain.Present( 0, IsWindowOccluded ? PresentFlags.Test : PresentFlags.None );
			if ( R.Code == (int) DXGIStatus.Occluded )
			{	// Don't render until we're unoccluded again...
				m_bWindowOccluded = true;
			}
			else if ( m_bWindowOccluded )
			{	// We're not occluded anymore
				m_bWindowOccluded = false;
			}
		}

		/// <summary>
		/// Sets the new render target
		/// </summary>
		/// <param name="_RenderTarget"></param>
		public void	SetRenderTarget( IRenderTarget _RenderTarget )
		{
			CurrentRenderTarget = _RenderTarget;
			CurrentDepthStencil = null;
			SetRenderTarget( _RenderTarget != null ? _RenderTarget.RenderTargetView : null );
		}

		/// <summary>
		/// Sets the new 3D render target
		/// </summary>
		/// <param name="_RenderTarget"></param>
		public void	SetRenderTarget( IRenderTarget3D _RenderTarget )
		{
			CurrentRenderTarget3D = _RenderTarget;
			CurrentDepthStencil = null;
			SetRenderTarget( _RenderTarget != null ? _RenderTarget.RenderTargetView : null );
		}

		/// <summary>
		/// Sets the new render target
		/// </summary>
		/// <param name="_RenderTargetView"></param>
		public void	SetRenderTarget( RenderTargetView _RenderTargetView )
		{
			CurrentRenderTargets = null;
			CurrentDepthStencil = null;
			m_Device.OutputMerger.SetTargets( _RenderTargetView );
		}

		/// <summary>
		/// Sets the new render target
		/// </summary>
		/// <param name="_RenderTarget"></param>
		/// <param name="_DepthStencil"></param>
		public void	SetRenderTarget( IRenderTarget _RenderTarget, IDepthStencil _DepthStencil )
		{
			CurrentRenderTarget = _RenderTarget;
			CurrentDepthStencil = _DepthStencil;
			SetRenderTarget( _RenderTarget != null ? _RenderTarget.RenderTargetView : null, _DepthStencil != null ? _DepthStencil.DepthStencilView : null );
		}

		/// <summary>
		/// Sets the new 3D render target
		/// </summary>
		/// <param name="_RenderTarget"></param>
		/// <param name="_DepthStencil"></param>
		public void	SetRenderTarget( IRenderTarget3D _RenderTarget, IDepthStencil _DepthStencil )
		{
			CurrentRenderTarget3D = _RenderTarget;
			CurrentDepthStencil = _DepthStencil;
			SetRenderTarget( _RenderTarget != null ? _RenderTarget.RenderTargetView : null, _DepthStencil != null ? _DepthStencil.DepthStencilView : null );
		}

		/// <summary>
		/// Sets the new render target
		/// </summary>
		/// <param name="_RenderTargetView"></param>
		/// <param name="_DepthStencilView"></param>
		public void	SetRenderTarget( RenderTargetView _RenderTargetView, DepthStencilView _DepthStencilView )
		{
			CurrentRenderTarget = null;
			CurrentDepthStencil = null;
			m_Device.OutputMerger.SetTargets( _DepthStencilView, _RenderTargetView );
		}

		/// <summary>
		/// Sets the new render targets for MRT
		/// </summary>
		/// <param name="_RenderTargets"></param>
		public void	SetMultipleRenderTargets<PF>( RenderTarget<PF>[] _RenderTargets ) where PF:struct,IPixelFormat
		{
			CurrentRenderTargets = _RenderTargets;
			CurrentDepthStencil = null;

			RenderTargetView[]	Views = new RenderTargetView[_RenderTargets.Length];
			for ( int RenderTargetIndex=0; RenderTargetIndex < _RenderTargets.Length; RenderTargetIndex++ )
				Views[RenderTargetIndex] = _RenderTargets[RenderTargetIndex].RenderTargetView;

			SetMultipleRenderTargets( Views );
		}

		public void	SetMultipleRenderTargets( RenderTargetView[] _RenderTargets )
		{
			CurrentRenderTargets = null;
			CurrentDepthStencil = null;

			m_Device.OutputMerger.SetTargets( _RenderTargets );
		}

		/// <summary>
		/// Sets the new render targets for MRT
		/// </summary>
		/// <param name="_RenderTargets"></param>
		public void	SetMultipleRenderTargets<PF,D>( RenderTarget<PF>[] _RenderTargets, DepthStencil<D> _DepthStencil ) where PF:struct,IPixelFormat where D:struct,IDepthFormat
		{
			CurrentRenderTargets = _RenderTargets;
			CurrentDepthStencil = _DepthStencil;

			RenderTargetView[]	Views = new RenderTargetView[_RenderTargets.Length];
			for ( int RenderTargetIndex=0; RenderTargetIndex < _RenderTargets.Length; RenderTargetIndex++ )
				Views[RenderTargetIndex] = _RenderTargets[RenderTargetIndex].RenderTargetView;

			SetMultipleRenderTargets( Views, _DepthStencil != null ? _DepthStencil.DepthStencilView : null );
		}

		public void	SetMultipleRenderTargets( RenderTargetView[] _RenderTargets, DepthStencilView _DepthStencil )
		{
			CurrentRenderTargets = null;
			CurrentDepthStencil = null;

			m_Device.OutputMerger.SetTargets( _DepthStencil, _RenderTargets );
		}

		/// <summary>
		/// Sets the rendering viewport
		/// </summary>
		/// <param name="_X"></param>
		/// <param name="_Y"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_ZMin"></param>
		/// <param name="_ZMax"></param>
		public void SetViewport( int _X, int _Y, int _Width, int _Height, float _ZMin, float _ZMax )
		{
			m_Device.Rasterizer.SetViewports( new Viewport( _X, _Y, _Width, _Height, _ZMin, _ZMax ) );
		}

		/// <summary>
		/// Default setup of a 2D target as viewport covering the entire target
		/// </summary>
		/// <param name="_Target"></param>
		public void SetViewport( ITexture2D _Target )
		{
			if ( _Target == null )
				throw new NException( this, "Invalid target to set as viewport !" );

			SetViewport( 0, 0, _Target.Width, _Target.Height, 0.0f, 1.0f );
		}

		#region Blend/Rasterizer/DepthStencil States

		/// <summary>
		/// Gets the default rasterizer state description used to initialize the device
		/// </summary>
		/// <returns></returns>
		public RasterizerStateDescription	GetDefaultRasterizerStateDescription()
		{
			RasterizerStateDescription	StateDesc = new RasterizerStateDescription();
			StateDesc.CullMode = CullMode.Back;	// Use clock-wise culling as we're using left-handed camera matrices !
			StateDesc.FillMode = FillMode.Solid;
			StateDesc.DepthBias = 0;
			StateDesc.DepthBiasClamp = 0.0f;
			StateDesc.IsAntialiasedLineEnabled = false;
			StateDesc.IsDepthClipEnabled = true;
			StateDesc.IsFrontCounterClockwise = true;
			StateDesc.IsMultisampleEnabled = false;
			StateDesc.IsScissorEnabled = false;
			StateDesc.SlopeScaledDepthBias = 0.0f;

			return StateDesc;
		}

		/// <summary>
		/// Gets the default depth-stencil state description used to initialize the device
		/// </summary>
		/// <returns></returns>
		public DepthStencilStateDescription	GetDefaultDepthStencilStateDescription()
		{
			DepthStencilStateDescription	DepthStateDesc = new DepthStencilStateDescription();
			DepthStateDesc.IsDepthEnabled = true;
			DepthStateDesc.IsStencilEnabled = false;
			DepthStateDesc.DepthComparison = Comparison.Less;
			DepthStateDesc.DepthWriteMask = DepthWriteMask.All;

			return DepthStateDesc;
		}

		/// <summary>
		/// Gets the default blend state description used to initialize the device
		/// </summary>
		/// <returns></returns>
		public BlendStateDescription		GetDefaultBlendStateDescription()
		{
			BlendStateDescription	BlendStateDesc = new BlendStateDescription();
			BlendStateDesc.IsAlphaToCoverageEnabled = false;
			BlendStateDesc.BlendOperation = BlendOperation.Add;
			BlendStateDesc.SourceBlend = BlendOption.One;
			BlendStateDesc.DestinationBlend = BlendOption.Zero;
			BlendStateDesc.AlphaBlendOperation = BlendOperation.Add;
			BlendStateDesc.SourceAlphaBlend = BlendOption.One;
			BlendStateDesc.DestinationAlphaBlend = BlendOption.Zero;
			for ( uint TargetIndex=0; TargetIndex < 8; TargetIndex++ )
			{
				BlendStateDesc.IsBlendEnabled[TargetIndex] = false;
				BlendStateDesc.RenderTargetWriteMask[TargetIndex] = ColorWriteMaskFlags.All;
			}

			return BlendStateDesc;
		}

		/// <summary>
		/// Gets one of the default stock rasterizer states
		/// </summary>
		/// <param name="_State"></param>
		/// <returns></returns>
		public RasterizerState	GetStockRasterizerState( HELPER_STATES _State )
		{
			switch ( _State )
			{
				case HELPER_STATES.CULL_BACK:					return m_DefaultRasterizerState;
				case HELPER_STATES.CULL_FRONT:					return m_RasterizerStateCullFront;
				case HELPER_STATES.NO_CULLING:					return m_RasterizerStateNoCulling;
				case HELPER_STATES.CULL_BACK_MULTISAMPLING:		return m_RasterizerStateCullBackMultiSampling;
				case HELPER_STATES.CULL_FRONT_MULTISAMPLING:	return m_RasterizerStateCullFrontMultiSampling;
				case HELPER_STATES.NO_CULLING_MULTISAMPLING:	return m_RasterizerStateNoCullingMultiSampling;
			}

			return null;
		}

		/// <summary>
		/// Sets one of the default stock rasterizer states (these states are the most common ones)
		/// </summary>
		/// <param name="_State">The stock state to setup</param>
		public void			SetStockRasterizerState( HELPER_STATES _State )
		{
			switch ( _State )
			{
				case HELPER_STATES.CULL_BACK:					m_Device.Rasterizer.State = m_DefaultRasterizerState; break;
				case HELPER_STATES.CULL_FRONT:					m_Device.Rasterizer.State = m_RasterizerStateCullFront; break;
				case HELPER_STATES.NO_CULLING:					m_Device.Rasterizer.State = m_RasterizerStateNoCulling; break;
				case HELPER_STATES.CULL_BACK_MULTISAMPLING:		m_Device.Rasterizer.State = m_RasterizerStateCullBackMultiSampling; break;
				case HELPER_STATES.CULL_FRONT_MULTISAMPLING:	m_Device.Rasterizer.State = m_RasterizerStateCullFrontMultiSampling; break;
				case HELPER_STATES.NO_CULLING_MULTISAMPLING:	m_Device.Rasterizer.State = m_RasterizerStateNoCullingMultiSampling; break;
			}
		}

		/// <summary>
		/// Gets one of the default stock depth-stencil states
		/// </summary>
		/// <param name="_State"></param>
		/// <returns></returns>
		public DepthStencilState	GetStockDepthStencilState( HELPER_DEPTH_STATES _State )
		{
			switch ( _State )
			{
				case HELPER_DEPTH_STATES.WRITE_CLOSEST:				return m_DefaultDepthStencilState;
				case HELPER_DEPTH_STATES.WRITE_CLOSEST_OR_EQUAL:	return m_DepthStateWriteTestClosestOrEqual;
				case HELPER_DEPTH_STATES.WRITE_FARTHEST:			return m_DepthStateWriteTestFarthest;
				case HELPER_DEPTH_STATES.WRITE_FARTHEST_OR_EQUAL:	return m_DepthStateWriteTestFarthestOrEqual;
				case HELPER_DEPTH_STATES.WRITE_EQUAL:				return m_DepthStateWriteTestEqual;
				case HELPER_DEPTH_STATES.WRITE_ALWAYS:				return m_DepthStateWriteAlways;

				// Testing but Non-writing states
				case HELPER_DEPTH_STATES.NOWRITE_CLOSEST:			return m_DepthStateNoWriteTestClosest;
				case HELPER_DEPTH_STATES.NOWRITE_CLOSEST_OR_EQUAL:	return m_DepthStateNoWriteTestClosestOrEqual;
				case HELPER_DEPTH_STATES.NOWRITE_FARTHEST:			return m_DepthStateNoWriteTestFarthest;
				case HELPER_DEPTH_STATES.NOWRITE_FARTHEST_OR_EQUAL:	return m_DepthStateNoWriteTestFarthestOrEqual;
				case HELPER_DEPTH_STATES.NOWRITE_EQUAL:				return m_DepthStateNoWriteTestEqual;

				// Fully disabled
				case HELPER_DEPTH_STATES.DISABLED:					return m_DepthStateDisabled;
			}

			return null;
		}

		/// <summary>
		/// Sets one of the default stock depth-stencil states (these states are the most common ones)
		/// </summary>
		/// <param name="_State">The stock state to setup</param>
		public void			SetStockDepthStencilState( HELPER_DEPTH_STATES _State )
		{
			SetStockDepthStencilState( _State, 0 );
		}
		
		public void			SetStockDepthStencilState( HELPER_DEPTH_STATES _State, int _StencilRef )
		{
			switch ( _State )
			{
				// Testing & Writing states
				case HELPER_DEPTH_STATES.WRITE_CLOSEST:
					m_Device.OutputMerger.SetDepthStencilState( m_DefaultDepthStencilState, _StencilRef );
					break;
				case HELPER_DEPTH_STATES.WRITE_CLOSEST_OR_EQUAL:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateWriteTestClosestOrEqual, _StencilRef );
					break;
				case HELPER_DEPTH_STATES.WRITE_FARTHEST:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateWriteTestFarthest, _StencilRef );
					break;
				case HELPER_DEPTH_STATES.WRITE_FARTHEST_OR_EQUAL:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateWriteTestFarthestOrEqual, _StencilRef );
					break;
				case HELPER_DEPTH_STATES.WRITE_EQUAL:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateWriteTestEqual, _StencilRef );
					break;
				case HELPER_DEPTH_STATES.WRITE_ALWAYS:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateWriteAlways, _StencilRef );
					break;

				// Testing but Non-writing states
				case HELPER_DEPTH_STATES.NOWRITE_CLOSEST:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateNoWriteTestClosest, _StencilRef );
					break;
				case HELPER_DEPTH_STATES.NOWRITE_CLOSEST_OR_EQUAL:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateNoWriteTestClosestOrEqual, _StencilRef );
					break;
				case HELPER_DEPTH_STATES.NOWRITE_FARTHEST:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateNoWriteTestFarthest, _StencilRef );
					break;
				case HELPER_DEPTH_STATES.NOWRITE_FARTHEST_OR_EQUAL:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateNoWriteTestFarthestOrEqual, _StencilRef );
					break;
				case HELPER_DEPTH_STATES.NOWRITE_EQUAL:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateNoWriteTestEqual, _StencilRef );
					break;

				// Fully disabled
				case HELPER_DEPTH_STATES.DISABLED:
					m_Device.OutputMerger.SetDepthStencilState( m_DepthStateDisabled, _StencilRef );
					break;
			}
		}

		/// <summary>
		/// Gets one of the default stock blend states
		/// </summary>
		/// <param name="_State"></param>
		/// <returns></returns>
		public BlendState	GetStockBlendState( HELPER_BLEND_STATES _State )
		{
			switch ( _State )
			{
				case HELPER_BLEND_STATES.DISABLED:				return m_DefaultBlendState;
				case HELPER_BLEND_STATES.ADDITIVE:				return m_BlendStateAdditive;
				case HELPER_BLEND_STATES.SUBTRACTIVE:			return m_BlendStateSubtractive;
				case HELPER_BLEND_STATES.BLEND:					return m_BlendStateBlend;
				case HELPER_BLEND_STATES.PREMULTIPLIED_ALPHA:	return m_BlendStatePremultiplyAlpha;
				case HELPER_BLEND_STATES.MIN:					return m_BlendStateMin;
				case HELPER_BLEND_STATES.MAX:					return m_BlendStateMax;
				case HELPER_BLEND_STATES.ALPHA2COVERAGE:		return m_BlendStateAlpha2Coverage;
			}

			return null;
		}

		/// <summary>
		/// Sets one of the default stock blend states (these states are the most common ones)
		/// </summary>
		/// <param name="_State">The stock state to setup</param>
		public void			SetStockBlendState( HELPER_BLEND_STATES _State )
		{
			SetStockBlendState( _State, (Color4) System.Drawing.Color.White, ~0 );
		}

		public void			SetStockBlendState( HELPER_BLEND_STATES _State, Color4 _BlendFactors, int _CoverageMask )
		{
			switch ( _State )
			{
				case HELPER_BLEND_STATES.DISABLED:
					m_Device.OutputMerger.SetBlendState( m_DefaultBlendState, _BlendFactors, _CoverageMask );
					break;
				case HELPER_BLEND_STATES.ADDITIVE:
					m_Device.OutputMerger.SetBlendState( m_BlendStateAdditive, _BlendFactors, _CoverageMask );
					break;
				case HELPER_BLEND_STATES.SUBTRACTIVE:
					m_Device.OutputMerger.SetBlendState( m_BlendStateSubtractive, _BlendFactors, _CoverageMask );
					break;
				case HELPER_BLEND_STATES.BLEND:
					m_Device.OutputMerger.SetBlendState( m_BlendStateBlend, _BlendFactors, _CoverageMask );
					break;
				case HELPER_BLEND_STATES.PREMULTIPLIED_ALPHA:
					m_Device.OutputMerger.SetBlendState( m_BlendStatePremultiplyAlpha, _BlendFactors, _CoverageMask );
					break;
				case HELPER_BLEND_STATES.MIN:
					m_Device.OutputMerger.SetBlendState( m_BlendStateMin, _BlendFactors, _CoverageMask );
					break;
				case HELPER_BLEND_STATES.MAX:
					m_Device.OutputMerger.SetBlendState( m_BlendStateMax, _BlendFactors, _CoverageMask );
					break;
				case HELPER_BLEND_STATES.ALPHA2COVERAGE:
					m_Device.OutputMerger.SetBlendState( m_BlendStateAlpha2Coverage, _BlendFactors, _CoverageMask );
					break;
			}
		}

		#endregion

		/// <summary>
		/// Called by materials when they are created
		/// </summary>
		/// <param name="_Material"></param>
		internal void		AddMaterial( IMaterial _Material )
		{
			_Material.EffectRecompiled += new EventHandler( Material_EffectRecompiled );
		}

		/// <summary>
		/// Called by materials when they are disposed
		/// </summary>
		/// <param name="_Material"></param>
		internal void		RemoveMaterial( IMaterial _Material )
		{
			_Material.EffectRecompiled -= new EventHandler( Material_EffectRecompiled );
		}

		/// <summary>
		/// Pushes a new current material (called by Material.Synchronize.ctor()) 
		/// </summary>
		/// <param name="_Material"></param>
		internal void		PushCurrentMaterial( IMaterial _Material )
		{
			m_CurrentMaterialStack.Push( _Material );
		}

		/// <summary>
		/// Pops a current material (called by Material.Synchronize.Dispose()) 
		/// </summary>
		/// <param name="_Material"></param>
		internal void		PopCurrentMaterial( IMaterial _Material )
		{
			if ( m_CurrentMaterialStack.Peek() != _Material )
				throw new NException( this, "Current material stack inconsistency ! You pushed too many current materials, did you forget a call to Dispose() on a Material.UseLock() or Material.Synchronize() ?" );

			m_CurrentMaterialStack.Pop();
		}

		#region Shader Interfaces

		/// <summary>
		/// Registers a new shader interface type
		/// </summary>
		/// <param name="_InterfaceType"></param>
		public void	DeclareShaderInterface( Type _InterfaceType )
		{
			if ( _InterfaceType == null )
				throw new NException( this, "Invalid interface type !" );
			if ( m_InterfaceType2ShaderInterface.ContainsKey( _InterfaceType ) )
				throw new NException( this, "Interface type \"" + _InterfaceType.FullName + "\" has already been registered !" );

			// Analyze all properties that have a Semantic attribute
			List<ShaderInterfaceSemantic>	Semantics = new List<ShaderInterfaceSemantic>();

			System.Reflection.PropertyInfo[]	Properties = _InterfaceType.GetProperties();
			foreach ( System.Reflection.PropertyInfo Property in Properties )
			{
				// Retrieve semantic
				SemanticAttribute[]	SemanticAttributes = Property.GetCustomAttributes( typeof(SemanticAttribute), false ) as SemanticAttribute[];
				if ( SemanticAttributes.Length != 1 )
					continue;

				string	Semantic = SemanticAttributes[0].Semantic;

				// Ensure it's the first one we encounter
				if ( m_Semantic2ShaderInterface.ContainsKey( Semantic ) )
					throw new NException( this, "There already exists a shader interface of type \"" + m_Semantic2ShaderInterface[Semantic].Type.FullName + "\" that uses the \"" + Semantic + "\" semantic !" );

				// Register the semantic
				Semantics.Add( new ShaderInterfaceSemantic( Semantic, Property.PropertyType ) );
			}

			if ( Semantics.Count == 0 )
				throw new NException( this, "Not a single property with [Semantic] could be found in the provided interface type !" );

			// Register a new interface with its semantics
			ShaderInterfaceEntry	Interface = new ShaderInterfaceEntry( _InterfaceType, Semantics.ToArray() );
			m_RegisteredShaderInterfaces.Add( Interface );
			m_InterfaceType2ShaderInterface.Add( _InterfaceType, Interface );

			foreach ( ShaderInterfaceSemantic Semantic in Interface.Semantics )
				m_Semantic2ShaderInterface.Add( Semantic.SemanticName, Interface );
		}

		public void	RegisterShaderInterfaceProvider( Type _SupportedInterfaceType, IShaderInterfaceProvider _Provider )
		{
			if ( _SupportedInterfaceType == null )
				throw new NException( this, "Invalid interface type !" );
			if ( _Provider == null )
				throw new NException( this, "Invalid provider for interface \"" + _SupportedInterfaceType.FullName + "\" !" );
			if ( !m_InterfaceType2ShaderInterface.ContainsKey( _SupportedInterfaceType ) )
				throw new NException( this, "You must declare the shader interface type \"" + _SupportedInterfaceType.FullName + "\" using the Device.DeclareShaderInterface() method first before registering any provider for that type !" );

			if ( !m_InterfaceType2Providers.ContainsKey( _SupportedInterfaceType ) )
				m_InterfaceType2Providers.Add( _SupportedInterfaceType, new Stack<IShaderInterfaceProvider>() );

			m_InterfaceType2Providers[_SupportedInterfaceType].Push( _Provider );

			// Notify
			if ( ShaderInterfaceProviderAdded != null )
				ShaderInterfaceProviderAdded( _Provider );
		}

		public void	UnRegisterShaderInterfaceProvider( Type _SupportedInterfaceType, IShaderInterfaceProvider _Provider )
		{
			if ( _SupportedInterfaceType == null )
				throw new NException( this, "Invalid interface type !" );
			if ( _Provider == null )
				throw new NException( this, "Invalid provider for interface \"" + _SupportedInterfaceType.FullName + "\" !" );
			if ( !m_InterfaceType2Providers.ContainsKey( _SupportedInterfaceType ) )
				throw new NException( this, "There is no provider registered for interface type \"" + _SupportedInterfaceType.FullName + "\" !" );

			Stack<IShaderInterfaceProvider>	Providers = m_InterfaceType2Providers[_SupportedInterfaceType];
			if ( Providers.Peek() != _Provider )
				throw new NException( this, "The provider at the top of the stack for interface type \"" + _SupportedInterfaceType.FullName + "\" is not your provider !\r\nYou can't remove your provider until the top-most provider is removed... Check registration order !" );

			// Remove...
			Providers.Pop();

			// Notify
			if ( ShaderInterfaceProviderRemoved != null )
				ShaderInterfaceProviderRemoved( _Provider );
		}

		/// <summary>
		/// Builds a list of shader interfaces given a set of effect variables
		/// </summary>
		/// <param name="_Variables"></param>
		/// <returns></returns>
		public IShaderInterface[]	RequestShaderInterfaces( Variable[] _Variables )
		{
			List<IShaderInterface>	Result = new List<IShaderInterface>();
			Dictionary<Type,IShaderInterface>	Type2Interface = new Dictionary<Type,IShaderInterface>();

			foreach ( Variable Variable in _Variables )
			{
				if ( Variable.Semantic == null || !m_Semantic2ShaderInterface.ContainsKey( Variable.Semantic ) )
					continue;	// This semantic is not recognized as part of a registered interface...

				ShaderInterfaceEntry	Interface = m_Semantic2ShaderInterface[Variable.Semantic];

				// Check if the interface has already been created...
				if ( !Type2Interface.ContainsKey( Interface.Type ) )
				{	// Create and add this interface...
					IShaderInterface	InterfaceInstance = m_Semantic2ShaderInterface[Variable.Semantic].CreateInstance();
					Type2Interface.Add( Interface.Type, InterfaceInstance );	// Mark it as created...
					Result.Add( InterfaceInstance );
				}

				// Assign the variable and its semantic
				Type2Interface[Interface.Type].SetEffectVariable( Variable.Semantic, Variable );
			}

			return Result.ToArray();
		}

		/// <summary>
		/// Provides the data for the given interface using one of our registered providers
		/// </summary>
		/// <param name="_Interface"></param>
		public void		ProvideDataForInterface( IShaderInterface _Interface )
		{
			if ( _Interface == null )
				throw new NException( this, "Invalid interface to provide data for !" );

			Type	InterfaceType = _Interface.GetType();
			if ( !m_InterfaceType2Providers.ContainsKey( InterfaceType ) )
				throw new NException( this, "There is currently no provider for the shader interfaces of type \"" + InterfaceType.FullName + "\" !" );

			// Make the provider provide...
			m_InterfaceType2Providers[InterfaceType].Peek().ProvideData( _Interface );
		}

		#endregion

		#region Device Wrapping

		/// <summary>
		/// Clears the specified render target
		/// </summary>
		/// <param name="_RenderTarget"></param>
		/// <param name="_ClearColor"></param>
		public void ClearRenderTarget( IRenderTarget _RenderTarget, Color _ClearColor )
		{
			m_Device.ClearRenderTargetView( _RenderTarget.RenderTargetView, (Color4) _ClearColor );
		}
		public void ClearRenderTarget( IRenderTarget _RenderTarget, Vector4 _ClearColor )
		{
			Color4	C = new Color4( _ClearColor.W, _ClearColor.X, _ClearColor.Y, _ClearColor.Z );
			m_Device.ClearRenderTargetView( _RenderTarget.RenderTargetView, C );
		}
        public void ClearRenderTarget( IRenderTarget _RenderTarget, Color4 _ClearColor)
		{
			m_Device.ClearRenderTargetView( _RenderTarget.RenderTargetView, _ClearColor );
		}
		public void	ClearRenderTarget( IRenderTarget3D _RenderTarget, Color4 _ClearColor )
		{
			m_Device.ClearRenderTargetView( _RenderTarget.RenderTargetView, _ClearColor );
		}
		public void	ClearRenderTarget( IRenderTarget3D _RenderTarget, Vector4 _ClearColor )
		{
			Color4	C = new Color4( _ClearColor.W, _ClearColor.X, _ClearColor.Y, _ClearColor.Z );
			m_Device.ClearRenderTargetView( _RenderTarget.RenderTargetView, C );
		}

		/// <summary>
		/// Clears the specified depth stencil buffer
		/// </summary>
		public void	ClearDepthStencil( IDepthStencil _DepthStencil, DepthStencilClearFlags _Flags, float _Depth, byte _Stencil )
		{
			m_Device.ClearDepthStencilView( _DepthStencil.DepthStencilView, _Flags, _Depth, _Stencil );
		}

		public void	Draw( int _VertexCount, int _StartVertex )
		{
			m_Device.Draw( _VertexCount, _StartVertex );
		}

		public void	DrawIndexed( int _IndexCount, int _StartIndex, int _StartVertex )
		{
			m_Device.DrawIndexed( _IndexCount, _StartIndex, _StartVertex );
		}

		public void	DrawInstanced( int _VertexCountPerInstance, int _InstancesCount, int _StartVertex, int _StartInstance )
		{
			m_Device.DrawInstanced( _VertexCountPerInstance, _InstancesCount, _StartVertex, _StartInstance );
		}

		public void	DrawIndexedInstanced( int _IndexCountPerInstance, int _InstancesCount, int _StartIndex, int _StartVertex, int _StartInstance )
		{
			m_Device.DrawIndexedInstanced( _IndexCountPerInstance, _InstancesCount, _StartIndex, _StartVertex, _StartInstance );
		}

		public void	DrawAuto()
		{
			m_Device.DrawAuto();
		}

		#endregion

		#region Helpers

		protected static Dictionary<Format,int>	ms_PixelFormat2MaxMSAASamples = new Dictionary<Format,int>();

		/// <summary>
		/// Retrieves the maximum amount of MSAA samples for a given pixel format
		/// </summary>
		/// <typeparam name="PF"></typeparam>
		/// <returns></returns>
		public static int GetMaximumMSAASamples<PF>() where PF:struct,IPixelFormat
		{
			PF		PFInstance = new PF();
			Format	PixelFormat = PFInstance.DirectXFormat;

			return GetMaximumMSAASamples( PixelFormat );
		}

		public static int GetMaximumMSAASamples( Format _PixelFormatType )
		{
			if ( ms_PixelFormat2MaxMSAASamples.ContainsKey( _PixelFormatType ) )
				return ms_PixelFormat2MaxMSAASamples[_PixelFormatType];

			int		MaxSamplesCount=1;
			for ( int SamplesCount=1; SamplesCount < 32; SamplesCount++ )
				if ( Device.Instance.DirectXDevice.CheckMultisampleQualityLevels( _PixelFormatType, SamplesCount ) != 0 )
					MaxSamplesCount = SamplesCount;

			return ms_PixelFormat2MaxMSAASamples[_PixelFormatType] = MaxSamplesCount;
		}

		/// <summary>
		/// Helper to load a file's content as a byte[]
		/// </summary>
		/// <param name="_File"></param>
		/// <returns></returns>
		public static byte[]	LoadFileContent( System.IO.FileInfo _File )
		{
			System.IO.FileStream	S = _File.OpenRead();
			byte[]	Content = new byte[S.Length];
			S.Read( Content, 0, (int) S.Length );
			S.Close();
			S.Dispose();

			return Content;
		}

		#endregion

		#region Profiling

		/// <summary>
		/// Starts frame profiling
		/// </summary>
		/// <param name="_bFlushOnEveryTask">True to flush command buffer on every task.
		/// This allows to process the previously pushed commands and monitor accurately the time of each task.
		/// If false, the commands from each task will be performed during the next "Present()"</param>
		public void		StartProfiling( bool _bFlushOnEveryTask )
		{
			if ( m_bProfilingStarted  )
				throw new NException( this, "You must end the current profiling before starting a new one !" );

			m_ProfilingStartTime = DateTime.Now;
			m_bFlushOnEveryTask = _bFlushOnEveryTask;

			// Recycle profile task infos...
			if ( m_ProfilingRootUsed != null )
			{
				ProfileTaskInfos	LastPTI = m_ProfilingRootUsed.Last;
				if ( m_ProfilingRootFree != null )
					m_ProfilingRootFree.Previous = LastPTI;
				LastPTI.Next = m_ProfilingRootFree;
				m_ProfilingRootFree = m_ProfilingRootUsed;
				m_ProfilingRootUsed = null;
			}

			m_bProfilingStarted = true;

			// Notify we started profiling
			if ( ProfilingStarted != null )
				ProfilingStarted( this, EventArgs.Empty );
		}

		/// <summary>
		/// Adds a profiling task
		/// </summary>
		/// <param name="_Source">The source component adding a task (can be null)</param>
		/// <param name="_Infos">The infos pertaining to the task</param>
		public void		AddProfileTask( Component _Source, string _Infos )
		{
			// Flush previous task's commands
			if ( m_bFlushOnEveryTask )
				m_Device.Flush();

			AddProfileTask( _Source, "", _Infos );
		}

		/// <summary>
		/// Adds a profiling task
		/// </summary>
		/// <param name="_Source">The source component adding a task (can be null)</param>
		/// <param name="_Category">The category of the task to add</param>
		/// <param name="_Infos">The infos pertaining to the task</param>
		public void		AddProfileTask( Component _Source, string _Category, string _Infos )
		{
			if ( !m_bProfilingStarted  )
				throw new NException( this, "You cannot add a profile task without starting profiling first !" );

			ProfileTaskInfos	PTI = null;
			if ( m_ProfilingRootFree != null )
			{	// Recycle
				PTI = m_ProfilingRootFree;
				m_ProfilingRootFree = m_ProfilingRootFree.Next;
			}
			else
				PTI = new ProfileTaskInfos( this );	// Create a new one

			// Update
			PTI.Update( _Source, _Category, _Infos );

			// Link to the end of the list of used profiling tasks
			if ( m_ProfilingRootUsed != null )
			{
				ProfileTaskInfos	LastPTI = m_ProfilingRootUsed.Last;
				PTI.Previous = LastPTI;
				PTI.Next = null;
				LastPTI.Next = PTI;
			}
			else
			{	// It's our new root
				PTI.Previous = PTI.Next = null;
				m_ProfilingRootUsed = PTI;
			}
		}

		/// <summary>
		/// Ends frame profiling
		/// </summary>
		public void		EndProfiling()
		{
			if ( !m_bProfilingStarted  )
				throw new NException( this, "There is not profiling started !" );

			m_ProfilingEndTime = DateTime.Now;
			m_bProfilingStarted = false;

			// Notify we stopped profiling
			if ( ProfilingStopped != null )
				ProfilingStopped( this, EventArgs.Empty );
		}

		#endregion

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			IReadableZBuffer	I = _Interface as IReadableZBuffer;
			I.ZBuffer = m_DefaultDepthStencil;
			I.ZBufferInvSize = m_DefaultDepthStencil.InvSize2;
		}

		#endregion

		/// <summary>
		/// Creates the singleton instance for a device
		/// </summary>
		/// <param name="_Description">The swap chain creation description</param>
		/// <param name="_TargetControl">The target control we render to</param>
		/// <param name="_DepthStencilMultiSamplesCount">The amount of multi samples to use for the default depth stencil</param>
		/// <returns>The created singleton device</returns>
		public static Device	CreateInstance( SwapChainDescription _Description, Control _TargetControl )
		{
			return CreateInstance( _Description, _TargetControl, 1 );
		}

		/// <summary>
		/// Creates the singleton instance for a device
		/// </summary>
		/// <param name="_Description">The swap chain creation description</param>
		/// <param name="_TargetControl">The target control we render to</param>
		/// <param name="_DepthStencilMultiSamplesCount">The amount of multi samples to use for the default depth stencil</param>
		/// <param name="_bReadableDepthStencil">True to create a readable depth stencil buffer</param>
		/// <returns>The created singleton device</returns>
		public static Device	CreateInstance( SwapChainDescription _Description, Control _TargetControl, bool _bReadableDepthStencil )
		{
			return CreateInstance( _Description, _TargetControl, 1, _bReadableDepthStencil );
		}

		/// <summary>
		/// Creates the singleton instance for a device
		/// </summary>
		/// <param name="_Description">The swap chain creation description</param>
		/// <param name="_TargetControl">The target control we render to</param>
		/// <param name="_DepthStencilMultiSamplesCount">The amount of multi samples to use for the default depth stencil</param>
		/// <returns>The created singleton device</returns>
		public static Device	CreateInstance( SwapChainDescription _Description, Control _TargetControl, int _DepthStencilMultiSamplesCount )
		{
			return CreateInstance( _Description, _TargetControl, _DepthStencilMultiSamplesCount, false );
		}

		/// <summary>
		/// Creates the singleton instance for a device
		/// </summary>
		/// <param name="_Description">The swap chain creation description</param>
		/// <param name="_TargetControl">The target control we render to</param>
		/// <param name="_DepthStencilMultiSamplesCount">The amount of multi samples to use for the default depth stencil</param>
		/// <param name="_bReadableDepthStencil">True to create a readable depth stencil buffer</param>
		/// <returns>The created singleton device</returns>
		public static Device	CreateInstance( SwapChainDescription _Description, Control _TargetControl, int _DepthStencilMultiSamplesCount, bool _bReadableDepthStencil )
		{
			if ( ms_Instance != null )
				throw new Exception( "There already exists an instance of Device ! You can only create a single instance !" );

			_Description.OutputHandle = _TargetControl.Handle;

			SharpDX.Direct3D10.Device1 D = null;
			SwapChain SC = null;

#if DEBUG
			DeviceCreationFlags CreationFlags = DeviceCreationFlags.Debug;
#else
			DeviceCreationFlags	CreationFlags = DeviceCreationFlags.None;
#endif

			// Attempt to create a 10.1 device with 10.1 features
			try
			{
				SharpDX.Direct3D10.Device1.CreateWithSwapChain(DriverType.Hardware,
					CreationFlags,
                    _Description,
					SharpDX.Direct3D10.FeatureLevel.Level_10_1,					
					out D,
					out SC );
			}
			catch ( SharpDXException _e )
			{	// If that fails, attempt to create a 10.1 device with 10.0 features (i.e. same as a 10.0 device)
				unchecked {
					if ( _e.ResultCode.Code != (int) 0x80004002 )
						throw _e;	// Rethrow at it's not the exception we were expecting...
				}

				try
				{
					SharpDX.Direct3D10.Device1.CreateWithSwapChain(
						DriverType.Hardware,
						CreationFlags,
                        _Description,
                        SharpDX.Direct3D10.FeatureLevel.Level_10_0,
						out D,
						out SC );
				}
				catch ( Exception _e2 )
				{	// Send some message to stoopid users who try to run this with XP or something...
					throw new Exception( "An error occurred while attempting to create the device ! DirectX 10 seems to be unavailable... Did you install the DirectX runtime ? Also check your PC supports DirectX10 (Windows Vista and 7 only !)", _e2 );
				}
			}

			if ( D == null )
				throw new Exception( "Device creation didn't fail but we didn't get a valid device !" );
			if ( SC == null )
				throw new Exception( "Device creation didn't fail but we didn't get a valid swap chain !" );

			return new Device( D, SC, _TargetControl, _DepthStencilMultiSamplesCount, _bReadableDepthStencil );
		}

		#endregion

		#region EVENT HANDLERS

		protected void Material_EffectRecompiled( object sender, EventArgs e )
		{
			// Forward notification
			if ( MaterialEffectRecompiled != null )
				MaterialEffectRecompiled( sender, e );
		}

		#endregion
	}
}
