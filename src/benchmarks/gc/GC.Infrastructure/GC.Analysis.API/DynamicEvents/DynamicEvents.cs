using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers.GCDynamic;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("GC.Analysis.API.UnitTests")]

namespace GC.Analysis.API.DynamicEvents
{
    public static class TraceGCExtensions
    {
        private readonly static ConditionalWeakTable<TraceGC, DynamicIndex> dynamicEventsCache = new ConditionalWeakTable<TraceGC, DynamicIndex>();

        public static dynamic DynamicEvents(this TraceGC traceGC)
        {
            DynamicIndex? dynamicIndex;
            if (!dynamicEventsCache.TryGetValue(traceGC, out dynamicIndex))
            {
                dynamicIndex = new DynamicIndex(traceGC.DynamicEvents);
                dynamicEventsCache.Add(traceGC, dynamicIndex);
            }

            return dynamicIndex;
        }
    }

    public sealed class DynamicEventSchema
    {
        internal static Dictionary<string, CompiledSchema> DynamicEventSchemas = new Dictionary<string, CompiledSchema>()
        {
            // These are the dynamic events that are currently emitted from the runtime.
            { GCDynamicEvents.SizeAdaptationSampleSchema.DynamicEventName, Compile(GCDynamicEvents.SizeAdaptationSampleSchema) },
            { GCDynamicEvents.SizeAdaptationTuningSchema.DynamicEventName, Compile(GCDynamicEvents.SizeAdaptationTuningSchema) },
            { GCDynamicEvents.SizeAdaptationFullGCTuningSchema.DynamicEventName, Compile(GCDynamicEvents.SizeAdaptationFullGCTuningSchema) },
            { GCDynamicEvents.OOMDetailsSchema.DynamicEventName, Compile(GCDynamicEvents.OOMDetailsSchema) },
        };

        internal static bool allowPartialSchema;

        public required string DynamicEventName { get; init; }

        public required List<KeyValuePair<string, Type>> Fields { get; init; }

        public int MinOccurrence { get; init; } = 0;

        public int MaxOccurrence { get; init; } = 1;

        internal static CompiledSchema Compile(DynamicEventSchema dynamicEventSchema, bool allowPartialSchema = true)
        {
            if (DynamicEventSchemas != null && DynamicEventSchemas.ContainsKey(dynamicEventSchema.DynamicEventName))
            {
                throw new Exception($"Provided schema has a duplicated event named {dynamicEventSchema.DynamicEventName}");
            }
            CompiledSchema schema = new CompiledSchema();
            if (dynamicEventSchema.MinOccurrence < 0)
            {
                throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a negative MinOccurrence");
            }
            if (dynamicEventSchema.MaxOccurrence < dynamicEventSchema.MinOccurrence)
            {
                throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a MaxOccurrence smaller than MinOccurrence");
            }
            schema.MinOccurrence = dynamicEventSchema.MinOccurrence;
            schema.MaxOccurrence = dynamicEventSchema.MaxOccurrence;
            int offset = 0;
            foreach (KeyValuePair<string, Type> field in dynamicEventSchema.Fields)
            {
                if (schema.ContainsKey(field.Key))
                {
                    DynamicEventSchemas?.Clear();
                    throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a duplicated field named {field.Key}");
                }
                schema.Add(field.Key, new DynamicEventField { FieldOffset = offset, FieldType = field.Value });

                if (field.Value == typeof(ushort))
                {
                    offset += 2;
                }
                else if (field.Value == typeof(uint))
                {
                    offset += 4;
                }
                else if (field.Value == typeof(float))
                {
                    offset += 4;
                }
                else if (field.Value == typeof(ulong))
                {
                    offset += 8;
                }
                else if (field.Value == typeof(byte))
                {
                    offset += 1;
                }
                else if (field.Value == typeof(bool))
                {
                    offset += 1;
                }
                else
                {
                    DynamicEventSchemas?.Clear();
                    throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a field named {field.Key} using an unsupported type {field.Value}");
                }
            }
            schema.Size = offset;
            return schema;
        }

        public static void Add(DynamicEventSchema dynamicEventSchema, bool allowPartialSchema = true)
        {
            CompiledSchema compiledSchema = Compile(dynamicEventSchema, allowPartialSchema);
            DynamicEventSchemas.Add(dynamicEventSchema.DynamicEventName, compiledSchema);
        }

        public static void Set(List<DynamicEventSchema> dynamicEventSchemas, bool allowPartialSchema = true)
        {
            DynamicEventSchemas.Clear();
            DynamicEventSchema.allowPartialSchema = allowPartialSchema;
            foreach (DynamicEventSchema dynamicEventSchema in dynamicEventSchemas)
            {
                Add(dynamicEventSchema, allowPartialSchema);
            }
        }
    }

    internal sealed class DynamicEventField
    {
        public required int FieldOffset { get; init; }
        public required Type FieldType { get; init; }
    }

    internal sealed class CompiledSchema : Dictionary<string, DynamicEventField>
    {
        public int MinOccurrence { get; set; }
        public int MaxOccurrence { get; set; }
        public int Size { get; set; }
    }

    internal sealed class DynamicIndex : DynamicObject
    {
        private readonly Dictionary<string, object?> index;

        public DynamicIndex(List<GCDynamicEvent> dynamicEvents)
        {
            this.index = new Dictionary<string, object?>();
            Dictionary<string, List<GCDynamicEvent>> indexedEvents = new Dictionary<string, List<GCDynamicEvent>>();
            foreach (string eventName in DynamicEventSchema.DynamicEventSchemas.Keys)
            {
                indexedEvents.Add(eventName, new List<GCDynamicEvent>());
            }
            foreach (GCDynamicEvent dynamicEvent in dynamicEvents)
            {
                List<GCDynamicEvent>? dynamicEventList;
                if (indexedEvents.TryGetValue(dynamicEvent.Name, out dynamicEventList))
                {
                    dynamicEventList.Add(dynamicEvent);
                }
                else
                {
                    if (!DynamicEventSchema.allowPartialSchema)
                    {
                        throw new Exception($"Event with unknown name {dynamicEvent.Name} is found.");
                    }
                }
            }
            foreach (string eventName in DynamicEventSchema.DynamicEventSchemas.Keys)
            {
                List<GCDynamicEvent> eventList = indexedEvents[eventName];
                CompiledSchema schema = DynamicEventSchema.DynamicEventSchemas[eventName];
                if (eventList.Count > schema.MaxOccurrence)
                {
                    throw new Exception($"More than {schema.MaxOccurrence} {eventName} is found.");
                }
                if (eventList.Count < schema.MinOccurrence)
                {
                    throw new Exception($"Less than {schema.MinOccurrence} {eventName} is found.");
                }
                if (schema.MaxOccurrence == 1)
                {
                    if (eventList.Count >= 1)
                    {
                        this.index.Add(eventName, new DynamicEventObject(eventList[0], schema));
                    }
                    else
                    {
                        this.index.Add(eventName, null);
                    }
                }
                else
                {
                    List<DynamicEventObject> output = new List<DynamicEventObject>();
                    foreach (GCDynamicEvent dynamicEvent in eventList)
                    {
                        output.Add(new DynamicEventObject(dynamicEvent, schema));
                    }
                    this.index.Add(eventName, output);
                }
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            return index.TryGetValue(binder.Name, out result);
        }
    }

    internal sealed class DynamicEventObject : DynamicObject
    {
        private string name;
        private Dictionary<string, object> fieldValues;

        public DynamicEventObject(GCDynamicEvent dynamicEvent, CompiledSchema schema)
        {
            this.name = dynamicEvent.Name;
            this.fieldValues = new Dictionary<string, object>();
            if (dynamicEvent.Payload.Length != schema.Size)
            {
                throw new Exception($"Event {dynamicEvent.Name} does not have matching size");
            }
            foreach (KeyValuePair<string, DynamicEventField> field in schema)
            {
                object? value = null;
                int fieldOffset = field.Value.FieldOffset;
                Type fieldType = field.Value.FieldType;

                if (fieldType == typeof(ushort))
                {
                    value = BitConverter.ToUInt16(dynamicEvent.Payload, fieldOffset);
                }
                else if (fieldType == typeof(uint))
                {
                    value = BitConverter.ToUInt32(dynamicEvent.Payload, fieldOffset);
                }
                else if (fieldType == typeof(float))
                {
                    value = BitConverter.ToSingle(dynamicEvent.Payload, fieldOffset);
                }
                else if (fieldType == typeof(ulong))
                {
                    value = BitConverter.ToUInt64(dynamicEvent.Payload, fieldOffset);
                }
                else if (fieldType == typeof(byte))
                {
                    value = dynamicEvent.Payload[fieldOffset];
                }
                else if (fieldType == typeof(bool))
                {
                    value = BitConverter.ToBoolean(dynamicEvent.Payload, fieldOffset);
                }
                else
                {
                    throw new Exception($"Provided schema has a field named {field.Key} using an unsupported type {fieldType}");
                }
                this.fieldValues.Add(field.Key, value);
            }
            this.fieldValues.Add("TimeStamp", dynamicEvent.TimeStamp);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            return this.fieldValues.TryGetValue(binder.Name, out result);
        }

        public override string ToString()
        {
            int fieldLength = 0;
            foreach (KeyValuePair<string, object> field in this.fieldValues)
            {
                fieldLength = Math.Max(fieldLength, field.Key.Length);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(this.name);
            foreach (KeyValuePair<string, object> field in this.fieldValues)
            {
                sb.AppendLine();
                sb.Append(field.Key);
                sb.Append(' ', fieldLength - field.Key.Length + 1);
                sb.Append(": ");
                sb.Append(field.Value);
            }

            return sb.ToString();
        }
    }
}
