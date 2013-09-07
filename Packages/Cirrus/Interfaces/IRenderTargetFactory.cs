using System;
using System.Collections.Generic;
using System.Linq;

using Nuaj;

namespace Nuaj.Cirrus
{
	/// <summary>
	/// Describes the usage to make of the queried render target
	/// A typical example is a render technique querying 3 targets :
	///  _ 2 are temporary targets used internally to ping-pong some results
	///		=> these targets should be created as DISCARD so they may be re-used by other techniques later
	///		(indeed, you don't care if someone overwrites these temporary results)
	///	
	///	 _ the 3rd target will hold the results of the render technique
	///		=> this target should be created as RESULT so another object that follows in the pipeline may read the results from your technique
	///		(a typical example would be a shadow map which is rendered by the ShadowMap technique and accessed by others)
	/// </summary>
	public enum RENDER_TARGET_USAGE
	{
		DISCARD,	// A "DISCARD" render target is only temporary and is not used across 2 render techniques so it can be re-used 
		RESULT,		// A "RESULT" render target is used as a result from a technique for one or other techniques so it's specific to the caller and cannot be re-used
	}

	/// <summary>
	/// This is an interface that should be implemented by any object that is able to create and manage render targets for a third party.
	/// The RenderTarget factory interface is very useful to centralize the generation of render targets used by render techniques.
	/// It's up to the implementer to manage and dispose the render targets so it's really easy to implement render targets caching
	///  and custom memory management.
	/// </summary>
	public interface IRenderTargetFactory
	{
		/// <summary>
		/// Queries a 2D render target
		/// </summary>
		/// <typeparam name="PF"></typeparam>
		/// <param name="_Caller"></param>
		/// <param name="_Usage"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		RenderTarget<PF>	QueryRenderTarget<PF>( Component _Caller, RENDER_TARGET_USAGE _Usage, string _Name, int _Width, int _Height, int _MipLevelsCount ) where PF:struct,IPixelFormat;

		/// <summary>
		/// Queries a 2D render target
		/// </summary>
		/// <typeparam name="PF"></typeparam>
		/// <param name="_Caller"></param>
		/// <param name="_Usage"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_ArraySize"></param>
		/// <returns></returns>
		RenderTarget<PF>	QueryRenderTarget<PF>( Component _Caller, RENDER_TARGET_USAGE _Usage, string _Name, int _Width, int _Height, int _MipLevelsCount, int _ArraySize ) where PF:struct,IPixelFormat;

		/// <summary>
		/// Queries a 2D render target
		/// </summary>
		/// <typeparam name="PF"></typeparam>
		/// <param name="_Caller"></param>
		/// <param name="_Usage"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <param name="_ArraySize"></param>
		/// <param name="_MultiSamplesCount"></param>
		/// <returns></returns>
		RenderTarget<PF>	QueryRenderTarget<PF>( Component _Caller, RENDER_TARGET_USAGE _Usage, string _Name, int _Width, int _Height, int _MipLevelsCount, int _ArraySize, int _MultiSamplesCount ) where PF:struct,IPixelFormat;

		/// <summary>
		/// Queries a 3D render target
		/// </summary>
		/// <typeparam name="PF"></typeparam>
		/// <param name="_Caller"></param>
		/// <param name="_Usage"></param>
		/// <param name="_Width"></param>
		/// <param name="_Height"></param>
		/// <param name="_Depth"></param>
		/// <param name="_MipLevelsCount"></param>
		/// <returns></returns>
		RenderTarget3D<PF>	QueryRenderTarget3D<PF>( Component _Caller, RENDER_TARGET_USAGE _Usage, string _Name, int _Width, int _Height, int _Depth, int _MipLevelsCount ) where PF:struct,IPixelFormat;
	}
}
