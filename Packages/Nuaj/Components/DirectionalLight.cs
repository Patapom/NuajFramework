using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;

namespace Nuaj
{
	/// <summary>
	/// The DirectionalLight class doesn't wrap any DirectX component per-se but helps a lot to handle
	///  basic orientation and color management
	/// </summary>
	public class DirectionalLight : PointLight
	{
		#region FIELDS

		protected Vector3	m_Direction = Vector3.UnitY;

		protected bool		m_bMainLight = true;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the light direction (the direction pointing TOWARD the light)
		/// </summary>
		public Vector3		Direction
		{
			get { return m_Direction; }
			set
			{
				m_Direction = value;
				m_Direction.Normalize();

				// Notify
				if ( LightDirectionChanged != null )
					LightDirectionChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the minimum radius
		/// </summary>
		public float		RadiusMin
		{
			get { return m_Data.Z; }
			set
			{
				m_Data.Z = value;
				if ( RadiusChanged != null )
					RadiusChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the maximum radius
		/// </summary>
		public float		RadiusMax
		{
			get { return m_Data.W; }
			set
			{
				m_Data.W = value;
				if ( RadiusChanged != null )
					RadiusChanged( this, EventArgs.Empty );
			}
		}

		public event EventHandler	LightDirectionChanged;
		public event EventHandler	RadiusChanged;

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default light
		/// </summary>
		/// <remarks>IMPORTANT : Don't forget to ACTIVATE the light once it's created otherwise materials won't have their light settings !</remarks>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_bIsMainLight">True if main light, false if rim light</param>
		public	DirectionalLight( Device _Device, string _Name, bool _bIsMainLight ) : base( _Device, _Name )
		{
			m_bMainLight = _bIsMainLight;
		}

		/// <summary>
		/// Makes that camera the active one (i.e. now providing projection matrices)
		/// </summary>
		public override void	Activate()
		{
			if ( !m_bActive )
				m_Device.RegisterShaderInterfaceProvider( m_bMainLight ? typeof(IDirectionalLight) : typeof(IDirectionalLight2), this );
			m_bActive = true;
		}

		/// <summary>
		/// Makes that camera inactive (i.e. not providing projection matrices anymore)
		/// </summary>
		public override void	DeActivate()
		{
			if ( m_bActive )
				m_Device.UnRegisterShaderInterfaceProvider( m_bMainLight ? typeof(IDirectionalLight) : typeof(IDirectionalLight2), this );
			m_bActive = false;
		}

		#region IShaderInterfaceProvider Members

		public override void ProvideData( IShaderInterface _Interface )
		{
			IDirectionalLight	Light = _Interface as IDirectionalLight;

			Light.Position = m_Position;
			Light.Direction = m_Direction;
			Light.Color = m_Color;
			Light.Data = m_Data;
		}

		#endregion

		#endregion
	}
}
