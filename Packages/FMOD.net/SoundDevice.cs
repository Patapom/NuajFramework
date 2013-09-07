using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using FMOD;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

namespace FMODNet
{
    class Method
    {
        public Action<FMOD.System> method;
        public bool async;
        public bool executed = false;
    }
    /// <summary>
    /// 
    /// </summary>
    public delegate void Action();
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    public delegate void Action<T>(T t);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public delegate T Func<T>();
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate U Func<T, U>(T t);
    /// <summary>
    /// Class used to configure the SoundDevice object during its initialization
    /// </summary>
    public class SoundDeviceConfiguration
    {
        /// <summary>
        /// The buffer size used for read operations
        /// </summary>
        public int StreamBufferSize { get; set; }
        /// <summary>
        /// Maximum number of simultaneous sound objects
        /// </summary>
        public short MaximumAllowedSounds { get; set; }
        /// <summary>
        /// Size of DSP input buffer size.
        /// </summary>
        /// <remarks>Setting the value to higher size tends to delay the dsp's output</remarks>
        public int DspBufferSize { get; set; }
        /// <summary>
        /// Number of buffers stored ahead the playing
        /// </summary>
        public short DspBufferCount { get; set; }
        /// <summary>
        /// If debug mode set to true, usage statistics are printed in Trace output
        /// </summary>
        public bool DebugMode { get; set; }
        internal static SoundDeviceConfiguration Default
        {
            get
            {
                return new SoundDeviceConfiguration
                {
                    StreamBufferSize = 65536,
                    MaximumAllowedSounds = 8,
                    DspBufferSize = 1024,
                    DspBufferCount = 4
                }; 
            }

        }
    }
    /// <summary>
    /// Specifies a sound system object from wich its possible to create sound objects. Its the main object from wich everything starts
    /// </summary>
    public class SoundDevice
    {
        int maxQueueSize;
        FMOD.System _system;
        bool isRunning;
        Queue<Method> queuedMethods = new Queue<Method>();
        Thread soundSystemThread;

        #region Sound creation
        /// <summary>
        /// Creates a Sound object using current filename
        /// </summary>
        /// <param name="filename">The file or url containing the media file</param>
        /// <returns>returns a new Sound object</returns>
        public Sound CreateSound(string filename)
        {
            return new Sound(filename, this);
        }
        /// <summary>
        /// Creates a new Sound object using current media stream
        /// </summary>
        /// <param name="musicStream">The Stream containing the audio data</param>
        /// <returns>returns a new Sound object</returns>
        public Sound CreateSound(Stream musicStream)
        {
            return new Sound(musicStream, this);
        }
        /// <summary>
        /// Creates a new Sound object using the filesystem provided in the constructor
        /// </summary>
        /// <param name="fileSystem">The filesystem object that contains method to open, close, seek and read operations</param>
        /// <returns>returns a new Sound object</returns>
        public Sound CreateSound(FileSystem fileSystem)
        {
            return new Sound(fileSystem, this);
        }
        #endregion
        void PrintUsageStatistics()
        {
            float _cpudsp = 0, _cpuStream = 0, _cpuTotal = 0, _cpuUpdate = 0;
            int currentallocatedRam = 0, maxAllocatedRam = 0, totalRam = 0;
            int numberOfChannelsPlaying = 0;
            uint totalMemory = 0;

            _system.getCPUUsage(ref _cpudsp, ref _cpuStream, ref _cpuUpdate, ref _cpuTotal);
            _system.getSoundRam(ref currentallocatedRam, ref maxAllocatedRam, ref totalRam);
            _system.getChannelsPlaying(ref numberOfChannelsPlaying);
            _system.getMemoryInfo((uint)FMOD.MEMBITS.ALL, (uint)FMOD.EVENT_MEMBITS.ALL, ref totalMemory, IntPtr.Zero);

            string cpu = string.Format("CPU Usage: dsp: {0}, streaming: {1}, update: {2}, total: {3}", _cpudsp, _cpuStream, _cpuUpdate, _cpuTotal);
            string memory = string.Format("Memory: current: {0}, max: {1}, total: {2}", currentallocatedRam, maxAllocatedRam, totalRam);
            string channels = string.Format("channels playing: {0}", numberOfChannelsPlaying);

            Trace.WriteLine(cpu);
            Trace.WriteLine(string.Format("System memory: {0}", totalMemory));
            Trace.WriteLine(memory);
            Trace.WriteLine(channels);
            Trace.WriteLine(string.Format("Queue size: {0}. Max queue: {1}", queuedMethods.Count, maxQueueSize));
        }
        /// <summary>
        /// Creates a new instance of Sound Device
        /// </summary>
        public SoundDevice()
        {
            SafeCall(() => FMOD.Factory.System_Create(ref _system), true);
            SafeCall(() => _system.setPluginPath("c:\\"), true);


        }
        /*
        public uint LoadExternalPlugin(string filename)
        {
            uint handle = 0;
            Initialize();
            SafeCall(() =>
                {
                    uint test = 0;
                    while (test < 10000)
                    {
                        try
                        {

                            RESULT res = _system.loadPlugin(filename, ref handle, test);
                            Debug.WriteLine(test);
                            if(res == RESULT.OK)
                                return res;
                            test++;

                        }
                        catch
                        {
                        }
                    }
                    return RESULT.ERR_UNIMPLEMENTED;
                }, true);
            return handle;
        }*/
        /// <summary>
        /// Initializes the SoundDevice object with the defined configuration
        /// </summary>
        /// <param name="configuration">The configuration object containing the configuration data</param>
        public void Initialize(SoundDeviceConfiguration configuration)
        {
            if (isRunning)
                return;
            soundSystemThread = new Thread(() =>
            {
                uint buffSize = (uint)configuration.StreamBufferSize;
                if(buffSize > 0)
                    SafeCall(() => _system.setStreamBufferSize(buffSize, TIMEUNIT.RAWBYTES), true);
                if(configuration.DspBufferSize > 0 && configuration.DspBufferCount > 0)
                    SafeCall(() => _system.setDSPBufferSize((uint)configuration.DspBufferSize, configuration.DspBufferCount), true);
                if(configuration.MaximumAllowedSounds > 0)
                    SafeCall(() => _system.init(configuration.MaximumAllowedSounds, INITFLAG.NORMAL, IntPtr.Zero), true);
                if (configuration.DebugMode)
                {
                    Timer tmr = new Timer(delegate
                    {
                        Queue(x => this.PrintUsageStatistics());
                    }, null, 5000, 5000);
                }
                startMainLoop();
            });
            soundSystemThread.Name = "SoundSystem";
            soundSystemThread.Start();
            while (!isRunning)
            {
                Thread.Sleep(10);
            }
        }
        /// <summary>
        /// Initializes the sound subsystem with the default configuration
        /// </summary>
        public void Initialize()
        {
            Initialize(SoundDeviceConfiguration.Default);
        }
        bool disposed;
        /// <summary>
        /// Closes the specified sound device
        /// </summary>
        public void Dispose()
        {
            disposed = true;
            isRunning = false;
        }
        
        #region Queueing methods
        /// <summary>
        /// Queue an action that will be executed by the current's device thread
        /// </summary>
        /// <param name="method">Method or operation to be executed</param>
        internal void Queue(Action method)
        {
            if (!disposed && !isRunning)
            {
                throw new InvalidOperationException("You have to call Initialize before queueing any method");
            }
            if (Thread.CurrentThread == soundSystemThread)
            {
                method();
                return;
            }
            Queue(c => method());
        }
        object _obj = new object();
        /// <summary>
        /// Queues an action that will be executed by the current's device thread
        /// </summary>
        /// <param name="method">Method or operation to be executed</param>
        internal void Queue(Action<FMOD.System> method)
        {
            
            if (!isRunning && !disposed)
            {
                throw new InvalidOperationException("You have to call Initialize before queueing any method");
            }
        
            if (Thread.CurrentThread == soundSystemThread)
            {
                method(_system);
                return;
            }
            //Monitor.Enter(_obj);

            try
            {
                Method m = new Method { method = method, async = true };
                queuedMethods.Enqueue(m);
                if (maxQueueSize < queuedMethods.Count)
                    maxQueueSize = queuedMethods.Count;
                
                
            }
            finally
            {
              //  Monitor.Exit(_obj);
            }
        }
        #endregion

        void startMainLoop()
        {
            isRunning = true;
            while (isRunning)
            {
                _system.update();
                Method method = null;
                while (queuedMethods.Count > 0)
                {
                    method = queuedMethods.Dequeue();
                    if (method == null)
                        continue;
                    try
                    {
                        method.method(_system);
                    }
                    finally
                    {
                        method.executed = true;
                    }
                }
                Thread.Sleep(1);
            }
            _system.close();
            _system.release();
        }
        
        #region SafeCall methods
        internal RESULT SafeCall(Func<RESULT> call, bool throwOnError)
        {
            RESULT result = call();
            if (result != RESULT.OK)
            {

                if (throwOnError)
                {
                    Trace.WriteLine(string.Format("Error: {0}. Description {1} at {2}", result, Error.String(result), call.Method));
                    throw new FMODException(string.Format("Error: {0}. Description {1} at {2}", result, Error.String(result), call.Method));
                }
                }
            return result;
        }
        #endregion

        #region CallSync Methods
        /*public static void CallSync(this SoundDevice dev, Action act)
        {
            bool handled = false;
            StackTrace stack = new StackTrace(1);
            Trace.WriteLine(stack.GetFrame(0).GetMethod().Name);
            dev.Queue(() =>
            {
                try
                {
                    act();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("error running operation: " + ex.ToString());

                }
                finally
                {
                    handled = true;
                }
            });
            int waitCount = 0;
            while (!handled)
            {
                Thread.Sleep(1);
                if (waitCount++ > 500)
                {
                    Trace.WriteLine(stack);
                    Debugger.Break();
                    break;
                }
            }
        }*/
        internal void CallSync(Action<FMOD.System> act, int waitMs)
        {
            bool handled = false;
            StackTrace stack = new StackTrace(2);
            this.Queue(x =>
            {
                try
                {
                    act(x);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("error running operation: " + ex.ToString());
                }
                finally
                {
                    handled = true;
                }
            });
            int waitCount = 0;
            while (!handled)
            {
                Thread.Sleep(1);
                if (waitCount++ > waitMs)
                {
                    Trace.WriteLine(string.Format("Waited too much ({0}) for the following method: {1}", waitMs, stack.GetFrame(0).GetMethod().Name));
                    Trace.WriteLine(stack);
                    break;
                }
            }
        }
        internal void CallSync(Action<FMOD.System> act)
        {
            CallSync(act, 5000);
        }
        #endregion


    }
    /// <summary>
    /// Specialized Exception that raises when a FMOD give something different than FMOD.RESULT.OK
    /// </summary>
    public sealed class FMODException : System.ApplicationException
    {

        /// <summary>
        /// Initializes a new Instance of FMOException
        /// </summary>
        /// <param name="msg">Message containing detailed error description</param>
        internal FMODException(string msg)
            : base(msg)
        {
        }
    }
}
