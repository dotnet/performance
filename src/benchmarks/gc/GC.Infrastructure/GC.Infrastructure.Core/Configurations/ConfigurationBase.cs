namespace GC.Infrastructure.Core.Configurations
{
    public sealed class TraceConfigurations
    {
        public required string Type { get; set; }
    }

    public class OutputBase
    {
        public required string Path { get; set; }
        public required List<string> Columns { get; set; }
        public double percentage_disk_remaining_to_stop_per_run { get; set; }
        public required List<string> AllColumns { get; set; }
        public required List<string> Formats { get; set; }
    }

    public class ConfigurationBase
    {
        public required string Name { get; set; }
        public required TraceConfigurations TraceConfigurations { get; set; }
    }

    public class CoreRunInfoBase
    {
        public required bool is_baseline { get; set; }
        public required string Path { get; set; }
        public required Dictionary<string, string> environment_variables { get; set; }
    }

    public class RunBase
    {
        public Dictionary<string, string>? environment_variables { get; set; }
    }
}
