using System;
using System.IO;

namespace Prequel.Tests
{
    /// <summary>
    /// Create a file with specified contents and delete when it goes out of scope
    /// </summary>
    internal class TempFile : IDisposable
    {
        public string FileName { get; private set; }

        public TempFile(string contents)
        {
            this.FileName = Path.GetTempFileName();
            File.WriteAllText(this.FileName, contents);
        }

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try { File.Delete(FileName); } catch { }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion


    }
}