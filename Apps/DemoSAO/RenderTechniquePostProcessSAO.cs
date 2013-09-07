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
	/// Separable Ambient Occlusion post-process
	/// </example>
	public class RenderTechniquePostProcessSAO : RenderTechnique
	{
		#region CONSTANTS

		#endregion

		#region NESTED TYPES

		public enum AO_STATE
		{
			DISABLED,	// No AO
			ENABLED,	// Show scene with AO
			AO_ONLY,	// Show only AO
		}

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
		protected Material<VS_Pt4>			m_Material = null;

		//////////////////////////////////////////////////////////////////////////
		// Textures & Render Targets
		protected RenderTarget<PF_RG16F>	m_AOTarget = null;
		protected ITexture2D				m_SourceBuffer = null;

		//////////////////////////////////////////////////////////////////////////
		// Geometry
		protected Helpers.ScreenQuad		m_Quad = null;

		// Parameters for AO
		protected AO_STATE					m_AOState = AO_STATE.ENABLED;
		protected float						m_AOSphereRadius = 3.4f;
		protected float						m_AOStrength = 0.5f;
		protected float						m_AOFetchScale = 10.0f;

		#endregion

		#region PROPERTIES

		public ITexture2D					SourceBuffer		{ get { return m_SourceBuffer; } set { m_SourceBuffer = value; } }
		public AO_STATE						AOState				{ get { return m_AOState; } set { m_AOState = value; } }
		public float						AOSphereRadius		{ get { return m_AOSphereRadius; } set { m_AOSphereRadius = value; } }
		public float						AOStrength			{ get { return m_AOStrength; } set { m_AOStrength = value; } }
		public float						AOFetchScale		{ get { return m_AOFetchScale; } set { m_AOFetchScale = value; } }

		#endregion

		#region METHODS

		public	RenderTechniquePostProcessSAO( Device _Device, string _Name, int _Width, int _Height ) : base( _Device, _Name )
		{
			//////////////////////////////////////////////////////////////////////////
			// Create our main material
			m_Material = ToDispose( new Material<VS_Pt4>( m_Device, "SAO Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/SAO/PostProcessSAO.fx" ) ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the fullscreen quad
			m_Quad = ToDispose( new Helpers.ScreenQuad( m_Device, "SAO Quad" ) );

			//////////////////////////////////////////////////////////////////////////
			// Create the temp targets in which to compute the AO
			m_AOTarget = ToDispose( new RenderTarget<PF_RG16F>( m_Device, "SAO Temp Target", _Width, _Height, 1 ) );
		}

		public override void	Render( int _FrameToken )
		{
 			m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
			m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
			m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );

			using ( m_Material.UseLock() )
			{
				//////////////////////////////////////////////////////////////////////////
				// Compute SAO terms
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "ComputeSAO" );
				m_Device.SetRenderTarget( m_AOTarget );

				CurrentMaterial.GetVariableByName( "InvSourceSize" ).AsVector.Set( m_SourceBuffer.InvSize3 );
				CurrentMaterial.GetVariableByName( "AOBufferToZBufferRatio" ).AsVector.Set( new Vector3( (float) m_Device.DefaultRenderTarget.Width / m_AOTarget.Width, (float) m_Device.DefaultRenderTarget.Height / m_AOTarget.Height, 0.0f ) );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();


				//////////////////////////////////////////////////////////////////////////
				// Apply SAO
				CurrentMaterial.CurrentTechnique = CurrentMaterial.GetTechniqueByName( "DisplaySAO" );
				m_Device.SetDefaultRenderTarget();

				CurrentMaterial.GetVariableByName( "InvSourceSize" ).AsVector.Set( m_SourceBuffer.InvSize3 );
				CurrentMaterial.GetVariableByName( "SourceBuffer" ).AsResource.SetResource( m_SourceBuffer );
				CurrentMaterial.GetVariableByName( "AOBuffer" ).AsResource.SetResource( m_AOTarget );
				CurrentMaterial.GetVariableByName( "AOState" ).AsScalar.Set( (int) m_AOState );
				CurrentMaterial.GetVariableByName( "AOSphereRadius" ).AsScalar.Set( m_AOSphereRadius );
				CurrentMaterial.GetVariableByName( "AOFetchScale" ).AsScalar.Set( 0.001f * m_AOFetchScale );
				CurrentMaterial.GetVariableByName( "AOStrength" ).AsScalar.Set( m_AOStrength );

				CurrentMaterial.ApplyPass( 0 );
				m_Quad.Render();
			}
		}

		#endregion
	}
}
