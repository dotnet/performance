using System;
using System.IO;
using System.Linq;


namespace AzDataMovementTest
{

    internal class TempFileStream : FileStream
    {
        public static string DefaultDirectory { get; set; } = Path.GetTempPath();

        // Use a large buffer to avoid hitting Azure IOPS limit
        public static int DefaultBufferSize { get; set; } = 4 * 1024 * 1024; // 4 MB

        private static Random random = new Random();
        private static string randomString(int length)
        {
            // All alphanumerics except O, 0, 1, and l
            const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public override long Length { get => this.length; }
        private long length = 0;

        public override void Write(byte[] buffer, int offset, int count) {
            base.Write(buffer, offset, count);
            length += count;
        }

        private static string makePath(string dir, string filename) {
            if (dir == null) {
                dir = DefaultDirectory;
            }

            if (filename == null) {
                filename = randomString(16) + ".tmp";
            }

            return Path.Combine(dir, filename);
        }

        public TempFileStream() : this(null, null) { }

        public TempFileStream(string dir = null, string filename = null, int? bufferSize = null)
            : base(
                path: makePath(dir, filename),
                mode: FileMode.Create,
                access: FileAccess.ReadWrite,
                share: FileShare.ReadWrite,
                bufferSize: bufferSize ?? DefaultBufferSize
            ) { }

        protected override void Dispose(bool disposing)
        {
            // Dispose of the stream first to free up handles
            base.Dispose(disposing);

            if (File.Exists(this.Name)) {
                try {
                    File.Delete(this.Name);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
