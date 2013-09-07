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
	/// Base Rendering Technique
	/// </example>
	public abstract class RenderTechniqueBase : RenderTechnique
	{
		#region FIELDS

		protected RendererSetup			m_Renderer = null;

		#endregion

		#region METHODS

		public	RenderTechniqueBase( RendererSetup _Renderer, string _Name ) : base( _Renderer.Device, _Name )
		{
			m_Renderer = _Renderer;
		}

		#endregion
	}
}
