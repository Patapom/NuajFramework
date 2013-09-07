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
	/// Volumetric fog with lighting
	/// </example>
	public class PostProcessFog : RenderTechniqueBase
	{
		#region CONSTANTS

		#endregion

		#region FIELDS

		//////////////////////////////////////////////////////////////////////////
		// Materials
 		protected Material<VS_Pt4V3T2>		m_MaterialPostProcess = null;

		//////////////////////////////////////////////////////////////////////////
		// Objects

		//////////////////////////////////////////////////////////////////////////
		// Textures & RenderTargets
		protected Texture3D<PF_RG16F>		m_VolumeFogTexture = null;

		//////////////////////////////////////////////////////////////////////////
		// Parameters for animation
		protected float						m_FogHeight = 2.0f;
		protected float						m_FogDepthStart = 0.0f;
		protected float						m_FogDepthEnd = -16.0f;

		protected float						m_ExtinctionFactor = 1.0f;
		protected float						m_InScatteringFactor = 1.0f;
		protected float						m_ScatteringAnisotropy = 0.6f;

		#endregion

		#region PROPERTIES

		public float						FogHeight				{ get { return m_FogHeight; } set { m_FogHeight = value; } }
		public float						FogDepthStart			{ get { return m_FogDepthStart; } set { m_FogDepthStart = value; } }
		public float						FogDepthEnd				{ get { return m_FogDepthEnd; } set { m_FogDepthEnd = value; } }

		public float						ExtinctionFactor		{ get { return m_ExtinctionFactor; } set { m_ExtinctionFactor = value; } }
		public float						ScatteringAnisotropy	{ get { return m_ScatteringAnisotropy; } set { m_ScatteringAnisotropy = value; } }
		public float						InScatteringFactor		{ get { return m_InScatteringFactor; } set { m_InScatteringFactor = value; } }

		#endregion

		#region METHODS

		public	PostProcessFog( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer, _Name )
		{
m_bEnabled = false;

			// Create our main materials
 			m_MaterialPostProcess = m_Renderer.LoadMaterial<VS_Pt4V3T2>( "Post-Process Motion Blur Material", ShaderModel.SM4_0, new System.IO.FileInfo( "FX/WaterColour/PostProcessFog.fx" ) );

			// Create the volume fog 3D texture
			CreateVolumeFogTexture( new System.IO.FileInfo( "./Data/WaterColour/VolumeFog0.fog" ) );
		}

		public override void	Render( int _FrameToken )
		{
			if ( !m_bEnabled )
				return;

			using ( m_MaterialPostProcess.UseLock() )
			{
				m_Device.SetStockRasterizerState( Device.HELPER_STATES.NO_CULLING );
				m_Device.SetStockDepthStencilState( Device.HELPER_DEPTH_STATES.DISABLED );
				m_Device.SetStockBlendState( Device.HELPER_BLEND_STATES.DISABLED );
				m_Renderer.SetFinalRenderTarget();	// Should render in MaterialBuffer2

				CurrentMaterial.GetVariableByName( "Time" ).AsScalar.Set( m_Renderer.Time );
				CurrentMaterial.GetVariableByName( "FogHeight" ).AsScalar.Set( m_FogHeight );
				CurrentMaterial.GetVariableByName( "FogDepthStart" ).AsScalar.Set( m_FogDepthStart );
				CurrentMaterial.GetVariableByName( "FogDepthEnd" ).AsScalar.Set( m_FogDepthEnd );
				CurrentMaterial.GetVariableByName( "ExtinctionFactor" ).AsScalar.Set( m_ExtinctionFactor );
				CurrentMaterial.GetVariableByName( "InScatteringFactor" ).AsScalar.Set( m_InScatteringFactor );
				CurrentMaterial.GetVariableByName( "ScatteringAnisotropy" ).AsScalar.Set( m_ScatteringAnisotropy );
				CurrentMaterial.GetVariableByName( "VolumeFogTexture" ).AsResource.SetResource( m_VolumeFogTexture );

				CurrentMaterial.ApplyPass( 0 );
				m_Renderer.RenderPostProcessQuad();
				m_Renderer.SwapFinalRenderTarget();
			}
		}

		protected void	CreateVolumeFogTexture( System.IO.FileInfo _VolumeFogFileName )
		{
			// Read the data into a table
			Vector2[,,]	FogTable = null;
			m_Renderer.ReadBinaryFile( _VolumeFogFileName, ( Reader ) =>
				{
					int	SizeX = Reader.ReadInt32();
					int	SizeY = Reader.ReadInt32();
					int	SizeZ = Reader.ReadInt32();

					FogTable = new Vector2[SizeX,SizeY,SizeZ];

					for ( int X=0; X < SizeX; X++ )
						for ( int Y=0; Y < SizeY; Y++ )
							for ( int Z=0; Z < SizeZ; Z++ )
							{
								FogTable[X,Y,Z].X = Reader.ReadSingle();
								FogTable[X,Y,Z].Y = Reader.ReadSingle();
							}
				} );

			// Build the texture from the table
			using ( Image3D<PF_RG16F> VolumeFogImage = new Image3D<PF_RG16F>( m_Device, "VolumeFogImage", FogTable.GetLength(0), FogTable.GetLength(1), FogTable.GetLength(2), ( int _X, int _Y, int _Z, ref Vector4 _Color ) =>
				{
					_Color.X = FogTable[_X,_Y,_Z].X;
					_Color.Y = FogTable[_X,_Y,_Z].Y;
				}, 1 ) )
			m_VolumeFogTexture = ToDispose( new Texture3D<PF_RG16F>( m_Device, "VolumeFog", VolumeFogImage ) );
		}

		#endregion
	}
}
