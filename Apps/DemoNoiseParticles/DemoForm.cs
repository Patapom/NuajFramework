#define LOAD_FROM_FBX	// Define this to load the mesh from the FBX file instead of the proprietary format

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

using Nuaj;
using Nuaj.Cirrus;

namespace Demo
{
	public partial class DemoForm : Form
	{
		#region CONSTANTS

		const int		BAKED_TEXTURE_SIZE = 512;	// The size of the texture in which to render the mesh's positions & normals
		const int		PARTICLES_SIDE_COUNT = 512;	// The actual particles count is that number squared

		#endregion

		#region FIELDS

		protected Nuaj.Device				m_Device = null;

		// The Cirrus renderer
		protected RendererSetupBasic		m_Renderer = null;
		protected Scene						m_Scene = null;

		// Particles data
		protected Scene.Mesh				m_SourceMesh = null;						// The source mesh from which particles are created (the mesh MUST be mapped with no UV overlaps, that means no mirroring !)
		protected RenderTarget<PF_RGBA16F>	m_TextureParticlesPositions = null;			// The render target in which we'll render start positions
		protected RenderTarget<PF_RGBA16F>	m_TextureParticlesPackedNormalUVs = null;	// The render target in which we'll render normals
		protected VertexBuffer<VS_T2>		m_ParticlesVB = null;						// The VB containing the uniform array of particles' UVs in [0,1]
		protected Material<VS_T2>			m_MaterialParticles = null;					// The material that will be used to render the particles

		// Noise data
		protected Texture3D<PF_RGBA16F>[]	m_NoiseTextures = new Texture3D<PF_RGBA16F>[4];	// 4 3D noise textures with mip maps (although we won't use them)
		protected Material<VS_Pt4V3T2>		m_MaterialDeform = null;					// The material that will be used to deform the positions in texture space
		protected RenderTarget<PF_RGBA16F>	m_TextureDeformedParticlesPositions = null;		// Same as above but deformed with noise
		protected RenderTarget<PF_RGBA16F>	m_TextureDeformedParticlesPackedNormalUVs = null;

		protected StringBuilder				m_Log = new StringBuilder();

		// Dispose stack
		protected Stack<IDisposable>		m_Disposables = new Stack<IDisposable>();

		#endregion

		#region METHODS

		public DemoForm()
		{
			InitializeComponent();

			//////////////////////////////////////////////////////////////////////////
			// Create the device
			try
			{
				SwapChainDescription	Desc = new SwapChainDescription()
				{
					BufferCount = 1,
					ModeDescription = new ModeDescription( panelOutput.Width, panelOutput.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm ),
					IsWindowed = true,
					SampleDescription = new SampleDescription( 1, 0 ),
					SwapEffect = SwapEffect.Discard,
					Usage = Usage.RenderTargetOutput
				};

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, panelOutput ) );
				m_Device.MaterialEffectRecompiled += new EventHandler( Device_MaterialEffectRecompiled );
			}
			catch ( Exception _e )
			{
				MessageBox.Show( this, "Failed to create the DirectX device ! (do you have the latest DX10 redist ?)" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
			}


			//////////////////////////////////////////////////////////////////////////
			// Create the renderer & a default scene
			try
			{
				RendererSetupBasic.BasicInitParams	Params = new RendererSetupBasic.BasicInitParams()
				{
					CameraFOV = 45.0f * (float) Math.PI / 180.0f,
					CameraAspectRatio = (float) ClientSize.Width / ClientSize.Height,
					CameraClipNear = 0.01f,
					CameraClipFar = 100.0f,
					bUseAlphaToCoverage = true
				};

				m_Renderer = ToDispose( new RendererSetupBasic( m_Device, "Renderer", Params ) );
			}
			catch ( UnsupportedShaderModelException _e )
			{
				MessageBox.Show( this, "This program requires a shader model not currently supported by your DirectX version !\r\n\r\n" + _e, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				throw;
			}

			// Setup the default lights
			m_Renderer.MainLight.Color = 10 * new Vector4( 1, 1, 1, 1 );
			m_Renderer.FillLight.Color = 4 * new Vector4( 1, 1, 1, 1 );
			m_Renderer.ToneMappingFactor = 0.12f;

			m_Scene = ToDispose( new Scene( m_Device, "Default Scene", m_Renderer.Renderer ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the actual rendering & deform materials
			m_MaterialParticles = ToDispose( new Material<VS_T2>( m_Device, "ParticlesMaterial", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/NoiseParticles/RenderParticles.fx" ) ) );
			m_MaterialDeform = ToDispose( new Material<VS_Pt4V3T2>( m_Device, "DeformMaterial", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/NoiseParticles/NoiseDeform.fx" ) ) );


			//////////////////////////////////////////////////////////////////////////
			// Create the noise textures used for deformation
			for ( int NoiseIndex=0; NoiseIndex < 4; NoiseIndex++ )
				m_NoiseTextures[NoiseIndex] = LoadNoiseTexture( NoiseIndex );


			//////////////////////////////////////////////////////////////////////////
			// Create the Texture Provider
			SceneTextureProvider	TextureProvider = ToDispose( new SceneTextureProvider( m_Device, "TextureProvider from disk", new System.IO.DirectoryInfo( "./Meshes/3DHead/Infinite_Scan_Ver0.1/ImagesLoRes" ) ) );
			TextureProvider.AddSourceRootPathToStripOff( "Infinite_Scan_Ver0.1/Images" );
			TextureProvider.ForceCreateMipMaps = false;


			//////////////////////////////////////////////////////////////////////////
			// Create the material mapper
			MaterialMap	MMap = new MaterialMap();
			MMap.RegisterDefaultMapper( ( Scene.MaterialParameters _MaterialParameters ) =>
				{	// Default mapper returns nothing...
					return null;	// We don't really support Lambert mode anyway... ^_^
				} );
			MMap.RegisterMapper( ( Scene.MaterialParameters _MaterialParameters ) =>
				{	// Phong mapper
					if ( _MaterialParameters.ShaderURL != "Phong" )
						return	null;

					return m_Renderer.DefaultTechnique;
				} );

			//////////////////////////////////////////////////////////////////////////
			// Load the scene
#if LOAD_FROM_FBX
			using ( Nuaj.Cirrus.FBX.SceneLoader SceneLoader = new Nuaj.Cirrus.FBX.SceneLoader( m_Device, "FBXScene" ) )
			{
				SceneLoader.Load( new System.IO.FileInfo( "./Meshes/3DHead/3DHead.fbx" ), m_Scene, MMap, TextureProvider );
			}

			System.IO.FileInfo	SaveFile = new System.IO.FileInfo( "./Scenes/Head.nuaj" );
			System.IO.Stream	Stream = SaveFile.Create();
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( Stream );
			m_Scene.Save( Writer );
			Writer.Close();
			Stream.Close();
			Writer.Dispose();
			Stream.Dispose();
#else
			System.IO.FileInfo	LoadFile = new System.IO.FileInfo( "./Scenes/Head.nuaj" );
			System.IO.Stream	Stream = LoadFile.OpenRead();
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );
			m_Scene.Load( Reader, TextureProvider );
			Reader.Close();
			Stream.Close();
			Reader.Dispose();
			Stream.Dispose();
#endif

			// Display statistics & errors
			richTextBoxOutput.Log( "Texture Provider :\n" );
			richTextBoxOutput.Log( "> " + TextureProvider.LoadedTexturesCount + " textures loaded.\n" );
			int	MinSize = (int) Math.Sqrt( TextureProvider.MinTextureSurface );
			int	MaxSize = (int) Math.Sqrt( TextureProvider.MaxTextureSurface );
			int	AvgSize = (int) Math.Sqrt( TextureProvider.AverageTextureSurface );
			int	TotalSize = (int) Math.Sqrt( TextureProvider.TotalTextureSurface );
			richTextBoxOutput.Log( "> Surface Min=" + MinSize + "x" + MinSize + " Max=" + MaxSize + "x" + MaxSize + " Avg=" + AvgSize + "x" + AvgSize + "\n" );
			richTextBoxOutput.LogWarning( "> Surface Total=" + TotalSize + "x" + TotalSize + " (Memory=" + (TextureProvider.TotalTextureMemory>>10) + " Kb)\n" );

			if ( TextureProvider.HasErrors )
			{	// Display errors
				richTextBoxOutput.Log( "The texture provider has some errors !\r\n\r\n" );
				foreach ( string Error in TextureProvider.TextureErrors )
					richTextBoxOutput.LogError( "   ●  " + Error + "\r\n" );
			}
			richTextBoxOutput.Log( "------------------------------------------------------------------\r\n\r\n" );

			BuildHierarchyTree();


			//////////////////////////////////////////////////////////////////////////
			// Render the mesh into multiple textures containing positions & normals
			m_TextureParticlesPositions = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "ParticlesPosition", BAKED_TEXTURE_SIZE, BAKED_TEXTURE_SIZE, 1 ) );
			m_TextureParticlesPackedNormalUVs = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "ParticlesNormalUV", BAKED_TEXTURE_SIZE, BAKED_TEXTURE_SIZE, 1 ) );
			m_TextureDeformedParticlesPositions = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "ParticlesPosition", BAKED_TEXTURE_SIZE, BAKED_TEXTURE_SIZE, 1 ) );
			m_TextureDeformedParticlesPackedNormalUVs = ToDispose( new RenderTarget<PF_RGBA16F>( m_Device, "ParticlesNormalUV", BAKED_TEXTURE_SIZE, BAKED_TEXTURE_SIZE, 1 ) );

			Material<VS_P3N3G3B3T2>	MaterialBakeParticles = new Material<VS_P3N3G3B3T2>( m_Device, "BakeParticles", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/NoiseParticles/BakeParticles.fx" ) );
			using ( MaterialBakeParticles.UseLock() )
			{
				m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetMultipleRenderTargets( new RenderTarget<PF_RGBA16F>[] { m_TextureParticlesPositions, m_TextureParticlesPackedNormalUVs } );
				m_Device.SetViewport( 0, 0, m_TextureParticlesPositions.Width, m_TextureParticlesPositions.Height, 0, 1 );
				m_Device.ClearRenderTarget( m_TextureParticlesPositions, new Color4( 0, 0, 0, 0 ) );

				// Render the mesh into our render targets
				m_SourceMesh = m_Scene.FindMesh( "group2", false );
				if ( m_SourceMesh == null )
					throw new Exception( "Failed to retrieve expected mesh !" );

				// Attach the primitive's parameters to the baking material and apply parameters (we only need the normal map here)
				foreach ( Scene.Mesh.Primitive P in m_SourceMesh.Primitives )
				{
					P.Parameters.AttachMaterial( MaterialBakeParticles );
					P.Parameters.Apply();
				}

				MaterialBakeParticles.Render( ( IMaterial _Sender, EffectPass _Pass, int _PassIndex ) =>
					{
						foreach ( Scene.Mesh.Primitive P in m_SourceMesh.Primitives )
							P.Render( 1 );
						
					} );

				// For the actual rendering process, we now attach the primitive's parameters to the rendering material and apply parameters
				// (we only need the diffuse map this time)
				foreach ( Scene.Mesh.Primitive P in m_SourceMesh.Primitives )
				{
					P.Parameters.AttachMaterial( m_MaterialParticles );
					P.Parameters.Apply();
				}

				m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.CULL_BACK );
				m_Device.SetStockDepthStencilState( Nuaj.Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
			}


			//////////////////////////////////////////////////////////////////////////
			// Build the particles' VB
			int		ParticlesCount = PARTICLES_SIDE_COUNT*PARTICLES_SIDE_COUNT;
			VS_T2[]	Particles = new VS_T2[ParticlesCount];
			for ( int Y=0; Y < PARTICLES_SIDE_COUNT; Y++ )
			{
				float V = (float) Y / PARTICLES_SIDE_COUNT;
				for ( int X=0; X < PARTICLES_SIDE_COUNT; X++ )
				{
					float U = (float) X / PARTICLES_SIDE_COUNT;
					Particles[PARTICLES_SIDE_COUNT*Y+X].UV.X = U;
					Particles[PARTICLES_SIDE_COUNT*Y+X].UV.Y = V;
				}
			}

			m_ParticlesVB = ToDispose( new VertexBuffer<VS_T2>( m_Device, "ParticlesVB", Particles ) );
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();

			base.OnClosing( e );
		}

		/// <summary>
		/// We'll keep you busy !
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void	RunMessageLoop()
		{
			//////////////////////////////////////////////////////////////////////////
			// Create a camera manipulator
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( panelOutput, m_Renderer.Camera );
			CamManip.InitializeCamera( new Vector3( 27.0f, 3.0f, 43.0f ), new Vector3( -2, 0, 0 ), Vector3.UnitY );


			//////////////////////////////////////////////////////////////////////////
			// Baked particles deform quad + debug overlay for baked textures
			Nuaj.Helpers.ScreenQuad	Quad = new Nuaj.Helpers.ScreenQuad( m_Device, "Quad" );
			Material<VS_Pt4V3T2>	QuadMaterial = new Material<VS_Pt4V3T2>( m_Device, "QuadMat", ShaderModel.SM4_0, Properties.Resources.BakedParticlesDisplay );
			VariableResource		vDebug0 = QuadMaterial.GetVariableByName( "ParticlesPositionAlphaTexture" ).AsResource;
			VariableResource		vDebug1 = QuadMaterial.GetVariableByName( "ParticlesNormalUVTexture" ).AsResource;


			//////////////////////////////////////////////////////////////////////////
			// Prefetch some variables

			// For noise deform
			VariableResource	vSourceParticlesPositionAlphaTexture = m_MaterialDeform.GetVariableByName( "ParticlesPositionAlphaTexture" ).AsResource;
			VariableResource	vSourceParticlesNormalUVTexture = m_MaterialDeform.GetVariableByName( "ParticlesNormalUVTexture" ).AsResource;
			VariableResource	vNoiseTexture0 = m_MaterialDeform.GetVariableByName( "NoiseTexture0" ).AsResource;
			VariableResource	vNoiseTexture1 = m_MaterialDeform.GetVariableByName( "NoiseTexture1" ).AsResource;
			VariableResource	vNoiseTexture2 = m_MaterialDeform.GetVariableByName( "NoiseTexture2" ).AsResource;
			VariableResource	vNoiseTexture3 = m_MaterialDeform.GetVariableByName( "NoiseTexture3" ).AsResource;
			VariableScalar		vTime = m_MaterialDeform.GetVariableByName( "Time" ).AsScalar;

			vSourceParticlesPositionAlphaTexture.SetResource( m_TextureParticlesPositions.TextureView );
			vSourceParticlesNormalUVTexture.SetResource( m_TextureParticlesPackedNormalUVs.TextureView );
			vNoiseTexture0.SetResource( m_NoiseTextures[0].TextureView );
			vNoiseTexture1.SetResource( m_NoiseTextures[1].TextureView );
			vNoiseTexture2.SetResource( m_NoiseTextures[2].TextureView );
			vNoiseTexture3.SetResource( m_NoiseTextures[3].TextureView );

			// For particles rendering
			EffectPass			ParticlesRenderPass = m_MaterialParticles.CurrentTechnique.GetPassByIndex( 0 );
			VariableResource	vOriginalParticlesPositionAlphaTexture = m_MaterialParticles.GetVariableByName( "OriginalParticlesPositionAlphaTexture" ).AsResource;
			VariableResource	vParticlesPositionAlphaTexture = m_MaterialParticles.GetVariableByName( "ParticlesPositionAlphaTexture" ).AsResource;
			VariableResource	vParticlesNormalUVTexture = m_MaterialParticles.GetVariableByName( "ParticlesNormalUVTexture" ).AsResource;
			VariableMatrix		vLocal2World = m_MaterialParticles.GetVariableByName( "Local2World" ).AsMatrix;


			//////////////////////////////////////////////////////////////////////////
			// Start the render loop
			DateTime	StartTime = DateTime.Now;
			DateTime	LastFrameTime = DateTime.Now;

			SharpDX.Windows.RenderLoop.Run( this, () =>
			{
				// Update time
				DateTime	CurrentFrameTime = DateTime.Now;				
				float	fDeltaTime = (float) (CurrentFrameTime - LastFrameTime).TotalSeconds;
				float	fTotalTime = (float) (CurrentFrameTime - StartTime).TotalSeconds;
				LastFrameTime = CurrentFrameTime;

				// =============== Render Scene ===============

				// Deform particles' positions & normals
				m_Device.SetMultipleRenderTargets( new RenderTarget<PF_RGBA16F>[] { m_TextureDeformedParticlesPositions, m_TextureDeformedParticlesPackedNormalUVs } );
				m_Device.SetViewport( 0, 0, m_TextureParticlesPositions.Width, m_TextureParticlesPositions.Height, 0, 1 );

				vTime.Set( fTotalTime );

				m_MaterialDeform.Render( (a,b,c) => 
					{
						Quad.Render();
					} );

				// Draw particles
				m_Device.SetDefaultRenderTarget();
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				using ( m_MaterialParticles.UseLock() )
				{
					Matrix	Local2World = m_SourceMesh.Local2World;

					m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

					vOriginalParticlesPositionAlphaTexture.SetResource( m_TextureParticlesPositions.TextureView );
					vParticlesPositionAlphaTexture.SetResource( m_TextureDeformedParticlesPositions.TextureView );
					vParticlesNormalUVTexture.SetResource( m_TextureDeformedParticlesPackedNormalUVs.TextureView );
					vLocal2World.SetMatrix( Local2World );

					ParticlesRenderPass.Apply();
					m_ParticlesVB.Use();
					m_ParticlesVB.Draw();
				}

				// DEBUG
				if ( true )
				{
// 					vDebug0.SetResource( m_TextureParticlesPositions.TextureView );
// 					vDebug1.SetResource( m_TextureParticlesPackedNormalUVs.TextureView );
					vDebug0.SetResource( m_TextureDeformedParticlesPositions.TextureView );
					vDebug1.SetResource( m_TextureDeformedParticlesPackedNormalUVs.TextureView );
					m_Device.SetViewport( 0, 0, 256, 256, 0.0f, 1.0f );
					using ( QuadMaterial.UseLock() ) QuadMaterial.Render( ( a, b, c ) => { Quad.Render(); } );
				}
				// DEBUG

				// Show !
				m_Device.Present();
			});
		}

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

		/// <summary>
		/// Loads a 3D noise texture from an embedded resource
		/// </summary>
		/// <param name="_NoiseIndex"></param>
		/// <returns></returns>
		public Texture3D<PF_RGBA16F>	LoadNoiseTexture( int _NoiseIndex )
		{
			const int	NOISE_SIZE = 16;
			float[,,]	Noise = new float[NOISE_SIZE,NOISE_SIZE,NOISE_SIZE];

			byte[][]	NoiseTextures = new byte[4][]
			{
				Properties.Resources.packednoise_half_16cubed_mips_00,
				Properties.Resources.packednoise_half_16cubed_mips_01,
				Properties.Resources.packednoise_half_16cubed_mips_02,
				Properties.Resources.packednoise_half_16cubed_mips_03,
			};

			// Read noise from resources
			System.IO.MemoryStream	Stream = new System.IO.MemoryStream( NoiseTextures[_NoiseIndex] );
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( Stream );
			int	XS, YS, ZS, PS;
			XS = Reader.ReadInt32();
			YS = Reader.ReadInt32();
			ZS = Reader.ReadInt32();
			PS = Reader.ReadInt32();

			Half	Temp = new Half();
			for ( int Z=0; Z < NOISE_SIZE; Z++ )
				for ( int Y=0; Y < NOISE_SIZE; Y++ )
					for ( int X=0; X < NOISE_SIZE; X++ )
					{
						Temp.RawValue = Reader.ReadUInt16();
						Reader.ReadUInt16();
						Reader.ReadUInt16();
						Reader.ReadUInt16();
						Noise[X,Y,Z] = (float) Temp;
					}
			Reader.Close();
			Reader.Dispose();

			// Build the 3D image and the 3D texture from it...
			using ( Image3D<PF_RGBA16F>	NoiseImage = new Image3D<PF_RGBA16F>( m_Device, "NoiseImage", NOISE_SIZE, NOISE_SIZE, NOISE_SIZE,
				( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{																			// (XYZ)
					_Color.X = Noise[_X,_Y,_Z];												// (000)
					_Color.Y = Noise[_X,(_Y+1) & (NOISE_SIZE-1),_Z];						// (010)
					_Color.Z = Noise[_X,_Y,(_Z+1) & (NOISE_SIZE-1)];						// (001)
					_Color.W = Noise[_X,(_Y+1) & (NOISE_SIZE-1),(_Z+1) & (NOISE_SIZE-1)];	// (011)

				}, 0 ) )
			{
				return ToDispose( new Texture3D<PF_RGBA16F>( m_Device, "Noise#"+_NoiseIndex, NoiseImage ) );
			}
		}

		#region Tree View Management

		protected TreeNode	m_ShaderInterfaceProvidersNode = null;
		protected Dictionary<IShaderInterfaceProvider,TreeNode>	m_ShaderInterfaceProvider2TreeNode = new Dictionary<IShaderInterfaceProvider,TreeNode>();
		protected void	BuildHierarchyTree()
		{
			// Build the renderer nodes
			TreeNode	RendererNode = new TreeNode( "Renderer" );
						RendererNode.Tag = m_Renderer;
			treeViewObjects.Nodes.Add( RendererNode );

			foreach ( Pipeline P in m_Renderer.Renderer.Pipelines )
			{
				TreeNode	PipelineNode = new TreeNode( P.Name + " (" + P.Type + ")" );
							PipelineNode.Tag = P;
				RendererNode.Nodes.Add( PipelineNode );

				foreach ( RenderTechnique RT in P.RenderTechniques )
				{
					TreeNode	RenderTechniqueNode = new TreeNode( RT.Name );
								RenderTechniqueNode.Tag = RT;
					PipelineNode.Nodes.Add( RenderTechniqueNode );
				}
			}

			// Build the renderer nodes
			m_ShaderInterfaceProvidersNode = new TreeNode( "Shader Providers" );
			m_ShaderInterfaceProvidersNode.Tag = m_Device;
			treeViewObjects.Nodes.Add( m_ShaderInterfaceProvidersNode );

			foreach ( IShaderInterfaceProvider SIP in m_Device.RegisteredShaderInterfaceProviders )
			{
				TreeNode	ProviderNode = new TreeNode( SIP.ToString() );
							ProviderNode.Tag = SIP;
				m_ShaderInterfaceProvidersNode.Nodes.Add( ProviderNode );
				m_ShaderInterfaceProvider2TreeNode[SIP] = ProviderNode;
			}

			m_Device.ShaderInterfaceProviderAdded += new Nuaj.Device.ShaderInterfaceEventHandler( Device_ShaderInterfaceProviderAdded );
			m_Device.ShaderInterfaceProviderRemoved += new Nuaj.Device.ShaderInterfaceEventHandler( Device_ShaderInterfaceProviderRemoved );

			treeViewObjects.ExpandAll();
		}

		void Device_ShaderInterfaceProviderAdded( IShaderInterfaceProvider _Provider )
		{
			TreeNode	ProviderNode = new TreeNode( _Provider.ToString() );
						ProviderNode.Tag = _Provider;
			m_ShaderInterfaceProvidersNode.Nodes.Add( ProviderNode );
			m_ShaderInterfaceProvider2TreeNode[_Provider] = ProviderNode;
		}

		void Device_ShaderInterfaceProviderRemoved( IShaderInterfaceProvider _Provider )
		{
			m_ShaderInterfaceProvidersNode.Nodes.Remove( m_ShaderInterfaceProvider2TreeNode[_Provider] );
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		private void Device_MaterialEffectRecompiled( object sender, EventArgs e )
		{
			if ( richTextBoxOutput.InvokeRequired )
			{
				richTextBoxOutput.BeginInvoke( new EventHandler( Device_MaterialEffectRecompiled ), sender, e );
				return;
			}

			IMaterial	M = sender as IMaterial;
			richTextBoxOutput.Log( "\"" + M.ToString() + "\" recompiled...\r\n" );
			if ( M.HasErrors )
				richTextBoxOutput.LogError( "ERRORS:\r\n" + M.CompilationErrors );
			else if ( M.CompilationErrors != "" )
				richTextBoxOutput.LogWarning( "WARNINGS:\r\n" + M.CompilationErrors );
			else
				richTextBoxOutput.LogSuccess( "0 error...\r\n" );
			richTextBoxOutput.Log( "------------------------------------------------------------------\r\n\r\n" );
		}

		private void treeViewObjects_AfterSelect( object sender, TreeViewEventArgs e )
		{
			propertyGrid.SelectedObject = e.Node.Tag;
		}

		#endregion
	}
}
