using System;
using System.Collections.Generic;
using System.Text;

namespace Pognac
{
	public class	CollectionsOperations<T>
	{
		/// <summary>
		/// Performs the union of 2 collections, actually removing any duplicate entries
		/// </summary>
		/// <param name="_Collection0">The first collection to work with (can be null)</param>
		/// <param name="_Collection1">The second collection to work with (can be null)</param>
		/// <returns>The union of the 2 collections</returns>
		public static T[]	Union( T[] _Collection0, T[] _Collection1 )
		{
			Dictionary<T,T>	Uniqueness = new Dictionary<T,T>( (_Collection0 != null ? _Collection0.Length : 0) + (_Collection1 != null ? _Collection1.Length : 0) );

			if ( _Collection0 != null )
				for ( int Index0=0; Index0 < _Collection0.Length; Index0++ )
				{
					T	Value = _Collection0[Index0];
					Uniqueness[Value] = Value;
				}

			if ( _Collection1 != null )
				for ( int Index1=0; Index1 < _Collection1.Length; Index1++ )
				{
					T	Value = _Collection1[Index1];
					Uniqueness[Value] = Value;
				}

			T[]	Result = new T[Uniqueness.Count];
			Uniqueness.Values.CopyTo( Result, 0 );

			return	Result;
		}

		/// <summary>
		/// Performs the intersection of 2 collections, only keeping entries existing in both collections
		/// </summary>
		/// <param name="_Collection0">The first collection to work with (can be null)</param>
		/// <param name="_Collection1">The second collection to work with (can be null)</param>
		/// <returns>The intersection of the 2 collections</returns>
		public static T[]	Intersection( T[] _Collection0, T[] _Collection1 )
		{
			if ( _Collection0 == null || _Collection1 == null )
				return new T[] {};	// Empty intersection...

			Dictionary<T,T>	Duplicates = new Dictionary<T,T>( Math.Max( _Collection0.Length, _Collection1.Length ) );
			List<T>			Result = new List<T>( Math.Max( _Collection0.Length, _Collection1.Length ) );

			// First fill with elements from the largest collection
			T[]	Source = _Collection0.Length > _Collection1.Length ? _Collection0 : _Collection1;
			for ( int Index0=0; Index0 < Source.Length; Index0++ )
			{
				T	Value = Source[Index0];
				Duplicates[Value] = Value;
			}

			// Then check for duplicates with elements from the smallest collection
			Source = _Collection0.Length <= _Collection1.Length ? _Collection0 : _Collection1;
			foreach ( T Element in Source )
				if ( Duplicates.ContainsKey( Element ) )
					Result.Add( Element );	// This is an element common to both collections!

			return	Result.ToArray();
		}

		/// <summary>
		/// Performs the subtraction of the second collection from the first one
		/// </summary>
		/// <param name="_Collection0">The first collection to work with (can be null)</param>
		/// <param name="_Collection1">The second collection to work with (can be null)</param>
		/// <returns>The subtraction of collection 1 from collection 0</returns>
		public static T[]	Subtraction( T[] _Collection0, T[] _Collection1 )
		{
			if ( _Collection0 == null )
				return new T[] {};		// Empty subtraction...
			if ( _Collection1 == null || _Collection1.Length == 0 )
				return	_Collection0;	// Nothing to subtract anyway...

			Dictionary<T,T>	Duplicates = new Dictionary<T,T>( _Collection0.Length );

			// First fill with elements from the first collection
			for ( int Index0=0; Index0 < _Collection0.Length; Index0++ )
			{
				T	Value = _Collection0[Index0];
				Duplicates[Value] = Value;
			}

			// Then check for duplicates with elements from the second collection
			foreach ( T Element in _Collection1 )
				if ( Duplicates.ContainsKey( Element ) )
					Duplicates.Remove( Element );		// Remove this common element...

			T[]	Result = new T[Duplicates.Count];
			Duplicates.Values.CopyTo( Result, 0 );

			return	Result;
		}

		/// <summary>
		/// Tells if the provided collections have the same content (not ordered)
		/// </summary>
		/// <param name="_Collection0">The first collection to work with (can be null)</param>
		/// <param name="_Collection1">The second collection to work with (can be null)</param>
		/// <returns>True if both collections have the same content, false otherwise</returns>
		public static bool	HaveSameContent( T[] _Collection0, T[] _Collection1 )
		{
			if ( _Collection0 == null && _Collection1 != null )
				return	false;
			if ( _Collection0 != null && _Collection1 == null )
				return	false;
			if ( _Collection0 == null && _Collection1 == null )
				return	true;
			if ( _Collection0.Length != _Collection1.Length )
				return	false;

			// Fill up a hashtable with the content of the second collection
			Dictionary<T,int>	Content1 = new Dictionary<T,int>();
			foreach ( T Item in _Collection1 )
				if ( Content1.ContainsKey( Item ) )
					Content1[Item]++;
				else
					Content1[Item] = 1;

			// Decrease content using the first collection
			foreach ( T Item in _Collection0 )
				if ( !Content1.ContainsKey( Item ) )
					return	false;
				else
					Content1[Item]--;

			// Ensure the hashtable contains only zeroes...
			foreach ( T Item in Content1.Keys )
				if ( Content1[Item] != 0 )
					return	false;

			return	true;
		}

		/// <summary>
		/// Compacts the provided collection by eliminating all null entries
		/// </summary>
		/// <param name="_Collection">The collection to compact</param>
		/// <returns>The compact collection</returns>
		public static T[]	Compact( T[] _Collection )
		{
			return	Compact( _Collection, false );
		}

		/// <summary>
		/// Compacts the provided collection by eliminating all null or duplicate entries
		/// </summary>
		/// <param name="_Collection">The collection to compact</param>
		/// <param name="_bRemoveDuplicates">Tells if the compaction should also remove duplicate entries</param>
		/// <returns>The compact collection</returns>
		public static T[]	Compact( T[] _Collection, bool _bRemoveDuplicates )
		{
			List<T>	CompactResult = new List<T>();

			foreach ( T Item in _Collection )
				if ( Item != null && (!_bRemoveDuplicates || !CompactResult.Contains( Item )) )
					CompactResult.Add( Item );

			return	CompactResult.ToArray();
		}
	}
}
