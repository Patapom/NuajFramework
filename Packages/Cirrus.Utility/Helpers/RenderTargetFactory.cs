using System;
using System.Collections.Generic;
using System.Linq;

using Nuaj;
using Nuaj.Cirrus;

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// This helper class is a possible implementation of the IRenderTargetFactory interface
	///  and can create and manage render targets for components that require them.
	/// It maintains a cache of render targets of the same dimensions that were created with
	///  the DISCARD usage so these can be re-used by several techniques.
	/// 
	/// For example:
	///  Technique1 => RenderTargetFactory.QueryTarget&lt;PF_RGBA16F&rt;( Technique1, RENDER_TARGET_USAGE.DISCARD, 128, 128, 1 );
	///  Technique2 => RenderTargetFactory.QueryTarget&lt;PF_RGBA16F&rt;( Technique2, RENDER_TARGET_USAGE.DISCARD, 128, 128, 1 );
	/// 
	/// Both techniques will receive the same render target.
	/// </summary>
	public class RenderTargetFactory : Component, IRenderTargetFactory
	{
		#region NESTED TYPES

		protected class TargetOwners
		{
			#region FIELDS

			protected IRenderTarget		m_RenderTarget = null;
			protected List<Component>	m_Owners = new List<Component>();

			#endregion

			#region PROPERTIES

			public IRenderTarget		RenderTarget	{ get { return m_RenderTarget; } }

			#endregion

			#region METHODS

			public TargetOwners( IRenderTarget _RenderTarget )
			{
				m_RenderTarget = _RenderTarget;
			}

			public bool		IsOwnedBy( Component _Owner )
			{
				return m_Owners.Contains( _Owner );
			}

			public void		AddOwner( Component _Owner )
			{
				m_Owners.Add( _Owner );
			}

			#endregion
		}

		#endregion

		#region FIELDS

		// RESULT Render Targets Management
		protected Dictionary<Component,List<IRenderTarget>>	m_Component2ResultTargets = new Dictionary<Component,List<IRenderTarget>>();

		// DISCARD Render Targets Management
 		protected Dictionary<string,List<TargetOwners>>	m_TargetSignature2TargetOwners = new Dictionary<string,List<TargetOwners>>();

		// Statistics
		protected int	m_TotalRenderTargetsCount = 0;
		protected int	m_TotalSurface = 0;				// Total surface in pixels

		#endregion

		#region PROPERTIES

		public int		TotalRenderTargetsCount		{ get { return m_TotalRenderTargetsCount; } }
		public int		TotalSurface				{ get { return m_TotalSurface; } }

		#endregion

		#region METHODS

		public RenderTargetFactory( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		public override void Dispose()
		{
			base.Dispose();

			// Dispose of RESULT render targets
			foreach ( Component Owner in m_Component2ResultTargets.Keys )
				RenderTargetOwnet_Disposing( Owner, EventArgs.Empty );
		}

		#region IRenderTargetFactory Members

		public RenderTarget<PF>  QueryRenderTarget<PF>(Component _Caller, RENDER_TARGET_USAGE _Usage, string _Name, int _Width, int _Height, int _MipLevelsCount) where PF : struct, IPixelFormat
		{
			return QueryRenderTarget<PF>( _Caller, _Usage, _Name, _Width, _Height, _MipLevelsCount, 1, 1 );
		}

		public RenderTarget<PF>  QueryRenderTarget<PF>(Component _Caller, RENDER_TARGET_USAGE _Usage, string _Name, int _Width, int _Height, int _MipLevelsCount, int _ArraySize) where PF : struct, IPixelFormat
		{
			return QueryRenderTarget<PF>( _Caller, _Usage, _Name, _Width, _Height, _MipLevelsCount, _ArraySize, 1 );
		}

		public RenderTarget<PF>  QueryRenderTarget<PF>(Component _Caller, RENDER_TARGET_USAGE _Usage, string _Name, int _Width, int _Height, int _MipLevelsCount, int _ArraySize, int _MultiSamplesCount) where PF : struct, IPixelFormat
		{
			if ( _Caller == null )
				throw new NException( this, "Invalid caller !" );

 			if ( _Usage == RENDER_TARGET_USAGE.RESULT )
			{	// RESULT Render Targets don't have to be cached and managed... We only track the component's disposal...
				RenderTarget<PF>	Result = new RenderTarget<PF>( m_Device, _Name, _Width, _Height, _MipLevelsCount, _ArraySize, _MultiSamplesCount );

				m_TotalRenderTargetsCount++;
				m_TotalSurface += _Width * _Height;

				if ( !m_Component2ResultTargets.ContainsKey( _Caller ) )
				{	// Register a new component and subscribe to its disposal event
					m_Component2ResultTargets[_Caller] = new List<IRenderTarget>();
					_Caller.Disposing += new EventHandler( RenderTargetOwnet_Disposing );
				}

				// Register the render target to its owner
				m_Component2ResultTargets[_Caller].Add( Result );

				return Result;
			}

			// DISCARD Render Targets, on the other hand, must be stored and cached for any other component querying the same kind of target

			// Build the hash key
			string	RTKey = typeof(PF).Name + "|" + _Width + "|" + _Height + "|" + _MipLevelsCount + "|" + _ArraySize + "|" + _MultiSamplesCount;
			if ( !m_TargetSignature2TargetOwners.ContainsKey( RTKey ) )
				m_TargetSignature2TargetOwners.Add( RTKey, new List<TargetOwners>() );

			// Check if we have a free target for that component...
			List<TargetOwners>	ExistingTargetOwners = m_TargetSignature2TargetOwners[RTKey];
			foreach ( TargetOwners TO in ExistingTargetOwners )
			{
				if ( TO.IsOwnedBy( _Caller ) )
					continue;	// This target is already owner by the caller, that means it's the same caller querying another target
								// of the same signature. We can't return it that target and we have to search for a new one !

				// This target fits the caller's demands !

				// Just add this caller to the list of owners...
				TO.AddOwner( _Caller );

				// And return the existing target...
				return TO.RenderTarget as RenderTarget<PF>;
			}

			// We need to create a brand new render target here...
			RenderTarget<PF>	Result2 = ToDispose( new RenderTarget<PF>( m_Device, _Name, _Width, _Height, _MipLevelsCount, _ArraySize, _MultiSamplesCount ) );

			m_TotalRenderTargetsCount++;
			m_TotalSurface += _Width * _Height;

			// And also create a brand new cell of target owners for it...
			TargetOwners	TO2 = new TargetOwners( Result2 );
			TO2.AddOwner( _Caller );
			ExistingTargetOwners.Add( TO2 );

			return Result2;
		}

		public RenderTarget3D<PF>  QueryRenderTarget3D<PF>(Component _Caller, RENDER_TARGET_USAGE _Usage, string _Name, int _Width, int _Height, int _Depth, int _MipLevelsCount) where PF : struct, IPixelFormat
		{
 			throw new NotImplementedException();
		}

		#endregion

		#endregion

		#region EVENT HANDLER

		void RenderTargetOwnet_Disposing( object sender, EventArgs e )
		{
			Component	Owner = sender as Component;
			Owner.Disposing -= new EventHandler( RenderTargetOwnet_Disposing );

			if ( !m_Component2ResultTargets.ContainsKey( Owner ) )
				return;

			// Dispose of render targets
			foreach ( IRenderTarget RT in m_Component2ResultTargets[Owner] )
			{
				RT.Dispose();

				m_TotalRenderTargetsCount--;
				m_TotalSurface += RT.Width * RT.Height;
			}

			// Remove the entry
			m_Component2ResultTargets.Remove( Owner );
		}

		#endregion
	}
}
