using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using SharpDX;
using SharpDX.Direct3D10;

namespace Nuaj
{
	/// <summary>
	/// This interface helps to access an index
	/// </summary>
	/// <typeparam name="I"></typeparam>
	public interface	IIndexAccessor<I> where I:struct
	{
		/// <summary>
		/// Reads an index into an integer
		/// </summary>
		/// <param name="_Index"></param>
		/// <returns></returns>
		int			ToInt( I _Index );

		/// <summary>
		/// Writes an index from an integer
		/// </summary>
		/// <param name="_Index"></param>
		/// <returns></returns>
		I			FromInt( int _Index );
	}
}
