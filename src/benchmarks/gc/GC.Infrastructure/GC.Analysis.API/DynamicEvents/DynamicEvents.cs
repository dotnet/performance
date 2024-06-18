// TODO, AndrewAu, remove this condition when new TraceEvent is available through Nuget.
#if CUSTOM_TRACE_EVENT

using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers.GCDynamic;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("GC.Analysis.API.UnitTests")]

namespace GC.Analysis.API.DynamicEvents
{
    public static class TraceGCExtensions
    {
        public static dynamic DynamicEvents(this TraceGC traceGC)
        {
            return new DynamicIndex(traceGC.DynamicEvents);
        }
    }

    public class DynamicEventSchema
    {
        internal static Dictionary<string, Dictionary<string, Tuple<int, Type>>> DynamicEventSchemas = new Dictionary<string, Dictionary<string, Tuple<int, Type>>>();

        public string DynamicEventName { get; set; }

        public List<KeyValuePair<string, Type>> Fields { get; set; }

        public static void Set(List<DynamicEventSchema> dynamicEventSchemas)
        {
            DynamicEventSchemas.Clear();
            foreach (DynamicEventSchema dynamicEventSchema in dynamicEventSchemas)
            {
                if (DynamicEventSchemas.ContainsKey(dynamicEventSchema.DynamicEventName))
                {
                    throw new Exception($"Provided schema has a duplicated event named {dynamicEventSchema.DynamicEventName}");
                }
                Dictionary<string, Tuple<int, Type>> fields = new Dictionary<string, Tuple<int, Type>>();
                int offset = 0;
                foreach (KeyValuePair<string, Type> field in dynamicEventSchema.Fields)
                {
                    if (fields.ContainsKey(field.Key))
                    {
                        throw new Exception($"Provided schema has a duplicated field named {field.Key}");
                    }
                    fields.Add(field.Key, Tuple.Create(offset, field.Value));

                    // TODO, AndrewAu, all types that we envision this will be needed
                    if (field.Value == typeof(ushort))
                    {
                        offset += 2;
                    }
                    else if (field.Value == typeof(ulong))
                    {
                        offset += 8;
                    }
                    else
                    {
                        throw new Exception($"Provided schema has a field named {field.Key} using an unsupported type {field.Value}");
                    }
                }
                DynamicEventSchemas.Add(dynamicEventSchema.DynamicEventName, fields);
            }
        }
    }

    internal class DynamicIndex : DynamicObject
    {
        private List<DynamicEvent> index;

        public DynamicIndex(List<DynamicEvent> dynamicEvents)
        {
            // TODO, andrewau, index the events by name at this point
            // TODO, andrewau, define multiplicity constraint
            // TODO, andrewau, define size constraint
            // TODO, andrewau, validate events according to constraints.
            this.index = dynamicEvents;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string name = binder.Name;
            Dictionary<string, Tuple<int, Type>> fieldOffsets;
            if (DynamicEventSchema.DynamicEventSchemas.TryGetValue(name, out fieldOffsets))
            {
                // TODO, at this point, we should already have the events indexed and validated
                result = new DynamicEventObject(index.Single(r => string.Equals(r.Name, binder.Name)), fieldOffsets);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }

    internal class DynamicEventObject : DynamicObject
    {
        private DynamicEvent dynamicEvent;
        private Dictionary<string, object> fieldValues;

        public DynamicEventObject(DynamicEvent dynamicEvent, Dictionary<string, Tuple<int, Type>> fieldOffsets)
        {
            this.dynamicEvent = dynamicEvent;
            this.fieldValues = new Dictionary<string, object>();
            foreach (KeyValuePair<string, Tuple<int, Type>> field in fieldOffsets)
            {
                object value = null;
                int fieldOffset = field.Value.Item1;
                Type fieldType = field.Value.Item2;
                
                if (fieldType == typeof(ushort))
                {
                    value = BitConverter.ToUInt16(dynamicEvent.Payload, fieldOffset);
                }
                else if (fieldType == typeof(ulong))
                {
                    value = BitConverter.ToUInt64(dynamicEvent.Payload, fieldOffset);
                }
                else
                {
                    Debug.Fail("Unknown field type");
                }
                this.fieldValues.Add(field.Key, value);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string name = binder.Name;
            if (string.Equals(name, "TimeStamp"))
            {
                result = this.dynamicEvent.TimeStamp;
                return true;
            }
            else
            if (this.fieldValues.TryGetValue(name, out var fieldValue))
            {
                result = fieldValue;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public override string ToString()
        {
            return "I am " + this.dynamicEvent.Name + " with these fields: \n" + string.Join("\n", this.fieldValues.Select(kvp => kvp.Key + "->" + kvp.Value));
        }
    }
}

#endif