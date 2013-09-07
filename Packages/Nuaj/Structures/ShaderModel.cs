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
	/// Defines a shader model
	/// Materials need to declare the shader model they're using, which is compared against the one supported by the device
	/// A material, obviously, should not be used if its shader model is not supported
	/// You should catch the "Material.UnsupportedShaderModelException" and default to a lower quality shader.
	/// That's especially true for shaders that support "exotic" DirectX features like the ones from DX10.1 (e.g. custom Alpha2Coverage mask access)
	/// </summary>
	public struct	ShaderModel
	{
		public int	Major;
		public int	Minor;

		public static ShaderModel	Empty = new ShaderModel( 0, 0 );	// Invalid
		public static ShaderModel	SM2_0 = new ShaderModel( 2, 0 );	// DirectX 9.1
		public static ShaderModel	SM3_0 = new ShaderModel( 3, 0 );	// DirectX 9.2
		public static ShaderModel	SM4_0 = new ShaderModel( 4, 0 );	// DirectX 10
		public static ShaderModel	SM4_1 = new ShaderModel( 4, 1 );	// DirectX 10.1
		public static ShaderModel	SM5_0 = new ShaderModel( 5, 0 );	// DirectX 11

		public ShaderModel( int _Major, int _Minor )
		{
			Major = _Major;
			Minor = _Minor;
		}

		public override string ToString()
		{
			return Major.ToString() + "." + Minor;
		}

		/// <summary>
		/// Tells if the provided model is supported by this model
		/// </summary>
		/// <param name="_RequiredModel"></param>
		/// <returns></returns>
		public bool	CanSupport( ShaderModel _RequiredModel )
		{
			if ( _RequiredModel.Major > Major )
				return false;
			if ( _RequiredModel.Minor > Minor )
				return false;

			return true;
		}
	}
}
