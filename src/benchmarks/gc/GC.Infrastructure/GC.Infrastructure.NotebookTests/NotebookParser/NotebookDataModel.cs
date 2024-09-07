using Newtonsoft.Json;

namespace GC.Infrastructure.NotebookTests.NotebookParser
{
    // NotebookRoot myDeserializedClass = JsonConvert.DeserializeObject<NotebookRoot>(myJsonResponse);
    public class Cell
    {
        [JsonProperty("cell_type")]
        public string CellType { get; set; }

        [JsonProperty("execution_count")]
        public object ExecutionCount { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("outputs")]
        public List<Output> Outputs { get; } = new List<Output>();

        [JsonProperty("source")]
        public List<string> Source { get; } = new List<string>();
    }

    public class Data
    {
        [JsonProperty("text/html")]
        public List<string> TextHtml { get; } = new List<string>();
    }

    public class DotnetInteractive
    {
        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("defaultKernelName")]
        public string DefaultKernelName { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; } = new List<Item>();
    }

    public class Item
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Kernelspec
    {
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class LanguageInfo
    {
        [JsonProperty("file_extension")]
        public string FileExtension { get; set; }

        [JsonProperty("mimetype")]
        public string Mimetype { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pygments_lexer")]
        public string PygmentsLexer { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("dotnet_repl_cellExecutionStartTime")]
        public DateTime DotnetReplCellExecutionStartTime { get; set; }

        [JsonProperty("dotnet_repl_cellExecutionEndTime")]
        public DateTime DotnetReplCellExecutionEndTime { get; set; }

        [JsonProperty("dotnet_interactive")]
        public DotnetInteractive DotnetInteractive { get; set; }

        [JsonProperty("polyglot_notebook")]
        public PolyglotNotebook PolyglotNotebook { get; set; }

        [JsonProperty("kernelspec")]
        public Kernelspec Kernelspec { get; set; }

        [JsonProperty("language_info")]
        public LanguageInfo LanguageInfo { get; set; }
    }

    public class Output
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("output_type")]
        public string OutputType { get; set; }

        [JsonProperty("text")]
        public List<string> Text { get; } = new List<string>();

        [JsonProperty("data")]
        public Data Data { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("ename")]
        public string Ename { get; set; }

        [JsonProperty("evalue")]
        public string Evalue { get; set; }

        [JsonProperty("traceback")]
        public List<string> Traceback { get; } = new List<string>();
    }

    public class PolyglotNotebook
    {
        [JsonProperty("kernelName")]
        public string KernelName { get; set; }

        [JsonProperty("defaultKernelName")]
        public string DefaultKernelName { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; } = new List<Item>();
    }

    public class NotebookRoot
    {
        [JsonProperty("cells")]
        public List<Cell> Cells { get; } = new List<Cell>();

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("nbformat")]
        public int Nbformat { get; set; }

        [JsonProperty("nbformat_minor")]
        public int NbformatMinor { get; set; }
    }
}
