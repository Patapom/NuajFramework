using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// Interface for render techniques that are "depth pass compliant"
	/// To be compliant with the depth-pass, a technique or object must display its opaque primitives
	///  using "RenderOverride()" so it's drawing with the currently assign depth material.
	/// </example>
	public interface IDepthPassRenderable
	{
		/// <summary>
		/// Renders the technique for the depth pass
		/// </summary>
		/// <param name="_FrameToken">A frame token that should be increased by one each new frame</param>
		/// <param name="_Pass">The depth effect pass</param>
		/// <param name="_vLocal2World">The Local2World transform variable to setup</param>
		void	RenderDepthPass( int _FrameToken, EffectPass _Pass, VariableMatrix _vLocal2World );
	}
}
