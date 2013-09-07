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
	/// This is the interface to a shader interface
	/// </summary>
	public interface IShaderInterface
	{
		/// <summary>
		/// Assigns a material variable with an associated semantic to the interface
		/// </summary>
		/// <param name="_Semantic"></param>
		/// <param name="_Variable"></param>
		void	SetEffectVariable( string _Semantic, Variable _Variable );
	}
}
