using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// The MaterialMap allows to build a map of materials and render techniques
	/// Typically, there are 2 kinds of "material structures" existing in Nuaj :
	///		1) Actual Nuaj.Material classes that wrap a shader that takes a bunch of parameters as input
	///		2) Scene.MaterialParameters that represent a block of parameters for a shader
	///	
	/// When loading a scene from an external format, like with the FBXSceneLoader for example, we need to
	///  map the FBX scene materials (typically, the name of the shader to use + a bunch of parameters for it)
	///  to actual runtime materials in Cirrus.
	/// 
	/// Runtime materials in Cirrus are hosted and supported by RenderTechniques.
	/// 
	/// So a MaterialMap is only a set of delegates that are interrogated in turn where each one is asked if it
	///  recognizes a given shader, and if so should return the RenderTechnique that is able to render the shader.
	/// 
	/// =================================================================================
	/// 
	/// If you look at the code, the delegates should not only return a RenderTechnique but also a technique that
	///  is able to support external objects : like creating and saving primitives that will be rendered with the technique.
	/// This is why the delegates should return a technique that implements the ITechniqueSupportsObjects interface.
	/// 
	/// To see an example of such a technique, refer to the <see cref="Nuaj.Cirrus.RenderTechniqueDefault"/>Nuaj.Cirrus.RenderTechniqueDefault</see> class.
	/// </summary>
	public class	MaterialMap
	{
		#region NESTED TYPES

		/// <summary>
		/// Use this delegate to register a new material mapper
		/// </summary>
		/// <param name="_MaterialParameters">The material parameters associated to the material</param>
		/// <returns>The render technique that is capable of handling that material and create pritimives for it, or null if not supported</returns>
		public delegate ITechniqueSupportsObjects	MaterialMapperDelegate( Cirrus.Scene.MaterialParameters _MaterialParameters );

		#endregion

		#region FIELDS

		public List<MaterialMapperDelegate>	m_Mappers = new List<MaterialMapperDelegate>();
		public MaterialMapperDelegate		m_DefaultMapper = null;

		#endregion

		#region METHODS

		public	MaterialMap()
		{
		}

		/// <summary>
		/// Registers the default mapper that will be called if all other mappers fail
		/// </summary>
		/// <param name="_DefaultMapper"></param>
		public void	RegisterDefaultMapper( MaterialMapperDelegate _DefaultMapper )
		{
			m_DefaultMapper = _DefaultMapper;
		}

		/// <summary>
		/// Registers a new mapper
		/// </summary>
		/// <param name="_Mapper"></param>
		public void	RegisterMapper( MaterialMapperDelegate _Mapper )
		{
			if ( _Mapper != null )
				m_Mappers.Add( _Mapper );
		}

		/// <summary>
		/// Clears the list of registered mappers
		/// </summary>
		public void	ClearMappers()
		{
			m_Mappers.Clear();
		}

		/// <summary>
		/// Attempts to map a weakly identified material into a strongly typed Cirrus render technique that is capable of handling the material and create primitives for it
		/// </summary>
		/// <param name="_ShaderParameterNames"></param>
		/// <returns></returns>
		public Cirrus.ITechniqueSupportsObjects	MapToTechnique( Cirrus.Scene.MaterialParameters _MaterialParameters )
		{
			if ( m_Mappers.Count == 0 && m_DefaultMapper == null )
				throw new Exception( "You must register at least one material mapper delegate before using the material map !" );

			foreach ( MaterialMapperDelegate Mapper in m_Mappers )
			{
				Cirrus.ITechniqueSupportsObjects	MappedTechnique = Mapper( _MaterialParameters );
				if ( MappedTechnique != null )
					return MappedTechnique;
			}

			// Resort to the default mapper
			return m_DefaultMapper != null ? m_DefaultMapper( _MaterialParameters ) : null;
		}

		#endregion
	}
}
