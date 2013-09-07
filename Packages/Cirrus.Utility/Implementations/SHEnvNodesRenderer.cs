using System;
using System.Collections.Generic;
using System.Linq;

using Nuaj;
using Nuaj.Cirrus;
using SharpDX;

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// This simple class shows how to render a set of SH Environment Nodes already existing in the SHEnvMapManager class
	/// </summary>
	public class SHEnvNodesRenderer
	{
		/// <summary>
		/// Renders the SH Environment Nodes
		/// </summary>
		/// <param name="_EnvMapManager">The manager hosting the nodes to render</param>
		/// <param name="_CubeMapsRenderer">The cube map renderer that will be able to render our environment nodes creation</param>
		/// <param name="_CubeMapSize">The size of the cube map used to sample the environment</param>
		/// <param name="_NearClip">Near clip for cube maps rendering</param>
		/// <param name="_FarClip">Far clip for cube maps rendering</param>
		/// <param name="_IndirectLightingBoostFactor">The boost factor to apply to indirect lighting (default is 1)</param>
		/// <param name="_PassesCount">The amount of passes to render (1=direct only, 2=Direct+Indirect single bounce, 3=Direct + 2 indirect bounces, etc.)</param>
		public static void		RenderSHEnvironmentNodes( SHEnvMapManager _EnvMapManager, ISHCubeMapsRenderer _CubeMapsRenderer, int _CubeMapsSize, float _NearClip, float _FarClip, float _IndirectLightingBoostFactor, int _PassesCount )
		{
			if ( _PassesCount < 1 )
				throw new Exception( "You must at least specify one rendering pass !" );

			_EnvMapManager.BeginEnvironmentRendering( _CubeMapsRenderer, _CubeMapsSize, _NearClip, _FarClip, _IndirectLightingBoostFactor );

			// =============================================================
			// Render multiple passes
			SHEnvMapManager.EnvironmentNode[]	EnvNodes = _EnvMapManager.EnvironementNodes;
			Vector4[][]		SHCoefficients = new Vector4[EnvNodes.Length][];
			Vector4[][]		SHCoefficientsAcc = new Vector4[EnvNodes.Length][];
			for ( int EnvNodeIndex=0; EnvNodeIndex < EnvNodes.Length; EnvNodeIndex++ )
				SHCoefficientsAcc[EnvNodeIndex] = new Vector4[9];

			for ( int PassIndex=0; PassIndex < _PassesCount; PassIndex++ )
			{
				// Compute SH coefficients for each node
				for ( int EnvNodeIndex=0; EnvNodeIndex < EnvNodes.Length; EnvNodeIndex++ )
				{
					SHEnvMapManager.EnvironmentNode	EnvNode = EnvNodes[EnvNodeIndex];

					// Render the cube map
					_EnvMapManager.RenderCubeMap( EnvNode.V.Position, new Vector3( 0.0f, 0.0f, 1.0f ), Vector3.UnitY );
			
					// Encode into SH
					if ( PassIndex == 0 )
						SHCoefficients[EnvNodeIndex] = _EnvMapManager.EncodeSHEnvironmentDirect();
					else
						SHCoefficients[EnvNodeIndex] = _EnvMapManager.EncodeSHEnvironmentIndirect( PassIndex );
				}

				// Update coefficients for next pass & accumulate
				for ( int EnvNodeIndex=0; EnvNodeIndex < EnvNodes.Length; EnvNodeIndex++ )
				{
					SHEnvMapManager.EnvironmentNode	EnvNode = EnvNodes[EnvNodeIndex];

					EnvNode.UpdateCoefficients( SHCoefficients[EnvNodeIndex] );
					for ( int SHCoeffIndex=0; SHCoeffIndex < 9; SHCoeffIndex++ )
						SHCoefficientsAcc[EnvNodeIndex][SHCoeffIndex] += SHCoefficients[EnvNodeIndex][SHCoeffIndex];
				}
			}

			// =============================================================
			// Update with accumulated coefficients for result
			for ( int EnvNodeIndex=0; EnvNodeIndex < EnvNodes.Length; EnvNodeIndex++ )
				EnvNodes[EnvNodeIndex].UpdateCoefficientsReflected( SHCoefficientsAcc[EnvNodeIndex] );

			_EnvMapManager.EndEnvironmentRendering();

			// All you have to do is save the nodes now...
		}
	}
}
