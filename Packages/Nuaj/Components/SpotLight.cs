using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;

namespace Nuaj
{
	/// <summary>
	/// The SpotLight class doesn't wrap any DirectX component per-se but helps a lot to handle
	///  basic orientation and color management
	/// </summary>
	public class SpotLight : PointLight
	{
		#region FIELDS

		protected Vector3	m_Direction = Vector3.UnitY;
		protected Vector4	m_Data2 = Vector4.Zero;

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
		/// Gets or sets the minimum cone angle
		/// </summary>
		public float		ConeAngleMin
		{
			get { return m_Data.Z; }
			set
			{
				m_Data.Z = value;
				m_Data2.X = (float) Math.Cos( 0.5 * value );
				if ( ConeAngleChanged != null )
					ConeAngleChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the maximum cone angle
		/// </summary>
		public float		ConeAngleMax
		{
			get { return m_Data.W; }
			set
			{
				m_Data.W = value;
				m_Data2.Y = (float) Math.Cos( 0.5 * value );
				if ( ConeAngleChanged != null )
					ConeAngleChanged( this, EventArgs.Empty );
			}
		}

		public Vector4		CachedData2
		{
			get { return m_Data2; }
		}

		public event EventHandler	LightDirectionChanged;
		public event EventHandler	ConeAngleChanged;

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default light
		/// </summary>
		/// <remarks>IMPORTANT : Don't forget to ACTIVATE the light once it's created otherwise materials won't have their light settings !</remarks>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	SpotLight( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		/// <summary>
		/// Makes that camera the active one (i.e. now providing projection matrices)
		/// </summary>
		public override void	Activate()
		{
			if ( !m_bActive )
				m_Device.RegisterShaderInterfaceProvider( typeof(ISpotLight), this );
			m_bActive = true;
		}

		/// <summary>
		/// Makes that camera inactive (i.e. not providing projection matrices anymore)
		/// </summary>
		public override void	DeActivate()
		{
			if ( m_bActive )
				m_Device.UnRegisterShaderInterfaceProvider( typeof(ISpotLight), this );
			m_bActive = false;
		}

		#region IShaderInterfaceProvider Members

		public override void ProvideData( IShaderInterface _Interface )
		{
			ISpotLight	Light = _Interface as ISpotLight;

			Light.Position = m_Position;
			Light.Direction = m_Direction;
			Light.Color = m_Color;
			Light.Data = m_Data;
			Light.Data2 = m_Data2;
		}

		#endregion

		#endregion
	}
}
