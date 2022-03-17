using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Linq;

public class ParameterFilter : IFilter
{
    private readonly Dictionary<string, string> _parameterValues;

    public ParameterFilter(Dictionary<string, string> parameterValues)
    {
        _parameterValues = parameterValues;
    }

    public bool Predicate(BenchmarkCase benchmarkCase)
    {
        Dictionary<string, object> items = benchmarkCase.Parameters.Items.ToDictionary<ParameterInstance, string, object>(instance => instance.Name, instance => instance.Value);

        if (_parameterValues == null)
            return true;

        foreach (string key in _parameterValues.Keys)
        {
            if (!items.Keys.Contains(key))
                return false;
            if (!items[key].ToString().Equals(_parameterValues[key]))
                return false;
        }
        
        return true;
    }
}
