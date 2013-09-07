using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using FMOD;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Threading;

namespace FMODNet
{



    /*
    public class Equalizer : DSP
    {
        FMOD.DSP __currentDSP;

        internal override FMOD.DSP getDsp()
        {
            return __currentDSP;
        }

        public Equalizer(SoundDevice device) : base(device)
        {
            __currentDSP = new FMOD.DSP();

            device.Queue(x =>
            {
                device.SafeCall(() => x.createDSPByType(DSP_TYPE.PARAMEQ, ref __currentDSP), false);
                loadParameters();
            }, false);
        }

        protected override sealed void loadParameters()
        {
            Settings = new List<DSPSetting>();
            int parameters = 0;
            __currentDSP.getNumParameters(ref parameters);
            for (int i = 0; i < parameters; i++)
            {
                StringBuilder name = new StringBuilder(1024);
                float min = 0;
                float max = 0;
                __currentDSP.getParameterInfo(i, name, null, null, 0, ref min, ref max);

                DSPSetting setting = CreateSetting();
                setting.MaxValue = max;
                setting.MinValue = min;
                setting.Name = name.ToString();
                setting.index = i;
                setting.__dsp = __currentDSP;
                setting.CurrentValue = 0;
                Settings.Add(setting);
            }
        }
    }*/
    /// <summary>
    /// Class containing implementation for audio playback. 
    /// </summary>
    public sealed class Sound
    {
        internal SoundDevice device;
        FileSystem fileSystem;
        internal FMOD.Sound sound;
        MODE openMode = MODE.CREATESTREAM | MODE.MPEGSEARCH | MODE.IGNORETAGS | MODE.ACCURATETIME | MODE.SOFTWARE;
        FMOD.Channel ch;
        CREATESOUNDEXINFO _info;
        Channel _channel;
        uint len;
        List<DSP> dsps = new List<DSP>();
        DSPConnection con = new DSPConnection();
        bool addedCallback = false;
        Dictionary<uint, SoundCallback> sndCallbacks = new Dictionary<uint, SoundCallback>();
        System.Timers.Timer timer = null;
        /// <summary>
        /// Add a new listener wich will be called when the position is reached during playback. After called it is removed from the pool
        /// </summary>
        /// <param name="position">Sound position in ms</param>
        /// <param name="callback">The method to be called when the position is reached</param>
        public void AddCallback(uint position, SoundCallback callback)
        {
            if (!addedCallback)
            {
                timer.Start();
                addedCallback = true;

            }
            sndCallbacks.Add(position, callback);
        }
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Channel ch = this.Channel;
            if (this.ch == null || ch == null)
            {
                return;
            }
            uint position = (uint)ch.Position;
            uint[] keys = new List<uint>(sndCallbacks.Keys).ToArray();
            foreach (uint key in keys)
            {
                if (position >= key)
                {
                    if (Monitor.TryEnter(sndCallbacks))
                    {
                        if (sndCallbacks.Count > 0)
                        {
                            SoundCallback cbk = null;
                            try
                            {
                                cbk = sndCallbacks[key];

                                sndCallbacks.Remove(key);
                            }
                            catch
                            {
                                Trace.WriteLine(string.Format("Error while reading sound callback at position {0}.", key));
                            }
                            if (cbk != null)
                            {
                                try
                                {
                                    cbk(this, key);
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine(string.Format("Error executing user callback method at position {0}. Error description: {1}", key, ex));
                                }
                            }
                            

                        }
                        Monitor.Exit(sndCallbacks);
                    }
                }
            }
            
            if (sndCallbacks.Count == 0)
            {
                addedCallback = false;

                timer.Stop();
            }

            
        }
        /// <summary>
        /// Gets the channel used for this sound to play the audio data.
        /// </summary>
        public Channel Channel
        {
            get { return _channel; }
        }
        /// <summary>
        /// Gets the audio length.
        /// </summary>
        public int Length
        {
            get
            {
                if (len > 0)
                {
                    return (int)len;
                }
                len = 0;
                if (sound != null)
                {
                    device.CallSync(c => device.SafeCall(() => sound.getLength(ref len, TIMEUNIT.MS), true));
                }
                else
                {
                    return int.MaxValue;
                }
                return (int)len;
            }
        }
        /// <summary>
        /// Gets or sets the specified name for this sound
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Returns the Tag value (using ID3 tags or wma tags)
        /// </summary>
        /// <param name="tagName">Tag to be read</param>
        /// <returns>returns a string containing the tag value</returns>
        public string GetTagValue(string tagName)
        {
            if ((this.openMode & MODE.IGNORETAGS) == MODE.IGNORETAGS)
            {
                return "";
            }
            string ret = "";
            if (this.sound == null)
                return ret;
            device.CallSync(x =>
                {
                    if (this.sound == null)
                    {
                        return;
                    }
                    TAG tg = new TAG();
                    lock (this.sound)
                    {
                        if (device.SafeCall(() => this.sound.getTag(tagName, 0, ref tg), false) == RESULT.OK)
                        {
                            switch (tg.datatype)
                            {
                                case TAGDATATYPE.STRING:
                                case TAGDATATYPE.STRING_UTF16:
                                case TAGDATATYPE.STRING_UTF16BE:
                                case TAGDATATYPE.STRING_UTF8:
                                    ret = Marshal.PtrToStringAnsi(tg.data);
                                    break;
                            }
                        }
                    }
                    ;
                });

            return ret;
        }
        /// <summary>
        /// Reads the audio data and returns all tags found
        /// </summary>
        /// <returns>Returns a string array containing all tags found in the audio data.</returns>
        public string[] GetAllTags()
        {
            if ((this.openMode & MODE.IGNORETAGS) == MODE.IGNORETAGS)
            {
                return new string[] { };
            }
            TAG tg = new TAG();
            int numTags = 0;
            int numTagsUpdated = 0;
            List<string> returnTags = new List<string>();
            device.CallSync(x =>
                {
                    if (sound == null)
                        return;
                    lock (sound)
                    {
                        device.SafeCall(() => this.sound.getNumTags(ref numTags, ref numTagsUpdated), true);
                        for (int i = 0; i < numTags; i++)
                        {
                            device.SafeCall(() => this.sound.getTag(null, i, ref tg), true);
                            returnTags.Add(tg.name);
                        }
                    }

                });
            return returnTags.ToArray();
        }

        
        internal Sound(FileSystem fileManipulationSystem, SoundDevice device)
        {
            timer = new System.Timers.Timer(23);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            
            this.device = device;
            fileSystem = fileManipulationSystem;
            _info = new CREATESOUNDEXINFO
            {
                userclose = FileSystemSingleton<StreamSystem>._closeCallback,
                useropen = FileSystemSingleton<StreamSystem>._openCallback,
                userread = FileSystemSingleton<StreamSystem>._readCallback,
                userseek = FileSystemSingleton<StreamSystem>._seekCallback,
            };
            _info.cbsize = Marshal.SizeOf(_info);

            device.CallSync(system =>
            {
                ch = new FMOD.Channel();
                this.sound = new FMOD.Sound();
                device.SafeCall(() => system.createSound(fileManipulationSystem.FileDescriptor, openMode, ref _info, ref sound), true);
                device.SafeCall(() => system.playSound(CHANNELINDEX.FREE, sound, true, ref ch), true);
                
                
            });
            _channel = new Channel(this.ch, device);
        }
        
        /*
        RESULT OnCallback(IntPtr channelRaw, CHANNEL_CALLBACKTYPE type, IntPtr command, IntPtr userData)
        {
            if (stopped)
                return RESULT.OK;
            if (type == CHANNEL_CALLBACKTYPE.END)
            {
                removeAllSyncPoints();
            }
            if (type == CHANNEL_CALLBACKTYPE.SYNCPOINT)
            {
                
                SoundCallback del = null;
                int syncPointInfo = command.ToInt32();
                StringBuilder sb = new StringBuilder(1024);
                uint offset = 0;
                IntPtr syncPoint = IntPtr.Zero;
                device.SafeCall(() => sound.getSyncPoint(command.ToInt32(), ref syncPoint), false);
                device.SafeCall(() => sound.getSyncPointInfo(syncPoint, sb, 1024, ref offset, TIMEUNIT.MS), false);
                string cbkName = sb.ToString();
                if (this.callbacks.ContainsKey(cbkName))
                {   
                    {
                        del = this.callbacks[cbkName];
                        if (del != null)
                            del(this, offset);

                        this.callbacks.Remove(cbkName);
                        device.SafeCall(() => sound.deleteSyncPoint(syncPoint), true);
                    }
                }
            }
            return RESULT.OK;
        }*/

        internal Sound(string filename, SoundDevice device)
            : this(StreamSystem.FromFile(filename), device)
        {
        }
        internal Sound(Stream stream, SoundDevice device) : this(StreamSystem.FromStream(stream), device)
        {

        }
        /// <summary>
        /// Stop the audio and closes all resources
        /// </summary>
        ~Sound()
        {
        
                Stop();
        }
        /// <summary>
        /// Plays the specified audio in the sound subsystem
        /// </summary>
        public void Play()
        {
            device.CallSync(x =>
            {
                device.SafeCall(() => this.ch.setPaused(false), true);
            });
        }

		/// <summary>
		/// Pauses the specified audio
		/// </summary>
		public void Pause()
		{
			device.CallSync(x =>
            {
                device.SafeCall(() => this.ch.setPaused(true), true);
            });
		}

        bool stopped = false;
        /// <summary>
        /// Stops and disposes the audio data and its children
        /// </summary>
        public void Stop()
        {
            if (stopped)
                return;
            stopped = true;
            using (timer)
            {
                this.sndCallbacks.Clear();
                timer.Elapsed -= timer_Elapsed;
            }
            device.CallSync(x =>
            {
                if (this.ch != null)
                    this.ch.stop();
                if (this.sound != null)
                    this.sound.release();
                bool playing = false;
                do
                {
                    this.ch.isPlaying(ref playing);
                }
                while(playing);
                this.ch = null;
                this.sound = null;
                foreach (DSP dsp in this.dsps)
                {
                    dsp.Release();
                }
                Debug.WriteLine("Sound::Stop");
            });
            
        }

        /// <summary>
        /// returns all available dsps containing the specified name
        /// </summary>
        /// <param name="name">Name to be used in the search</param>
        /// <returns>returns an array containing all found dsps</returns>
        public DSP[] GetDSP(string name)
        {
            return this.dsps.Where(c => c.Name == name).ToArray();
        }
        /// <summary>
        /// Adds a dsp into the Dsp chain
        /// </summary>
        /// <param name="dsp">Dsp instance to be used</param>
        public T AddDSP<T>(T dsp)where T:DSP
        {
            device.CallSync((x) =>
                {
                    device.SafeCall(() => this.ch.addDSP(dsp.getDsp(), ref con), true);
                    device.SafeCall(() => dsp.getDsp().setActive(true), true);
                });
            this.dsps.Add(dsp);
            return dsp;
        }
        /// <summary>
        /// Removes and disposes specified dsp from the chain
        /// </summary>
        /// <param name="dsp">Dsp instance</param>
        public void RemoveDSP(DSP dsp)
        {
            dsps.Remove(dsp);
            dsp.Release();
        }
    }
    /// <summary>
    /// Delegate used to tell when a syncpoint has reached
    /// </summary>
    /// <param name="sound">Current sound</param>
    /// <param name="positionMs">Position wich the syncpoint was registered</param>
    public delegate void SoundCallback(Sound sound, uint positionMs);
#pragma warning disable 1591


    public static class LinqExtensions
    {
        public static IEnumerable<T> Where<T>(this IEnumerable<T> list, Predicate<T> predicate)
        {
            foreach (var item in list)  
            {
                if (predicate(item))
                    yield return item;
                
            }
        }
        public static T[] ToArray<T>(this IEnumerable<T> list)
        {
            return new List<T>(list).ToArray();
            
        }
        public static T FirstOrDefault<T>(this IEnumerable<T> list)
        {
            return FirstOrDefault(list, c => true);
        }
        public static T FirstOrDefault<T>(this IEnumerable<T> list, Predicate<T> condition)
        {
            
            foreach (var i in list)
            {
                if (condition == null)
                    return i;
                if (condition(i))
                    return i;

            }
            return default(T);
        }
        public static IEnumerable<U> Select<T, U>(this IEnumerable<T> items, Func<T, U> selector)
        {
            foreach (var i in items)
                yield return selector(i);
        }
        public static List<T> ToList<T>(this IEnumerable<T> items)
        {
            return new List<T>(items);
        }
    }


}

// namespace System.Runtime.CompilerServices
// {
//     public class ExtensionAttribute : Attribute { }
// }
#pragma warning restore 1591
