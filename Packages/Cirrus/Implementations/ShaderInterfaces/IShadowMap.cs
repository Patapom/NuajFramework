using System;
using System.Collections.Generic;
using System.Linq;

using SharpDX;
using SharpDX.Direct3D10;

namespace Nuaj
{
	/// <summary>
	/// Shadow Map support interface
	/// </summary>
	public class	IShadowMap : ShaderInterfaceBase
	{
		[Semantic( "SHADOW_SLICES_COUNT" )]
		public int			ShadowSlicesCount	{ set { SetScalar( "SHADOW_SLICES_COUNT", value ); } }

		[Semantic( "SHADOW_SLICE_RANGES" )]
		public Vector4[]	ShadowSliceRanges	{ set { SetVector( "SHADOW_SLICE_RANGES", value ); } }

		[Semantic( "WORLD2SHADOWMAPS" )]
		public Matrix[]		World2ShadowMaps	{ set { SetMatrix( "WORLD2SHADOWMAPS", value ); } }

		[Semantic( "SHADOWMAPS2WORLD" )]
		public Matrix[]		ShadowMaps2World	{ set { SetMatrix( "SHADOWMAPS2WORLD", value ); } }

		[Semantic( "SHADOW_MAPS" )]
		public ShaderResourceView	ShadowMaps	{ set { SetResource( "SHADOW_MAPS", value ); } }
	}

}
