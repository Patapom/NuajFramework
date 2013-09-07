using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using FMODNet;

namespace SoundLib
{
	/// <summary>
	/// MP3 Player
	/// </summary>
	public class	MP3Player : IDisposable
	{
		#region FIELDS

		protected SoundDevice	m_Device = null;
		protected Sound			m_Sound = null;
		protected bool			m_bPlaying = false;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the music volume
		/// </summary>
		public float	Volume		{ get { return m_Sound != null ? m_Sound.Channel.Volume : 0.0f; } set { if ( m_Sound != null ) m_Sound.Channel.Volume = value; } }

		/// <summary>
		/// Gets or sets the music pitch frequency
		/// </summary>
		public float	Pitch		{ get { return m_Sound != null ? m_Sound.Channel.Pitch : 0.0f; } set { if ( m_Sound != null ) m_Sound.Channel.Pitch = value; } }

		/// <summary>
		/// Gets or sets the music pan
		/// </summary>
		public float	Pan			{ get { return m_Sound != null ? m_Sound.Channel.Pan : 0.0f; } set { if ( m_Sound != null ) m_Sound.Channel.Pan = value; } }

		/// <summary>
		/// Gets or sets the music position (in milliseconds)
		/// </summary>
		public int		Position
		{
			get { return m_Sound != null ? m_Sound.Channel.Position : 0; }
			set
			{
				if ( m_Sound == null || Math.Abs( value - m_Sound.Channel.Position ) < 10 )
					return;	// No change...

				bool	bIsPlaying = m_bPlaying;
				if ( bIsPlaying )
					Pause();	// Pause

				m_Sound.Channel.Position = value;

				if ( bIsPlaying )
					Play();		// Then restart...
			}
		}

		/// <summary>
		/// Tells if the music is currently playing
		/// </summary>
//		public bool		Playing		{ get { return m_Sound != null && m_Sound.Channel.IsPlaying; } }
		public bool		Playing		{ get { return m_bPlaying; } }

		#endregion

		#region METHODS

		public	MP3Player()
		{
			m_Device = new SoundDevice();

//#if DEBUG
#if false
			SoundDeviceConfiguration	Config = new SoundDeviceConfiguration()
			{
				DebugMode = true,
				DspBufferCount = 4,
				MaximumAllowedSounds = 10,
				StreamBufferSize = 16384,
			};

			m_Device.Initialize( Config );
#else
			m_Device.Initialize();
#endif
		}

		public void	Load( Stream _MP3Stream )
		{
			m_Sound = m_Device.CreateSound( _MP3Stream );
		}

		public void	Play()
		{
			if ( m_Sound == null )
				return;

			m_Sound.Play();
			m_bPlaying = true;
		}

		public void	Pause()
		{
			if ( m_Sound == null )
				return;

			m_Sound.Pause();
			m_bPlaying = false;
		}

		public void	Stop()
		{
			if ( m_Sound == null )
				return;

			m_Sound.Pause();
			m_bPlaying = false;
			Position = 0;
		}

		#region IDisposable Members

		public void Dispose()
		{
			if ( m_Sound != null )
				m_Sound.Stop();
			m_Device.Dispose();
		}

		#endregion

		#endregion
	}
}
