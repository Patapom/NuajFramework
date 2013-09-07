using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using FMOD;
using System.Diagnostics;

namespace FMODNet
{
    
    /// <summary>
    /// Base class used to create DSP objects.
    /// </summary>
    /// <remarks>Unlike the abstract class DSP, it provides some methods that the sound device will call eventually while the audio is playing</remarks>
    public abstract class DSPBase : DSP
    {        
        internal RESULT READCALLBACK(ref FMOD.DSP_STATE dsp_state, IntPtr inbuf, IntPtr outbuf, uint length, int inchannels, int outchannels)
        {
            return this.OnRead(inbuf, outbuf, inchannels, outchannels, length);
        }
        internal RESULT RELEASECALLBACK(ref DSP_STATE state)
        {
            Debug.WriteLine("Releasing DSP");
            releaseCallbacks.Remove(RELEASECALLBACK);
            readCallbacks.Remove(READCALLBACK);
            return RESULT.OK;
        }

        internal FMOD.DSP dsp;
        static List<DSP_READCALLBACK> readCallbacks = new List<DSP_READCALLBACK>();
        static List<DSP_RELEASECALLBACK> releaseCallbacks = new List<DSP_RELEASECALLBACK>();
        /// <summary>
        /// Passes the sound object to the base class
        /// </summary>
        /// <param name="device">Instance of the sound device</param>
        protected DSPBase(SoundDevice device)
            : base(device)
        {
        }

        internal override sealed FMOD.DSP getDsp()
        {
            if (dsp != null)
                return dsp;

            dsp = new FMOD.DSP();
            device.CallSync(x => 
                {

                    DSP_RELEASECALLBACK rel = new DSP_RELEASECALLBACK(this.RELEASECALLBACK);
                    DSP_READCALLBACK read = new DSP_READCALLBACK(this.READCALLBACK);
                    releaseCallbacks.Add(rel);
                    readCallbacks.Add(read);
                DSP_DESCRIPTION desc = new DSP_DESCRIPTION
                {
                    read = read,
                    release = rel,
                    name = this.Name.PadRight(32).Substring(0, 32).ToCharArray(),
                    channels = 0
                };
                device.SafeCall(() => x.createDSP(ref desc, ref this.dsp), false);

                });
            return dsp;
        }
        /// <summary>
        /// Reading and output manipulating callback. The sound device will call this method while the audio is playing.
        /// The default operation is to copy the input into the output with no changes in the data.
        /// </summary>
        /// <param name="input">Pointer that contains the input data</param>
        /// <param name="output">Pointer that contains the output data</param>
        /// <param name="inChannels">Number of channels in the input</param>
        /// <param name="outChannels">Number of channels to output</param>
        /// <param name="length">The length (per channel) that the pointer contains</param>
        /// <returns>returns a enum value containing the result of operation.</returns>
        public virtual RESULT OnRead(IntPtr input, IntPtr output, int inChannels, int outChannels, uint length)
        {
            unsafe
            {
                float* inbuffer = (float*)input.ToPointer();
                float* outbuffer = (float*)output.ToPointer();
                int count, count2;
                for (count = 0; count < length; count++)
                {
                    for (count2 = 0; count2 < outChannels; count2++)
                    {
                        float x = inbuffer[(count * inChannels) + count2];
                        outbuffer[(count * outChannels) + count2] = x;
                    }
                }
            }
            return RESULT.OK;
        }
        /// <summary>
        /// Enqueues a new operation to be called as soon as possible
        /// </summary>
        /// <param name="action">The method or operation to be executed</param>
        /// <param name="async">Set the async mode of the action. If set to false, the method will be blocked until the operation is finished the execution</param>
        protected void Enqueue(Action action, bool async)
        {
            if (async)
                device.Queue(action);
            else
            {
                device.CallSync(x=>action());
            }
        }
    }
}
