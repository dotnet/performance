using System.IO;
using System.Text;
using Serilog;

namespace ArtifactsUploader
{
    public class LogWrapper : TextWriter
    {
        public LogWrapper(ILogger log) => Log = log;

        private ILogger Log { get; }
        
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string value) => Log.Error(value); // this logger will be used only for parsing errors
    }
}