using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;

namespace DSCript
{
    public sealed class DSCTempFile : IDisposable
    {
        private FileStream m_fileStream = null;
        private bool m_isClosed = false;
        
        internal int Handle { get; set; } = -1;
        internal int Length { get; set; } = 0;
        
        private FileStream FileStream
        {
            get
            {
                VerifyAccess();

                // create a 1kb .tmp file
                if (m_fileStream == null)
                    m_fileStream = File.Create(DSCTempFileManager.GenerateTempFileName(), 1024, FileOptions.RandomAccess | FileOptions.DeleteOnClose);

                return m_fileStream;
            }
        }
        
        private void VerifyAccess()
        {
            // make sure it's not closed
            if (m_isClosed)
                throw new InvalidOperationException("Cannot use a temp file once it's been closed!");
            // if the handle is -1, the temp file is NOT registered and can't be used
            if (Handle == -1)
                throw new InvalidOperationException("The temp file was not initialized properly!");
        }
        
        public void Dispose()
        {
            VerifyAccess();

            // unregister from the system
            DSCTempFileManager.UnregisterTempFile(this);

            // release the filestream
            if (m_fileStream != null)
            {
                m_fileStream.Dispose(); // should also delete the file for us
                m_fileStream = null;
            }

            // make sure code doesn't dispose this temp file then try to use it again...
            m_isClosed = true;
        }

        public byte[] GetBuffer()
        {
            VerifyAccess();

            if (Length == 0)
                return null;

            FileStream.Position = 0;

            return FileStream.ReadBytes(Length);
        }

        public void SetBuffer(byte[] buffer)
        {
            VerifyAccess();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null!");

            Length = buffer.Length;

            FileStream.SetLength(Length);

            FileStream.Position = 0;
            FileStream.Write(buffer, 0, buffer.Length);
        }

        public void ClearBuffer()
        {
            VerifyAccess();

            if (Length > 0)
                FileStream.SetLength(0);
        }

        public DSCTempFile()
        {
            DSCTempFileManager.RegisterTempFile(this);
        }
    }

    public static class DSCTempFileManager
    {
        public static string TempDirectory { get; } = Path.Combine(Path.GetTempPath(), "libDSC");

        private static int _maxTempHandles = 255;
        private static int _nextTempHandle = 0;

        private static DSCTempFile[] _tempFiles = new DSCTempFile[_maxTempHandles];

        /*
            Public methods
        */
        public static int GetNumberOfTempFilesOpen()
        {
            if (Directory.Exists(TempDirectory))
                return Directory.GetFiles(TempDirectory).Length;

            return 0;
        }

        public static long GetTempDirectorySize()
        {
            var size = 0L;

            if (Directory.Exists(TempDirectory))
            {
                var files = from file in Directory.GetFiles(TempDirectory)
                            select new FileInfo(file);

                foreach (var file in files)
                    size += file.Length;

                return size;
            }
            
            return size;
        }

        /*
            Internal methods
        */
        internal static bool IsTempFileInitialized(DSCTempFile tempFile)
        {
            if (tempFile == null)
                throw new ArgumentNullException(nameof(tempFile), "Cannot check the initialized status of a null temp file!");

            return (tempFile.Handle > -1);
        }

        internal static bool IsTempFileRegistered(DSCTempFile tempFile)
        {
            if (tempFile == null)
                throw new ArgumentNullException(nameof(tempFile), "Cannot check the registered status of a null temp file!");
            
            return ((tempFile.Handle > -1) && _tempFiles[tempFile.Handle] == tempFile);
        }

        internal static int FindFreeHandle(int index)
        {
            for (int handle = index; handle < _maxTempHandles; handle++)
            {
                if (_tempFiles[handle] == null)
                    return handle;
            }

            return -1;
        }

        internal static int AllocateHandles(int count)
        {
            // will be first available handle after allocation
            var first = _maxTempHandles;

            DSC.Log("Increasing max number of temp handles...");

            // time to start hogging up more memory >_>
            _maxTempHandles += count;

            var tempFiles = new DSCTempFile[_maxTempHandles];
            Array.Copy(_tempFiles, tempFiles, _tempFiles.Length);

            _tempFiles = tempFiles;
            
            return first;
        }

        internal static int GetFreeHandle()
        {
            if ((_nextTempHandle >= _maxTempHandles) || (_tempFiles[_nextTempHandle] != null))
            {
                _nextTempHandle = FindFreeHandle(0);

                // was there a free handle we could use?
                // if not, allocate some more
                if (_nextTempHandle == -1)
                    _nextTempHandle = AllocateHandles(250);
            }
            
            return _nextTempHandle++;
        }

        internal static void RegisterTempFile(DSCTempFile tempFile)
        {
            if (IsTempFileInitialized(tempFile))
            {
                if (!IsTempFileRegistered(tempFile))
                    throw new InvalidOperationException("Cannot add an orphaned temp file - how did this happen?!");

                throw new InvalidOperationException("Cannot add a temp file that was already initialized!");
            }

            var handle = GetFreeHandle();

            tempFile.Handle = handle;
            _tempFiles[handle] = tempFile;
        }

        internal static void UnregisterTempFile(DSCTempFile tempFile)
        {
            if (IsTempFileInitialized(tempFile))
            {
                if (!IsTempFileRegistered(tempFile))
                    throw new InvalidOperationException("Cannot remove an orphaned temp file - how did this happen?!");

                var handle = tempFile.Handle;

                _tempFiles[handle] = null;
                tempFile.Handle = -1;

                // this handle is now free to be used
                _nextTempHandle = handle;
            }
            else
            {
                throw new InvalidOperationException("Cannot remove a temp file that was never initialized!");
            }
        }

        internal static string GenerateTempFileName()
        {
            var tmpFile = Path.GetTempFileName();

            // delete temp file if it gets created
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);

            return Path.Combine(TempDirectory, Path.GetFileName(tmpFile));
        }

        static DSCTempFileManager()
        {
            // Setup temp directory
            if (!Directory.Exists(TempDirectory))
            {
                Directory.CreateDirectory(TempDirectory);
            }
            else
            {
                // this might be dangerous if multiple instances of a DSCript program are running...
                var files = Directory.GetFiles(TempDirectory);

                if (files.Length > 0)
                {
                    DSC.Log("Cleaning up temp directory...");

                    int count = 0;

                    foreach (var file in files)
                    {
                        File.Delete(file);
                        ++count;
                    }

                    DSC.Log("Cleaned out {0} files from temp directory.", count);
                }
            }
        }
    }
}
