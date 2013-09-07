using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// A pipeline is an ordered collection of render techniques
	/// </summary>
	public class Pipeline : Component
	{
		#region NESTED TYPES

		/// <summary>
		/// Standard pipeline types
		/// </summary>
		public enum		TYPE
		{
			UNKNOWN,
			DEPTH_PASS,			// This pipeline should render the objetcts into the depth stencil so we obtain a preview of the ZBuffer
			GEOMETRY,			// This pipeline should render geometry (i.e. position and/or normals) so we obtain a preview of the geometry buffers
			SHADOW_MAPPING,		// This pipeline should render shadow maps and make them available to subsequent passes
			EMISSIVE_UNLIT,		// This pipeline should render emissive or unlit materials in a special buffer
			MAIN_RENDERING,		// This is the main pipeline where standard objects should be rendered
			DEFERRED_LIGHTING,	// This pipeline should render lighting in a special light buffer
			POST_PROCESSING		// This pipeline should render the post-process effects that will combine all previous passes
		}

		public delegate void	RenderTechniquesEventHandler( Pipeline _Sender, RenderTechnique _Technique );
		public delegate void	PipelineRenderingEventHandler( Pipeline _Sender );

		#endregion

		#region FIELDS

		// The current pipeline type
		protected TYPE					m_Type = TYPE.UNKNOWN;

		// Our list of render techniques that we should call in turn
		protected List<RenderTechnique>	m_RenderTechniques = new List<RenderTechnique>();

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the pipeline type
		/// </summary>
		public TYPE					Type				{ get { return m_Type; } }

		/// <summary>
		/// Gets the render techniques registered to this pipeline
		/// </summary>
		public RenderTechnique[]	RenderTechniques	{ get { return m_RenderTechniques.ToArray(); } }

		public event RenderTechniquesEventHandler	RenderTechniqueAdded;
		public event RenderTechniquesEventHandler	RenderTechniqueRemoved;
		public event PipelineRenderingEventHandler	RenderingStart;
		public event PipelineRenderingEventHandler	RenderingEnd;

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default pipeline of given type
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	Pipeline( Device _Device, string _Name, TYPE _Type ) : base( _Device, _Name )
		{
			m_Type = _Type;
		}

		/// <summary>
		/// Renders the pipeline
		/// </summary>
		/// <param name="_FrameToken">A frame token that should be increased by one each new frame</param>
		public void	Render( int _FrameToken )
		{
			// Notify of rendering start
			if ( RenderingStart != null )
				RenderingStart( this );

			// Render all techniques in turn
			foreach ( RenderTechnique RT in m_RenderTechniques )
				RT.Render( _FrameToken );

			// Notify of rendering end
			if ( RenderingEnd != null )
				RenderingEnd( this );
		}

		/// <summary>
		/// Appends a technique to the end of the pipeline
		/// </summary>
		/// <param name="_Technique"></param>
		public void	AddTechnique( RenderTechnique _Technique )
		{
			if ( _Technique == null )
				throw new NException( this, "Invalid technique !" );

			m_RenderTechniques.Add( _Technique );

			// Notify
			if ( RenderTechniqueAdded != null )
				RenderTechniqueAdded( this, _Technique );
		}

		/// <summary>
		/// Inserts a technique at the specified index in the pipeline
		/// </summary>
		/// <param name="_Index"></param>
		/// <param name="_Technique"></param>
		public void	InsertTechnique( int _Index, RenderTechnique _Technique )
		{
			if ( _Technique == null )
				throw new NException( this, "Invalid technique !" );

			m_RenderTechniques.Insert( _Index, _Technique );

			// Notify
			if ( RenderTechniqueAdded != null )
				RenderTechniqueAdded( this, _Technique );
		}

		/// <summary>
		/// Removes a technique from the pipeline
		/// </summary>
		/// <param name="_Technique"></param>
		public void	RemoveTechnique( RenderTechnique _Technique )
		{
			if ( _Technique == null )
				throw new NException( this, "Invalid technique !" );

			m_RenderTechniques.Remove( _Technique );

			// Notify
			if ( RenderTechniqueRemoved != null )
				RenderTechniqueRemoved( this, _Technique );
		}

		/// <summary>
		/// Finds a render technique by name
		/// </summary>
		/// <param name="_Name"></param>
		/// <returns></returns>
		public RenderTechnique	FindTechnique( string _Name )
		{
			foreach ( RenderTechnique RT in m_RenderTechniques )
				if ( RT.Name == _Name )
					return RT;

			return null;
		}

		/// <summary>
		/// Gets the index of a given render technique
		/// </summary>
		/// <param name="_Technique"></param>
		/// <returns></returns>
		public int			IndexOfTechnique( RenderTechnique _Technique )
		{
			return m_RenderTechniques.IndexOf( _Technique );
		}

		#endregion
	}
}
