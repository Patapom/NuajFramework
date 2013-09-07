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
	/// Interface to components
	/// </summary>
	public interface IComponent : IDisposable
	{
		/// <summary>
		/// Gets the device managing the component
		/// </summary>
		Device		Device			{ get; }

		/// <summary>
		/// Gets the name of the component
		/// </summary>
		string		Name			{ get; }
	}
}
