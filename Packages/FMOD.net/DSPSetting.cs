using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace FMODNet
{
    /// <summary>
    /// Class containing the setting for a dsp object
    /// </summary>
    public class DSPSetting
    {
        /// <summary>
        /// Name of the setting
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// gets the minimum value for this setting
        /// </summary>
        public float MinValue { get; internal set; }
        /// <summary>
        /// gets the maximum value for this setting
        /// </summary>
        public float MaxValue { get; internal set; }
        /// <summary>
        /// Gets or sets the current value for this setting
        /// </summary>
        public float CurrentValue
        {
            get
            {
                float paramValue = 0;
                
                device.CallSync((x) => __dsp.getParameter(index, ref paramValue, null, 0));
                return paramValue;

            }
            set
            {
                device.Queue(() => __dsp.setParameter(index, value));
            }
        }

        internal int index = 0;
        internal FMOD.DSP __dsp = new FMOD.DSP();
        /// <summary>
        /// The sound device
        /// </summary>
        protected readonly SoundDevice device;
        internal DSPSetting(SoundDevice device)
        {
            this.device = device;
        }
    }     
}
