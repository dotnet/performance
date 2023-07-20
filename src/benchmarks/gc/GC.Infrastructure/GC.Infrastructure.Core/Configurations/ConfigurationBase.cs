namespace GC.Infrastructure.Core.Configurations
{
    public sealed class TraceConfigurations
    {
        public string Type { get; set; }
    }

    public class OutputBase
    {
        public string Path { get; set; }
        public List<string> Columns { get; set; }
        public double percentage_disk_remaining_to_stop_per_run { get; set; }
        public List<string> AllColumns { get; set; }
        public List<string> Formats { get; set; }
    }

    public class ConfigurationBase
    {
        public string Name { get; set; }
        public TraceConfigurations TraceConfigurations { get; set; }
    }

    public class RunBase
    {
        public Dictionary<string, string>? environment_variables { get; set; }
    }
}
