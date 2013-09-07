using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using FMOD;
using System.Diagnostics;
using System.Threading;

namespace FMODNet
{
    /// <summary>
    /// Represents an audio into the sound subsystem.
    /// </summary>
    public class Channel
    {
        FMOD.Channel _ch;
        float baseFrequency;
        SoundDevice device;
        float _pitch = 0;
        uint lastPos = 0;

        internal Channel(FMOD.Channel ch, SoundDevice device)
        {
            this.device = device;
            _ch = ch;
            device.CallSync(c => _ch.getFrequency(ref baseFrequency));
        }
        /// <summary>
        /// Gets or sets the balance value for the specified channel. -1.0f represents left open and right closed. 1.0f represents right open and left close.
        /// 0.0f represents all sides open.
        /// </summary>
        public float Pan
        {
            get
            {
                float _pan = 0;
                device.CallSync(c => _ch.getPan(ref _pan));
                return _pan;
            }
            set
            {
                device.Queue(c => _ch.setPan(value));
            }
        }
        /// <summary>
        /// Gets or sets the frequency used to play the audio data
        /// </summary>
        public float Pitch
        {
            get
            {
                return _pitch;
            }
            set
            {
                device.CallSync(c => device.SafeCall(() => _ch.setFrequency(baseFrequency + value), false));
                _pitch = value;
            }
        }
        /// <summary>
        /// Gets or sets the position for the audio in ms.
        /// </summary>
        public int Position
        {
            get
            {
                //if (this.IsPlaying)
                {
                    
                    device.CallSync(c => device.SafeCall(()=>_ch.getPosition(ref lastPos, TIMEUNIT.MS), false), 100);
                    return (int)lastPos;
                }
               // return (int)lastPos;
                
            }
            set
            {

                device.CallSync(c => device.SafeCall(() => _ch.setPosition((uint)value, TIMEUNIT.MS), true));
            }
        }
        /// <summary>
        /// Gets or sets the volume for the audio
        /// </summary>
        public float Volume
        {
            get
            {
                float _volume = 0;

                device.CallSync(c => _ch.getVolume(ref _volume));
                return _volume;
            }
            set
            {
                if(_ch != null)
                    device.Queue(c => _ch.setVolume(value));
            }
        }
        /// <summary>
        /// Gets the Playing state for the audio data.
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                if (_ch == null)
                    return false;
                bool isPlaying = false;
                device.CallSync(c => device.SafeCall(() => 
                    {
                        RESULT res = _ch.isPlaying(ref isPlaying);
                        
                        return res;

                    }, false));


                return isPlaying;
            }
        }        
    }
}
