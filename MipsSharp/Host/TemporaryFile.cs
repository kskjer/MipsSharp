using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MipsSharp.Host
{
    public class TemporaryFile : IDisposable
    {
        public string Path { get; }

        public TemporaryFile(string path)
        {
            Path = path;
        }

        public TemporaryFile()
            : this(System.IO.Path.GetTempFileName())
        {

        }

        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            try
            {
                File.Delete(Path);
            }
            catch
            {

            }
        }

        ~TemporaryFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
