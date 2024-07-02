using Microsoft.Data.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System.Reflection;
using System.Text;
using XPlot.Plotly;

namespace GC.Analysis.API
{
    public sealed class AxisInfo
    {
        public string? Name { get; set; }
        public List<double>? XAxis { get; set; }
        public List<double>? YAxis { get; set; }
    }

    public sealed class MultiAxisInfo
    {
        public string? Name { get; set; }
        public List<double>? XAxis { get; set; }
        public List<double>? YAxis1 { get; set; }
        public List<double>? YAxis2 { get; set; }
    }

    public sealed class ChartInfo
    {
        public string? YAxisLabel { get; set; } = null;
        public string? XAxisLabel { get; set; } = null;
        public double? Width { get; set; } = null;
        public double? Height { get; set; } = null;
    }

    public static class ChartingHelpers
    {
        internal static Layout.Layout ConstructLayout(string title,
                                                      string fieldName,
                                                      string xAxis,
                                                      ChartInfo? chartInfo)
        {
            var layout = new Layout.Layout
            {
                title = title,
                xaxis = new Xaxis { title = chartInfo?.XAxisLabel ?? xAxis },
                yaxis = new Yaxis { title = chartInfo?.YAxisLabel ?? fieldName },
            };

            if (chartInfo != null)
            {
                if (chartInfo.Width.HasValue)
                {
                    layout.width = chartInfo.Width.Value;
                }

                if (chartInfo.Height.HasValue)
                {
                    layout.height = chartInfo.Height.Value;
                }
            }

            return layout;
        }

        internal static Layout.Layout ConstructLayout(string title,
                                                      IEnumerable<string> fieldNames,
                                                      string xAxis,
                                                      ChartInfo? chartInfo)
        {
            var layout = new Layout.Layout
            {
                title = title,
                xaxis = new Xaxis { title = chartInfo?.XAxisLabel ?? xAxis },
                yaxis = new Yaxis { title = chartInfo?.YAxisLabel ?? string.Join(",", fieldNames) },
            };

            if (chartInfo != null)
            {
                if (chartInfo.Width.HasValue)
                {
                    layout.width = chartInfo.Width.Value;
                }

                if (chartInfo.Height.HasValue)
                {
                    layout.height = chartInfo.Height.Value;
                }
            }

            return layout;
        }
    }

    public static class GoodLinq
    {
        public static List<R> Select<T, R>(this IEnumerable<T> data, Func<T, R> map)
        {
            List<R> result = new();
            foreach (var d in data)
            {
                result.Add(map(d));
            }

            return result;
        }

        public static List<T> Where<T>(this IEnumerable<T> data, Func<T, bool> predicate)
        {
            List<T> result = new();
            foreach (var d in data)
            {
                if (predicate(d))
                {
                    result.Add(d);
                }
            }

            return result;
        }

        public static double Sum<TSource>(this IEnumerable<TSource> data, Func<TSource, double> map)
        {
            double sum = 0;
            if (data == null || data.Count() == 0)
            {
                return sum;
            }

            foreach (var sourceItem in data)
            {
                sum += (double)map(sourceItem);
            }

            return sum;
        }

        public static double Average<TSource>(this IEnumerable<TSource> data, Func<TSource, double> map)
        {
            int count = data.Count();
            if (data == null || count == 0)
            {
                return double.NaN;
            }

            double sum = 0;
            foreach (var sourceItem in data)
            {
                sum += (double)map(sourceItem);
            }

            return sum / count;
        }
    }

    public static class ReflectionHelpers
    {
        public static List<double> GetDoubleValueFromFieldForCustomObjects(IEnumerable<object> customObjects, string fieldName)
        {
            List<double> values = new();
            foreach (var customObject in customObjects)
            {
                double? val = GetDoubleValueBasedOnField(customObject, fieldName);
                values.Add(val.Value); // Let this except and bubble up to the user in case the field isn't found.
            }

            return values;
        }

        public static List<double> GetDoubleValueForGCField(IEnumerable<TraceGC> gcs, string fieldName)
        {
            List<double> values = new();
            foreach (var gc in gcs)
            {
                double? val = GetDoubleValueBasedOnField(gc, fieldName);
                values.Add(val.Value); // Let this except and bubble up to the user in case the field isn't found.
            }

            return values;
        }

        public static double GetDoubleValueForGCStatsField(GCStats stats, string fieldName)
            => GetDoubleValueBasedOnField(stats, fieldName).Value;

        public static double? GetDoubleValueBasedOnField(object data, string fieldName)
        {
            Type dataType = data.GetType();
            var fieldInfo = dataType.GetField(fieldName);
            if (fieldInfo != null)
            {
                if (!double.TryParse(fieldInfo?.GetValue(data)?.ToString(), out var result))
                {
                    return double.NaN;
                }

                else
                {
                    return result;
                }
            }

            else
            {
                var propertyInfo = dataType.GetProperty(fieldName);
                if (propertyInfo == null)
                {
                    return double.NaN;
                }

                if (!double.TryParse(propertyInfo?.GetValue(data)?.ToString(), out var result))
                {
                    return double.NaN;
                }

                else
                {
                    return result;
                }
            }
        }
    }

    public static class DataFrameHelpers
    {
        public static string CreateMarkdown(this DataFrame dataFrame)
        {
            var columnNames = new List<string>();
            for (int i = 0; i < dataFrame.Columns.Count; i++)
            {
                columnNames.Add(dataFrame.Columns[i].Name);
            }

            string runningString = string.Empty;
            StringBuilder sb = new StringBuilder();

            // Add the 2 header rows.
            sb.Append("|");
            foreach (var columnName in columnNames)
            {
                sb.Append(columnName);
                sb.Append("|");
            }

            runningString += sb.ToString() + "\n";
            sb.Clear();

            sb.Append("|");
            foreach (var columnName in columnNames)
            {
                sb.Append("------");
                sb.Append("|");
            }
            runningString += sb.ToString() + "\n";
            sb.Clear();

            foreach (var row in dataFrame.Rows)
            {
                sb.Append("|");
                foreach (var cell in row)
                {
                    var data = cell.ToString();
                    sb.Append(data);
                    sb.Append(" |");
                }
                runningString = sb.ToString() + "\n";
                sb.Clear();
            }

            return runningString;
        }

        public static void ToMarkdown(this DataFrame dataFrame, string path)
        {
            var columnNames = new List<string>();
            for (int i = 0; i < dataFrame.Columns.Count; i++)
            {
                columnNames.Add(dataFrame.Columns[i].Name);
            }

            using (StreamWriter markdownFile = new StreamWriter(path))
            {
                var record = new StringBuilder();
                StringBuilder sb = new StringBuilder();

                // Add the 2 header rows.
                sb.Append("|");
                foreach (var columnName in columnNames)
                {
                    sb.Append(columnName);
                    sb.Append("|");
                }

                markdownFile.WriteLine(sb.ToString());
                sb.Clear();

                sb.Append("|");
                foreach (var columnName in columnNames)
                {
                    sb.Append("------");
                    sb.Append("|");
                }
                markdownFile.WriteLine(sb.ToString());
                sb.Clear();

                foreach (var row in dataFrame.Rows)
                {
                    sb.Append("|");
                    foreach (var cell in row)
                    {
                        var data = cell.ToString();
                        sb.Append(data);
                        sb.Append(" |");
                    }
                    markdownFile.WriteLine(sb.ToString());
                    sb.Clear();
                }
            }
        }

        public static double Round2(double value) => Math.Round(value, 2);

        public static DataFrame ConstructDataFrameFromCustomArrayData(object data)
        {
            if (data == null)
            {
                return null;
            }

            var dataArray = data as object[];
            if (dataArray == null)
            {
                throw new ArgumentException($"{nameof(data)} must be of an object[] type.");
            }

            if (dataArray.Length == 0)
            {
                throw new ArgumentException($"{nameof(data)} is an empty object[].");
            }

            // Establish the columns. 
            object firstDatum = dataArray[0];
            Type type = firstDatum.GetType();
            FieldInfo[] fieldsInfo = type.GetFields();
            Dictionary<string, StringDataFrameColumn> columnsData = new();

            foreach (var field in fieldsInfo)
            {
                StringDataFrameColumn column = new(field.Name);
                columnsData[field.Name] = new StringDataFrameColumn(field.Name);
            }

            // For all data points, go through all the fields and append the values to the columns.
            foreach (var datum in dataArray)
            {
                FieldInfo[] fields = type.GetFields();

                // For each field in fields, get the value.
                foreach (var field in fields)
                {
                    object obtainedValue = field.GetValue(datum);
                    string obtainedValueAsString = obtainedValue.ToString();

                    StringDataFrameColumn column = columnsData[field.Name];

                    // For doubles, round them by 2.
                    if (double.TryParse(obtainedValueAsString, out var doubleValue))
                    {
                        column.Append(doubleValue.ToString("N2"));
                    }

                    // For all other types save them as is.
                    else
                    {
                        column.Append(obtainedValueAsString);
                    }
                }
            }

            return new DataFrame(columnsData.Values);
        }
    }
}
