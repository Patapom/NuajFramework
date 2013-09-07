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
	/// A scene is a collection of meshes, lights, cameras, textures and material parameters
	/// A mesh is a collection of primitives that are rendered by the renderer's render techniques
	/// 
	/// You can load and save a scene in the Nuaj.Cirrus proprietary format.
	/// Scenes can also be imported through 3rd party formats (cf. FBXSceneLoader) then saved into the proprietary format.
	/// </summary>
	public class Scene : Component
	{
		#region NESTED TYPES

		/// <summary>
		/// The delegate to provide to be notified of a texture update
		/// </summary>
		/// <param name="_Caller"></param>
		/// <param name="_Old"></param>
		/// <param name="_New"></param>
		public delegate void	TextureUpdatedEventHandler( ITextureProvider _Caller, ITexture2D _Old, ITexture2D _New );

		/// <summary>
		/// This should be implemented by any object capable of providing textures given a texture URL as identifier
		/// </summary>
		/// <example>You can find an example of such a provider in the SceneTextureProvider class in the Implementations/TextureProvider folder of that same project</example>
		public interface	ITextureProvider
		{
			/// <summary>
			/// Loads the texture of the provided URL
			/// </summary>
			/// <param name="_URL">The URL to the texture</param>
			/// <param name="_OpacityURL">An optional URL to the opacity texture to use as alpha channel</param>
			/// <param name="_bCreateMipMaps">True to create a texture with mip maps</param>
			/// <param name="_TextureUpdatedHandler">An optional handler to be notified of a texture update</param>
			/// <returns></returns>
			ITexture2D		LoadTexture( string _URL, string _OpacityURL, bool _bCreateMipMaps, TextureUpdatedEventHandler _TextureUpdatedHandler );
		}

		/// <summary>
		/// Supported node types in a scene
		/// </summary>
		public enum		NODE_TYPE
		{
			NODE,	// Basic node with only PRS informations
			MESH,
			CAMERA,
			LIGHT,
		}

		public delegate bool	FindNodeDelegate( Node _Node );

		/// <summary>
		/// The base scene node class that holds a Local->Parent transform
		/// </summary>
		public class	Node : IDisposable
		{
			#region FIELDS

			protected Scene			m_Owner = null;
			protected int			m_ID = -1;
			protected Node			m_Parent = null;
			protected string		m_Name = null;
			protected Matrix		m_Local2Parent = Matrix.Identity;
			protected bool			m_bVisible = true;
			protected bool			m_bCulled = false;

			protected List<Node>	m_Children = new List<Node>();

			// Cached state
			protected bool			m_bStateDirty = true;	// Needs propagation of state ?
			protected bool			m_bPropagatedVisibility = true;

			protected bool			m_bFirstLocal2WorldAssignment = true;
			protected Matrix		m_Local2World = Matrix.Identity;
			protected Matrix		m_PreviousLocal2World = Matrix.Identity;	// The object transform matrix from the previous frame

			protected bool			m_bDeltaMotionDirty = true;
			protected Vector3		m_DeltaPosition = Vector3.Zero;
			protected Quaternion	m_DeltaRotation = Quaternion.Identity;
			protected Vector3		m_DeltaPivot = Vector3.Zero;

			#endregion

			#region PROPERTIES

			public Scene				Owner			{ get { return m_Owner; } }
			public virtual NODE_TYPE	NodeType		{ get { return NODE_TYPE.NODE; } }
			public int					ID				{ get { return m_ID; } }
			public virtual Node			Parent			{ get { return m_Parent; } }
			public virtual string		Name			{ get { return m_Name; } }
			public virtual Matrix		Local2Parent	{ get { return m_Local2Parent; } set { m_Local2Parent = value; PropagateDirtyState(); } }
			public virtual Matrix		Local2World		{ get { return m_Local2World; } set { m_Local2World = value; } }
			public virtual bool			Visible			{ get { return m_bVisible && m_bPropagatedVisibility; } set { m_bVisible = value; PropagateDirtyState(); } }
			public virtual Node[]		Children		{ get { return m_Children.ToArray(); } }

			public virtual Matrix		PreviousLocal2World	{ get { return m_PreviousLocal2World; } set { m_PreviousLocal2World = value; } }

			/// <summary>
			/// Gets or sets the culled state of that node
			/// This state should be updated every frame based on the current camera
			/// It should embed the "Visible" state as well, meaning that if Visible is false then Culled should be true
			///  so finally render techniques only need to test culled to know if they should render a primitive or not.
			/// </summary>
			public virtual bool			Culled			{ get { return m_bCulled; } set { m_bCulled = value; } }

			/// <summary>
			/// Internal event one can subscribe to to be notified a node was updated
			/// </summary>
			internal event EventHandler	StatePropagated;

			#endregion

			#region METHODS

			/// <summary>
			/// Creates a new scene node
			/// </summary>
			/// <param name="_Owner"></param>
			/// <param name="_ID"></param>
			/// <param name="_Name"></param>
			/// <param name="_Parent"></param>
			/// <param name="_Local2Parent"></param>
			internal Node( Scene _Owner, int _ID, string _Name, Node _Parent, Matrix _Local2Parent )
			{
				m_Owner = _Owner;
				m_ID = _ID;
				m_Name = _Name;
				m_Parent = _Parent;
				m_Local2Parent = _Local2Parent;

				// Append the node to its parent
				if ( _Parent != null )
					_Parent.AddChild( this );
			}

			/// <summary>
			/// Creates a scene node from a stream
			/// </summary>
			/// <param name="_Owner"></param>
			/// <param name="_Parent"></param>
			/// <param name="_Reader"></param>
			internal Node( Scene _Owner, Node _Parent, System.IO.BinaryReader _Reader )
			{
				m_Owner = _Owner;

//				m_NodeType = _Reader.ReadInt32();	// Don't read back the node type as it has already been consumed by the parent
				m_ID = _Reader.ReadInt32();
				m_Owner.m_ID2Node[m_ID] = this;
				m_Name = _Reader.ReadString();

				m_Parent = _Parent;
				if ( _Parent != null )
					m_Parent.AddChild( this );

				// Read the matrix
				m_Local2Parent.M11 = _Reader.ReadSingle();
				m_Local2Parent.M12 = _Reader.ReadSingle();
				m_Local2Parent.M13 = _Reader.ReadSingle();
				m_Local2Parent.M14 = _Reader.ReadSingle();
				m_Local2Parent.M21 = _Reader.ReadSingle();
				m_Local2Parent.M22 = _Reader.ReadSingle();
				m_Local2Parent.M23 = _Reader.ReadSingle();
				m_Local2Parent.M24 = _Reader.ReadSingle();
				m_Local2Parent.M31 = _Reader.ReadSingle();
				m_Local2Parent.M32 = _Reader.ReadSingle();
				m_Local2Parent.M33 = _Reader.ReadSingle();
				m_Local2Parent.M34 = _Reader.ReadSingle();
				m_Local2Parent.M41 = _Reader.ReadSingle();
				m_Local2Parent.M42 = _Reader.ReadSingle();
				m_Local2Parent.M43 = _Reader.ReadSingle();
				m_Local2Parent.M44 = _Reader.ReadSingle();

				// Read specific data
				LoadSpecific( _Reader );

				// Read children
				int	ChildrenCount = _Reader.ReadInt32();
				for ( int ChildIndex=0; ChildIndex < ChildrenCount; ChildIndex++ )
				{
					NODE_TYPE	ChildType = (NODE_TYPE) _Reader.ReadByte();
					switch ( ChildType )
					{
					case NODE_TYPE.NODE:
						new Node( _Owner, this, _Reader );
						break;

					case NODE_TYPE.MESH:
						new Mesh( _Owner, this, _Reader );
						break;

					case NODE_TYPE.LIGHT:
						new Light( _Owner, this, _Reader );
						break;

					case NODE_TYPE.CAMERA:
						new Camera( _Owner, this, _Reader );
						break;
					}
				}
			}

			public override string ToString()
			{
				return m_Name;
			}

			#region IDisposable Members

			public void Dispose()
			{
				DisposeSpecific();

				// Dispose of children
				foreach ( Node Child in m_Children )
					Child.Dispose();
				m_Children.Clear();
			}

			#endregion

			public void		AddChild( Node _Child )
			{
				m_Children.Add( _Child );
				m_bStateDirty = true;
			}

			public void		RemoveChild( Node _Child )
			{
				m_Children.Remove( _Child );
			}

			/// <summary>
			/// Propagates this node's state to its children (e.g. visibility, Local2World transform, etc.)
			/// This should be done only once per frame and is usually automatically taken care of by the renderer
			/// </summary>
			/// <returns>True if this node's state or the state of one of its children was modified</returns>
			public virtual bool		PropagateState()
			{
				bool	bModified = false;
				if ( m_bStateDirty )
				{
					m_PreviousLocal2World = m_Local2World;	// Current becomes previous...
					m_bDeltaMotionDirty = true;				// Delta values become dirty...

					if ( m_Parent == null )
					{
						m_bPropagatedVisibility = m_bVisible;						// Use our own visibility
						m_Local2World = m_Local2Parent;								// Use our own transform
					}
					else
					{
						m_bPropagatedVisibility = m_Parent.m_bVisible;				// Use parent's visibility
						m_Local2World = m_Local2Parent * m_Parent.m_Local2World;	// Compose with parent...
					}

					if ( m_bFirstLocal2WorldAssignment )
						m_PreviousLocal2World = m_Local2World;	// For first assignment, previous & current matrices are the same !

					m_bStateDirty = false;	// We're good !
					m_bFirstLocal2WorldAssignment = false;
					bModified = true;
				}

				// Propagate to children
				foreach ( Node Child in m_Children )
					bModified |= Child.PropagateState();

				// Notify of propagation
				if ( bModified && StatePropagated != null )
					StatePropagated( this, EventArgs.Empty );

				return bModified;
			}

			/// <summary>
			/// This is a helper to compute relative motion between current and last frame, usually used for motion blur
			/// </summary>
			/// <param name="_DeltaPosition">Returns the difference in position from last frame</param>
			/// <param name="_DeltaRotation">Returns the difference in rotation from last frame</param>
			/// <param name="_Pivot">Returns the pivot position the object rotated about</param>
			public void		ComputeDeltaPositionRotation( out Vector3 _DeltaPosition, out Quaternion _DeltaRotation, out Vector3 _Pivot )
			{
				if ( m_bDeltaMotionDirty )
					Scene.ComputeObjectDeltaPositionRotation( ref m_PreviousLocal2World, ref m_Local2World, out m_DeltaPosition, out m_DeltaRotation, out m_DeltaPivot );

				_DeltaPosition = m_DeltaPosition;
				_DeltaRotation = m_DeltaRotation;
				_Pivot = m_DeltaPivot;

				m_bDeltaMotionDirty = false;
			}

			internal void	Save( System.IO.BinaryWriter _Writer )
			{
				_Writer.Write( (byte) NodeType );
				_Writer.Write( m_ID );
				_Writer.Write( m_Name );

				// Write the matrix
				_Writer.Write( m_Local2Parent.M11 );
				_Writer.Write( m_Local2Parent.M12 );
				_Writer.Write( m_Local2Parent.M13 );
				_Writer.Write( m_Local2Parent.M14 );
				_Writer.Write( m_Local2Parent.M21 );
				_Writer.Write( m_Local2Parent.M22 );
				_Writer.Write( m_Local2Parent.M23 );
				_Writer.Write( m_Local2Parent.M24 );
				_Writer.Write( m_Local2Parent.M31 );
				_Writer.Write( m_Local2Parent.M32 );
				_Writer.Write( m_Local2Parent.M33 );
				_Writer.Write( m_Local2Parent.M34 );
				_Writer.Write( m_Local2Parent.M41 );
				_Writer.Write( m_Local2Parent.M42 );
				_Writer.Write( m_Local2Parent.M43 );
				_Writer.Write( m_Local2Parent.M44 );

				// Write specific data
				SaveSpecific( _Writer );

				// Write children
				_Writer.Write( m_Children.Count );
				foreach ( Node Child in m_Children )
					Child.Save( _Writer );
			}

			/// <summary>
			/// Override this to restore internal references once the scene has loaded
			/// </summary>
			internal virtual void	RestoreReferences()
			{
			}

			protected virtual void	LoadSpecific( System.IO.BinaryReader _Reader )
			{
			}

			protected virtual void	SaveSpecific( System.IO.BinaryWriter _Writer )
			{
			}

			protected virtual void	DisposeSpecific()
			{
			}

			/// <summary>
			/// Mark this node and children as dirty
			/// </summary>
			protected virtual void PropagateDirtyState()
			{
				m_bStateDirty = true;
				foreach ( Node Child in m_Children )
					Child.PropagateDirtyState();
			}

			#endregion
		}

		/// <summary>
		/// The mesh class node hosts a collection of primitives
		/// </summary>
		public class	Mesh : Node
		{
			#region NESTED TYPES

			/// <summary>
			/// A primitive wraps a basic Nuaj primitive and contains additional informations like material parameters to setup to render the primitive
			/// Primitives should be created via a ITechniqueSupportsObjects render technique using the CreatePrimitive() method
			/// </summary>
			public class Primitive : IDisposable
			{
				#region FIELDS

				protected Mesh					m_Parent = null;
				protected bool					m_bVisible = true;
				protected bool					m_bCastShadow = true;
				protected bool					m_bReceiveShadow = true;
				protected ITechniqueSupportsObjects	m_RenderTechnique = null;	// The render technique that created the primitive
				protected IPrimitive			m_Primitive = null;
				protected MaterialParameters	m_Parameters = null;
				protected int					m_FrameToken = -1;			// The token for the last rendered frame

				#endregion

				#region PROPERTIES

				/// <summary>
				/// Gets the parent mesh
				/// </summary>
				public Mesh					Parent				{ get { return m_Parent; } }

				/// <summary>
				/// Gets or sets the visible state of that primitive, an invisible primitive won't be rendered (obviously)
				/// </summary>
				public bool					Visible				{ get { return m_bVisible; } set { m_bVisible = value; } }

				/// <summary>
				/// Gets the culled state.
				/// This is the only state you need to test to know if you must render a primitive or not as the "Visible" state
				///  is embedded in the Culled state (meaning that if a primitive is not visible then Culled is true)
				/// </summary>
				public bool					Culled				{ get { return m_Parent.Culled; } }

				/// <summary>
				/// Gets or sets the "cast shadow" state of that primitive
				/// </summary>
				public bool					CastShadow			{ get { return m_bCastShadow; } set { m_bCastShadow = value; } }

				/// <summary>
				/// Gets or sets the "receive shadow" state of that primitive
				/// </summary>
				public bool					ReceiveShadow		{ get { return m_bReceiveShadow; } set { m_bReceiveShadow = value; } }

				/// <summary>
				/// Gets the render technique that created the primitive
				/// </summary>
				public ITechniqueSupportsObjects	RenderTechnique		{ get { return m_RenderTechnique; } }

				/// <summary>
				/// Gets the wrapped primitive
				/// </summary>
				public IPrimitive			RenderingPrimitive	{ get { return m_Primitive; } }

				/// <summary>
				/// Gets the parameters to render the primitive
				/// </summary>
				public MaterialParameters	Parameters			{ get { return m_Parameters; } }

				#endregion

				#region METHODS

				/// <summary>
				/// Creates a primitive with parameters
				/// </summary>
				/// <param name="_Parent">The parent mesh for that primitive</param>
				/// <param name="_RenderTechnique">The render technique that created that primitive</param>
				/// <param name="_Primitive"></param>
				/// <param name="_Parameters"></param>
				internal	Primitive( Mesh _Parent, ITechniqueSupportsObjects _RenderTechnique, IPrimitive _Primitive, MaterialParameters _Parameters )
				{
					m_Parent = _Parent;
					if ( _RenderTechnique == null )
						throw new Exception( "The render technique cannot be null ! A primitive MUST be created through a render technique !" );

					m_RenderTechnique = _RenderTechnique;
					m_Primitive = _Primitive;
					m_Parameters = _Parameters;
				}

				public override string ToString()
				{
					return m_Primitive != null ? m_Primitive.ToString() : "<NO RENDER PRIMITIVE>";
				}

				/// <summary>
				/// Tells if the primitive can be rendered at that frame
				/// </summary>
				/// <param name="_FrameToken"></param>
				/// <returns></returns>
				public bool		CanRender( int _FrameToken )
				{
					return _FrameToken != m_FrameToken;
				}

				/// <summary>
				/// Renders the primitive
				/// </summary>
				/// <param name="_FrameToken"></param>
				public void	Render( int _FrameToken )
				{
					m_FrameToken = _FrameToken;
					m_Primitive.RenderOverride();
				}

				#region IDisposable Members

				public void Dispose()
				{
					m_RenderTechnique.RemovePrimitive( this );
				}

				#endregion

				#endregion
			}

			#endregion

			#region FIELDS

			protected BoundingBox			m_BoundingBox = new BoundingBox();
			protected BoundingSphere		m_BoundingSphere = new BoundingSphere();
			protected List<Primitive>		m_Primitives = new List<Primitive>();
			protected bool					m_bCastShadow = true;
			protected bool					m_bReceiveShadow = true;

			// Cached world BBox
			protected bool					m_bWorldBBoxDirty = true;
			protected BoundingBox			m_WorldBBox;

			#endregion

			#region PROPERTIES

			public override NODE_TYPE	NodeType	{ get { return NODE_TYPE.MESH; } }

			public override bool		Visible
			{
				get { return m_bVisible; }
				set
				{
					base.Visible = value;

					// Also forward the visible state to each of our primitives
					foreach ( Primitive P in m_Primitives )
						P.Visible = value;
				}
			}

			/// <summary>
			/// Gets or sets the "Cast Shadow" state of that mesh and all its primitives
			/// </summary>
			public bool					CastShadow
			{
				get { return m_bCastShadow; }
				set
				{
					m_bCastShadow = value;

					// Also forward the visible state to each of our primitives
					foreach ( Primitive P in m_Primitives )
						P.CastShadow = value;
				}
			}

			/// <summary>
			/// Gets or sets the "Receive Shadow" state of that mesh and all its primitives
			/// </summary>
			public bool					ReceiveShadow
			{
				get { return m_bReceiveShadow; }
				set
				{
					m_bReceiveShadow = value;

					// Also forward the visible state to each of our primitives
					foreach ( Primitive P in m_Primitives )
						P.CastShadow = value;
				}
			}

			public BoundingBox			BBox		{ get { return m_BoundingBox; } set { m_BoundingBox = value; } }
			public BoundingBox			WorldBBox
			{
				get
				{
					if ( m_bWorldBBoxDirty )
					{	// Update world BBox
						m_WorldBBox = new BoundingBox( +float.MaxValue * Vector3.One, -float.MaxValue * Vector3.One );

						foreach ( Vector3 Corner in m_BoundingBox.GetCorners() )
						{
							Vector3	WorldCorner = Vector3.TransformCoordinate( Corner, m_Local2World );
							m_WorldBBox.Minimum = Vector3.Min( m_WorldBBox.Minimum, WorldCorner );
							m_WorldBBox.Maximum = Vector3.Max( m_WorldBBox.Maximum, WorldCorner );
						}

						m_bWorldBBoxDirty = false;
					}

					return m_WorldBBox;
				}
			}

			public BoundingSphere		BSphere		{ get { return m_BoundingSphere; } set { m_BoundingSphere = value; } }
			public Primitive[]			Primitives	{ get { return m_Primitives.ToArray(); } }

			#endregion

			#region METHODS

			internal Mesh( Scene _Owner, int _ID, string _Name, Node _Parent, Matrix _Local2Parent ) : base( _Owner, _ID, _Name, _Parent, _Local2Parent )
			{
			}

			internal Mesh( Scene _Owner, Node _Parent, System.IO.BinaryReader _Reader ) : base( _Owner, _Parent, _Reader )
			{
				m_Owner.m_Meshes.Add( this );
			}

			public void		AddPrimitive( Primitive _Primitive )
			{
				if ( _Primitive == null )
					return;

				m_Primitives.Add( _Primitive );
				_Primitive.Visible = m_bVisible;
			}

			public void		RemovePrimitive( Primitive _Primitive )
			{
				if ( !m_Primitives.Contains( _Primitive ) )
					return;

				_Primitive.Dispose();
				m_Primitives.Remove( _Primitive );
			}

			public void		ClearPrimitives()
			{
				foreach ( Primitive P in m_Primitives )
					P.Dispose();
				m_Primitives.Clear();
			}

			protected override void		LoadSpecific( System.IO.BinaryReader _Reader )
			{
				// Read bounding-box
				m_BoundingBox.Minimum.X = _Reader.ReadSingle();
				m_BoundingBox.Minimum.Y = _Reader.ReadSingle();
				m_BoundingBox.Minimum.Z = _Reader.ReadSingle();
				m_BoundingBox.Maximum.X = _Reader.ReadSingle();
				m_BoundingBox.Maximum.Y = _Reader.ReadSingle();
				m_BoundingBox.Maximum.Z = _Reader.ReadSingle();

				// Read bounding-sphere
				m_BoundingSphere.Center.X = _Reader.ReadSingle();
				m_BoundingSphere.Center.Y = _Reader.ReadSingle();
				m_BoundingSphere.Center.Z = _Reader.ReadSingle();
				m_BoundingSphere.Radius = _Reader.ReadSingle();

				// Write shadow states
				m_bCastShadow = _Reader.ReadBoolean();
				m_bReceiveShadow = _Reader.ReadBoolean();

				// Read primitives
				m_Primitives.Clear();
				int	PrimitivesCount = _Reader.ReadInt32();
				for ( int PrimitiveIndex=0; PrimitiveIndex < PrimitivesCount; PrimitiveIndex++ )
				{
					string	RenderTechniqueName = _Reader.ReadString();
					ITechniqueSupportsObjects	RT = m_Owner.m_Renderer.FindRenderTechnique( RenderTechniqueName ) as ITechniqueSupportsObjects;
					if ( RT == null )
						throw new NException( m_Owner, "Failed to retrieve render technique \"" + RenderTechniqueName + "\" to create primitive !" );

					// Ask the render technique to rebuild our primitive
					m_Primitives.Add( RT.CreatePrimitive( this, _Reader ) );
				}
			}

			protected override void		SaveSpecific( System.IO.BinaryWriter _Writer )
			{
				// Write bounding-box
				_Writer.Write( m_BoundingBox.Minimum.X );
				_Writer.Write( m_BoundingBox.Minimum.Y );
				_Writer.Write( m_BoundingBox.Minimum.Z );
				_Writer.Write( m_BoundingBox.Maximum.X );
				_Writer.Write( m_BoundingBox.Maximum.Y );
				_Writer.Write( m_BoundingBox.Maximum.Z );

				// Write bounding-sphere
				_Writer.Write( m_BoundingSphere.Center.X );
				_Writer.Write( m_BoundingSphere.Center.Y );
				_Writer.Write( m_BoundingSphere.Center.Z );
				_Writer.Write( m_BoundingSphere.Radius );

				// Write shadow states
				_Writer.Write( m_bCastShadow );
				_Writer.Write( m_bReceiveShadow );

				// Write primitives
				_Writer.Write( m_Primitives.Count );
				foreach ( Primitive P in m_Primitives )
				{
					_Writer.Write( P.RenderTechnique.Name );
					P.RenderTechnique.SavePrimitive( P, _Writer );
				}
			}

			protected override void DisposeSpecific()
			{
				base.DisposeSpecific();
				ClearPrimitives();
			}

			protected override void PropagateDirtyState()
			{
				base.PropagateDirtyState();
				m_bWorldBBoxDirty = true;
			}

			#endregion
		}

		/// <summary>
		/// The camera class node hosts parameters defining a camera
		/// </summary>
		public class	Camera : Node
		{
			#region NESTED TYPES

			public enum PROJECTION_TYPE
			{
				PERSPECTIVE = 0,
				ORTHOGRAPHIC = 1,
			}

			#endregion

			#region FIELDS

			protected PROJECTION_TYPE		m_Type = PROJECTION_TYPE.PERSPECTIVE;
			protected Vector3				m_Target = Vector3.Zero;
			protected float					m_FOV = 0.0f;
			protected float					m_AspectRatio = 0.0f;
			protected float					m_ClipNear = 0.0f;
			protected float					m_ClipFar = 0.0f;
			protected float					m_Roll = 0.0f;

			protected Node					m_TargetNode = null;
			protected int					m_TempTargetNodeID = -1;	// De-serialized target node ID waiting for rebinding as reference

			protected Nuaj.Camera			m_InternalCamera = null;

			#endregion

			#region PROPERTIES

			public override NODE_TYPE	NodeType	{ get { return NODE_TYPE.CAMERA; } }

			/// <summary>
			/// Gets or sets the camera projection type
			/// </summary>
			public PROJECTION_TYPE		Type
			{
				get { return m_Type; }
				set { m_Type = value; RebuildInternalCamera(); }
			}

			/// <summary>
			/// Gets or sets the camera target
			/// </summary>
			public Vector3				Target
			{
				get { return m_Target; }
				set { m_Target = value; }
			}

			/// <summary>
			/// Gets or sets the camera FOV
			/// </summary>
			public float				FOV
			{
				get { return m_FOV; }
				set { m_FOV = value;  RebuildInternalCamera(); }
			}

			/// <summary>
			/// Gets or sets the camera aspect ratio
			/// </summary>
			public float				AspectRatio
			{
				get { return m_AspectRatio; }
				set { m_AspectRatio = value; RebuildInternalCamera(); }
			}

			/// <summary>
			/// Gets or sets the camera near clip distance
			/// </summary>
			public float				ClipNear
			{
				get { return m_ClipNear; }
				set { m_ClipNear = value; RebuildInternalCamera(); }
			}

			/// <summary>
			/// Gets or sets the camera far clip distance
			/// </summary>
			public float				ClipFar
			{
				get { return m_ClipFar; }
				set { m_ClipFar = value; RebuildInternalCamera(); }
			}

			/// <summary>
			/// Gets or sets the camera roll
			/// </summary>
			public float				Roll
			{
				get { return m_Roll; }
				set { m_Roll = value; }
			}

			/// <summary>
			/// Gets or sets the optional camera target node
			/// </summary>
			public Node					TargetNode
			{
				get { return m_TargetNode; }
				set
				{
					if ( value == m_TargetNode )
						return;

					if ( m_TargetNode != null )
						m_TargetNode.StatePropagated -= new EventHandler(TargetNode_StatePropagated);

					m_TargetNode = value;

					if ( m_TargetNode != null )
						m_TargetNode.StatePropagated += new EventHandler(TargetNode_StatePropagated);
				}
			}

			/// <summary>
			/// Gets the internal Nuaj' camera
			/// </summary>
			public Nuaj.Camera			InternalCamera
			{
				get { return m_InternalCamera; }
			}

			#endregion

			#region METHODS

			internal Camera( Scene _Owner, int _ID, string _Name, Node _Parent, Matrix _Local2Parent ) : base( _Owner, _ID, _Name, _Parent, _Local2Parent )
			{
				m_InternalCamera = new Nuaj.Camera( _Owner.m_Device, Name );
			}

			internal Camera( Scene _Owner, Node _Parent, System.IO.BinaryReader _Reader ) : base( _Owner, _Parent, _Reader )
			{
				m_InternalCamera = new Nuaj.Camera( _Owner.m_Device, Name );
				m_Owner.m_Cameras.Add( this );
			}

			protected override void DisposeSpecific()
			{
				base.DisposeSpecific();

				m_InternalCamera.Dispose();
			}

			internal override void RestoreReferences()
			{
				base.RestoreReferences();

				TargetNode = m_Owner.FindNode( m_TempTargetNodeID );
			}

			public override bool PropagateState()
			{
				if ( !base.PropagateState() )
					return	false;	// No modification

				// This should update our world transform
				TargetNode_StatePropagated( this, EventArgs.Empty );

				return true;
			}

			protected void	RebuildInternalCamera()
			{
				if ( m_Type == PROJECTION_TYPE.PERSPECTIVE )
					m_InternalCamera.CreatePerspectiveCamera( m_FOV, m_AspectRatio, m_ClipNear, m_ClipFar );
				else
					throw new Exception( "TODO!" );
//					m_InternalCamera.CreateOrthoCamera( m_OrthoHeight, m_AspectRatio, m_ClipNear, m_ClipFar );
			}

			protected override void		SaveSpecific( System.IO.BinaryWriter _Writer )
			{
				_Writer.Write( (int) m_Type );
				_Writer.Write( m_Target.X );
				_Writer.Write( m_Target.Y );
				_Writer.Write( m_Target.Z );
				_Writer.Write( m_FOV );
				_Writer.Write( m_AspectRatio );
				_Writer.Write( m_ClipNear );
				_Writer.Write( m_ClipFar );
				_Writer.Write( m_Roll );
				_Writer.Write( m_TargetNode != null ? m_TargetNode.ID : -1 );
			}

			protected override void		LoadSpecific( System.IO.BinaryReader _Reader )
			{
				m_Type = (PROJECTION_TYPE) _Reader.ReadInt32();
				m_Target.X = _Reader.ReadSingle();
				m_Target.Y = _Reader.ReadSingle();
				m_Target.Z = _Reader.ReadSingle();
				m_FOV = _Reader.ReadSingle();
				m_AspectRatio = _Reader.ReadSingle();
				m_ClipNear = _Reader.ReadSingle();
				m_ClipFar = _Reader.ReadSingle();
				m_Roll = _Reader.ReadSingle();
				m_TempTargetNodeID = _Reader.ReadInt32();

				RebuildInternalCamera();
			}

			#endregion

			#region EVENT HANDLERS

			protected void TargetNode_StatePropagated( object sender, EventArgs e )
			{
				// Rebuild a look at camera
				Vector3	Target = (Vector3) (m_TargetNode != null ? m_TargetNode.Local2World.Row4 : m_Local2World.Row4 + m_Local2World.Row3);

				Vector3	At = Target - (Vector3) m_Local2World.Row4;
				At.Normalize();

				Vector3	Right = Vector3.Cross( At, (Vector3) m_Local2World.Row2 );
				Right.Normalize();

				Vector3	Up = Vector3.Cross( Right, At );

				m_Local2World.Row1 = new Vector4( Right, 0.0f );
				m_Local2World.Row2 = new Vector4( Up, 0.0f );
				m_Local2World.Row3 = new Vector4( At, 0.0f );

				m_InternalCamera.Camera2World = m_Local2World;
			}

			#endregion
		}

		/// <summary>
		/// The light class node hosts parameters defining a light
		/// </summary>
		public class	Light : Node
		{
			#region NESTED TYPES

			public enum LIGHT_TYPE
			{
				POINT = 0,
				DIRECTIONAL = 1,
				SPOT = 2,
			}

			public enum DECAY_TYPE
			{
				LINEAR = 0,
				QUADRATIC = 1,
				CUBIC = 2,
			}

			#endregion

			#region FIELDS

			protected LIGHT_TYPE			m_Type = LIGHT_TYPE.POINT;
			protected Vector3				m_Color = Vector3.Zero;
			protected float					m_Intensity = 0.0f;
			protected bool					m_bCastShadow = false;
			protected bool					m_bEnableNearAttenuation = false;
			protected float					m_NearAttenuationStart = 0.0f;
			protected float					m_NearAttenuationEnd = 0.0f;
			protected bool					m_bEnableFarAttenuation = false;
			protected float					m_FarAttenuationStart = 0.0f;
			protected float					m_FarAttenuationEnd = 0.0f;
			protected float					m_HotSpot = 0.0f;	// (aka inner cone angle)
			protected float					m_ConeAngle = 0.0f;	// Valid for spotlights only
			protected DECAY_TYPE			m_DecayType = DECAY_TYPE.QUADRATIC;
			protected float					m_DecayStart = 0.0f;

			protected Node					m_TargetNode = null;
			protected int					m_TempTargetNodeID = -1;	// De-serialized target node ID waiting for rebinding as reference

			protected DirectionalLight		m_InternalLightDirectional = null;
			protected PointLight			m_InternalLightPoint = null;
			protected SpotLight				m_InternalLightSpot = null;

			protected Vector3				m_CachedDirection = Vector3.UnitY;

			protected static float			ms_GlobalIntensityMultiplier = 1.0f;

			#endregion

			#region PROPERTIES

			public override NODE_TYPE	NodeType	{ get { return NODE_TYPE.LIGHT; } }

			/// <summary>
			/// Gets or sets the light type
			/// </summary>
			public LIGHT_TYPE			Type
			{
				get { return m_Type; }
				set { m_Type = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets the light direction
			/// </summary>
			public Vector3				Direction
			{
				get { return m_CachedDirection; }
			}

			/// <summary>
			/// Gets or sets the light color
			/// </summary>
			public Vector3				Color
			{
				get { return m_Color; }
				set { m_Color = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets or sets the light intensity
			/// </summary>
			public float				Intensity
			{
				get { return m_Intensity; }
				set { m_Intensity = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets or sets the "Cast Shadow" state of that light
			/// </summary>
			public bool					CastShadow
			{
				get { return m_bCastShadow; }
				set { m_bCastShadow = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets or sets the "Enable Near Attenuation" state
			/// </summary>
			public bool					EnableNearAttenuation
			{
				get { return m_bEnableNearAttenuation; }
				set { m_bEnableNearAttenuation = value; }
			}

			/// <summary>
			/// Gets or sets the light near attenuation start distance
			/// </summary>
			public float				NearAttenuationStart
			{
				get { return m_NearAttenuationStart; }
				set { m_NearAttenuationStart = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets or sets the light near attenuation end distance
			/// </summary>
			public float				NearAttenuationEnd
			{
				get { return m_NearAttenuationEnd; }
				set { m_NearAttenuationEnd = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets or sets the "Enable Far Attenuation" state
			/// </summary>
			public bool					EnableFarAttenuation
			{
				get { return m_bEnableFarAttenuation; }
				set { m_bEnableFarAttenuation = value; }
			}

			/// <summary>
			/// Gets or sets the light far attenuation start distance
			/// </summary>
			public float				FarAttenuationStart
			{
				get { return m_FarAttenuationStart; }
				set { m_FarAttenuationStart = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets or sets the light far attenuation end distance
			/// </summary>
			public float				FarAttenuationEnd
			{
				get { return m_FarAttenuationEnd; }
				set { m_FarAttenuationEnd = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets or sets the light hotspot distance (i.e. inner cone angle)
			/// </summary>
			public float				HotSpot
			{
				get { return m_HotSpot; }
				set { m_HotSpot = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets or sets the light cone angle (i.e. outer cone angle)
			/// </summary>
			public float				ConeAngle
			{
				get { return m_ConeAngle; }
				set { m_ConeAngle = value; RebuildInternalLightData(); }
			}

			/// <summary>
			/// Gets or sets the light decay type
			/// </summary>
			public DECAY_TYPE			DecayType
			{
				get { return m_DecayType; }
				set { m_DecayType = value; }
			}

			/// <summary>
			/// Gets or sets the light decay start distance
			/// </summary>
			public float				DecayStart
			{
				get { return m_DecayStart; }
				set { m_DecayStart = value; }
			}

			/// <summary>
			/// Gets or sets the optional camera target node
			/// </summary>
			public Node					TargetNode
			{
				get { return m_TargetNode; }
				set
				{
					if ( value == m_TargetNode )
						return;

					if ( m_TargetNode != null )
						m_TargetNode.StatePropagated -= new EventHandler(TargetNode_StatePropagated);

					m_TargetNode = value;

					if ( m_TargetNode != null )
						m_TargetNode.StatePropagated += new EventHandler(TargetNode_StatePropagated);
				}
			}

			/// <summary>
			/// Gets the internal directional light that mirrors this light's parameters
			/// </summary>
			public DirectionalLight		InternalLightDirectional
			{
				get { return m_InternalLightDirectional; }
			}

			/// <summary>
			/// Gets the internal point light that mirrors this light's parameters
			/// </summary>
			public PointLight			InternalLightPoint
			{
				get { return m_InternalLightPoint; }
			}

			/// <summary>
			/// Gets the internal spot light that mirrors this light's parameters
			/// </summary>
			public SpotLight			InternalLightSpot
			{
				get { return m_InternalLightSpot; }
			}

			/// <summary>
			/// Gets or sets the global intensity multiplier for all lights
			/// </summary>
			public static float			GlobalIntensityMultiplier
			{
				get { return ms_GlobalIntensityMultiplier; }
				set { ms_GlobalIntensityMultiplier = value; }
			}

			#endregion

			#region METHODS

			internal Light( Scene _Owner, int _ID, string _Name, Node _Parent, Matrix _Local2Parent ) : base( _Owner, _ID, _Name, _Parent, _Local2Parent )
			{
				m_InternalLightDirectional = new DirectionalLight( m_Owner.m_Device, Name, true );
				m_InternalLightPoint = new PointLight( m_Owner.m_Device, Name );
				m_InternalLightSpot = new SpotLight( m_Owner.m_Device, Name );
			}

			internal Light( Scene _Owner, Node _Parent, System.IO.BinaryReader _Reader ) : base( _Owner, _Parent, _Reader )
			{
				m_InternalLightDirectional = new DirectionalLight( m_Owner.m_Device, Name, true );
				m_InternalLightPoint = new PointLight( m_Owner.m_Device, Name );
				m_InternalLightSpot = new SpotLight( m_Owner.m_Device, Name );

				m_Owner.m_Lights.Add( this );
			}

			public override bool PropagateState()
			{
				if ( !base.PropagateState() )
					return false;

				// This should update our direction
				TargetNode_StatePropagated( this, EventArgs.Empty );

				return true;
			}

			protected override void DisposeSpecific()
			{
				base.DisposeSpecific();

				m_InternalLightDirectional.Dispose();
				m_InternalLightPoint.Dispose();
				m_InternalLightSpot.Dispose();
			}

			internal override void RestoreReferences()
			{
				base.RestoreReferences();

				TargetNode = m_Owner.FindNode( m_TempTargetNodeID );
			}

			protected void	 RebuildInternalLightData()
			{
				Vector4	Color = ms_GlobalIntensityMultiplier * m_Intensity * new Vector4( m_Color, 1.0f );
				Vector3	Position = (Vector3) m_Local2World.Row4;

				// Use first available range
				float	RangeMin = 0.0f;
				float	RangeMax = 1e2f;
				if ( m_bEnableFarAttenuation )
				{
					RangeMin = m_FarAttenuationStart;
					RangeMax = m_FarAttenuationEnd;
				}
				else if ( m_bEnableNearAttenuation )
				{
					RangeMin = m_NearAttenuationEnd;
					RangeMax = m_NearAttenuationStart;
				}

				// Update point light
				m_InternalLightPoint.Position = Position;
				m_InternalLightPoint.Color = Color;
				m_InternalLightPoint.RangeMin = RangeMin;
				m_InternalLightPoint.RangeMax = RangeMax;

				// Update directional light
				m_InternalLightDirectional.Position = Position;
				m_InternalLightDirectional.Direction = m_CachedDirection;
				m_InternalLightDirectional.Color = Color;
				m_InternalLightDirectional.RangeMin = RangeMin;
				m_InternalLightDirectional.RangeMax = RangeMax;
				m_InternalLightDirectional.RadiusMin = m_HotSpot;
				m_InternalLightDirectional.RadiusMax = m_ConeAngle;

				// Update spot light
				m_InternalLightSpot.Position = Position;
				m_InternalLightSpot.Direction = m_CachedDirection;
				m_InternalLightSpot.Color = Color;
				m_InternalLightSpot.RangeMin = RangeMin;
				m_InternalLightSpot.RangeMax = RangeMax;
				m_InternalLightSpot.ConeAngleMin = m_HotSpot;
				m_InternalLightSpot.ConeAngleMax = m_ConeAngle;
			}

			protected override void		SaveSpecific( System.IO.BinaryWriter _Writer )
			{
				_Writer.Write( (int) m_Type );
				_Writer.Write( m_Color.X );
				_Writer.Write( m_Color.Y );
				_Writer.Write( m_Color.Z );
				_Writer.Write( m_Intensity );
				_Writer.Write( m_bCastShadow );
				_Writer.Write( m_bEnableNearAttenuation );
				_Writer.Write( m_NearAttenuationStart );
				_Writer.Write( m_NearAttenuationEnd );
				_Writer.Write( m_bEnableFarAttenuation );
				_Writer.Write( m_FarAttenuationStart );
				_Writer.Write( m_FarAttenuationEnd );
				_Writer.Write( m_HotSpot );
				_Writer.Write( m_ConeAngle );
				_Writer.Write( (int) m_DecayType );
				_Writer.Write( m_DecayStart );
				_Writer.Write( m_TargetNode != null ? m_TargetNode.ID : -1 );
			}

			protected override void		LoadSpecific( System.IO.BinaryReader _Reader )
			{
				m_Type = (LIGHT_TYPE) _Reader.ReadInt32();
				m_Color.X = _Reader.ReadSingle();
				m_Color.Y = _Reader.ReadSingle();
				m_Color.Z = _Reader.ReadSingle();
				m_Intensity = _Reader.ReadSingle();
				m_bCastShadow = _Reader.ReadBoolean();
				m_bEnableNearAttenuation = _Reader.ReadBoolean();
				m_NearAttenuationStart = _Reader.ReadSingle();
				m_NearAttenuationEnd = _Reader.ReadSingle();
				m_bEnableFarAttenuation = _Reader.ReadBoolean();
				m_FarAttenuationStart = _Reader.ReadSingle();
				m_FarAttenuationEnd = _Reader.ReadSingle();
				m_HotSpot = _Reader.ReadSingle();
				m_ConeAngle = _Reader.ReadSingle();
				m_DecayType = (DECAY_TYPE) _Reader.ReadInt32();
				m_DecayStart = _Reader.ReadSingle();
				m_TempTargetNodeID = _Reader.ReadInt32();

				RebuildInternalLightData();
			}

			#endregion

			#region EVENT HANDLERS

			protected void TargetNode_StatePropagated( object sender, EventArgs e )
			{
				if ( m_TargetNode != null )
					m_CachedDirection = (Vector3) (m_Local2World.Row4 - m_TargetNode.Local2World.Row4);
				else
					m_CachedDirection = (Vector3) m_Local2World.Row4;

				// Update internal light data
				Vector3	Position = (Vector3) m_Local2World.Row4;

				m_InternalLightPoint.Position = Position;

				m_InternalLightDirectional.Position = Position;
				m_InternalLightDirectional.Direction = m_CachedDirection;

				m_InternalLightSpot.Position = Position;
				m_InternalLightSpot.Direction = m_CachedDirection;
			}

			#endregion
		}

		/// <summary>
		/// The texture 2D class wraps a standard Nuaj texture and attaches a unique URL and ID to it so it can be serialized
		/// </summary>
		public class	Texture2D
		{
			#region FIELDS

			protected Scene					m_Owner = null;
			protected int					m_ID = -1;
			protected string				m_URL = null;
			protected string				m_OpacityURL = null;
			protected ITexture2D			m_Texture = null;

			protected System.Threading.Mutex	m_Lock = new System.Threading.Mutex();

			#endregion

			#region PROPERTIES

			public int				ID				{ get { return m_ID; } }
			public string			URL				{ get { return m_URL; } }
			public ITexture2D		Texture
			{
				get
				{
					if ( !m_Lock.WaitOne() )
						return null;

					ITexture2D	Result = m_Texture;

					m_Lock.ReleaseMutex();

					return Result;
				}
				set { m_Texture = value; }
			}

			#endregion

			#region METHODS

			internal Texture2D( Scene _Owner, int _ID, string _URL, string _OpacityURL, bool _bCreateMipMaps, ITextureProvider _TextureProvider )
			{
				m_Owner = _Owner;
				m_ID = _ID;
				m_URL = _URL != null ? _URL : "";
				m_OpacityURL = _OpacityURL != null ? _OpacityURL : "";
				m_Texture = _TextureProvider.LoadTexture( _URL, _OpacityURL, _bCreateMipMaps, new TextureUpdatedEventHandler( TextureUpdated ) );
			}

			internal Texture2D( Scene _Owner, System.IO.BinaryReader _Reader, ITextureProvider _TextureProvider )
			{
				m_Owner = _Owner;
				m_ID = _Reader.ReadInt32();
				m_URL = _Reader.ReadString();
				m_OpacityURL = _Reader.ReadString();

				bool	bHasMipMaps = _Reader.ReadBoolean();
				m_Texture = _TextureProvider.LoadTexture( m_URL, m_OpacityURL, bHasMipMaps, new TextureUpdatedEventHandler( TextureUpdated ) );
			}

			public override string ToString()
			{
				return m_URL;
			}

			internal void	Save( System.IO.BinaryWriter _Writer )
			{
				_Writer.Write( m_ID );
				_Writer.Write( m_URL );
				_Writer.Write( m_OpacityURL );
				_Writer.Write( m_Texture != null && m_Texture.HasMipMaps );
				// We don't save the texture as it should be handled by the ITextureProvider...
			}

			#endregion

			#region EVENT HANDLERS

			protected void	TextureUpdated( ITextureProvider _Caller, ITexture2D _Old, ITexture2D _New )
			{
				if ( !m_Lock.WaitOne() )
					return;

				m_Texture = _New;

				m_Lock.ReleaseMutex();
			}

			#endregion
		}

		/// <summary>
		/// The Material Parameters class is a serializable class that encapsulates all the parameters needed by a material for a particular primitive.
		/// Some examples of material parameters are the Local2World transform matrix, colors or specular intensity values associated with a primitive.
		/// 
		/// You must make the distinction between material variables that are the variables exposed by a shader to render a particular material,
		///  and the values of these variables which are contained by an instance of this MaterialParameters class.
		/// </summary>
		public class	MaterialParameters
		{
			#region NESTED TYPES

			/// <summary>
			/// A list of the supported parameter types
			/// </summary>
			public enum	PARAMETER_TYPE
			{
				BOOL,
				INT,
				FLOAT,
				FLOAT2,
				FLOAT3,
				FLOAT4,
				MATRIX4,
				TEXTURE2D,
			};

			/// <summary>
			/// Base parameter class
			/// </summary>
			public abstract class	Parameter
			{
				#region FIELDS

				protected MaterialParameters	m_Owner = null;
				protected string				m_Name = null;			// Parameter name
				protected Variable				m_Variable = null;		// Associated shader variable

				#endregion

				#region PROPERTIES

				public string				Name		{ get { return m_Name; } }
				public Variable				Variable	{ get { return m_Variable; } set { m_Variable = value; } }
				public abstract PARAMETER_TYPE	Type	{ get; }

				// Fast casts
				public ParameterBool		AsBool		{ get { return this as ParameterBool; } }
				public ParameterInt			AsInt		{ get { return this as ParameterInt; } }
				public ParameterFloat		AsFloat		{ get { return this as ParameterFloat; } }
				public ParameterFloat2		AsFloat2	{ get { return this as ParameterFloat2; } }
				public ParameterFloat3		AsFloat3	{ get { return this as ParameterFloat3; } }
				public ParameterFloat4		AsFloat4	{ get { return this as ParameterFloat4; } }
				public ParameterMatrix4		AsMatrix4	{ get { return this as ParameterMatrix4; } }
				public ParameterTexture2D	AsTexture2D	{ get { return this as ParameterTexture2D; } }

				#endregion

				#region METHODS

				public	Parameter( MaterialParameters _Owner, string _Name )
				{
					m_Owner = _Owner;
					m_Name = _Name;
				}

				public override string ToString()
				{
					return m_Name;
				}

				/// <summary>
				/// Applies the parameter to the attached effect variable (if any)
				/// </summary>
				public abstract void	Apply();
				public abstract void	Load( System.IO.BinaryReader _Reader );
				public abstract void	Save( System.IO.BinaryWriter _Writer );

				#endregion
			}

			public class	ParameterBool : Parameter
			{
				#region FIELDS

				protected bool		m_bValue = false;

				#endregion

				#region PROPERTIES

				public override PARAMETER_TYPE	Type	{ get { return PARAMETER_TYPE.BOOL; } }
				public bool			Value	{ get { return m_bValue; } set { m_bValue = value; } }

				#endregion

				#region METHODS

				public	ParameterBool( MaterialParameters _Owner, string _Name ) : base( _Owner, _Name )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " " + m_bValue;
				}

				public override void	Apply()
				{
					if ( m_Variable != null )
						m_Variable.AsScalar.Set( m_bValue );
				}

				public override void	Load( System.IO.BinaryReader _Reader )
				{
					m_bValue = _Reader.ReadBoolean();
				}

				public override void	Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( m_bValue );
				}

				public override bool	Equals( object _Other )
				{
					ParameterBool	Other = _Other as ParameterBool;
					if ( Other == null )
						throw new Exception( "Other parameter is not of type FLOAT !" );

					return Other.m_bValue == m_bValue;
				}

				public override int GetHashCode()
				{
					return base.GetHashCode();
				}

				#endregion
			}

			public class	ParameterInt : Parameter
			{
				#region FIELDS

				protected int			m_Value = 0;

				#endregion

				#region PROPERTIES

				public override PARAMETER_TYPE	Type	{ get { return PARAMETER_TYPE.INT; } }
				public int			Value	{ get { return m_Value; } set { m_Value = value; } }

				#endregion

				#region METHODS

				public	ParameterInt( MaterialParameters _Owner, string _Name ) : base( _Owner, _Name )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " " + m_Value;
				}

				public override void	Apply()
				{
					if ( m_Variable != null )
						m_Variable.AsScalar.Set( m_Value );
				}

				public override void	Load( System.IO.BinaryReader _Reader )
				{
					m_Value = _Reader.ReadInt32();
				}

				public override void	Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( m_Value );
				}

				public override bool	Equals( object _Other )
				{
					ParameterInt	Other = _Other as ParameterInt;
					if ( Other == null )
						throw new Exception( "Other parameter is not of type FLOAT !" );

					return Other.m_Value == m_Value;
				}

				public override int GetHashCode()
				{
					return base.GetHashCode();
				}

				#endregion
			}

			public class	ParameterFloat : Parameter
			{
				#region FIELDS

				protected float			m_Value = 0.0f;

				#endregion

				#region PROPERTIES

				public override PARAMETER_TYPE	Type	{ get { return PARAMETER_TYPE.FLOAT; } }
				public float			Value	{ get { return m_Value; } set { m_Value = value; } }

				#endregion

				#region METHODS

				public	ParameterFloat( MaterialParameters _Owner, string _Name ) : base( _Owner, _Name )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " " + m_Value;
				}

				public override void	Apply()
				{
					if ( m_Variable != null )
						m_Variable.AsScalar.Set( m_Value );
				}

				public override void	Load( System.IO.BinaryReader _Reader )
				{
					m_Value = _Reader.ReadSingle();
				}

				public override void	Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( m_Value );
				}

				public override bool	Equals( object _Other )
				{
					ParameterFloat	Other = _Other as ParameterFloat;
					if ( Other == null )
						throw new Exception( "Other parameter is not of type FLOAT !" );

					return Other.m_Value == m_Value;
				}

				public override int GetHashCode()
				{
					return base.GetHashCode();
				}

				#endregion
			}

			public class	ParameterFloat2 : Parameter
			{
				#region FIELDS

				protected Vector2		m_Value = Vector2.Zero;

				#endregion

				#region PROPERTIES

				public override PARAMETER_TYPE	Type	{ get { return PARAMETER_TYPE.FLOAT2; } }
				public Vector2			Value	{ get { return m_Value; } set { m_Value = value; } }

				#endregion

				#region METHODS

				public	ParameterFloat2( MaterialParameters _Owner, string _Name ) : base( _Owner, _Name )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " " + m_Value;
				}

				public override void	Apply()
				{
					if ( m_Variable != null )
						m_Variable.AsVector.Set( m_Value );
				}

				public override void	Load( System.IO.BinaryReader _Reader )
				{
					m_Value.X = _Reader.ReadSingle();
					m_Value.Y = _Reader.ReadSingle();
				}

				public override void	Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( m_Value.X );
					_Writer.Write( m_Value.Y );
				}

				public override bool	Equals( object _Other )
				{
					ParameterFloat2	Other = _Other as ParameterFloat2;
					if ( Other == null )
						throw new Exception( "Other parameter is not of type FLOAT2 !" );

					return Other.m_Value == m_Value;
				}

				public override int GetHashCode()
				{
					return base.GetHashCode();
				}

				#endregion
			}

			public class	ParameterFloat3 : Parameter
			{
				#region FIELDS

				protected Vector3		m_Value = Vector3.Zero;

				#endregion

				#region PROPERTIES

				public override PARAMETER_TYPE	Type	{ get { return PARAMETER_TYPE.FLOAT3; } }
				public Vector3			Value	{ get { return m_Value; } set { m_Value = value; } }

				#endregion

				#region METHODS

				public	ParameterFloat3( MaterialParameters _Owner, string _Name ) : base( _Owner, _Name )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " " + m_Value;
				}

				public override void	Apply()
				{
					if ( m_Variable != null )
						m_Variable.AsVector.Set( m_Value );
				}

				public override void	Load( System.IO.BinaryReader _Reader )
				{
					m_Value.X = _Reader.ReadSingle();
					m_Value.Y = _Reader.ReadSingle();
					m_Value.Z = _Reader.ReadSingle();
				}

				public override void	Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( m_Value.X );
					_Writer.Write( m_Value.Y );
					_Writer.Write( m_Value.Z );
				}

				public override bool	Equals( object _Other )
				{
					ParameterFloat3	Other = _Other as ParameterFloat3;
					if ( Other == null )
						throw new Exception( "Other parameter is not of type FLOAT3 !" );

					return Other.m_Value == m_Value;
				}

				public override int GetHashCode()
				{
					return base.GetHashCode();
				}

				#endregion
			}

			public class	ParameterFloat4 : Parameter
			{
				#region FIELDS

				protected Vector4		m_Value = Vector4.Zero;

				#endregion

				#region PROPERTIES

				public override PARAMETER_TYPE	Type	{ get { return PARAMETER_TYPE.FLOAT4; } }
				public Vector4			Value	{ get { return m_Value; } set { m_Value = value; } }

				#endregion

				#region METHODS

				public	ParameterFloat4( MaterialParameters _Owner, string _Name ) : base( _Owner, _Name )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " " + m_Value;
				}

				public override void	Apply()
				{
					if ( m_Variable != null )
						m_Variable.AsVector.Set( m_Value );
				}

				public override void	Load( System.IO.BinaryReader _Reader )
				{
					m_Value.X = _Reader.ReadSingle();
					m_Value.Y = _Reader.ReadSingle();
					m_Value.Z = _Reader.ReadSingle();
					m_Value.W = _Reader.ReadSingle();
				}

				public override void	Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( m_Value.X );
					_Writer.Write( m_Value.Y );
					_Writer.Write( m_Value.Z );
					_Writer.Write( m_Value.W );
				}

				public override bool	Equals( object _Other )
				{
					ParameterFloat4	Other = _Other as ParameterFloat4;
					if ( Other == null )
						throw new Exception( "Other parameter is not of type FLOAT4 !" );

					return Other.m_Value == m_Value;
				}
			
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}

				#endregion
			}

			public class	ParameterMatrix4 : Parameter
			{
				#region FIELDS

				protected Matrix		m_Value = Matrix.Identity;

				#endregion

				#region PROPERTIES

				public override PARAMETER_TYPE	Type	{ get { return PARAMETER_TYPE.MATRIX4; } }
				public Matrix			Value	{ get { return m_Value; } set { m_Value = value; } }

				#endregion

				#region METHODS

				public	ParameterMatrix4( MaterialParameters _Owner, string _Name ) : base( _Owner, _Name )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " " + m_Value;
				}

				public override void	Apply()
				{
					if ( m_Variable != null )
						m_Variable.AsMatrix.SetMatrix( m_Value );
				}

				public override void	Load( System.IO.BinaryReader _Reader )
				{
					m_Value.M11 = _Reader.ReadSingle();
					m_Value.M12 = _Reader.ReadSingle();
					m_Value.M13 = _Reader.ReadSingle();
					m_Value.M14 = _Reader.ReadSingle();
					m_Value.M21 = _Reader.ReadSingle();
					m_Value.M22 = _Reader.ReadSingle();
					m_Value.M23 = _Reader.ReadSingle();
					m_Value.M24 = _Reader.ReadSingle();
					m_Value.M31 = _Reader.ReadSingle();
					m_Value.M32 = _Reader.ReadSingle();
					m_Value.M33 = _Reader.ReadSingle();
					m_Value.M34 = _Reader.ReadSingle();
					m_Value.M41 = _Reader.ReadSingle();
					m_Value.M42 = _Reader.ReadSingle();
					m_Value.M43 = _Reader.ReadSingle();
					m_Value.M44 = _Reader.ReadSingle();
				}

				public override void	Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( m_Value.M11 );
					_Writer.Write( m_Value.M12 );
					_Writer.Write( m_Value.M13 );
					_Writer.Write( m_Value.M14 );
					_Writer.Write( m_Value.M21 );
					_Writer.Write( m_Value.M22 );
					_Writer.Write( m_Value.M23 );
					_Writer.Write( m_Value.M24 );
					_Writer.Write( m_Value.M31 );
					_Writer.Write( m_Value.M32 );
					_Writer.Write( m_Value.M33 );
					_Writer.Write( m_Value.M34 );
					_Writer.Write( m_Value.M41 );
					_Writer.Write( m_Value.M42 );
					_Writer.Write( m_Value.M43 );
					_Writer.Write( m_Value.M44 );
				}

				public override bool	Equals( object _Other )
				{
					ParameterMatrix4	Other = _Other as ParameterMatrix4;
					if ( Other == null )
						throw new Exception( "Other parameter is not of type MATRIX4 !" );

					return Other.m_Value == m_Value;
				}
			
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}

				#endregion
			}

			public class	ParameterTexture2D : Parameter
			{
				#region FIELDS

				protected Texture2D	m_Value = null;

				#endregion

				#region PROPERTIES

				public override PARAMETER_TYPE	Type	{ get { return PARAMETER_TYPE.TEXTURE2D; } }
				public Texture2D		Value
				{
					get { return m_Value; }
					set
					{
						if ( value == m_Value )
							return;	// No change...

						if ( m_Value != null )
							m_Owner.RemoveTextureParameter( m_Value );

						m_Value = value;

						if ( m_Value != null )
							m_Owner.AddTextureParameter( m_Value );
					}
				}

				#endregion

				#region METHODS

				public	ParameterTexture2D( MaterialParameters _Owner, string _Name ) : base( _Owner, _Name )
				{
				}

				public override string ToString()
				{
					return base.ToString() + " " + m_Value;
				}

				public override void	Apply()
				{
					if ( m_Variable != null )
					{
						if ( m_Value != null && m_Value.Texture != null )
							m_Variable.AsResource.SetResource( m_Value.Texture.TextureView );
						else
							m_Variable.AsResource.SetResource( m_Owner.m_Owner.Device.MissingTexture.TextureView );	// Assign a missing texture instead because assigning a null texture is dramatically slow...
					}
				}

				public override void	Load( System.IO.BinaryReader _Reader )
				{
					int	TextureID = _Reader.ReadInt32();
					Value = m_Owner.m_Owner.FindTexture( TextureID );
				}

				public override void	Save( System.IO.BinaryWriter _Writer )
				{
					_Writer.Write( (int) (m_Value != null ? m_Value.ID : -1) );
				}

				public override bool	Equals( object _Other )
				{
					ParameterTexture2D	Other = _Other as ParameterTexture2D;
					if ( Other == null )
						throw new Exception( "Other parameter is not of type TEXTURE2D !" );

					return Other.m_Value == m_Value;
				}

				public override int GetHashCode()
				{
					return base.GetHashCode();
				}

				#endregion
			}

			#endregion

			#region FIELDS

			protected Scene							m_Owner = null;
			protected int							m_ID = -1;
			protected string						m_Name = "";
			protected string						m_ShaderURL = "";
			protected IMaterial						m_AttachedMaterial = null;
			protected List<Parameter>				m_Parameters = new List<Parameter>();
			protected Dictionary<string,Parameter>	m_Name2Parameter = new Dictionary<string,Parameter>();

			// Cached data
			protected List<Texture2D>				m_TextureParameters = new List<Texture2D>();
			protected bool							m_bIsOpaque = true;
			protected bool							m_bHasTexturesWithAlpha = false;

			#endregion

			#region PROPERTIES

			/// <summary>
			/// Gets the ID
			/// </summary>
			public int				ID					{ get { return m_ID; } }

			/// <summary>
			/// Gets its name
			/// </summary>
			public string			Name				{ get { return m_Name; } }

			/// <summary>
			/// Gets the shader URL
			/// </summary>
			public string			ShaderURL			{ get { return m_ShaderURL; } }

			/// <summary>
			/// Gets the material these parameters are attached to
			/// </summary>
			public IMaterial		AttachedMaterial	{ get { return m_AttachedMaterial; } }

			/// <summary>
			/// Gets the list of parameters
			/// </summary>
			public Parameter[]		Parameters			{ get { return m_Parameters.ToArray(); } }

			/// <summary>
			/// Gets the list of textures from Texture Parameters
			/// </summary>
			public Texture2D[]		TexturesFromParameters	{ get { return m_TextureParameters.ToArray(); } }

			/// <summary>
			/// Get or sets this material opaque state which is combined with the HasTexturesWithAlpha porperty to know if the material is really opaque
			/// </summary>
			public bool				IsOpaque			{ get { return m_bIsOpaque; } set { m_bIsOpaque = value; } }

			/// <summary>
			/// True if any of the textures within this parameter block has alpha
			/// </summary>
			public bool				HasTexturesWithAlpha	{ get { return m_bHasTexturesWithAlpha; } }

			/// <summary>
			/// Evaluates the combination of IsOpaque and HasTexturesWithAlpha to determine if the material should be considered as opaque or transparent
			/// </summary>
			public bool				EvalOpaque			{ get { return m_bIsOpaque && !m_bHasTexturesWithAlpha; } }

			#endregion

			#region METHODS

			internal	MaterialParameters( Scene _Owner, int _ID, string _Name, string _ShaderURL )
			{
				m_Owner = _Owner;
				m_ID = _ID;
				m_Name = _Name != null ? _Name : "";
				m_ShaderURL = _ShaderURL != null ? _ShaderURL : "";
			}

			/// <summary>
			/// Loads parameters from a stream
			/// </summary>
			/// <param name="_Owner"></param>
			/// <param name="_Reader"></param>
			internal	MaterialParameters( Scene _Owner, System.IO.BinaryReader _Reader )
			{
				m_Owner = _Owner;

				m_ID = _Reader.ReadInt32();
				m_Name = _Reader.ReadString();
				m_ShaderURL = _Reader.ReadString();
				m_bIsOpaque = _Reader.ReadBoolean();

				int	ParametersCount = _Reader.ReadInt32();
				for ( int ParameterIndex=0; ParameterIndex < ParametersCount; ParameterIndex++ )
				{
					string			Name = _Reader.ReadString();
					PARAMETER_TYPE	Type = (PARAMETER_TYPE) _Reader.ReadByte();

					Parameter		NewParam = CreateParameter( Name, Type );

					// De-serialize the parameter
					NewParam.Load( _Reader );
				}
			}

			public override string ToString()
			{
				return m_Name + (EvalOpaque ? " [OPAQUE]" : " [TRANSPARENT]") + " (" + m_Parameters.Count + " Parameters)";
			}

			/// <summary>
			/// Attaches a material to this block of parameters
			/// </summary>
			/// <param name="_Material"></param>
			public void			AttachMaterial( IMaterial _Material )
			{
				if ( _Material == m_AttachedMaterial )
					return;	// No change...

				// Detach any previous material
				if ( m_AttachedMaterial != null )
				{
					m_AttachedMaterial.EffectRecompiled -= new EventHandler( AttachedMaterial_EffectRecompiled );
					foreach ( Parameter P in m_Parameters )
						P.Variable = null;
				}

				m_AttachedMaterial = _Material;

				if ( m_AttachedMaterial != null )
				{
					m_AttachedMaterial.EffectRecompiled += new EventHandler( AttachedMaterial_EffectRecompiled );
					foreach ( Parameter P in m_Parameters )
						P.Variable = m_AttachedMaterial.GetVariableByName( P.Name );
				}
			}

			/// <summary>
			/// Applies the parameters to the attached material
			/// </summary>
			public void			Apply()
			{
				if ( m_AttachedMaterial == null )
//					return;	// No material attached...
					throw new Exception( "MaterialParameters \"" + m_Name + "\" has no attached material ! Can't apply parameters !\r\n(you must call the AttachMaterial() method when creating a MaterialParameters object !)" );

				foreach ( Parameter P in m_Parameters )
					P.Apply();
			}

			/// <summary>
			/// Applies the parameters to the attached material only if they differ from previous parameters
			/// </summary>
			/// <param name="_Previous">The previously applied parameters (can be null, in which case all our parameters are applied)</param>
			public void			ApplyDifference( MaterialParameters _Previous )
			{
				if ( m_AttachedMaterial == null )
//					return;	// No material attached...
					throw new Exception( "MaterialParameters \"" + m_Name + "\" has no attached material ! Can't apply parameters !\r\n(you must call the AttachMaterial() method when creating a MaterialParameters object !)" );
				if ( _Previous == this )
					return;	// No difference...

				if ( _Previous == null )
				{	// Standard apply...
					Apply();
					return;
				}

				foreach ( Parameter P in m_Parameters )
				{
					Parameter	PreviousP = _Previous.m_Name2Parameter.ContainsKey( P.Name ) ? _Previous.m_Name2Parameter[P.Name] : null;
					if ( PreviousP == null || !P.Equals( PreviousP ) )
						P.Apply();
				}
			}

			/// <summary>
			/// Creates a new parameter
			/// </summary>
			/// <param name="_Name"></param>
			/// <param name="_Type"></param>
			/// <returns></returns>
			public Parameter	CreateParameter( string _Name, PARAMETER_TYPE _Type )
			{
				Parameter	Result = null;
				switch ( _Type )
				{
					case PARAMETER_TYPE.BOOL:
						Result = new ParameterBool( this, _Name );
						break;
					case PARAMETER_TYPE.INT:
						Result = new ParameterInt( this, _Name );
						break;
					case PARAMETER_TYPE.FLOAT:
						Result = new ParameterFloat( this, _Name );
						break;
					case PARAMETER_TYPE.FLOAT2:
						Result = new ParameterFloat2( this, _Name );
						break;
					case PARAMETER_TYPE.FLOAT3:
						Result = new ParameterFloat3( this, _Name );
						break;
					case PARAMETER_TYPE.FLOAT4:
						Result = new ParameterFloat4( this, _Name );
						break;
					case PARAMETER_TYPE.MATRIX4:
						Result = new ParameterMatrix4( this, _Name );
						break;
					case PARAMETER_TYPE.TEXTURE2D:
						Result = new ParameterTexture2D( this, _Name );
						break;

					default:
						throw new Exception( "Unsupported parameter type !" );
				}

				// Add it...
				m_Parameters.Add( Result );
				m_Name2Parameter.Add( Result.Name, Result );

				return Result;
			}

			/// <summary>
			/// Finds a parameter by name
			/// </summary>
			/// <param name="_ParameterName"></param>
			/// <returns></returns>
			public Parameter	Find( string _ParameterName )
			{
				return m_Name2Parameter.ContainsKey( _ParameterName ) ? m_Name2Parameter[_ParameterName] : null;
			}

			/// <summary>
			/// Clears all registered parameters
			/// </summary>
			public void			ClearParameters()
			{
				m_Parameters.Clear();
				m_Name2Parameter.Clear();
			}

			/// <summary>
			/// Saves parameters to a stream
			/// </summary>
			/// <param name="_Stream"></param>
			internal void		Save( System.IO.BinaryWriter _Writer )
			{
				_Writer.Write( m_ID );
				_Writer.Write( m_Name );
				_Writer.Write( m_ShaderURL );
				_Writer.Write( m_bIsOpaque );
				_Writer.Write( m_Parameters.Count );
				foreach ( Parameter Param in m_Parameters )
				{
					_Writer.Write( Param.Name );
					_Writer.Write( (byte) Param.Type );
					Param.Save( _Writer );
				}
			}

			/// <summary>
			///  Adds a texture parameter (called by one of our TextureParameter which was assigned a texture)
			/// </summary>
			/// <param name="_Texture"></param>
			protected void	AddTextureParameter( Texture2D _Texture )
			{
				if ( _Texture == null )
					return;

				m_TextureParameters.Add( _Texture );
				m_bHasTexturesWithAlpha |= _Texture.Texture != null && _Texture.Texture.HasAlpha;
			}

			/// <summary>
			///  Removes a texture parameter (called by one of our TextureParameter which was assigned a texture)
			/// </summary>
			/// <param name="_Texture"></param>
			protected void	RemoveTextureParameter( Texture2D _Texture )
			{
				if ( _Texture == null || !m_TextureParameters.Contains( _Texture ) )
					return;

				m_TextureParameters.Remove( _Texture );

				m_bHasTexturesWithAlpha = false;
				foreach ( Texture2D T in m_TextureParameters )
					m_bHasTexturesWithAlpha |= T.Texture != null && T.Texture.HasAlpha;
			}

			#endregion

			#region EVENT HANDLERS

			protected void	AttachedMaterial_EffectRecompiled( object sender, EventArgs e )
			{
				// We must re-attach all the effect variables
				foreach ( Parameter P in m_Parameters )
					P.Variable = m_AttachedMaterial.GetVariableByName( P.Name );
			}

			#endregion
		}

		#endregion

		#region FIELDS

		protected Renderer			m_Renderer = null;

		// Nodes hierarchy
		protected int				m_NodeIDCounter = 0;
		protected Node				m_Root = null;
		protected Dictionary<int,Node>	m_ID2Node = new Dictionary<int,Node>();

		// Object classes
		protected List<Mesh>		m_Meshes = new List<Mesh>();
		protected List<Light>		m_Lights = new List<Light>();
		protected List<Camera>		m_Cameras = new List<Camera>();

		// Textures
		protected int				m_TextureIDCounter = 0;
		protected List<Texture2D>	m_Textures = new List<Texture2D>();
		protected Dictionary<string,Texture2D>	m_URL2Texture = new Dictionary<string,Texture2D>();
		protected Dictionary<int,Texture2D>	m_ID2Texture = new Dictionary<int,Texture2D>();

		// Material Parameters
		protected int				m_MaterialParametersIDCounter = 0;
		protected List<MaterialParameters>	m_MaterialParameters = new List<MaterialParameters>();
		protected Dictionary<int,MaterialParameters>	m_ID2MaterialParameters = new Dictionary<int,MaterialParameters>();

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the scene's root node
		/// </summary>
		public Node				RootNode
		{
			get { return m_Root; }
		}

		/// <summary>
		/// Gets the scene's meshes collapsed as an array
		/// </summary>
		public Mesh[]			Meshes
		{
			get { return m_Meshes.ToArray(); }
		}

		/// <summary>
		/// Gets the scene's lights collapsed as an array
		/// </summary>
		public Light[]			Lights
		{
			get { return m_Lights.ToArray(); }
		}

		/// <summary>
		/// Gets the scene's cameras collapsed as an array
		/// </summary>
		public Camera[]			Cameras
		{
			get { return m_Cameras.ToArray(); }
		}

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default scene
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_Renderer">The renderer that can render the scene</param>
		public	Scene( Device _Device, string _Name, Renderer _Renderer ) : base( _Device, _Name )
		{
			if ( _Renderer == null )
				throw new NException( this, "Invalid renderer ! A scene must have a valid renderer to use..." );

			m_Renderer = _Renderer;
			m_Renderer.FrameTokenChanged += new EventHandler( Renderer_FrameTokenChanged );
		}

		/// <summary>
		/// Creates a new node for the scene
		/// </summary>
		/// <param name="_Name"></param>
		/// <param name="_Parent"></param>
		/// <param name="_Local2Parent"></param>
		/// <returns></returns>
		public Node			CreateNode( string _Name, Node _Parent, Matrix _Local2Parent )
		{
			Node	Result = new Node( this, m_NodeIDCounter++, _Name, _Parent, _Local2Parent );
			m_ID2Node[Result.ID] = Result;

			if ( _Parent == null )
			{	// New root ?
				if ( m_Root != null )
					throw new NException( this, "You're providing a root (i.e. no parent node) whereas there is already one ! Did you forget to clear the nodes ?" );
			
				m_Root = Result;	// Got ourselves a new root !
			}

			return Result;
		}

		/// <summary>
		/// Creates a new mesh for the scene
		/// </summary>
		/// <param name="_Name"></param>
		/// <param name="_Parent"></param>
		/// <param name="_Local2Parent"></param>
		/// <returns></returns>
		public Mesh			CreateMesh( string _Name, Node _Parent, Matrix _Local2Parent )
		{
			Mesh	Result = new Mesh( this, m_NodeIDCounter++, _Name, _Parent, _Local2Parent );
			m_ID2Node[Result.ID] = Result;
			m_Meshes.Add( Result );

			return Result;
		}

		/// <summary>
		/// Creates a new camera for the scene
		/// </summary>
		/// <param name="_Name"></param>
		/// <param name="_Parent"></param>
		/// <param name="_Local2Parent"></param>
		/// <returns></returns>
		public Camera		CreateCamera( string _Name, Node _Parent, Matrix _Local2Parent )
		{
			Camera	Result = new Camera( this, m_NodeIDCounter++, _Name, _Parent, _Local2Parent );
			m_ID2Node[Result.ID] = Result;
			m_Cameras.Add( Result );

			return Result;
		}

		/// <summary>
		/// Creates a new light for the scene
		/// </summary>
		/// <param name="_Name"></param>
		/// <param name="_Parent"></param>
		/// <param name="_Local2Parent"></param>
		/// <returns></returns>
		public Light		CreateLight( string _Name, Node _Parent, Matrix _Local2Parent )
		{
			Light	Result = new Light( this, m_NodeIDCounter++, _Name, _Parent, _Local2Parent );
			m_ID2Node[Result.ID] = Result;
			m_Lights.Add( Result );

			return Result;
		}

		/// <summary>
		/// Clear the hierarchy of nodes
		/// </summary>
		public void			ClearNodes()
		{
			if ( m_Root != null )
				m_Root.Dispose();
			m_Root = null;
			m_ID2Node.Clear();
			m_Meshes.Clear();
			m_Lights.Clear();
			m_Cameras.Clear();
			m_NodeIDCounter = 0;
		}

		/// <summary>
		/// Finds a node by name
		/// </summary>
		/// <param name="_Name"></param>
		/// <param name="_bCaseSensitive"></param>
		/// <returns></returns>
		public Node			FindNode( string _Name, bool _bCaseSensitive )
		{
			return m_Root != null ? FindNode( m_Root, _bCaseSensitive ? _Name : _Name.ToLower(), _bCaseSensitive ) : null;
		}

		protected Node		FindNode( Node _Node, string _Name, bool _bCaseSensitive )
		{
			if ( _bCaseSensitive ? _Node.Name == _Name : _Node.Name.ToLower() == _Name )
				return _Node;	// Found it !

			foreach ( Node Child in _Node.Children )
			{
				Node	Result = FindNode( Child, _Name, _bCaseSensitive );
				if ( Result != null )
					return Result;
			}

			return null;
		}

		/// <summary>
		/// Finds a node by ID
		/// </summary>
		/// <param name="_NodeID"></param>
		/// <returns></returns>
		public Node			FindNode( int _NodeID )
		{
			return m_Root != null ? FindNode( m_Root, _NodeID ) : null;
		}

		protected Node		FindNode( Node _Node, int _NodeID )
		{
			if ( _Node.ID == _NodeID )
				return _Node;

			foreach ( Node Child in _Node.Children )
			{
				Node	Result = FindNode( Child, _NodeID );
				if ( Result != null )
					return Result;
			}

			return null;
		}

		/// <summary>
		/// Custom node finder
		/// </summary>
		/// <param name="_Node"></param>
		/// <param name="_D"></param>
		/// <returns></returns>
		public Node		FindNode( Node _Node,  FindNodeDelegate _D )
		{
			if ( _D( _Node ) )
				return _Node;	// Found it !

			foreach ( Node Child in _Node.Children )
			{
				Node	Result = FindNode( Child, _D );
				if ( Result != null )
					return Result;
			}

			return null;
		}

		/// <summary>
		/// Finds a mesh by name
		/// </summary>
		/// <param name="_Name"></param>
		/// <param name="_bCaseSensitive"></param>
		/// <returns></returns>
		public Mesh			FindMesh( string _Name, bool _bCaseSensitive )
		{
			return m_Root != null ? FindMesh( m_Root, _bCaseSensitive ? _Name : _Name.ToLower(), _bCaseSensitive ) : null;
		}

		protected Mesh		FindMesh( Node _Node, string _Name, bool _bCaseSensitive )
		{
			if ( _Node is Mesh && (_bCaseSensitive ? _Node.Name == _Name : _Node.Name.ToLower() == _Name) )
				return _Node as Mesh;	// Found it !

			foreach ( Node Child in _Node.Children )
			{
				Mesh	Result = FindMesh( Child, _Name, _bCaseSensitive );
				if ( Result != null )
					return Result;
			}

			return null;
		}

		/// <summary>
		/// Creates a new texture for the scene
		/// </summary>
		/// <param name="_URL">The relative texture URL to identify the texture</param>
		/// <param name="_OpacityURL">The optional relative URL to identify the opacity texture</param>
		/// <param name="_bCreateMipMaps">True if the texture should be created using mip-maps</param>
		/// <param name="_TextureProvider">The texture provider capable of creating the actual texture</param>
		/// <returns></returns>
		public Texture2D	CreateTexture( string _URL, string _OpacityURL, bool _bCreateMipMaps, ITextureProvider _TextureProvider )
		{
			string	FullURL = _URL + "|" + _OpacityURL;	// The full URL is a concatenation of both URLs
			if ( m_URL2Texture.ContainsKey( FullURL ) )
				return m_URL2Texture[FullURL];	// Already registered...

			Texture2D	Result = new Texture2D( this, m_TextureIDCounter++, _URL, _OpacityURL, _bCreateMipMaps, _TextureProvider );
			m_Textures.Add( Result );
			m_URL2Texture[FullURL] = Result;
			m_ID2Texture[Result.ID] = Result;

			return Result;
		}

		/// <summary>
		/// Finds a texture by ID
		/// </summary>
		/// <param name="_ID"></param>
		/// <returns></returns>
		public Texture2D	FindTexture( int _ID )
		{
			return m_ID2Texture.ContainsKey( _ID ) ? m_ID2Texture[_ID] : null;
		}

		/// <summary>
		/// Clear the list of textures
		/// </summary>
		public void			ClearTextures()
		{
			m_Textures.Clear();
			m_URL2Texture.Clear();
			m_ID2Texture.Clear();
			m_TextureIDCounter = 0;
		}

		/// <summary>
		/// Creates a new material parameter block
		/// </summary>
		/// <param name="_Name">The name of the parameter block</param>
		/// <param name="_ShaderURL">The URL of the shader that uses theses parameters (this can be a path to an actual shader, or an identifier like Phong, Lambert, Blinn, whatever really as anyway these parameters are later read and identified by you so you can use whatever makes you comfortable)</param>
		/// <returns></returns>
		public MaterialParameters	CreateMaterialParameters( string _Name, string _ShaderURL )
		{
			MaterialParameters	Result = new MaterialParameters( this, m_MaterialParametersIDCounter++, _Name, _ShaderURL );
			m_MaterialParameters.Add( Result );
			m_ID2MaterialParameters.Add( Result.ID, Result );

			return Result;
		}
		
		/// <summary>
		/// Finds a material parameter block by ID
		/// </summary>
		/// <param name="_ID"></param>
		/// <returns></returns>
		public MaterialParameters	FindMaterialParameters( int _ID )
		{
			return m_ID2MaterialParameters.ContainsKey( _ID ) ? m_ID2MaterialParameters[_ID] : null;
		}
		
		/// <summary>
		/// Finds a material parameter block by name
		/// </summary>
		/// <param name="_Name"></param>
		/// <returns></returns>
		public MaterialParameters	FindMaterialParameters( string _Name )
		{
			foreach ( MaterialParameters Params in m_MaterialParameters )
				if ( Params.Name == _Name )
					return Params;

			return null;	// Not found
		}

		/// <summary>
		/// Clear the list of material parameters
		/// </summary>
		public void			ClearMaterialParameters()
		{
			m_MaterialParameters.Clear();
			m_ID2MaterialParameters.Clear();
			m_MaterialParametersIDCounter = 0;
		}

		/// <summary>
		/// Performs a basic node culling of meshes using the provided frustum
		/// 
		/// This is a really basic culling method that only tests the bounding spheres of every mesh against the frustum.
		///  That's lame but it's up to you to provide a better culling method, that's
		///  pretty easy to hook the meshes from the scene and build a better culling
		///  data structure like BSP, PVS, octrees or whatever...
		/// </summary>
		/// <param name="_World2Frustum">A matrix to bring world space coordinates into frustum space</param>
		/// <param name="_Frustum">The frustum to use for mesh culling</param>
		public void			PerformCulling( Matrix _World2Frustum, Frustum _Frustum )
		{
			if ( m_Root != null )
				PerformCulling( m_Root, _World2Frustum, _Frustum );
		}

		protected void		PerformCulling( Node _Node, Matrix _World2Frustum, Frustum _Frustum )
		{
			Mesh	M = _Node as Mesh;
			if ( M != null )
			{
				if ( !M.Visible )
					M.Culled = true;	// Easy !
				else
				{	// Transform mesh bounding-sphere into frustum space
					Matrix	Local2Frustum = M.Local2World * _World2Frustum;

					Vector3	CenterMesh = M.BSphere.Center;
					Vector3	RadiusMesh = new Vector3( M.BSphere.Radius, M.BSphere.Radius, M.BSphere.Radius );

					Vector3	Center;
					Vector3.TransformCoordinate( ref CenterMesh, ref Local2Frustum, out Center );

					float	fMaxScale = Math.Max( Local2Frustum.Row1.Length(), Local2Frustum.Row2.Length() );
							fMaxScale = Math.Max( fMaxScale, Local2Frustum.Row2.Length() );
					float	fRadius = M.BSphere.Radius * fMaxScale;

					// Cull...
					M.Culled = !_Frustum.IsInsideInclusive( Center, fRadius );
				}
			}

			// Recurse through children
			foreach ( Node Child in _Node.Children )
				PerformCulling( Child, _World2Frustum, _Frustum );
		}

		/// <summary>
		/// Creates a scene from a stream
		/// </summary>
		/// <param name="_Reader"></param>
		/// <param name="_TextureProvider"></param>
		/// <returns></returns>
		public void		Load( System.IO.BinaryReader _Reader, ITextureProvider _TextureProvider )
		{
			// Read back textures
			ClearTextures();
			int	TexturesCount = _Reader.ReadInt32();
			for ( int TextureIndex=0; TextureIndex < TexturesCount; TextureIndex++ )
			{
				Texture2D	T = new Texture2D( this, _Reader, _TextureProvider );
				m_Textures.Add( T );
				m_URL2Texture.Add( T.URL, T );
				m_ID2Texture.Add( T.ID, T );
			}
			m_TextureIDCounter = m_Textures.Count;

			// Read back material parameters
			ClearMaterialParameters();
			int	MaterialParametersCount = _Reader.ReadInt32();
			for ( int MaterialParameterIndex=0; MaterialParameterIndex < MaterialParametersCount; MaterialParameterIndex++ )
			{
				MaterialParameters	MP = new MaterialParameters( this, _Reader );
				m_MaterialParameters.Add( MP );
				m_ID2MaterialParameters.Add( MP.ID, MP );
			}
			m_MaterialParametersIDCounter = m_MaterialParameters.Count;

			// Read back the nodes hierarchy
			ClearNodes();
			bool	bHasRoot = _Reader.ReadBoolean();
			if ( !bHasRoot )
				return;

			// Read back root type
			NODE_TYPE	Type = (NODE_TYPE) _Reader.ReadByte();
			switch ( Type )
			{
				case NODE_TYPE.NODE:
					m_Root = new Node( this, null, _Reader );
					break;

				case NODE_TYPE.MESH:
					m_Root = new Mesh( this, null, _Reader );
					break;

				case NODE_TYPE.LIGHT:
					m_Root = new Light( this, null, _Reader );
					break;

				case NODE_TYPE.CAMERA:
					m_Root = new Camera( this, null, _Reader );
					break;
			}

			// Propagate state once so matrices are up to date
			m_Root.PropagateState();
		}

		/// <summary>
		/// Writes a scene to a stream
		/// </summary>
		/// <param name="_Stream"></param>
		public void		Save( System.IO.BinaryWriter _Writer )
		{
			// Write textures
			_Writer.Write( m_Textures.Count );
			foreach ( Texture2D T in m_Textures )
				T.Save( _Writer );

			// Write material parameters
			_Writer.Write( m_MaterialParameters.Count );
			foreach ( MaterialParameters MatParams in m_MaterialParameters )
				MatParams.Save( _Writer );

			// Recursively save nodes
			if ( m_Root == null )
				_Writer.Write( false );	// No root...
			else
			{
				_Writer.Write( true );
				m_Root.Save( _Writer );
			}
		}

		#region Motion Blur Helpers

		/// <summary>
		/// Helper method that helps to compute the difference in translation and rotation of an object moving from one frame to the other
		/// This is essentially used for motion blur
		/// </summary>
		/// <param name="_Previous">The object's matrix at previous frame</param>
		/// <param name="_Current">The object's matrix at current frame</param>
		/// <param name="_DeltaPosition">Returns the difference in position from last frame</param>
		/// <param name="_DeltaRotation">Returns the difference in rotation from last frame</param>
		/// <param name="_Pivot">Returns the pivot position the object rotated about</param>
		public static void	ComputeObjectDeltaPositionRotation( ref Matrix _Previous, ref Matrix _Current, out Vector3 _DeltaPosition, out Quaternion _DeltaRotation, out Vector3 _Pivot )
		{
			// Compute the rotation the matrix sustained
			Quaternion	PreviousRotation = QuatFromMatrix( _Previous );
			Quaternion	CurrentRotation = QuatFromMatrix( _Current );
			_DeltaRotation = QuatMultiply( QuatInvert( PreviousRotation ), CurrentRotation );

			Vector3	PreviousPosition = (Vector3) _Previous.Row4;
			Vector3	CurrentPosition = (Vector3) _Current.Row4;

			// Retrieve the pivot point about which that rotation occurred
			_Pivot = CurrentPosition;

			float	RotationAngle = _DeltaRotation.Angle;
			if ( Math.Abs( RotationAngle ) > 1e-4f )
			{
				Vector3	RotationAxis = _DeltaRotation.Axis;
				Vector3	Previous2Current = CurrentPosition - PreviousPosition;
				float	L = Previous2Current.Length();
				if ( L > 1e-4f )
				{
					Previous2Current /= L;
					Vector3	N = Vector3.Cross( Previous2Current, RotationAxis );
					N.Normalize();

					Vector3	MiddlePoint = 0.5f * (PreviousPosition + CurrentPosition);
					float	Distance2Pivot = 0.5f * L / (float) Math.Tan( 0.5f * RotationAngle );
					_Pivot = MiddlePoint + N * Distance2Pivot;
				}

				// Rotate previous position about pivot, this should yield us current position
				Vector3	RotatedPreviousPosition = RotateAbout( PreviousPosition, _Pivot, _DeltaRotation );

//				// Update previous position so the remaining position gap is filled by delta translation
//				PreviousPosition = RotatedPreviousPosition;
				PreviousPosition = CurrentPosition;	// Close the gap so we have no delta translation
			}

			_DeltaPosition = CurrentPosition - PreviousPosition;	// Easy !
		}

		static Quaternion	QuatFromMatrix( Matrix M )
		{
			Quaternion	q = new Quaternion();

			float	s = (float) System.Math.Sqrt( M.M11 + M.M22 + M.M33 + 1.0f );
			q.W = s * 0.5f;
			s = 0.5f / s;
			q.X = (M.M32 - M.M23) * s;
			q.Y = (M.M13 - M.M31) * s;
			q.Z = (M.M21 - M.M12) * s;

			return	q;
		}

		static Quaternion	QuatInvert( Quaternion q )
		{
			float	fNorm = q.LengthSquared();
			if ( fNorm < float.Epsilon )
				return q;

			float	fINorm = -1.0f / fNorm;
			q.X *=  fINorm;
			q.Y *=  fINorm;
			q.Z *=  fINorm;
			q.W *= -fINorm;

			return q;
		}

		static Vector3	RotateAbout( Vector3 _Point, Vector3 _Pivot, Quaternion _Rotation )
		{
			Quaternion	Q = new Quaternion( _Point - _Pivot, 0.0f );
			Quaternion	RotConjugate = _Rotation;
			RotConjugate.Conjugate();
			Quaternion	Pr = QuatMultiply( QuatMultiply( _Rotation, Q ), RotConjugate );
			Vector3		Protated = new Vector3( Pr.X, Pr.Y, Pr.Z );
			return _Pivot + Protated;
		}

		static Quaternion	QuatMultiply( Quaternion q0, Quaternion q1 )
		{
			Quaternion	q;
			Vector3	V0 = new Vector3( q0.X, q0.Y, q0.Z );
			Vector3	V1 = new Vector3( q1.X, q1.Y, q1.Z );
			q.W = q0.W * q1.W - Vector3.Dot( V0, V1 );
			Vector3	V = q0.W * V1 + V0 * q1.W + Vector3.Cross( V0, V1 );
			q.X = V.X;
			q.Y = V.Y;
			q.Z = V.Z;

			return q;
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		protected void	Renderer_FrameTokenChanged( object sender, EventArgs e )
		{
			// Re-evaluate node visibility and Local => World state
			if ( m_Root != null )
				m_Root.PropagateState();
		}

		#endregion
	}
}
