using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// The renderer is a singleton (or at least it should be) and contains a collection of pipelines.
	/// </summary>
	public class Renderer : Component
	{
		#region FIELDS

		// Our list of render pipelines that we should call in turn
		protected List<Pipeline>	m_Pipelines = new List<Pipeline>();
		protected Pipeline			m_CurrentPipeline = null;	// The currently executing pipeline

		// Vertex signatures mapping
		protected Dictionary<string,RenderTechnique>	m_Name2Technique = new Dictionary<string,RenderTechnique>();
		protected List<IVertexSignature>				m_VertexSignatures = new List<IVertexSignature>();
		protected Dictionary<IVertexSignature,RenderTechnique>	m_VertexSignature2Technique = new Dictionary<IVertexSignature,RenderTechnique>();

		// Rendering data
		protected int				m_FrameToken = 0;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the pipelines registered to the renderer
		/// </summary>
		public Pipeline[]		Pipelines		{ get { return m_Pipelines.ToArray(); } }

		/// <summary>
		/// Gets the currently executing pipeline
		/// </summary>
		public Pipeline			CurrentPipeline	{ get { return m_CurrentPipeline; } }

		/// <summary>
		/// Gets the current frame token
		/// </summary>
		public int				FrameToken		{ get { return m_FrameToken; } }

		/// <summary>
		/// Subscribe to this event to be notified the frame token changed
		/// </summary>
		public event EventHandler	FrameTokenChanged;

		// Primitive events forwarded from techniques
		public event PrimitiveCollectionChangedEventHandler	PrimitiveAdded;
		public event PrimitiveCollectionChangedEventHandler	PrimitiveRemoved;

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default renderer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	Renderer( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		public override void Dispose()
		{
			// Remove pipelines
			while ( m_Pipelines.Count > 0 )
				RemovePipeline( m_Pipelines[0] );

			base.Dispose();
		}

		/// <summary>
		/// Renders all of the pipelines in order
		/// </summary>
		public void	Render()
		{
			if ( m_Device.IsWindowOccluded )
			{	// Don't render if occluded...
				System.Threading.Thread.Sleep( 50 );
				return;
			}

			// Increase the frame token
			m_FrameToken++;

			// Notify
			if ( FrameTokenChanged != null )
				FrameTokenChanged( this, EventArgs.Empty );

			// Render all pipelines in turn
			foreach ( Pipeline P in m_Pipelines )
			{
				m_CurrentPipeline = P;
				P.Render( m_FrameToken );
			}

			m_CurrentPipeline = null;
		}

		/// <summary>
		/// Attempts to retrieve the render technique that is the best able to create primitives for the provided vertex signature
		/// </summary>
		/// <param name="_Signature"></param>
		/// <returns></returns>
		public RenderTechnique	GetSupportForPrimitiveCreation( IVertexSignature _Signature )
		{
			if ( _Signature == null )
				throw new NException( this, "Invalid signature !" );

			// Retrieve the registered signature that matches best the provided signature
			foreach ( IVertexSignature RegisteredSignature in m_VertexSignatures )
				if ( RegisteredSignature.CheckMatch( _Signature ) )
					return m_VertexSignature2Technique[RegisteredSignature];

			return null;	// Not found :(
		}

		/// <summary>
		/// Finds a render technique by name
		/// </summary>
		/// <param name="_RenderTechniqueName"></param>
		/// <returns></returns>
		public RenderTechnique		FindRenderTechnique( string _RenderTechniqueName )
		{
			return m_Name2Technique.ContainsKey( _RenderTechniqueName ) ? m_Name2Technique[_RenderTechniqueName] : null;
		}

		#region Pipelines Registration

		/// <summary>
		/// Appends a pipeline to the end of the list
		/// </summary>
		/// <param name="_Pipeline"></param>
		public void	AddPipeline( Pipeline _Pipeline )
		{
			if ( _Pipeline == null )
				throw new NException( this, "Invalid pipeline !" );

			m_Pipelines.Add( _Pipeline );

			// Subscribe to events
			_Pipeline.RenderTechniqueAdded += new Pipeline.RenderTechniquesEventHandler( Pipeline_RenderTechniqueAdded );
			_Pipeline.RenderTechniqueRemoved += new Pipeline.RenderTechniquesEventHandler( Pipeline_RenderTechniqueRemoved );
		}

		/// <summary>
		/// Inserts a pipeline at the specified index in the renderer
		/// </summary>
		/// <param name="_Index"></param>
		/// <param name="_Pipeline"></param>
		public void	InsertPipeline( int _Index, Pipeline _Pipeline )
		{
			if ( _Pipeline == null )
				throw new NException( this, "Invalid pipeline !" );

			m_Pipelines.Insert( _Index, _Pipeline );

			// Subscribe to events
			_Pipeline.RenderTechniqueAdded += new Pipeline.RenderTechniquesEventHandler( Pipeline_RenderTechniqueAdded );
			_Pipeline.RenderTechniqueRemoved += new Pipeline.RenderTechniquesEventHandler( Pipeline_RenderTechniqueRemoved );
		}

		/// <summary>
		/// Removes a pipeline from the renderer
		/// </summary>
		/// <param name="_Pipeline"></param>
		public void	RemovePipeline( Pipeline _Pipeline )
		{
			if ( _Pipeline == null )
				throw new NException( this, "Invalid pipeline !" );

			// Un-Subscribe from events
			_Pipeline.RenderTechniqueAdded -= new Pipeline.RenderTechniquesEventHandler( Pipeline_RenderTechniqueAdded );
			_Pipeline.RenderTechniqueRemoved -= new Pipeline.RenderTechniquesEventHandler( Pipeline_RenderTechniqueRemoved );

			m_Pipelines.Remove( _Pipeline );
		}

		/// <summary>
		/// Finds a pipeline by name
		/// </summary>
		/// <param name="_Name"></param>
		/// <returns></returns>
		public Pipeline		FindPipeline( string _Name )
		{
			foreach ( Pipeline P in m_Pipelines )
				if ( P.Name == _Name )
					return P;

			return null;
		}

		/// <summary>
		/// Finds a pipeline by type
		/// </summary>
		/// <param name="_Type"></param>
		/// <returns></returns>
		public Pipeline		FindPipeline( Pipeline.TYPE _Type )
		{
			foreach ( Pipeline P in m_Pipelines )
				if ( P.Type == _Type )
					return P;

			return null;
		}

		/// <summary>
		/// Gets the index of a given pipeline
		/// </summary>
		/// <param name="_Pipeline"></param>
		/// <returns></returns>
		public int			IndexOfPipeline( Pipeline _Pipeline )
		{
			return m_Pipelines.IndexOf( _Pipeline );
		}

		#endregion

		#endregion

		#region EVENT HANDLERS

		void Pipeline_RenderTechniqueAdded( Pipeline _Sender, RenderTechnique _Technique )
		{
			if ( m_Name2Technique.ContainsKey( _Technique.Name ) )
				throw new NException( this, "There already is a technique named \"" + _Technique.Name + "\" !" );
			m_Name2Technique[_Technique.Name] = _Technique;

			ITechniqueSupportsObjects	SupportsObjects = _Technique as ITechniqueSupportsObjects;
			if ( SupportsObjects == null )
				return;

			// Register vertex signatures mapping
			m_VertexSignatures.Add( SupportsObjects.RecognizedSignature );
			m_VertexSignature2Technique.Add( SupportsObjects.RecognizedSignature, _Technique );

			// Register for primitive events
			SupportsObjects.PrimitiveAdded += new PrimitiveCollectionChangedEventHandler( Technique_PrimitiveAdded );
			SupportsObjects.PrimitiveRemoved += new PrimitiveCollectionChangedEventHandler( Technique_PrimitiveRemoved );
		}

		void Pipeline_RenderTechniqueRemoved( Pipeline _Sender, RenderTechnique _Technique )
		{
			m_Name2Technique.Remove( _Technique.Name );

			ITechniqueSupportsObjects	SupportsObjects = _Technique as ITechniqueSupportsObjects;
			if ( SupportsObjects == null )
				return;

			// Unregister from primitive events
			SupportsObjects.PrimitiveAdded -= new PrimitiveCollectionChangedEventHandler( Technique_PrimitiveAdded );
			SupportsObjects.PrimitiveRemoved -= new PrimitiveCollectionChangedEventHandler( Technique_PrimitiveRemoved );

			// Unregister vertex signatures
			m_VertexSignatures.Remove( SupportsObjects.RecognizedSignature );
			m_VertexSignature2Technique.Remove( SupportsObjects.RecognizedSignature );
		}

		void Technique_PrimitiveAdded( ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive )
		{
			// Forward the event
			if ( PrimitiveAdded != null )
				PrimitiveAdded( _Sender, _Primitive );
		}

		void Technique_PrimitiveRemoved( ITechniqueSupportsObjects _Sender, Scene.Mesh.Primitive _Primitive )
		{
			// Forward the event
			if ( PrimitiveRemoved != null )
				PrimitiveRemoved( _Sender, _Primitive );
		}

		#endregion
	}
}
