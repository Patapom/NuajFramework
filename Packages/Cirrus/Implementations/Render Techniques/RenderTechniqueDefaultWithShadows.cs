using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// This is a simple technique copied from the default technique but that makes use of the shadow maps
	/// </example>
	public class RenderTechniqueDefaultWithShadows : RenderTechniqueDefault
	{
		#region METHODS

		public	RenderTechniqueDefaultWithShadows( Device _Device, string _Name, bool _bUseAlphaToCoverage ) : base( _Device, _Name, _bUseAlphaToCoverage )
		{
			// Remove and dispose of existing material
			RemoveFromDisposeStack( m_Material );
			m_Material.Dispose();

			// Create our new main material override
			m_Material = ToDispose( new Material<VS_P3N3G3B3T2>( m_Device, "DefaultMaterial with Shadows", ShaderModel.SM4_0, new System.IO.FileInfo( "./FX/DefaultShaderWithShadows.fx" ) ) );
		}

		#endregion
	}
}
