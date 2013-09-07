using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using FMOD;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FMODNet
{
    /// <summary>
    /// Class used to expose stream access to FMOD
    /// </summary>
    public class StreamSystem : FileSystem
    {
        /// <summary>
        /// Method to expose Open file feature for FMOD to open file
        /// </summary>
        /// <param name="name">name or identifier of the file</param>
        /// <param name="unicode">if different from zero, open file as unicode</param>
        /// <param name="filesize">size of file to be opened</param>
        /// <param name="handle">handle of the file</param>
        /// <param name="userdata">user custom data</param>
        /// <returns>RESULT that FMOD use to ensure the method was called with no errors</returns>
        public override RESULT OpenCallback(string name, int unicode, ref uint filesize, ref IntPtr handle, ref IntPtr userdata)
        {
            try
            {
                Debug.WriteLine(string.Format("Opening file {0}", name));
                handle = new IntPtr(int.Parse(name));
                Stream str = GetStream(handle.ToInt32());

                Stream stream = GetStream(handle.ToInt32());
                filesize = (uint)stream.Length;
                return RESULT.OK;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Error opening file: {0}", ex));
                return RESULT.ERR_FILE_BAD;
            }
        }
        /// <summary>
        /// Create a new Stream System from a managed Stream
        /// </summary>
        /// <param name="str">Stream containing the audio data</param>
        /// <returns>a new instance of StreamSystem</returns>
        public static StreamSystem FromStream(Stream str)
        {
            StreamSystem sys = new StreamSystem();
            IntPtr handle = sys.GetHandle();
            sys.FileDescriptor = handle.ToInt32().ToString();
            _streams.Add(handle.ToInt32(), str);
            return sys;
        }
        /// <summary>
        /// static resource used to fool the garbage collector and ensure that fmod will get the resource when it needs
        /// </summary>
        protected static Dictionary<int, Stream> _streams = new Dictionary<int, Stream>();
        static Random rnd = new Random();
        IntPtr _handle = IntPtr.Zero;
        /// <summary>
        /// Return a handle that identifies the current File System object
        /// </summary>
        /// <returns>a unique pointer identifier</returns>
        
        public override IntPtr GetHandle()
        {
            if (_handle != IntPtr.Zero)
                return _handle;

            int x = 0;
            while (_streams.ContainsKey((x = rnd.Next(int.MaxValue))))
            {
            }
            return new IntPtr(x);
        }
        /// <summary>
        /// Returns a stream that corresponds to the pointer
        /// </summary>
        /// <param name="ptr">pointer</param>
        /// <returns>returns a stream</returns>
        protected static Stream GetStream(int ptr)
        {
            if (_streams.ContainsKey(ptr))
                return _streams[ptr];
            return Stream.Null;
        }
        /// <summary>
        /// Method for closing file handle
        /// </summary>
        /// <param name="handle">file's handle</param>
        /// <param name="userdata">user custom data</param>
        /// <returns>RESULT that FMOD use to ensure the method was called with no errors</returns>
        public override RESULT CloseCallback(IntPtr handle, IntPtr userdata)
        {
            Debug.WriteLine("Closing handle");
            using (Stream stream = GetStream(handle.ToInt32()))
            {
                if (stream == null)
                {
                    Debug.WriteLine("Closing file error");
                    return RESULT.ERR_FILE_BAD;
                }
                stream.Close();
                _streams.Remove(handle.ToInt32());
            }
            GC.Collect();
            return RESULT.OK;
        }

        /// <summary>
        /// Method for seek operation in the file handle
        /// </summary>
        /// <param name="handle">handle of the file</param>
        /// <param name="pos">position to be seek'ed</param>
        /// <param name="userdata">user custom data</param>
        /// <returns>RESULT that FMOD use to ensure the method was called with no errors</returns>
        public override RESULT SeekCallback(IntPtr handle, int pos, IntPtr userdata)
        {
            Stream stream = GetStream(handle.ToInt32());
            if (stream == null)
            {
                Debug.WriteLine("Seeking file error");

                return RESULT.ERR_FILE_BAD;
            }
            stream.Seek(pos, SeekOrigin.Begin);
            return RESULT.OK;
        }

        /// <summary>
        /// Method for reading file data
        /// </summary>
        /// <param name="handle">handle of the file</param>
        /// <param name="buffer">pointer that needs to be filled with data</param>
        /// <param name="sizebytes">number of bytes to be read</param>
        /// <param name="bytesread">number of bytes that the method was capable of read</param>
        /// <param name="userdata">user custom data</param>
        /// <returns>RESULT that FMOD use to ensure the method was called with no errors</returns>
        public override RESULT ReadCallback(IntPtr handle, IntPtr buffer, uint sizebytes, ref uint bytesread, IntPtr userdata)
        {
            Stream stream = GetStream(handle.ToInt32());
            if (stream == null)
            {
                Debug.WriteLine("Reading file error");
                return RESULT.ERR_FILE_BAD;
            }
            byte[] data = new byte[(int)sizebytes];
            bytesread = (uint)stream.Read(data, 0, data.Length);
            Marshal.Copy(data, 0, buffer, (int)bytesread);
            return RESULT.OK;
        }
        /// <summary>
        /// Closes all unmanaged resources and free memory
        /// </summary>
        public override void Dispose()
        {
            using (GetStream(this._handle.ToInt32()))
            {
                if (_streams.ContainsKey(this._handle.ToInt32()))
                    _streams.Remove(this._handle.ToInt32());
            }
        }
        internal static FileSystem FromFile(string filename)
        {
            return FromStream(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }
    }
    
}
