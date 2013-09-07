using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using Nuaj;

namespace TreeGloumibule
{
	public partial class TreeForm : Form, IShaderInterfaceProvider
	{
		#region FIELDS

		protected Nuaj.Device			m_Device = null;
		protected Stack<IDisposable>	m_Disposables = new Stack<IDisposable>();

		protected Camera				m_Camera = null;
		protected Tree					m_Tree = new Tree();
		protected LightHemisphere		m_Light = new LightHemisphere( 257 );

		#endregion

		#region NESTED TYPES

		// This defines a trunk segment
		[StructLayout( LayoutKind.Sequential )]
		protected struct VS_SEGMENT
		{
			[Semantic( "P" )]
			public Vector3	Position;		// Position in space
			[Semantic( "X" )]
			public Vector3	X;				// Directional basis (the segment is aligned with +Z)
			[Semantic( "Y" )]
			public Vector3	Y;
			[Semantic( "Z" )]
			public Vector3	Z;
			[Semantic( "R" )]
			public float	Radius;			// Radius of the segment
			[Semantic( "M" )]
			public float	Mass;			// Mass of the following segments + sub-hierarchical levels
			[Semantic( "T" )]
			public float	Torque;			// Torque exerted by that segment to the trunk
			[Semantic( "LA" )]
			public float	LightAbs;		// Absolute amount of light absorbed by the segment
			[Semantic( "LR" )]
			public float	LightRel;		// Relative amount of light absorbed by the segment (compared to the entire amount of light absorbed by the tree)
			[Semantic( "NA" )]
			public float	NutrientAbs;	// Absolute amount of nutrients absorbed by the segment
			[Semantic( "NR" )]
			public float	NutrientRel;	// Relative amount of nutrients absorbed by the segment (compared to the entire amount of nutrients absorbed by the tree)
		}

		#endregion

		#region METHODS

		public TreeForm()
		{
			InitializeComponent();

			//////////////////////////////////////////////////////////////////////////
			// Create the device
			try
			{
				SwapChainDescription	Desc = new SwapChainDescription()
				{
					BufferCount = 1,
					ModeDescription = new ModeDescription( outputPanel.Width, outputPanel.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm ),
					IsWindowed = true,
					SampleDescription = new SampleDescription( 1, 0 ),
					SwapEffect = SwapEffect.Discard,
					Usage = Usage.RenderTargetOutput
				};

				m_Device = ToDispose( Nuaj.Device.CreateInstance( Desc, outputPanel ) );
				m_Device.MaterialEffectRecompiled += new EventHandler( Device_MaterialEffectRecompiled );
			}
			catch ( Exception _e )
			{
				throw new Exception( "Failed to create the DirectX device !", _e );
			}

			InitializeResources();

			//////////////////////////////////////////////////////////////////////////
			// Create a perspective camera
			m_Camera = ToDispose( new Camera( m_Device, "Default Camera" ) );
			m_Camera.CreatePerspectiveCamera( 60.0f * (float) Math.PI / 180.0f, (float) ClientSize.Width / ClientSize.Height, 0.01f, 100.0f );
			m_Camera.Activate();

			//////////////////////////////////////////////////////////////////////////
			// Initialize the tree & companions
			m_Light.SetFromLightSource( Vector3.UnitZ );	// Nice dome lit from above
			ShowLightDome();

			m_Tree.LightHemisphere = m_Light;
			m_Tree.Initialize( Vector3.UnitY, 0.05f, 1.0f, 1 );
			m_Tree.PropagateEvalFrame();

			// Let's browse the tree parameters
			propertyGridParameters.SelectedObject = m_Tree.Params;

			// Initialize curves panel
			curvesPanel.InitCurves( 4, new Pen[] {
				Pens.Red,		// Available light
				Pens.Orange,	// Light needs
				Pens.Blue,		// Available nutrients
				Pens.Cyan		// Nutrients needs
			},
			new int[4] {
				0,
				0,	// Total light needs is relative to available light
				2,
				2,	// Total nutrients needs is relative to available nutrients
			} );
		}

		protected void	ShowLightDome()
		{
//			m_Light.SetFromLightSource( new Vector3( -2.0f, 0.5f, 0.7f ) );
// 			m_Light.AddBlockerSpherical( new Vector3( 1.0f, 0.02f, 2.1f ), 0.5f, 1.0f );
// 			m_Light.AddBlockerSpherical( new Vector3( 0.5f, -1.4f, 2.1f ), 0.5f, 1.0f );
//			m_Light.AddBlockerCylindrical( new Vector2( 2.0f, 0.0f ), 0.5f, 4.0f, 0.75f );
// 			m_Light.AddBlockerBranch( new Vector3( -0.25f, -0.2f, 0.0f ), m_Tree.Trunk, 1.0f );

			// Check
			panelLightDome.FillBitmap( ( X, Y, W, H ) =>
				{
					float	L = m_Light[X*m_Light.Size/W, Y*m_Light.Size/H];
					byte	C = (byte) (255 * Math.Max( 0.0f,  Math.Min( 1.0f, L )) );
					return 0xFF000000 | (uint) (C * 0x00010101);

// 					Vector2	Gr = 20.0f * m_Light.Grad( X*m_Light.Size/W, Y*m_Light.Size/H );
// 					byte	R = (byte) Math.Max( 0, Math.Min( 255, 127 * (1.0 + Gr.X) ) );
// 					byte	G = (byte) Math.Max( 0, Math.Min( 255, 127 * (1.0 + Gr.Y) ) );
// // 					byte	R = (byte) Math.Max( 0, Math.Min( 255, 255 * Math.Abs( Gr.X ) ) );
// // 					byte	G = (byte) Math.Max( 0, Math.Min( 255, 255 * Math.Abs( Gr.Y ) ) );
// // 					byte	R = (byte) Math.Max( 0, Math.Min( 255, 255 * Gr.X ) );
// // 					byte	G = (byte) Math.Max( 0, Math.Min( 255, 255 * Gr.Y ) );
// 					return 0xFF000040 | (uint) ((R << 16) | (G << 8));
				} );

			Vector3	OriginalDirection = new Vector3( 0.9f, -0.0f, 0.1f );
			OriginalDirection.Normalize();
			Vector3	NewDirection = m_Light.ComputePreferedDirection( OriginalDirection );
//			NewDirection = m_Light.ComputePreferedDirection( NewDirection );

			using ( Graphics G = Graphics.FromImage( panelLightDome.m_Bitmap ) )
			{
				for ( int StepIndex=0; StepIndex < LightHemisphere.GRADIENT_STEPS_COUNT; StepIndex++ )
					G.DrawLine( Pens.Red,
						panelLightDome.m_Bitmap.Width * m_Light.m_MarchedPos[StepIndex].X / m_Light.Size,
						panelLightDome.m_Bitmap.Height * m_Light.m_MarchedPos[StepIndex].Y / m_Light.Size,
						panelLightDome.m_Bitmap.Width * m_Light.m_MarchedPos[1+StepIndex].X / m_Light.Size,
						panelLightDome.m_Bitmap.Height * m_Light.m_MarchedPos[1+StepIndex].Y / m_Light.Size
						);
			}
			panelLightDome.Refresh();
		}

		#region Display Management

		protected Texture2D<PF_RGBA8>		m_TextureLeaf = null;
		protected Material<VS_SEGMENT>		m_MaterialTree = null;
		protected Material<VS_P3N3T2>		m_MaterialLeaf = null;
		protected VertexBuffer<VS_P3N3T2>	m_PlaneVB = null;

		protected void	InitializeResources()
		{
			m_TextureLeaf = ToDispose( Texture2D<PF_RGBA8>.CreateFromBitmap( m_Device, "Leaf Texture", Properties.Resources.Leaf, 0, 2.2f ) );
			m_MaterialTree = ToDispose( new Material<VS_SEGMENT>( m_Device, "Tree Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/TreeGloumibule/Tree.fx" ) ) );
			m_MaterialLeaf = ToDispose( new Material<VS_P3N3T2>( m_Device, "Leaf Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/TreeGloumibule/Leaf.fx" ) ) );
			m_MaterialLeaf.GetVariableByName( "TextureLeaf" ).AsResource.SetResource( m_TextureLeaf.TextureView );

			// Build the plane VB
			float	PlaneHalfSize = 4.0f;
			VS_P3N3T2[]	PlaneVertices = new VS_P3N3T2[]
			{
				new VS_P3N3T2() { Position=PlaneHalfSize*new Vector3( -1.0f, 0.0f, -1.0f ), Normal=Vector3.UnitY, UV=new Vector2( 0.0f, 0.0f ) },
				new VS_P3N3T2() { Position=PlaneHalfSize*new Vector3( -1.0f, 0.0f, +1.0f ), Normal=Vector3.UnitY, UV=new Vector2( 0.0f, 1.0f ) },
				new VS_P3N3T2() { Position=PlaneHalfSize*new Vector3( +1.0f, 0.0f, -1.0f ), Normal=Vector3.UnitY, UV=new Vector2( 1.0f, 0.0f ) },
				new VS_P3N3T2() { Position=PlaneHalfSize*new Vector3( +1.0f, 0.0f, +1.0f ), Normal=Vector3.UnitY, UV=new Vector2( 1.0f, 1.0f ) },
			};
			m_PlaneVB = ToDispose( new VertexBuffer<VS_P3N3T2>( m_Device, "Ground Plane", PlaneVertices ) );
		}

		/// <summary>
		/// We'll keep you busy !
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void	RunMessageLoop()
		{
			Nuaj.Helpers.CameraManipulator	CamManip = new Nuaj.Helpers.CameraManipulator();
			CamManip.Attach( outputPanel, m_Camera );
			CamManip.InitializeCamera( new Vector3( 0.0f, 2.0f, 4.0f ), new Vector3( 0.0f, 0.0f, 0.0f ), Vector3.UnitY );

			//////////////////////////////////////////////////////////////////////////
			// Create a simple directional light provider
			m_Device.RegisterShaderInterfaceProvider( typeof(IDirectionalLight), this );

			//////////////////////////////////////////////////////////////////////////
			// Build initial tree & leaves primitives
			UpdateGeometry();

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

				// Clear render target
				m_Device.ClearRenderTarget( m_Device.DefaultRenderTarget, Color.CornflowerBlue );
				m_Device.ClearDepthStencil( m_Device.DefaultDepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0 );

				m_TreeGeometryLock.WaitOne();
				{
					// Render tree
					m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.CULL_BACK );
					m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.DISABLED );
					m_TreePrimitive.Render();
					m_RootsPrimitive.Render();

					// Render leaves
					m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.ALPHA2COVERAGE );
					m_MaterialLeaf.CurrentTechnique = m_MaterialLeaf.GetTechniqueByName( "DrawLeaf" );
					m_LeavesPrimitive.Render();
				}
				m_TreeGeometryLock.ReleaseMutex();

				// Render ground plane
				using ( m_MaterialLeaf.UseLock() )
				{
					m_Device.SetStockRasterizerState( Nuaj.Device.HELPER_STATES.NO_CULLING );
					m_Device.SetStockBlendState( Nuaj.Device.HELPER_BLEND_STATES.ADDITIVE );
					m_Device.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
					m_MaterialLeaf.CurrentTechnique = m_MaterialLeaf.GetTechniqueByName( "DrawGroundPlane" );
					m_PlaneVB.Use();
					m_MaterialLeaf.Render( ( A, B, C ) => { m_PlaneVB.Draw(); } );
				}

				// Show !
				m_Device.Present();
			} );

			//////////////////////////////////////////////////////////////////////////
			// Dispose of tree & leaves primitives
			if ( m_TreePrimitive != null )
				m_TreePrimitive.Dispose();
			if ( m_RootsPrimitive != null )
				m_RootsPrimitive.Dispose();
			if ( m_LeavesPrimitive != null )
				m_LeavesPrimitive.Dispose();
		}

		/// <summary>
		/// Updates the tree geometry from the current tree
		/// </summary>
		protected System.Threading.Mutex	m_TreeGeometryLock = new System.Threading.Mutex();
		protected Primitive<VS_SEGMENT,int>	m_TreePrimitive = null;
		protected Primitive<VS_SEGMENT,int>	m_RootsPrimitive = null;
		protected Primitive<VS_P3N3T2,int>	m_LeavesPrimitive = null;
		protected void	UpdateGeometry()
		{
			m_TreeGeometryLock.WaitOne();

			// Dispose of existing geometry
			if ( m_TreePrimitive != null )
				m_TreePrimitive.Dispose();
			if ( m_RootsPrimitive != null )
				m_RootsPrimitive.Dispose();
			if ( m_LeavesPrimitive != null )
				m_LeavesPrimitive.Dispose();

			// Build new geometry
			BuildVertexBuffers( m_Tree, out m_TreePrimitive, out m_RootsPrimitive, out m_LeavesPrimitive );

			m_TreePrimitive.Material = m_MaterialTree;
			m_RootsPrimitive.Material = m_MaterialTree;
			m_LeavesPrimitive.Material = m_MaterialLeaf;

			m_TreeGeometryLock.ReleaseMutex();
		}

		/// <summary>
		/// Builds the vertex buffers to display the tree and its leaves
		/// </summary>
		/// <param name="_Tree"></param>
		protected void	BuildVertexBuffers( Tree _Tree, out Primitive<VS_SEGMENT,int> _TreePrimitive, out Primitive<VS_SEGMENT,int> _RootsPrimitive, out Primitive<VS_P3N3T2,int> _LeavesPrimitive )
		{
			// Propragate reference frame evaluation
			_Tree.PropagateEvalFrame();

			// Recursively build vertices & indices
			List<VS_SEGMENT>	TrunkSegmentVertices = new List<VS_SEGMENT>();
			List<int>			TrunkSegmentIndices = new List<int>();
			List<VS_SEGMENT>	RootsSegmentVertices = new List<VS_SEGMENT>();
			List<int>			RootsSegmentIndices = new List<int>();
			List<VS_P3N3T2>		LeafVertices = new List<VS_P3N3T2>();

			RecurseBuildVertexBuffers( _Tree.Trunk, TrunkSegmentVertices, TrunkSegmentIndices, LeafVertices );
			RecurseBuildVertexBuffers( _Tree.Root, RootsSegmentVertices, RootsSegmentIndices, null );

			// Build tree primitive
			_TreePrimitive = new Primitive<VS_SEGMENT,int>( m_Device, "Tree Primitive", PrimitiveTopology.LineList, TrunkSegmentVertices.ToArray(), TrunkSegmentIndices.ToArray() );

			// Build root primitive
			_RootsPrimitive = new Primitive<VS_SEGMENT,int>( m_Device, "Roots Primitive", PrimitiveTopology.LineList, RootsSegmentVertices.ToArray(), RootsSegmentIndices.ToArray() );

			// Build leaves primitive
			_LeavesPrimitive = new Primitive<VS_P3N3T2,int>( m_Device, "Leaves Primitive", PrimitiveTopology.PointList, LeafVertices.ToArray() );
		}

		protected void	RecurseBuildVertexBuffers( Tree.Branch _Branch, List<VS_SEGMENT> _SegmentVertices, List<int> _SegmentIndices, List<VS_P3N3T2> _LeafVertices )
		{
			Tree.Branch.Segment	Previous = _Branch.StartSegment;
			Tree.Branch.Segment	Current = Previous.Next;

			// Check for branch recursion
			if ( Previous is Tree.Branch.SplitSegment )
				RecurseBuildVertexBuffers( (Previous as Tree.Branch.SplitSegment).SplittingBranch, _SegmentVertices, _SegmentIndices, _LeafVertices );

			// Build start segment's vertex
			int	PreviousSegmentVertexIndex = _SegmentVertices.Count;
			BuildSegmentVertex( Previous, _SegmentVertices );

			while ( Current != null )
			{
				// Build current segment's vertex
				int	CurrentSegmentVertexIndex = _SegmentVertices.Count;
				BuildSegmentVertex( Current, _SegmentVertices );

				// Build new indices couple (we're drawing line lists here)
				_SegmentIndices.Add( PreviousSegmentVertexIndex );
				_SegmentIndices.Add( CurrentSegmentVertexIndex );

				// Check for branch recursion
				if ( Current is Tree.Branch.SplitSegment )
					RecurseBuildVertexBuffers( (Current as Tree.Branch.SplitSegment).SplittingBranch, _SegmentVertices, _SegmentIndices, _LeafVertices );

				// Go to next segment
				PreviousSegmentVertexIndex = CurrentSegmentVertexIndex;
				Previous = Current;
				Current = Current.Next;
			}

			if ( _LeafVertices == null )
				return;	// Don't need leaves...

			// Build a tiny (and really cute) leaf at the end :)
			VS_P3N3T2	LeafVertex = new VS_P3N3T2();
			LeafVertex.Position = Previous.WorldPosition - 0.05f * Previous.WorldY;
			LeafVertex.Normal = Previous.WorldY;
			LeafVertex.UV = new Vector2( 0.4f, 0.5f );	// Arbitrary !
			_LeafVertices.Add( LeafVertex );
		}

		protected void	BuildSegmentVertex( Tree.Branch.Segment _Segment, List<VS_SEGMENT> _SegmentVertices )
		{
			VS_SEGMENT	S = new VS_SEGMENT();
			S.Position = _Segment.WorldPosition;
			S.X = _Segment.WorldX;
			S.Y = _Segment.WorldY;
			S.Z = _Segment.WorldZ;
			S.Radius = _Segment.Radius;
			S.Torque = _Segment.Torque.Length();
			S.Mass = _Segment.AccumulatedMass;
			S.LightRel = 0.0f;
			S.LightAbs = 0.0f;
			S.NutrientRel = 0.0f;
			S.NutrientAbs = 0.0f;
			_SegmentVertices.Add( S );
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			IDirectionalLight	I = _Interface as IDirectionalLight;
			I.Color = (Vector4) (Color4) Color.White;
			I.Direction = new Vector3( 1.0f, 1.0f, 1.0f );
		}

		#endregion

		#endregion

		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

		protected override void OnClosing( CancelEventArgs e )
		{
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();

			base.OnClosing( e );
		}

		#endregion

		#region EVENT HANDLERS

		private void Device_MaterialEffectRecompiled( object sender, EventArgs e )
		{
// 			if ( richTextBoxOutput.InvokeRequired )
// 			{
// 				richTextBoxOutput.BeginInvoke( new EventHandler( Device_MaterialEffectRecompiled ), sender, e );
// 				return;
// 			}
// 
// 			IMaterial	M = sender as IMaterial;
// 			richTextBoxOutput.Log( "\"" + M.ToString() + "\" recompiled...\r\n" );
// 			if ( M.HasErrors )
// 				richTextBoxOutput.LogError( "ERRORS:\r\n" + M.CompilationErrors );
// 			else if ( M.CompilationErrors != "" )
// 				richTextBoxOutput.LogWarning( "WARNINGS:\r\n" + M.CompilationErrors );
// 			else
// 				richTextBoxOutput.LogSuccess( "0 error...\r\n" );
// 			richTextBoxOutput.Log( "------------------------------------------------------------------\r\n\r\n" );
		}

		private void toolStripButtonStep_Click( object sender, EventArgs e )
		{
			timer_Tick( sender, e );
		}

		private void toolStripButtonPlayStop_Click( object sender, EventArgs e )
		{
			if ( !timer.Enabled )
			{
				toolStripButtonPlayStop.Image = Properties.Resources.Stop;
				timer.Enabled = true;
			}
			else
			{
				toolStripButtonPlayStop.Image = Properties.Resources.Play;
				timer.Enabled = false;
			}
		}

		int m_StepsCount = 0;
		private void timer_Tick( object sender, EventArgs e )
		{
			// Grow !
			m_Tree.GrowOneStep();

			// Update the tree geometry for display
			UpdateGeometry();

			// Update curves
			curvesPanel.AddCurvePoints( new Vector2[4]
			{
				new Vector2( m_StepsCount, m_Tree.Params.AvailableLight ),
				new Vector2( m_StepsCount, m_Tree.Params.TotalLightNeeds ),
				new Vector2( m_StepsCount, m_Tree.Params.AvailableNutrients ),
				new Vector2( m_StepsCount, m_Tree.Params.TotalNutrientNeeds ),
			} );
			curvesPanel.UpdateBitmap();

			m_StepsCount++;
		}

		private void outputPanel_MouseDown( object sender, MouseEventArgs e )
		{
			float	X = (float) e.X / outputPanel.Width;
			float	Y = (float) e.Y / outputPanel.Height;
			Vector3	P, V;
			m_Camera.BuildWorldRay( X, Y, out P, out V );
			Tree.Branch.RayHit[]	Hits = m_Tree.Hit( P, V );

			Tree.Branch.RayHit	Hit = Hits.Length > 0 ? Hits[0] : null;
			propertyGridSelection.SelectedObject = Hit;
		}

		#endregion
	}
}
