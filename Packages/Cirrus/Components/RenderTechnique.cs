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
	/// A render technique is the main render unit in Cirrus.
	/// Every primitive can be rendered on its own but it's always better to render multiple primitives using a single technique.
	/// 
	/// A render technique is usually tightly tied to one or several materials that it has full knowledge of and
	/// deals with primitives whose vertex format is also well known as it's fit to be rendered with the technique.
	/// </summary>
	/// <remarks>A render technique must have a unique name</remarks>
	/// <example>
	/// An example of a simple technique is Phong mapping where you don't want to setup redundant informations for Phong lighting
	///  to every object and render them one by one. Instead, you create a "Phong" technique and render a bunch of objects with it,
	///  the technique will then know best how to organize the rendering and reduce the amount of draw calls and state changes for
	///  the rendering to be the fastest.
	/// </example>
	[System.ComponentModel.TypeConverter(typeof(RenderTechniqueTypeConverter))]
	public abstract class RenderTechnique : Component
	{
		#region FIELDS

		// The frame token that gets incremented every frame so we know if we're called several times on the same frame
		protected int					m_FrameToken = -1;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the currently used material (for internal usage only)
		/// This value is set as soon as you enter a "using ( Material.UseLock() ) {}" loop and is reset when the lock on the material is disposed of
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		protected IMaterial					CurrentMaterial			{ get { return m_Device.CurrentMaterial; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default technique
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	RenderTechnique( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		/// <summary>
		/// Renders the technique
		/// </summary>
		/// <param name="_FrameToken">A frame token that should be increased by one each new frame</param>
		public abstract void		Render( int _FrameToken );

		#endregion
	}

	// The type converter for the property grid
	public class RenderTechniqueTypeConverter : System.ComponentModel.TypeConverter
	{
		// Sub-properties
		public override bool GetPropertiesSupported( System.ComponentModel.ITypeDescriptorContext _Context )
		{
			return	true;
		}

		public override System.ComponentModel.PropertyDescriptorCollection	GetProperties( System.ComponentModel.ITypeDescriptorContext _Context, object _Value, System.Attribute[] _Attributes )
		{
			return	System.ComponentModel.TypeDescriptor.GetProperties( _Value.GetType(), new System.Attribute[] { new System.ComponentModel.BrowsableAttribute( true ) } );
		}
	}
}
