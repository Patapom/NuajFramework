using System;
using System.Collections.Generic;
using System.Linq;

namespace Nuaj.Cirrus
{
	public delegate void 	FileReaderDelegate( System.IO.BinaryReader _Reader );

	/// <summary>
	/// This is an interface that should be implemented by any object that is able to load files.
	/// The file loader interface is very useful to abstract the source of serialized data
	///  as it be easily interchanged with a disk loader, an archive loader or a memory loader.
	/// </summary>
	/// <example>
	/// A default IFileLoader from disk should be implemented like this :
	/// 
	///	public System.IO.Stream OpenFile( System.IO.FileInfo _FileName )
	///	{
	///		return _FileName.OpenRead();
	///	}
	///	
	///	public void ReadBinaryFile( System.IO.FileInfo _FileName, FileReaderDelegate _Reader )
	///	{
	///		using ( System.IO.BinaryReader Reader = new System.IO.BinaryReader( OpenFile( _FileName ) ) )
	///			_Reader( Reader );
	///	}
	/// </example>
	public interface IFileLoader
	{
		/// <summary>
		/// Returns the file stream given a file name
		/// </summary>
		/// <param name="_FileName">The name of the file to open</param>
		/// <returns>The corresponding file stream</returns>
		System.IO.Stream		OpenFile( System.IO.FileInfo _FileName );

		/// <summary>
		/// Opens the given binary file name
		/// </summary>
		/// <param name="_FileName">The name of the file to open</param>
		/// <param name="_Reader">The reader delegate</param>
		void					ReadBinaryFile( System.IO.FileInfo _FileName, FileReaderDelegate _Reader );
	}
}
