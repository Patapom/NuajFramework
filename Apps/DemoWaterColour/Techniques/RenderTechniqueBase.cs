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
	/// Base render technique
	/// </example>
	public abstract class RenderTechniqueBase : RenderTechnique
	{
		#region FIELDS

		protected RendererSetupDemo		m_Renderer = null;
		protected bool					m_bEnabled = false;

		#endregion

		#region PROPERTIES

		public bool						Enabled					{ get { return m_bEnabled; } set { m_bEnabled = value; } }

		#endregion

		#region METHODS

		public RenderTechniqueBase( RendererSetupDemo _Renderer, string _Name ) : base( _Renderer.Device, _Name )
		{
			m_Renderer = _Renderer;
		}

		#endregion
	}
}
