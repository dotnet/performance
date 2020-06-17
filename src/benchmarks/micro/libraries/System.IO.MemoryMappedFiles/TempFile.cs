// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    /// <summary>
    /// Represents a temporary file. Creating an instance creates a file at the specified path,
    /// and disposing the instance deletes the file.
    /// </summary>
    public sealed class TempFile : IDisposable
    {
        /// <summary>Gets the created file's path.</summary>
        public string Path { get; }

        public TempFile(string path, long length = 0) : this(path, length > -1 ? new byte[length] : null)
        {
        }

        public TempFile(string path, byte[] data)
        {
            Path = path;

            if (data != null)
            {
                File.WriteAllBytes(path, data);
            }
        }

        ~TempFile() => DeleteFile();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DeleteFile();
        }

        private void DeleteFile()
        {
            try { File.Delete(Path); }
            catch { /* Ignore exceptions on disposal paths */ }
        }
    }
}
