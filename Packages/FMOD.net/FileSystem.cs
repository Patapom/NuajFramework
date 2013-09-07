using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using FMOD;

namespace FMODNet
{
    class FileSystemSingleton<T> where T : FileSystem, new()
    {
        static T instance = new T();
        internal static readonly FILE_OPENCALLBACK _openCallback = instance.OpenCallback;
        internal static readonly FILE_CLOSECALLBACK _closeCallback = instance.CloseCallback;
        internal static readonly FILE_SEEKCALLBACK _seekCallback = instance.SeekCallback;
        internal static readonly FILE_READCALLBACK _readCallback = instance.ReadCallback;
        static FileSystemSingleton()
        {
            _openCallback = instance.OpenCallback;
        }
    }
    /// <summary>
    /// A class that exposes basic functionality to open files in FMOD's sound system
    /// </summary>
    public abstract class FileSystem : IDisposable
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
        public abstract RESULT OpenCallback(string name, int unicode, ref uint filesize, ref IntPtr handle, ref IntPtr userdata);
        /// <summary>
        /// Method for closing file handle
        /// </summary>
        /// <param name="handle">file's handle</param>
        /// <param name="userdata">user custom data</param>
        /// <returns>RESULT that FMOD use to ensure the method was called with no errors</returns>
        public abstract RESULT CloseCallback(IntPtr handle, IntPtr userdata);
        /// <summary>
        /// Method for reading file data
        /// </summary>
        /// <param name="handle">handle of the file</param>
        /// <param name="buffer">pointer that needs to be filled with data</param>
        /// <param name="sizebytes">number of bytes to be read</param>
        /// <param name="bytesread">number of bytes that the method was capable of read</param>
        /// <param name="userdata">user custom data</param>
        /// <returns>RESULT that FMOD use to ensure the method was called with no errors</returns>
        public abstract RESULT ReadCallback(IntPtr handle, IntPtr buffer, uint sizebytes, ref uint bytesread, IntPtr userdata);
        /// <summary>
        /// Method for seek operation in the file handle
        /// </summary>
        /// <param name="handle">handle of the file</param>
        /// <param name="pos">position to be seek'ed</param>
        /// <param name="userdata">user custom data</param>
        /// <returns>RESULT that FMOD use to ensure the method was called with no errors</returns>
        public abstract RESULT SeekCallback(IntPtr handle, int pos, IntPtr userdata);
        /// <summary>
        /// Unique identifier for the file system
        /// </summary>
        public virtual string FileDescriptor { get; set; }
        
        /// <summary>
        /// Return a handle that identifies the current File System object
        /// </summary>
        /// <returns>a unique pointer identifier</returns>
        public abstract IntPtr GetHandle();

        /// <summary>
        /// Closes all unmanaged resources and free memory
        /// </summary>
        public abstract void Dispose();
    }    
}
