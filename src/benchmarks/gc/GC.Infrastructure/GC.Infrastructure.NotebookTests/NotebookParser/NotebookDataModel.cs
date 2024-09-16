using Newtonsoft.Json;

namespace GC.Infrastructure.NotebookTests.NotebookParser
{
    // NotebookRoot myDeserializedClass = JsonConvert.DeserializeObject<NotebookRoot>(myJsonResponse);
    public class Cell
    {
        [JsonProperty("cell_type")]
        public required string CellType { get; set; }

        [JsonProperty("execution_count")]
        public required object ExecutionCount { get; set; }

        [JsonProperty("metadata")]
        public required Metadata Metadata { get; set; }

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
        public required string Language { get; set; }

        [JsonProperty("defaultKernelName")]
        public required string DefaultKernelName { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; } = new List<Item>();
    }

    public class Item
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
    }

    public class Kernelspec
    {
        [JsonProperty("display_name")]
        public required string DisplayName { get; set; }

        [JsonProperty("language")]
        public required string Language { get; set; }

        [JsonProperty("name")]
        public required string Name { get; set; }
    }

    public class LanguageInfo
    {
        [JsonProperty("file_extension")]
        public required string FileExtension { get; set; }

        [JsonProperty("mimetype")]
        public required string Mimetype { get; set; }

        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("pygments_lexer")]
        public required string PygmentsLexer { get; set; }

        [JsonProperty("version")]
        public required string Version { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("dotnet_repl_cellExecutionStartTime")]
        public DateTime DotnetReplCellExecutionStartTime { get; set; }

        [JsonProperty("dotnet_repl_cellExecutionEndTime")]
        public DateTime DotnetReplCellExecutionEndTime { get; set; }

        [JsonProperty("dotnet_interactive")]
        public required DotnetInteractive DotnetInteractive { get; set; }

        [JsonProperty("polyglot_notebook")]
        public required PolyglotNotebook PolyglotNotebook { get; set; }

        [JsonProperty("kernelspec")]
        public required Kernelspec Kernelspec { get; set; }

        [JsonProperty("language_info")]
        public required LanguageInfo LanguageInfo { get; set; }
    }

    public class Output
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("output_type")]
        public required string OutputType { get; set; }

        [JsonProperty("text")]
        public List<string> Text { get; } = new List<string>();

        [JsonProperty("data")]
        public required Data Data { get; set; }

        [JsonProperty("metadata")]
        public required Metadata Metadata { get; set; }

        [JsonProperty("ename")]
        public required string Ename { get; set; }

        [JsonProperty("evalue")]
        public required string Evalue { get; set; }

        [JsonProperty("traceback")]
        public List<string> Traceback { get; } = new List<string>();
    }

    public class PolyglotNotebook
    {
        [JsonProperty("kernelName")]
        public required string KernelName { get; set; }

        [JsonProperty("defaultKernelName")]
        public required string DefaultKernelName { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; } = new List<Item>();
    }

    public class NotebookRoot
    {
        [JsonProperty("cells")]
        public List<Cell> Cells { get; } = new List<Cell>();

        [JsonProperty("metadata")]
        public required Metadata Metadata { get; set; }

        [JsonProperty("nbformat")]
        public int Nbformat { get; set; }

        [JsonProperty("nbformat_minor")]
        public int NbformatMinor { get; set; }
    }
}
