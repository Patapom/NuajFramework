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
	/// Interface for objects that are "shadow pass compliant"
	/// On top of being IDepthPassRenderable, an object must also publish the list of AABBs of all its shadow casters and receivers
	/// </example>
	public interface IShadowMapRenderable : IDepthPassRenderable
	{
		/// <summary>
		/// Gets the list of AABB in WORLD space for all shadow casters
		/// </summary>
		BoundingBox[]	ShadowCastersWorldAABB		{ get; }

		/// <summary>
		/// Gets the list of AABB in WORLD space for all shadow receivers
		/// </summary>
		BoundingBox[]	ShadowReceiversWorldAABB	{ get; }
	}
}
