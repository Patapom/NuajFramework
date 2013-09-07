using System;
using System.Collections.Generic;
using System.Linq;

using Nuaj;
using Nuaj.Cirrus;
using SharpDX;

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// This is the simple (but complicated to implement) interface to cube map renderers
	///  that need to be compliant with the SHEnvMap rendering technique.
	/// You can find an implementation in the Implementations/SHCubeMapRendererExample class.
	/// 
	/// A typical rendering goes like this :
	/// 
	/// . BeginRender()
	/// foreach Cube Map ro render
	/// {
	///   // Render the cube map
	///   . BeginRenderCubeMap()
	///   . RenderCubeMapFace()
	///   . EndRenderCubeMap()
	///  
	///   // Read each cube face
	///   foreach Cube Map face
	///   {
	///     . BeginReadCubeMapFace()
	///     foreach X,Y
	///       . ReadPixel()
	///     . EndReadCubeMapFace()
	///   }
	/// }
	/// . EndRender()
	/// </summary>
	public interface ISHCubeMapsRenderer
	{
		/// <summary>
		/// Notifies we're goint to start environment rendering
		/// </summary>
		/// <param name="_CubeMapSize"></param>
		void	BeginRender( int _CubeMapSize );

		/// <summary>
		/// Notifies we're going to start rendering a cube map
		/// </summary>
		/// <returns>The value at which the depth buffer was initialized (in world units, no homogeneous bullshit here !) (by default, return camera far clip distance)</returns>
		float	BeginRenderCubeMap();

		/// <summary>
		/// Asks the renderer to render a cube map face given the camera, already configured to watch in the cube map's face direction
		/// </summary>
		/// <param name="_Camera">The configured camera that should view the cube map face</param>
		/// <param name="_CubeMapSize">The size of the cube map to render</param>
		void	RenderCubeMapFace( Camera _Camera, int _CubeMapFaceIndex );

		/// <summary>
		/// Notifies we've stopped rendering the cube map
		/// </summary>
		void	EndRenderCubeMap();

		/// <summary>
		/// Notifies we're going to start reading back cube map face data
		/// </summary>
		/// <param name="_FaceIndex"></param>
		void	BeginReadCubeMapFace( int _FaceIndex );

		/// <summary>
		/// Reads a single pixel from the stream of pixels.
		/// The read is performed by a loop like this :
		///  for ( int Y=0; Y < CubeMapSize; Y++ )
		///		for ( int X=0; X < CubeMapSize; X++ )
		///			ReadPixel( Albedo, Normal, Depth );
		///	So expect CubeMapSize*CubeMapSize calls once the BeginReadCubeMapFace() method has been called.
		/// </summary>
		/// <param name="_Albedo"></param>
		/// <param name="_WorldNormal"></param>
		/// <param name="_Depth"></param>
		void	ReadPixel( ref Vector3 _Albedo, ref Vector3 _WorldNormal, ref float _Depth );

		/// <summary>
		/// Notifies we've stopped reading back cube map face data
		/// </summary>
		/// <param name="_FaceIndex"></param>
		void	EndReadCubeMapFace( int _FaceIndex );

		/// <summary>
		/// Notifies we've stopped environment rendering
		/// </summary>
		void	EndRender();
	}
}
