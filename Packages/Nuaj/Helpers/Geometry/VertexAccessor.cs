using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using SharpDX;
using SharpDX.Direct3D10;

namespace Nuaj.Helpers
{
	/// <summary>
	/// This class helps to access vertex structure members by semantic
	/// </summary>
	/// <typeparam name="VS"></typeparam>
	public class	VertexAccessor<VS> where VS:struct
	{
		#region FIELDS

		protected Type							m_VertexType = null;
		protected Dictionary<string,FieldInfo>	m_Semantic2Field = new Dictionary<string,FieldInfo>();

		#endregion

		#region METHODS

		public VertexAccessor()
		{
			m_VertexType = typeof(VS);
		}

		/// <summary>
		/// Gets the value of the instance field with the requested semantic
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_Instance">The vertex instance to read</param>
		/// <param name="_Semantic">The semantic to get the value of</param>
		/// <returns>The requested value</returns>
		public T		GetValue<T>( VS _Instance, string _Semantic ) where T:struct
		{
			return (T) GetValue( _Instance, _Semantic );
		}

		/// <summary>
		/// Sets the value of the instance field with the requested semantic
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_Instance">The vertex instance to write</param>
		/// <param name="_Semantic">The semantic to get the value of</param>
		/// <param name="_Value">The value to set</param>
		public VS		SetValue<T>( object _Instance, string _Semantic, T _Value ) where T:struct
		{
			return SetValue( _Instance, _Semantic, _Value as object );
		}

		/// <summary>
		/// Gets the value of the instance field with the requested semantic
		/// </summary>
		/// <param name="_Instance">The vertex instance to read</param>
		/// <param name="_Semantic">The semantic to get the value of</param>
		/// <returns>The requested value</returns>
		public object	GetValue( VS _Instance, string _Semantic )
		{
			if ( !m_Semantic2Field.ContainsKey( _Semantic ) )
			{
				BuildSemantics();

				// Re-check...
				if ( !m_Semantic2Field.ContainsKey( _Semantic ) )
					throw new Exception( "There is no field with semantic \"" + _Semantic + "\" in the vertex structure !" );
			}

			FieldInfo	RequiredField = m_Semantic2Field[_Semantic];
			return RequiredField.GetValue( _Instance );
		}

		/// <summary>
		/// Sets the value of the instance field with the requested semantic
		/// </summary>
		/// <param name="_Instance">The vertex instance to write</param>
		/// <param name="_Semantic">The semantic to set the value of</param>
		/// <param name="_Value">The new value to set</param>
		public VS		SetValue( object _Instance, string _Semantic, object _Value )
		{
			if ( !m_Semantic2Field.ContainsKey( _Semantic ) )
			{
				BuildSemantics();

				// Re-check...
				if ( !m_Semantic2Field.ContainsKey( _Semantic ) )
					throw new Exception( "There is no field with semantic \"" + _Semantic + "\" in the vertex structure !" );
			}

			FieldInfo	RequiredField = m_Semantic2Field[_Semantic];
			RequiredField.SetValue( _Instance as object, _Value );

			return	(VS) _Instance;
		}

		/// <summary>
		/// Caches a map of semantics pointing to a type FieldInfo
		/// </summary>
		protected void	BuildSemantics()
		{
			if ( m_Semantic2Field.Count != 0 )
				return;	// Already built !

			// Build the map of semantics
			FieldInfo[]	Fields = m_VertexType.GetFields();
			foreach ( FieldInfo Field in Fields )
			{
				// Retrieve semantic
				SemanticAttribute[]	Semantics = Field.GetCustomAttributes( typeof(SemanticAttribute), false ) as SemanticAttribute[];
				if ( Semantics.Length == 0 )
					continue;
				if ( Semantics.Length > 1 )
					throw new Exception( "Field \"" + Field.Name + "\" has more than one Semantic attribute !" );
				string	Semantic = Semantics[0].Semantic;
//				int		Index = Semantics[0].Index;

				m_Semantic2Field.Add( Semantic, Field );
			}
		}

		#endregion
	}
}
