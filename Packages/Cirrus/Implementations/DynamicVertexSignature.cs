using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using SharpDX;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This is a little helper that lets you (un)register vertex fields dynamically,
	/// making the process of declaring vertex signatures easier.
	/// 
	/// It also has a copy constructor and a Load() and Save() method for serialization.
	/// </summary>
	public class	DynamicVertexSignature : IVertexSignature
	{
		#region NESTED TYPES

		public class	VertexField
		{
			#region FIELDS

			protected string				m_Name = null;
			protected VERTEX_FIELD_USAGE	m_Usage = VERTEX_FIELD_USAGE.UNKNOWN;
			protected VERTEX_FIELD_TYPE		m_Type = VERTEX_FIELD_TYPE.UNKNOWN;
			protected int					m_Index = 0;

			#endregion

			#region PROPERTIES

			public string				Name		{ get { return m_Name; } }
			public VERTEX_FIELD_USAGE	Usage		{ get { return m_Usage; } }
			public VERTEX_FIELD_TYPE	Type		{ get { return m_Type; } }
			public int					Index		{ get { return m_Index; } }

			#endregion

			#region METHODS

			public VertexField( string _Name, VERTEX_FIELD_USAGE _Usage, VERTEX_FIELD_TYPE _Type, int _Index )
			{
				m_Name = _Name != null && _Name != string.Empty ? _Name : null;
				m_Usage = _Usage;
				m_Type = _Type;
				m_Index = _Index;
			}

			public override string ToString()
			{
				return "\"" + m_Name + "\" (" + m_Usage + "#" + m_Index + " " + m_Type + ")";
			}

			/// <summary>
			/// Checks if this field matches the data from another field
			/// </summary>
			/// <param name="_Field"></param>
			/// <returns>A score that gives the amount of matching</returns>
			public int	CheckMatch( VertexField _Field )
			{
				return CheckMatch( _Field.Name, _Field.Usage, _Field.Type, _Field.Index );
			}

			/// <summary>
			/// Checks if this field matches the data from another field
			/// </summary>
			/// <param name="m_Name"></param>
			/// <param name="_Usage"></param>
			/// <param name="_Type"></param>
			/// <param name="_Index"></param>
			/// <returns>A score that gives the amount of matching</returns>
			public int	CheckMatch( string _Name, VERTEX_FIELD_USAGE _Usage, VERTEX_FIELD_TYPE _Type, int _Index )
			{
				if ( _Usage == m_Usage )
					return	0;	// Discard immediately !
				if ( _Index != m_Index )
					return	0;	// Same here...

				int	MatchCount = 1;
				if ( _Type == m_Type )
					MatchCount++;
				if ( _Name != null && m_Name != null && _Name == m_Name )
					MatchCount++;

				return MatchCount;
			}

			#endregion
		}

		public struct	UsageIndexID
		{
			public VERTEX_FIELD_USAGE	Usage;
			public int					Index;

			public UsageIndexID( VERTEX_FIELD_USAGE _Usage, int _Index )
			{
				Usage = _Usage;
				Index = _Index;
			}

			public override bool Equals( object obj )
			{
				if ( obj == null || !(obj is UsageIndexID) )
					return false;

				UsageIndexID	Other = (UsageIndexID) obj;
				return Other.Usage == Usage && Other.Index == Index;
			}

			public override int GetHashCode()
			{
				return (Usage.GetHashCode() ^ Index).GetHashCode();
			}
		}

		#endregion

		#region FIELDS

		protected List<VertexField>						m_Fields = new List<VertexField>();
		protected Dictionary<UsageIndexID,VertexField>	m_Usage2Field = new Dictionary<UsageIndexID,VertexField>();

		#endregion

		#region METHODS

		public	DynamicVertexSignature()
		{
		}

		public	DynamicVertexSignature( IVertexSignature _Signature )
		{
			for ( int FieldIndex=0; FieldIndex < _Signature.VertexFieldsCount; FieldIndex++ )
				AddField( _Signature.GetVertexFieldName( FieldIndex ), _Signature.GetVertexFieldUsage( FieldIndex ), _Signature.GetVertexFieldType( FieldIndex ), _Signature.GetVertexFieldIndex( FieldIndex ) );
		}

		public VertexField	AddField( string _Name, VERTEX_FIELD_USAGE _Usage, VERTEX_FIELD_TYPE _Type, int _Index )
		{
			VertexField	Field = new VertexField( _Name, _Usage, _Type, _Index );
			m_Fields.Add( Field );
			m_Usage2Field.Add( new UsageIndexID( _Usage, _Index ), Field );

			return Field;
		}

		public void			RemoveField( VertexField _Field )
		{
			if ( _Field == null )
				return;

			m_Fields.Remove( _Field );
			m_Usage2Field.Remove( new UsageIndexID( _Field.Usage, _Field.Index ) );
		}

		public void			ClearFields()
		{
			m_Fields.Clear();
			m_Usage2Field.Clear();
		}

		/// <summary>
		/// Loads a signature from a stream
		/// </summary>
		/// <param name="_Stream"></param>
		public void			Load( System.IO.Stream _Stream )
		{
			ClearFields();

			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( _Stream );
			int	FieldsCount = Reader.ReadInt32();
			for ( int FieldIndex=0; FieldIndex < FieldsCount; FieldIndex++ )
			{
				string	Name = Reader.ReadString();
				VERTEX_FIELD_USAGE	Usage = (VERTEX_FIELD_USAGE) Reader.ReadInt32();
				VERTEX_FIELD_TYPE	Type = (VERTEX_FIELD_TYPE) Reader.ReadInt32();
				int					Index = Reader.ReadInt32();

				AddField( Name, Usage, Type, Index );
			}
		}

		/// <summary>
		/// Saves a signature to a stream
		/// </summary>
		/// <param name="_Stream"></param>
		public void			Save( System.IO.Stream _Stream )
		{
			System.IO.BinaryWriter	Writer = new System.IO.BinaryWriter( _Stream );
			Writer.Write( m_Fields.Count );
			foreach ( VertexField Field in m_Fields )
			{
				Writer.Write( Field.Name != null ? Field.Name : "" );
				Writer.Write( (int) Field.Usage );
				Writer.Write( (int) Field.Type );
				Writer.Write( Field.Index );
			}
		}

		#region IVertexSignature Members

		public int					VertexFieldsCount		{ get { return m_Fields.Count; } }

		public VERTEX_FIELD_USAGE	GetVertexFieldUsage( int _FieldIndex )
		{
			return m_Fields[_FieldIndex].Usage;
		}

		public VERTEX_FIELD_TYPE	GetVertexFieldType( int _FieldIndex )
		{
			return m_Fields[_FieldIndex].Type;
		}

		public string				GetVertexFieldName( int _FieldIndex )
		{
			return m_Fields[_FieldIndex].Name;
		}

		public int					GetVertexFieldIndex( int _FieldIndex )
		{
			return m_Fields[_FieldIndex].Index;
		}

		public bool					CheckMatch( IVertexSignature _Signature )
		{
			return GetVertexFieldsMap( _Signature ) != null;
		}

		public Dictionary<int,int>	GetVertexFieldsMap( IVertexSignature _Signature )
		{
			Dictionary<int,int>	Result = new Dictionary<int,int>();

			for ( int VertexFieldIndex=0; VertexFieldIndex < m_Fields.Count; VertexFieldIndex++ )
			{
				VertexField	Field = m_Fields[VertexFieldIndex];
				VERTEX_FIELD_USAGE	OurUsage = Field.Usage;
				VERTEX_FIELD_TYPE	OurType = Field.Type;
				int					OurIndex = Field.Index;

				bool	bMatched = false;
				for ( int OtherVertexFieldIndex=0; OtherVertexFieldIndex < _Signature.VertexFieldsCount; OtherVertexFieldIndex++ )
				{
					VERTEX_FIELD_USAGE	OtherUsage = _Signature.GetVertexFieldUsage( OtherVertexFieldIndex );
					if ( OtherUsage != OurUsage )
						continue;	// Not the same usage !

					VERTEX_FIELD_TYPE	OtherType = _Signature.GetVertexFieldType( OtherVertexFieldIndex );
					if ( OtherType != OurType )
						continue;	// Not the same type !

					int					OtherIndex = _Signature.GetVertexFieldIndex( OtherVertexFieldIndex );
					if ( OtherIndex != OurIndex )
						continue;	// Not the same index !

					// Found a perfect match !
					bMatched = true;
					Result.Add( VertexFieldIndex, OtherVertexFieldIndex );
					break;
				}

				if ( !bMatched )
					return null;	// Incomplete match... Can't provide a valid map !
			}

			return Result;
		}

		#endregion

		#endregion
	}
}
