using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using SharpDX;
using SharpDX.Direct3D10;

namespace Nuaj
{
	/// <summary>
	/// Wraps a DirectX effect variable
	/// </summary>
	public abstract class	Variable
	{
		#region NESTED TYPES

		// The type converter for the property grid
		public class TypeConverter : System.ComponentModel.TypeConverter
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

		#endregion

		#region FIELDS

		protected IMaterial				m_Owner = null;
		protected int					m_Index = 0;
		protected string				m_Name = null;
		protected string				m_Semantic = null;
		protected bool					m_bLastAssignmentFailed = false;

		#endregion

		#region PROPERTIES

		public int				Index		{ get { return m_Index; } }
		public string			Name		{ get { return m_Name; } }
		public string			Semantic	{ get { return m_Semantic; } }

		public VariableScalar	AsScalar	{ get { return this as VariableScalar; } }
		public VariableVector	AsVector	{ get { return this as VariableVector; } }
		public VariableMatrix	AsMatrix	{ get { return this as VariableMatrix; } }
		public VariableResource	AsResource	{ get { return this as VariableResource; } }

		#endregion

		#region METHODS

		internal Variable( IMaterial _Owner, int _Index, EffectVariable _Variable )
		{
			m_Owner = _Owner;
			m_Index = _Index;
            m_Name = _Variable.Description.Name ?? "";
            m_Semantic = _Variable.Description.Semantic ?? "";
        }

		public override string ToString()
		{
			return m_Name + "(" + m_Semantic + ") #" + m_Index;
		}

		internal virtual void	EffectRecompiled( int _NewIndex, EffectVariable _NewVariable )
		{
			m_Index = _NewIndex;
			m_bLastAssignmentFailed = false;	// Give another chance to failed variables...
		}

		#endregion
	}

	[System.ComponentModel.TypeConverter(typeof(Variable.TypeConverter))]
	public class	VariableScalar : Variable
	{
		#region FIELDS

		protected EffectScalarVariable	m_Variable = null;

		#endregion

		#region PROPERTIES

		public bool		ValueBool	{ get { return m_Variable != null ? m_Variable.GetBool() : false; } set { Set( value ); } }
		public int		ValueInt	{ get { return m_Variable != null ? m_Variable.GetInt() : 0; } set { Set( value ); } }
		public float	ValueFloat	{ get { return m_Variable != null ? m_Variable.GetFloat() : 0.0f; } set { Set( value ); } }

		#endregion

		#region METHODS

		internal VariableScalar( IMaterial _Owner, int _Index, EffectScalarVariable _Variable ) : base( _Owner, _Index, _Variable )
		{
			EffectRecompiled( _Index, _Variable );
		}

		public void		Set( bool _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( bool[] _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( int _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( int[] _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( float _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( float[] _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		internal override void	EffectRecompiled( int _NewIndex, EffectVariable _NewVariable )
		{
			base.EffectRecompiled( _NewIndex, _NewVariable );
			m_Variable = _NewVariable.AsScalar();
		}

		#endregion
	}

	[System.ComponentModel.TypeConverter(typeof(Variable.TypeConverter))]
	public class	VariableVector : Variable
	{
		#region FIELDS

		protected EffectVectorVariable	m_Variable = null;

#if DEBUG
		// Cached versions of vectors
		protected Vector2				m_CachedVector2 = Vector2.Zero;
		protected Vector3				m_CachedVector3 = Vector3.Zero;
		protected Vector4				m_CachedVector4 = Vector4.Zero;
		protected Color4				m_CachedColor = new Color4( Vector4.Zero );
		protected Quaternion			m_CachedQuat = Quaternion.Identity;
#endif
		#endregion

		#region PROPERTIES
#if DEBUG
		public Vector2	ValueVector2	{ get { return m_CachedVector2; } set { Set( value ); } }
		public Vector3	ValueVector3	{ get { return m_CachedVector3; } set { Set( value ); } }
		public Vector4	ValueVector4	{ get { return m_CachedVector4; } set { Set( value ); } }
#endif
		#endregion

		#region METHODS

		internal VariableVector( IMaterial _Owner, int _Index, EffectVectorVariable _Variable ) : base( _Owner, _Index, _Variable )
		{
			EffectRecompiled( _Index, _Variable );
		}

		public void		Set( bool[] _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( int[] _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( Color4 _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );

#if DEBUG
				m_CachedColor = _Value;
#endif
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( Color4[] _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( Vector2 _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
#if DEBUG
				m_CachedVector2 = _Value;
#endif
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( Vector3 _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
#if DEBUG
				m_CachedVector3 = _Value;
#endif
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( Vector4 _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
#if DEBUG
				m_CachedVector4 = _Value;
#endif
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		Set( Vector4[] _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		/// <summary>
		/// This method translates the quaternion to a 4D vector (XYZ = Quat.XYZ & W = Quat.W)
		/// </summary>
		/// <param name="_Value"></param>
		public void		Set( Quaternion _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.Set( new Vector4( _Value.X, _Value.Y, _Value.Z, _Value.W ) );
#if DEBUG
				m_CachedQuat = _Value;
#endif
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		internal override void	EffectRecompiled( int _NewIndex, EffectVariable _NewVariable )
		{
			base.EffectRecompiled( _NewIndex, _NewVariable );
			m_Variable = _NewVariable.AsVector();
		}

		#endregion
	}

	public class	VariableMatrix : Variable
	{
		#region FIELDS

		protected EffectMatrixVariable	m_Variable = null;

		#endregion

		#region METHODS

		internal VariableMatrix( IMaterial _Owner, int _Index, EffectMatrixVariable _Variable ) : base( _Owner, _Index, _Variable )
		{
			EffectRecompiled( _Index, _Variable );
		}

		public void		SetMatrix( Matrix _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.SetMatrix( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		public void		SetMatrix( Matrix[] _Value )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.SetMatrix( _Value );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		internal override void	EffectRecompiled( int _NewIndex, EffectVariable _NewVariable )
		{
			base.EffectRecompiled( _NewIndex, _NewVariable );
			m_Variable = _NewVariable.AsMatrix();
		}

		#endregion
	}

	public class	VariableResource : Variable
	{
		#region FIELDS

		protected EffectShaderResourceVariable	m_Variable = null;

		#endregion

		#region METHODS

		internal VariableResource( IMaterial _Owner, int _Index, EffectShaderResourceVariable _Variable ) : base( _Owner, _Index, _Variable )
		{
			EffectRecompiled( _Index, _Variable );
		}

		public ShaderResourceView	GetResource()
		{
			return m_Variable != null && m_Variable.IsValid ? m_Variable.GetResource() : null;
		}

		public void		SetResource( ITexture2D _Texture )
		{
			SetResource( _Texture != null ? _Texture.TextureView : null );
		}

		public void		SetResource( ITexture3D _Texture )
		{
			SetResource( _Texture != null ? _Texture.TextureView : null );
		}

		public void		SetResource( ShaderResourceView _View )
		{
			try
			{
				if ( !m_bLastAssignmentFailed && m_Variable != null && m_Variable.IsValid )
					m_Variable.SetResource( _View );
			}
			catch ( Exception )
			{	
				m_bLastAssignmentFailed = true;
			}
		}

		internal override void	EffectRecompiled( int _NewIndex, EffectVariable _NewVariable )
		{
			base.EffectRecompiled( _NewIndex, _NewVariable );
			m_Variable = _NewVariable.AsShaderResource();
	
			// Set the "missing texture" texture by default...
			// This is quite important otherwise, sampling a really missing texture (i.e. null) is VERY SLOW !
			SetResource( m_Owner.Device.MissingTexture.TextureView );
		}

		#endregion
	}
}
