using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;

namespace Nuaj
{
	/// <summary>
	/// This is thrown by material constructors if they require an unsupported shader model
	/// </summary>
	public class	UnsupportedShaderModelException : NException
	{
		public UnsupportedShaderModelException( IMaterial _Sender ) : base( _Sender as Component, "Shader model unsupported by the device ! Can't use that material..." ) {}
		public UnsupportedShaderModelException( IMaterial _Sender, Exception _e ) : base( _Sender as Component, "Shader model unsupported by the device ! Can't use that material...", _e ) {}
	}

	/// <summary>
	/// The material class encompasses a DirectX Effect
	/// It provides several helpers like automatic :
	///		_ vertex layout generation based on the provided template vertex structure type
	///		_ Automatic listing of shader interfaces supported by the compiled shader
	///		_ Automatic recompilation of shaders coming from files (if you use the File constructor)
	/// </summary>
	/// <typeparam name="VS">The vertex structure that can be used with this material</typeparam>
	public class Material<VS> : Component, Include, IMaterial where VS:struct
	{
		#region FIELDS

		protected Effect			m_Effect = null;
		protected EffectTechnique	m_CurrentTechnique = null;

		// The shader model used by that material
		protected ShaderModel		m_RequiredShaderModel = ShaderModel.Empty;

		// The optinal Include resolver override
		protected Include			m_IncludeOverride = null;

		// Errors
		protected bool				m_bHasErrors = false;
		protected string			m_CompilationErrors = "";
#if DEBUG
		protected bool				m_bFirstCompilation = true;	// Set to true if you want a shader to fail if issuing errors on first compilation...
#else
		protected bool				m_bFirstCompilation = true;	// Set to true if you want a shader to fail if issuing errors on first compilation...
#endif

		// Vertex layout
		protected InputLayout		m_VertexLayout = null;

		// Cached variables
		protected List<Variable>	m_Variables = new List<Variable>();
		protected Dictionary<int,Variable>		m_Index2Variable = new Dictionary<int,Variable>();
		protected Dictionary<string,Variable>	m_Name2Variable = new Dictionary<string,Variable>();
		protected Dictionary<string,Variable>	m_Semantic2Variable = new Dictionary<string,Variable>();

		// Shader interfaces
		// These are filled up after shader compilation
		protected IShaderInterface[]	m_ShaderInterfaces = new IShaderInterface[0];

		// File system watcher for file-based effects
		protected FileSystemWatcher	m_Watcher = null;
		protected FileInfo			m_EffectSourceFile = null;

		// Mutex for effect synchronization so that any file watcher event triggering recompile
		// won't interfere in a current rendering...
		System.Threading.Mutex		m_EffectLock = new System.Threading.Mutex( false, "Effect Lock" );


		protected static Effect		ms_DefaultEffect = null;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the amount of available techniques in this material
		/// </summary>
		public int				TechniqueCount			{ get { using ( Synchronize() ) { return m_Effect.Description.TechniqueCount; } } }

		/// <summary>
		/// Gets the amount of available global variables in this material
		/// </summary>
		public int				GlobalVariableCount		{ get { using ( Synchronize() ) { return m_Effect.Description.GlobalVariableCount; } } }

		/// <summary>
		/// Gets the list of variables for that material
		/// </summary>
		public Variable[]		Variables				{ get { using ( Synchronize() ) { return m_Variables.ToArray(); } } }

		/// <summary>
		/// Gets the amount of available constant buffers in this material
		/// </summary>
		public int				ConstantBufferCount		{ get { using ( Synchronize() ) { return m_Effect.Description.ConstantBufferCount; } } }

		/// <summary>
		/// Gets or sets the current technique used with this material
		/// </summary>
		public EffectTechnique	CurrentTechnique
		{
			get { using ( Synchronize() ) { return m_CurrentTechnique; } }
			set
			{
				using ( Synchronize() )
				{
					if ( value == m_CurrentTechnique )
						return;	// No change

					m_CurrentTechnique = value;
#if DEBUG
					if ( m_CurrentTechnique != null && !m_CurrentTechnique.IsValid )
						throw new NException( this, "The provided technique is not valid ! Make sure the name matches in your effect file !" );
#endif
				}
			}
		}

		/// <summary>
		/// Gets the vertex layout associated to that material
		/// </summary>
		public InputLayout		VertexLayout			{ get { return m_VertexLayout; } }

		/// <summary>
		/// Gets the shader interfaces implemented by that material
		/// </summary>
		public IShaderInterface[]	ShaderInterfaces	{ get { return m_ShaderInterfaces; } }

		/// <summary>
		/// Gets the last error state
		/// </summary>
		public bool				HasErrors				{ get { return m_bHasErrors; } }

		/// <summary>
		/// Gets the last compilation error for that material
		/// </summary>
		public string			CompilationErrors		{ get { return m_CompilationErrors; } }

		/// <summary>
		/// Notifies the effect has recompiled (possibly with errors, who knows ?)
		/// </summary>
		public event EventHandler	EffectRecompiled;

		/// <summary>
		/// Gets the default singleton effect used instead of an effect that failed compiling
		/// </summary>
		public static Effect	DefaultEffect
		{
			get
			{
				if ( ms_DefaultEffect == null )
					ms_DefaultEffect = new Effect( Nuaj.Device.Instance.DirectXDevice, ShaderBytecode.Compile( Properties.Resources.DefaultShader, "", "fx_4_0", ShaderFlags.Debug, EffectFlags.None ) );

				return ms_DefaultEffect;
			}
		}

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a material from a string-based shader
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_RequiredShaderModel">The required shader model for this material to work</param>
		/// <param name="_EffectSource">The source code for the shader</param>
		/// <exception cref="UnsupportedShaderModelException">The effects requires an unsupported shader model</exception>
		public	Material( Device _Device, string _Name, ShaderModel _RequiredShaderModel, string _EffectSource ) : base( _Device, _Name )
		{
			// Check the shader model is supported
			m_RequiredShaderModel = _RequiredShaderModel;
			if ( !m_Device.SupportsShaderModel( m_RequiredShaderModel ) )
				throw new UnsupportedShaderModelException( this );

			m_Device.AddMaterial( this );
			CompileEffect( _EffectSource );
		}

		/// <summary>
		/// Creates a material from a byte array shader code
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_RequiredShaderModel">The required shader model for this material to work</param>
		/// <param name="_EffectSource">The byte array holding the code for the shader</param>
		/// <exception cref="UnsupportedShaderModelException">The effects requires an unsupported shader model</exception>
		public	Material( Device _Device, string _Name, ShaderModel _RequiredShaderModel, byte[] _EffectSource ) : base( _Device, _Name )
		{
			// Check the shader model is supported
			m_RequiredShaderModel = _RequiredShaderModel;
			if ( !m_Device.SupportsShaderModel( m_RequiredShaderModel ) )
				throw new UnsupportedShaderModelException( this );

			m_Device.AddMaterial( this );
			CompileEffect( _EffectSource );
		}

		/// <summary>
		/// Creates a material from a file
		/// The file is watched for changes and the shader is recompiled as soon as changes occur
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_RequiredShaderModel">The required shader model for this material to work</param>
		/// <param name="_EffectFile"></param>
		/// <exception cref="UnsupportedShaderModelException">The effects requires an unsupported shader model</exception>
		/// <exception cref="FileNotFoundException">The provided effect file must exist on disk</exception>
		public	Material( Device _Device, string _Name, ShaderModel _RequiredShaderModel, FileInfo _EffectFile ) : base( _Device, _Name )
		{
			// Check the shader model is supported
			m_RequiredShaderModel = _RequiredShaderModel;
			if ( !m_Device.SupportsShaderModel( m_RequiredShaderModel ) )
				throw new UnsupportedShaderModelException( this );

			// Check the file exists
			if ( _EffectFile == null )
				throw new NException( this, "Invalid effect file info !" );
			if ( !_EffectFile.Exists )
				throw new FileNotFoundException( "Effect file \"" + _EffectFile.FullName + "\" does not exist !" );

			m_Device.AddMaterial( this );

			// Create the watcher for that file
			m_Watcher = ToDispose( new FileSystemWatcher( _EffectFile.DirectoryName, _EffectFile.Name ) );
			m_Watcher.IncludeSubdirectories = false;
			m_Watcher.Changed += new FileSystemEventHandler( Watcher_EffectChanged );
			m_Watcher.Deleted += new FileSystemEventHandler( Watcher_EffectDeleted );
			m_Watcher.Error += new ErrorEventHandler( Watcher_Error );
			m_Watcher.EnableRaisingEvents = true;

			CompileEffect( _EffectFile );
		}

		/// <summary>
		/// Creates a material from a stream
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_RequiredShaderModel">The required shader model for this material to work</param>
		/// <param name="_EffectStream"></param>
		/// <param name="_Include">An optional include resolver to override the default one (i.e. from disk)</param>
		/// <exception cref="UnsupportedShaderModelException">The effects requires an unsupported shader model</exception>
		/// <exception cref="FileNotFoundException">The provided effect file must exist on disk</exception>
		public	Material( Device _Device, string _Name, ShaderModel _RequiredShaderModel, Stream _EffectStream, Include _Include ) : base( _Device, _Name )
		{
			// Check the shader model is supported
			m_RequiredShaderModel = _RequiredShaderModel;
			if ( !m_Device.SupportsShaderModel( m_RequiredShaderModel ) )
				throw new UnsupportedShaderModelException( this );

			m_IncludeOverride = _Include;

			m_Device.AddMaterial( this );
			CompileEffect( _EffectStream );
		}

		public override string ToString()
		{
			return (HasErrors ? "[!ERRORS!] " : "") + base.ToString();
		}

		/// <summary>
		/// Uses the material (i.e. sends it to the Input Assembler) and locks it for exclusive access
		/// </summary>
		/// <returns>A disposable lock on the material</returns>
		/// <remarks>USE WITH CAUTION ! YOU MUST DISPOSE OF THE LOCK WHEN YOU'RE FINISHED WITH THE MATERIAL !</remarks>
		public IDisposable		UseLock()
		{
			IDisposable	Lock = Synchronize();

			m_Device.InputAssembler.InputLayout = m_VertexLayout;

			// Resolve our shader interfaces' variables
			foreach ( IShaderInterface Interface in m_ShaderInterfaces )
				m_Device.ProvideDataForInterface( Interface );

			return Lock;
		}

		/// <summary>
		/// Applies the parameters to run the specified pass
		/// </summary>
		/// <param name="_PassIndex"></param>
		public void		ApplyPass( int _PassIndex )
		{
			m_CurrentTechnique.GetPassByIndex( _PassIndex ).Apply();
		}

		/// <summary>
		/// Renders a primitive using the material
		/// </summary>
		/// <param name="_Delegate"></param>
		public void		Render( RenderDelegate _Delegate )
		{
			if ( _Delegate == null )
				throw new NException( this, "Invalid rendering delegate !" );
			if ( m_CurrentTechnique == null )
				throw new NException( this, "Invalid technique for rendering !" );
			
			using ( UseLock() )
				try
				{
					int	PassesCount = m_CurrentTechnique.Description.PassCount;
					for ( int PassIndex=0; PassIndex < PassesCount; PassIndex++ )
					{
						EffectPass	Pass = m_CurrentTechnique.GetPassByIndex( PassIndex );
						Pass.Apply();

						// Render
						_Delegate( this, Pass, PassIndex );
					}
				}
				catch ( Exception _e )
				{
					throw new NException( this, "An exception occurred while rendering !", _e );
				}
		}

		public override void Dispose()
		{
			m_Device.RemoveMaterial( this );

			if ( m_Effect != null && m_Effect != ms_DefaultEffect )
				m_Effect.Dispose();
			m_Effect = null;
			if ( m_VertexLayout != null )
				m_VertexLayout.Dispose();
			m_VertexLayout = null;

			// Dispose ICallbackable.Shadow
			if ( m_Shadow != null )
				m_Shadow.Dispose();
			m_Shadow = null;

			base.Dispose();
		}

		#region Effect Wrapping Methods

		public EffectConstantBuffer	GetConstantBufferByIndex( int _Index )
		{
			using ( Synchronize() ) { return m_Effect.GetConstantBufferByIndex( _Index ); }
		}

		public EffectConstantBuffer	GetConstantBufferByName( string _Name )
		{
			using ( Synchronize() ) { return m_Effect.GetConstantBufferByName( _Name ); }
		}

		public EffectTechnique	GetTechniqueByIndex( int _Index )
		{
			using ( Synchronize() ) { return m_Effect.GetTechniqueByIndex( _Index ); }
		}

		public EffectTechnique	GetTechniqueByName( string _Name )
		{
			using ( Synchronize() ) { return m_Effect.GetTechniqueByName( _Name ); }
		}

		public Variable	GetVariableByIndex( int _Index )
		{
			return m_Index2Variable.ContainsKey( _Index ) ? m_Index2Variable[_Index] : null;
		}

		public Variable	GetVariableByName( string _Name )
		{
			return m_Name2Variable.ContainsKey( _Name ) ? m_Name2Variable[_Name] : null;
		}

		public Variable	GetVariableBySemantic( string _Semantic )
		{
			return m_Semantic2Variable.ContainsKey( _Semantic ) ? m_Semantic2Variable[_Semantic] : null;
		}

		#endregion

		#region Variable Helpers

		public virtual void		SetScalar( int _Index, float _Value )			{ GetVariableByIndex( _Index ).AsScalar.Set( _Value ); }
		public virtual void		SetScalar( string _Name, float _Value )			{ GetVariableByName( _Name ).AsScalar.Set( _Value ); }
		public virtual void		SetScalar( int _Index, int _Value )				{ GetVariableByIndex( _Index ).AsScalar.Set( _Value ); }
		public virtual void		SetScalar( string _Name, int _Value )			{ GetVariableByName( _Name ).AsScalar.Set( _Value ); }
		public virtual void		SetScalar( int _Index, bool _Value )			{ GetVariableByIndex( _Index ).AsScalar.Set( _Value ); }
		public virtual void		SetScalar( string _Name, bool _Value )			{ GetVariableByName( _Name ).AsScalar.Set( _Value ); }
		public virtual void		SetVector( int _Index, Vector2 _Value )			{ GetVariableByIndex( _Index ).AsVector.Set( _Value ); }
		public virtual void		SetVector( string _Name, Vector2 _Value )		{ GetVariableByName( _Name ).AsVector.Set( _Value ); }
		public virtual void		SetVector( int _Index, Vector3 _Value )			{ GetVariableByIndex( _Index ).AsVector.Set( _Value ); }
		public virtual void		SetVector( string _Name, Vector3 _Value )		{ GetVariableByName( _Name ).AsVector.Set( _Value ); }
		public virtual void		SetVector( int _Index, Vector4 _Value )			{ GetVariableByIndex( _Index ).AsVector.Set( _Value ); }
		public virtual void		SetVector( string _Name, Vector4 _Value )		{ GetVariableByName( _Name ).AsVector.Set( _Value ); }
		public virtual void		SetMatrix( int _Index, Matrix _Value )			{ GetVariableByIndex( _Index ).AsMatrix.SetMatrix( _Value ); }
		public virtual void		SetMatrix( string _Name, Matrix _Value )		{ GetVariableByName( _Name ).AsMatrix.SetMatrix( _Value ); }
		public virtual void		SetResource( int _Index, ITexture2D _Value )	{ GetVariableByIndex( _Index ).AsResource.SetResource( _Value ); }
		public virtual void		SetResource( string _Name, ITexture2D _Value )	{ GetVariableByName( _Name ).AsResource.SetResource( _Value ); }
		public virtual void		SetResource( int _Index, ITexture3D _Value )	{ GetVariableByIndex( _Index ).AsResource.SetResource( _Value ); }
		public virtual void		SetResource( string _Name, ITexture3D _Value )	{ GetVariableByName( _Name ).AsResource.SetResource( _Value ); }

		#endregion

		#region Compilation

		protected bool	CompileEffect( FileInfo _EffectFile )
		{
			if ( _EffectFile == null )
				throw new NException( this, "Invalid effect file info !" );
			if ( !_EffectFile.Exists )
				throw new FileNotFoundException( "Effect file \"" + _EffectFile.FullName + "\" does not exist !" );

			m_EffectSourceFile = _EffectFile;

			// Read the code
			string	ShaderSource = null;
			try
			{
				StreamReader	Reader = _EffectFile.OpenText();
				ShaderSource = Reader.ReadToEnd();
				Reader.Close();
				Reader.Dispose();
			}
			catch ( Exception _e )
			{
				throw new NException( this, "An error occurred while attempting to read effect file \"" + _EffectFile.FullName + "\" !", _e );
			}

			// Recompile
			return CompileEffect( ShaderSource );
		}

		protected bool	CompileEffect( Stream _EffectStream )
		{
			// Read the code
			string	ShaderSource = null;
			try
			{
				StreamReader	Reader = new StreamReader( _EffectStream, true );
				ShaderSource = Reader.ReadToEnd();
				Reader.Close();
				Reader.Dispose();
			}
			catch ( Exception _e )
			{
				throw new NException( this, "An error occurred while attempting to read effect stream !", _e );
			}

			// Recompile
			return CompileEffect( ShaderSource );
		}

		protected bool	CompileEffect( string _Source )
		{
			return CompileEffect( System.Text.UTF8Encoding.UTF8.GetBytes( _Source ) );
		}

		/// <summary>
		/// Compiles the effect from a byte-array source code
		/// An exception is thrown if the effect failed to compile for an external reason
		/// If there are errors in the code then a place-holder "error effect" is used instead and
		///  errors can be checked through the "CompilationErrors" property...
		/// </summary>
		/// <param name="_Source"></param>
		/// <returns>True if the compilation was successful, false otherwise</returns>
		protected bool	CompileEffect( byte[] _Source )
		{
			bool	bSuccess = false;

			using ( Synchronize() )
			{
				// Dispose of previous effect
				if ( m_Effect != null && m_Effect != ms_DefaultEffect )
					m_Effect.Dispose();
				m_Effect = null;

				m_bHasErrors = false;

				// Try and compile the source
				try
				{
#if DEBUG
					SharpDX.Direct3D.ShaderMacro[]	Macros = new SharpDX.Direct3D.ShaderMacro[] { new SharpDX.Direct3D.ShaderMacro( "_DEBUG", "1" ) };	// DEFINES
					ShaderFlags						Flags = ShaderFlags.NoPreshader | ShaderFlags.Debug | ShaderFlags.WarningsAreErrors | ShaderFlags.EnableStrictness;
#else
					SharpDX.Direct3D.ShaderMacro[]	Macros = null;
					ShaderFlags						Flags = ShaderFlags.OptimizationLevel3;
#endif

//					m_Effect = Effect.FromMemory( m_Device.DirectXDevice, _Source, m_Device.EffectCompilationProfileString, ShaderFlags.NoPreshader | ShaderFlags.Debug | ShaderFlags.EnableStrictness | ShaderFlags.WarningsAreErrors, EffectFlags.None, null, this, out m_CompilationErrors );

					string			PreProcessedShader = ShaderBytecode.Preprocess( _Source, Macros, this, out m_CompilationErrors );
					ShaderBytecode	CompiledShader = ShaderBytecode.Compile(
												PreProcessedShader,
												"",
												m_Device.EffectCompilationProfileString,
												Flags,
												EffectFlags.None,
												Macros,
												this,
												m_EffectSourceFile != null ? m_EffectSourceFile.FullName : "" );

					m_Effect = new Effect(	m_Device.DirectXDevice, CompiledShader );
					bSuccess = m_Effect != null;
					if ( m_Effect == null )
						throw new NException( this, "The compiler didn't output any error but failed to generate a valid shader !" );

					// Check the effect is valid
					bSuccess = m_Effect.IsValid;
					if ( !m_Effect.IsValid )
						throw new NException( this, "The compiled effect is invalid !" );

					m_CompilationErrors = m_CompilationErrors ?? "";
					m_bFirstCompilation = false;
				}
				catch ( SharpDXException _e )
				{
					// Special check for bad shader version
					// That's a bit ugly but I'm using a regexp to retrieve the typical exception message
					//	as DirectX is always so explicit on its errors (damnit!)...
					System.Text.RegularExpressions.Regex	RX = new System.Text.RegularExpressions.Regex( "Shader model .._._. is not allowed in", System.Text.RegularExpressions.RegexOptions.IgnoreCase );
					if ( RX.IsMatch( _e.Message ) )
						throw new UnsupportedShaderModelException( this, _e );

					// Standard errors
					m_bHasErrors = true;
					m_CompilationErrors = FormatErrors( m_CompilationErrors + "\r\n" + _e.Message );
					if ( m_bFirstCompilation )
						throw new NException( this, m_CompilationErrors, _e );	// The first compilation must succeed otherwise we will fail using the correct vertex declaration

					m_Effect = DefaultEffect;
				}
				catch ( Exception _e )
				{
					throw new NException( this, "Effect for material \"" + this + "\" failed to compile !", _e );
				}
				finally
				{
					// Rebuild variables
					if ( m_Effect != null )
						RebuildVariables();

					// Notify the effect recompiled
					if ( EffectRecompiled != null )
						EffectRecompiled( this, EventArgs.Empty );
				}

				m_CurrentTechnique = m_Effect.GetTechniqueByIndex( 0 );
				if ( m_CurrentTechnique == null || !m_CurrentTechnique.IsValid )
					throw new NException( this, "Effect for material \"" + this + "\" compiled but didn't issue any technique !\r\nCheck your .FX syntax !" );

				// Build vertex layout from the template vertex structre
				BuildVertexLayout();

				// Ask for our shader interfaces
				m_ShaderInterfaces = m_Device.RequestShaderInterfaces( ListShaderVariables() );
			}

			return bSuccess;
		}

		/// <summary>
		/// Rebuilds the shader variables
		/// </summary>
		protected void	RebuildVariables()
		{
			EffectVariable[]	Variables = new EffectVariable[m_Effect.Description.GlobalVariableCount];
			for ( int VariableIndex=0; VariableIndex < Variables.Length; VariableIndex++ )
			{
				EffectVariable	Var = m_Effect.GetVariableByIndex( VariableIndex );
				string			VarName = Var.Description.Name;
				if ( m_Name2Variable.ContainsKey( VarName ) )
				{	// This variable already exists, assign it with a new value
					Variable	ExistingVar = m_Name2Variable[VarName];
					ExistingVar.EffectRecompiled( VariableIndex, Var );
					m_Index2Variable[VariableIndex] = ExistingVar;		// Also remap its index as it may have changed
					continue;
				}

				Variable		WrappedVar = null;
				if ( Var.AsScalar() != null )
					WrappedVar = new VariableScalar( this, VariableIndex, Var.AsScalar() );
				else if ( Var.AsVector() != null )
					WrappedVar = new VariableVector( this, VariableIndex, Var.AsVector() );
				else if ( Var.AsMatrix() != null )
					WrappedVar = new VariableMatrix( this, VariableIndex, Var.AsMatrix() );
				else if ( Var.AsShaderResource() != null )
					WrappedVar = new VariableResource( this, VariableIndex, Var.AsShaderResource() );
				else
//					throw new NException( this, "Unsupported variable type !" );
					continue;

				m_Variables.Add( WrappedVar );
				m_Index2Variable[VariableIndex] = WrappedVar;
				m_Name2Variable[WrappedVar.Name] = WrappedVar;
				m_Semantic2Variable[WrappedVar.Semantic] = WrappedVar;
			}
		}

		/// <summary>
		/// Builds the vertex layout by analyzing the template vertex structure
		/// </summary>
		/// <remarks>
		/// The vertex structure must contain only supported types (float->float4, color3/color4 and matrix)
		///  and also each of the declared fields must have a "[Semantic()]" attribute attached
		/// </remarks>
		/// <seealso cref="VertexStructure"/>
		protected void	BuildVertexLayout()
		{
			// Dispose of previous layout
			if ( m_VertexLayout != null )
				m_VertexLayout.Dispose();
			m_VertexLayout = null;

			try
			{
				m_VertexLayout = Helpers.VertexLayoutBuilder.BuildVertexLayout<VS>( m_CurrentTechnique );
			}
			catch ( Exception _e )
			{
				throw new NException( this, "Vertex layout generation failed ! Check your vertex structure against vertex shader input declaration !", _e );
			}
		}

		/// <summary>
		/// Builds a list of shader variables (valid only if shader successfully compiled)
		/// </summary>
		/// <returns></returns>
		public Variable[]	ListShaderVariables()
		{
			if ( m_Effect == null )
				throw new NException( this, "Invalid effect to list variables for !" );

			return m_Variables.ToArray();
		}

		/// <summary>
		/// Formats the errors returned by the compiler
		/// </summary>
		/// <param name="_Errors"></param>
		/// <returns></returns>
		protected string	FormatErrors( string _Errors )
		{
 			if ( m_EffectSourceFile != null )
 				_Errors = m_EffectSourceFile.FullName + " " + _Errors;

			return _Errors;
// 			StringBuilder	Errors = new StringBuilder();
// 
// 			if ( m_EffectSourceFile != null )
// 				Errors.Append( m_EffectSourceFile.FullName );
// 
// 			bool	bFoundOne = false;
// 			int		LastIndex = 0;
// 			int		Index = 0;
// 			while ( (Index = _Errors.IndexOf( @"n\a(", LastIndex )) != -1 )
// 			{
// 				int	EndOfLineIndex = _Errors.IndexOf( "\n", Index );
// 				if ( EndOfLineIndex == -1 )
// 					EndOfLineIndex = _Errors.Length;	// Last occurrence...
// 				LastIndex = Index+1;
// 
// 				string	ErrorLine = _Errors.Substring( Index+3, EndOfLineIndex-Index-3 );
// 
// 				Errors.Append( ErrorLine );
// 
// 				bFoundOne = true;
// 			}
// 
// 			if ( !bFoundOne )
// 				Errors.Append( "  " + _Errors );	// This happens for global errors with no line number...
// 
// 			return Errors.ToString();
		}

		#endregion

		#region Synchronization

		private class	SynchronizationToken : IDisposable
		{
			Material<VS>	m_Owner = null;

			public SynchronizationToken( Material<VS> _Owner )
			{
				m_Owner = _Owner;
				m_Owner.m_EffectLock.WaitOne( -1 );
				m_Owner.m_Device.PushCurrentMaterial( m_Owner );
			}

			public void Dispose()
			{
				m_Owner.m_Device.PopCurrentMaterial( m_Owner );
				m_Owner.m_EffectLock.ReleaseMutex();
			}
		}

		public IDisposable	Synchronize()
		{
			return new SynchronizationToken( this );
		}

		#endregion

		#region Include Members

		protected IDisposable	m_Shadow = null;
		public IDisposable	Shadow
		{
			get { return m_Shadow; }
			set { m_Shadow = value; }
		}

		public void Close( Stream stream )
		{
			if ( m_IncludeOverride != null )
				m_IncludeOverride.Close( stream );
			else
			{
				stream.Close();
				stream.Dispose();
			}
		}

		public Stream	Open( IncludeType type, string fileName, Stream parentStream )
		{
			if ( m_IncludeOverride != null )
			{	// Use override instead...
				return m_IncludeOverride.Open( type, fileName, parentStream );
			}

			DirectoryInfo	BaseDir = m_EffectSourceFile != null ? m_EffectSourceFile.Directory : new DirectoryInfo( "." );
			string			FullPath = Path.Combine( BaseDir.FullName, fileName );
			FileInfo		F = new FileInfo( FullPath );
			if ( !F.Exists )
			{	// Try in the "FX" subdirectory...
				FullPath = Path.Combine( "./FX", fileName );
				F = new FileInfo( FullPath );
				if ( !F.Exists )
					throw new FileNotFoundException( "Failed to resolve path for #include \"" + fileName + "\" !", FullPath );
			}

// Commented out as opening UTF8 shader files crashes
//			return F.OpenRead();

			// Open the shader text as UTF8
			StreamReader	Reader = F.OpenText();
			string			ShaderSource = Reader.ReadToEnd();
			Reader.Close();
			Reader.Dispose();

			byte[]	UTF8Shader = System.Text.UTF8Encoding.UTF8.GetBytes( ShaderSource );
			return new MemoryStream( UTF8Shader );
		}

		#endregion

		#endregion

		#region EVENTS

		protected DateTime	m_LastChanged = DateTime.Now;
		void Watcher_EffectChanged( object sender, FileSystemEventArgs e )
		{
			if ( (DateTime.Now - m_LastChanged).TotalSeconds < 1.0 )
				return;	// Too soon ! Sometimes we're notified several times...

			m_Watcher.EnableRaisingEvents = false;

			// Recompile...
			try
			{
				CompileEffect( new FileInfo( e.FullPath ) );
				m_LastChanged = DateTime.Now;
			}
			catch ( Exception )
			{
				// Silently fail... :(
			}
			finally
			{
				m_Watcher.EnableRaisingEvents = true;
			}
		}

		void Watcher_EffectDeleted( object sender, FileSystemEventArgs e )
		{
			throw new NException( this, "Effect file \"" + e.FullPath + "\" needed for material \"" + this + "\" has been deleted !" );
		}

		void Watcher_Error( object sender, ErrorEventArgs e )
		{
			throw new NException( this, "Effect file watcher has an error !", e.GetException() );
		}

		#endregion
	}
}
