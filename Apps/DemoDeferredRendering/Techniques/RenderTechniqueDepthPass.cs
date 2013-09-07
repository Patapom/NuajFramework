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
	/// Depth-pass pre-process
	/// </example>
	public class RenderTechniqueDepthPass : DeferredRenderTechnique
	{
		#region FIELDS

		protected Material<VS_P3>				m_Material = null;
		protected List<IDepthPassRenderable>	m_Renderables = new List<IDepthPassRenderable>();

		#endregion

		#region METHODS

		public RenderTechniqueDepthPass( Renderer _Renderer, string _Name ) : base( _Renderer, _Name )
		{
			// Create our main materials
			m_Material = ToDispose( new Material<VS_P3>( m_Device, "DepthPass Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/Deferred/DepthPass.fx" ) ) );
		}

		/// <summary>
		/// Adds a renderable object/technique
		/// </summary>
		/// <param name="_Renderable"></param>
		public void		AddRenderable( IDepthPassRenderable _Renderable )
		{
			m_Renderables.Add( _Renderable );
		}

		public override void	Render( int _FrameToken )
		{
#if DEBUG
			if ( m_Device.HasProfilingStarted )
				m_Device.AddProfileTask( this, "Depth Pass", "<START>" );
#endif
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.CULL_BACK );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.WRITE_CLOSEST );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				EffectPass		Pass = CurrentMaterial.CurrentTechnique.GetPassByIndex( 0 );
				VariableMatrix	vLocal2World = CurrentMaterial.GetVariableByName( "Local2World" ).AsMatrix;

				int FrameToken = m_FrameToken-10;	// -10 so we can always render objects for the ZPass and so they can render again in normal passes afterward

				foreach ( IDepthPassRenderable Renderable in m_Renderables )
					Renderable.RenderDepthPass( FrameToken, Pass, vLocal2World );
			}

#if DEBUG
			if ( m_Device.HasProfilingStarted )
			m_Device.AddProfileTask( this, "Depth Pass", "<END>" );
#endif
		}

		#endregion
	}
}
