using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SequencorLib
{
	public interface ISerializable
	{
		/// <summary>
		/// Saves content to a writer
		/// </summary>
		/// <param name="_Writer"></param>
		void	Save( BinaryWriter _Writer );

		/// <summary>
		/// Loads content from a reader
		/// </summary>
		/// <param name="_Reader"></param>
		void	Load( BinaryReader _Reader );
	}
}
