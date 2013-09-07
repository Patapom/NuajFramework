using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace FMODNet
{
    /*
    [Obsolete("Not implemented", true)]
    public class ExternalDSP : DSP
    {
        uint handle;
        public ExternalDSP(uint handle, SoundDevice device)
            : base(device)
        {
            this.handle = handle;
        }
        FMOD.DSP dsp;
        internal override FMOD.DSP getDsp()
        {
            if (dsp == null)
            {
                dsp = new FMOD.DSP();
                IntPtr ptr = dsp.getRaw();
                device.Queue(x => device.SafeCall(()=>x.createDSPByPlugin(handle, ref ptr), true), false);
                dsp.setRaw(ptr);
                loadParameters();
            }
            return dsp;
        }
    }*/
    /// <summary>
    /// Base class used to manipulate dsp objects into the sound subsystem
    /// </summary>
    public abstract class DSP
    {
        /// <summary>
        /// List containing the available settings for specified dsp
        /// </summary>
        public List<DSPSetting> Settings { get; protected set; }
        /// <summary>
        /// Method called when the dsps is created. This method will populated the Settings properties
        /// </summary>
        protected virtual void loadParameters()
        {
            this.device.CallSync(c =>
                {
                    this.Settings = new List<DSPSetting>();
                    int paramCount = 0;
                    this.getDsp().getNumParameters(ref paramCount);
                    for (int i = 0; i < paramCount; i++)
                    {
                        StringBuilder name = new StringBuilder(1024);
                        float min = 0; 
                        float max = 0;
                        device.SafeCall(()=>this.getDsp().getParameterInfo(i, name, null, null, 0, ref min, ref max), true);
                        DSPSetting set = this.CreateSetting();
                        set.Name = name.ToString();
                        set.MinValue = min;
                        set.MaxValue = max;
                        this.Settings.Add(set);
                    }
                });
        }

        internal abstract FMOD.DSP getDsp();
        /// <summary>
        /// Current Sound device
        /// </summary>
        protected readonly SoundDevice device;
        /// <summary>
        /// Creates a new instance of an DSP object
        /// </summary>
        /// <param name="device"></param>
        protected DSP(SoundDevice device)
        {
            this.device = device;
        }
        string _name;
        /// <summary>
        /// The name of dsp object
        /// </summary>
        public virtual string Name
        {
            get
            {
                if (_name != null)
                    return _name;
                StringBuilder sb = new StringBuilder(32);
                device.CallSync((x) =>
                {
                    uint ver = 0;
                    int nch = 0, cw = 0, ch = 0;
                    this.getDsp().getInfo(sb, ref ver, ref nch, ref cw, ref ch);
                });
                return _name = sb.ToString();
            }
        }
        /// <summary>
        /// If set to true, the dsp will be used by the output during audio execution
        /// </summary>
        public bool Active
        {
            get
            {
                bool active = false;
                device.CallSync((x) =>
                {
                    this.getDsp().getActive(ref active);
                });
                return active;
            }
            set
            {
                device.CallSync((x) => this.getDsp().setActive(value));
            }
        }
        /// <summary>
        /// If set to true, the dsp will not be used to change the output's audio
        /// </summary>
        public bool Bypass
        {
            get
            {
                bool val = false;
                device.CallSync((x) =>
                        this.getDsp().getBypass(ref val)
                    );
                return val;
            }
            set
            {
                device.Queue(() => this.getDsp().setBypass(value));
            }
        }
        internal void Release()
        {
            device.Queue(() =>
            {
                try
                {
                    this.getDsp().remove();
                }
                catch
                {
                }
                try
                {
                    this.getDsp().release();
                }
                catch
                {
                }
            });
        }
        /// <summary>
        /// Create a new DSP setting
        /// </summary>
        /// <returns>returns a new instance of DSPSetting object</returns>
        protected DSPSetting CreateSetting()
        {
            return new DSPSetting(this.device);
        }
    }
}
