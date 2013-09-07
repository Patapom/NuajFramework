using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;

namespace Nuaj
{
	/// <summary>
	/// The PointLight class doesn't wrap any DirectX component per-se but helps a lot to handle
	///  basic orientation and color management
	/// </summary>
	public class PointLight : Component, IShaderInterfaceProvider
	{
		#region FIELDS

		protected Vector3	m_Position = Vector3.Zero;
		protected Vector4	m_Color = (Vector4) (Color4) System.Drawing.Color.White;
		protected Vector4	m_Data = Vector4.Zero;

		protected bool		m_bActive = false;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the light position
		/// </summary>
		public Vector3		Position
		{
			get { return m_Position; }
			set
			{
				m_Position = value;

				// Notify
				if ( LightPositionChanged != null )
					LightPositionChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the light color
		/// </summary>
		public Vector4		Color
		{
			get { return m_Color; }
			set
			{
				m_Color = value;

				// Notify
				if ( LightColorChanged != null )
					LightColorChanged( this, EventArgs.Empty );
			}
		}

		protected static readonly Vector3	LUMINANCE = new Vector3( 0.2126f, 0.7152f, 0.0722f );

		/// <summary>
		/// Gets or sets the light intensity
		/// </summary>
		public float		Intensity
		{
			get { return Vector3.Dot( (Vector3) m_Color, LUMINANCE ); }
			set
			{
				float	CurrentIntensity = Intensity;
				if ( Math.Abs( CurrentIntensity ) > 1e-4f )
					m_Color = new Vector4( (Vector3) m_Color * value / CurrentIntensity, m_Color.W );
				else
					m_Color = new Vector4( value, value, value, 1.0f );
			}
		}

		/// <summary>
		/// Gets or sets the minimum range
		/// </summary>
		public float		RangeMin
		{
			get { return m_Data.X; }
			set
			{
				m_Data.X = value;
				if ( RangeChanged != null )
					RangeChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the maximum range
		/// </summary>
		public float		RangeMax
		{
			get { return m_Data.Y; }
			set
			{
				m_Data.Y = value;
				if ( RangeChanged != null )
					RangeChanged( this, EventArgs.Empty );
			}
		}

		public Vector4		CachedData
		{
			get { return m_Data; }
		}

		public event EventHandler	LightPositionChanged;
		public event EventHandler	LightColorChanged;
		public event EventHandler	RangeChanged;

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default light
		/// </summary>
		/// <remarks>IMPORTANT : Don't forget to ACTIVATE the light once it's created otherwise materials won't have their light settings !</remarks>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	PointLight( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		/// <summary>
		/// Makes that camera the active one (i.e. now providing projection matrices)
		/// </summary>
		public virtual void	Activate()
		{
			if ( !m_bActive )
				m_Device.RegisterShaderInterfaceProvider( typeof(IPointLight), this );
			m_bActive = true;
		}

		/// <summary>
		/// Makes that camera inactive (i.e. not providing projection matrices anymore)
		/// </summary>
		public virtual void	DeActivate()
		{
			if ( m_bActive )
				m_Device.UnRegisterShaderInterfaceProvider( typeof(IPointLight), this );
			m_bActive = false;
		}

		public override void Dispose()
		{
			DeActivate();	// De-activate if active...
			base.Dispose();
		}

		#region IShaderInterfaceProvider Members

		public virtual void ProvideData( IShaderInterface _Interface )
		{
			IPointLight	Light = _Interface as IPointLight;

			Light.Position = m_Position;
			Light.Color = m_Color;
			Light.Data = m_Data;
		}

		#endregion

		#endregion
	}
}
