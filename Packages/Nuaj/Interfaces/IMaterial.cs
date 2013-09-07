using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj
{
	/// <summary>
	/// Use this delegate to render a single pass of an object
	/// </summary>
	/// <param name="_Sender">The caller material</param>
	/// <param name="_Pass">The current pass being rendered</param>
	/// <param name="_PassIndex">The index of that pass</param>
	public delegate void	RenderDelegate( IMaterial _Sender, EffectPass _Pass, int _PassIndex );

	/// <summary>
	/// Interface to material objects
	/// </summary>
	[System.ComponentModel.TypeConverter(typeof(MaterialTypeConverter))]
	public interface IMaterial : IComponent
	{
		/// <summary>
		/// Gets the vertex layout associated to that material
		/// </summary>
		InputLayout			VertexLayout		{ get; }

		/// <summary>
		/// Gets the list of variables for this material
		/// </summary>
		Variable[]			Variables			{ get; }

		/// <summary>
		/// Gets the shader interfaces implemented by that material
		/// </summary>
		IShaderInterface[]	ShaderInterfaces	{ get; }

		/// <summary>
		/// Gets the amount of techniques available in that material
		/// </summary>
		int					TechniqueCount		{ get; }

		/// <summary>
		/// Gets or sets the technique to use to render that material
		/// </summary>
		EffectTechnique		CurrentTechnique	{ get; set; }

		/// <summary>
		/// Tells if the material has errors (meaning it shouldn't be used)
		/// </summary>
		bool				HasErrors			{ get; }

		/// <summary>
		/// Gets the errors from that material
		/// </summary>
		string				CompilationErrors	{ get; }

		/// <summary>
		/// Notifies the effect has recompiled
		/// </summary>
		event EventHandler	EffectRecompiled;

		/// <summary>
		/// Uses that material for rendering (i.e. setting its vertex layout and queries the shader interface providers)
		/// </summary>
		/// <returns>A disposable lock on the material</returns>
		/// <remarks>USE WITH CAUTION ! YOU MUST DISPOSE OF THE LOCK WHEN YOU'RE FINISHED WITH THE MATERIAL !</remarks>
		/// <example> Prefer calling this method in a "using" block like this :
		/// using ( MyMaterial.UseLock() )
		/// {
		///		(do something with the material...)
		/// }
		/// </example>
		IDisposable			UseLock();

		/// <summary>
		/// Applies the currently assigned material parameters to run the specified pass
		/// </summary>
		/// <param name="_PassIndex"></param>
		void				ApplyPass( int _PassIndex );

		/// <summary>
		/// Renders the material using the current technique
		/// </summary>
		/// <param name="_Delegate"></param>
		void				Render( RenderDelegate _Delegate );

		/// DirectX Effect Wrapping
		EffectConstantBuffer	GetConstantBufferByIndex( int _Index );
		EffectConstantBuffer	GetConstantBufferByName( string _Name );
		EffectTechnique			GetTechniqueByIndex( int _Index );
		EffectTechnique			GetTechniqueByName( string _Name );
		Variable				GetVariableByIndex( int _Index );
		Variable				GetVariableByName( string _Name );
		Variable				GetVariableBySemantic( string _Name );

		// Helpers
		void					SetScalar( int _Index, float _Value );
		void					SetScalar( string _Name, float _Value );
		void					SetScalar( int _Index, int _Value );
		void					SetScalar( string _Name, int _Value );
		void					SetScalar( int _Index, bool _Value );
		void					SetScalar( string _Name, bool _Value );
		void					SetVector( int _Index, Vector2 _Value );
		void					SetVector( string _Name, Vector2 _Value );
		void					SetVector( int _Index, Vector3 _Value );
		void					SetVector( string _Name, Vector3 _Value );
		void					SetVector( int _Index, Vector4 _Value );
		void					SetVector( string _Name, Vector4 _Value );
		void					SetMatrix( int _Index, Matrix _Value );
		void					SetMatrix( string _Name, Matrix _Value );
		void					SetResource( int _Index, ITexture2D _Value );
		void					SetResource( string _Name, ITexture2D _Value );
		void					SetResource( int _Index, ITexture3D _Value );
		void					SetResource( string _Name, ITexture3D _Value );
	}

	// The type converter for the property grid
	public class MaterialTypeConverter : System.ComponentModel.TypeConverter
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
