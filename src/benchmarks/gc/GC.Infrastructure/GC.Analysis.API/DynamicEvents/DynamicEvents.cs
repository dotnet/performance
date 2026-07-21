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
        };

        internal static bool allowPartialSchema;

        public required string DynamicEventName { get; init; }

        public required List<KeyValuePair<string, Type>> Fields { get; init; }

        public int MinOccurrence { get; init; } = 0;

        public int MaxOccurrence { get; init; } = 1;

        internal static bool TryComputeOffset(byte[] payload, List<int> unadjustedArrayLengthOffsets, List<int> arrayElementSizes, int unadjustedOffset, out int adjustedOffset)
        {
            //
            // Any offsets on or before the first array content will have their
            // actual offset equals to the unadjusted offsets. In particular,
            // the offset to the first array's length is never adjusted. So we can
            // find the length of the first array.
            //
            int adjustment = 0;
            for (int i = 0; i < unadjustedArrayLengthOffsets.Count; i++)
            {
                int unadjustedArrayLengthOffset = unadjustedArrayLengthOffsets[i];
                if (unadjustedOffset > unadjustedArrayLengthOffset)
                {
                    if (payload.Length <= unadjustedArrayLengthOffset)
                    {
                        adjustedOffset = 0;
                        return false;
                    }

                    //
                    // If we had a second array, the second arrays unadjusted offsets will
                    // be earlier than its actual offset, but we know how to adjust it. So
                    // we can get the actual offset to the second array's length.
                    //
                    byte arrayLength = payload[unadjustedArrayLengthOffset + adjustment];
                    adjustment += arrayLength * arrayElementSizes[i];
                }
                else
                {
                    //
                    // If the offset are are looking for is not after the kth array length
                    // Then we should stop computing the adjustment
                    //
                    break;
                }
            }

            adjustedOffset = unadjustedOffset + adjustment;
            return true;
        }

        internal static int ComputeOffset(byte[] payload, List<int> unadjustedArrayLengthOffsets, List<int> arrayElementSizes, int unadjustedOffset)
        {
            if (TryComputeOffset(payload, unadjustedArrayLengthOffsets, arrayElementSizes, unadjustedOffset, out int adjustedOffset))
            {
                return adjustedOffset;
            }
            else
            {
                throw new Exception("Fail to compute offset, this should not happen");
            }
        }

        internal static bool IsSupportedPrimitiveType(Type type)
        {
            return
            (type == typeof(ushort)) ||
            (type == typeof(uint)) ||
            (type == typeof(float)) ||
            (type == typeof(ulong)) ||
            (type == typeof(byte)) ||
            (type == typeof(bool)) ||
            false;
        }

        internal static int Size(Type type)
        {
            if (type == typeof(ushort)) { return 2; }
            else if (type == typeof(uint)) { return 4; }
            else if (type == typeof(float)) { return 8; }
            else if (type == typeof(ulong)) { return 8; }
            else if (type == typeof(byte)) { return 1; }
            else if (type == typeof(bool)) { return 1; }
            else { throw new Exception("Wrong type"); }
        }

        internal static object Decode(Type type, byte[] payload, int offset)
        {
            if (type == typeof(ushort)) { return BitConverter.ToUInt16(payload, offset); }
            else if (type == typeof(uint)) { return BitConverter.ToUInt32(payload, offset); }
            else if (type == typeof(float)) { return BitConverter.ToSingle(payload, offset); }
            else if (type == typeof(ulong)) { return BitConverter.ToUInt64(payload, offset); }
            else if (type == typeof(byte)) { return payload[offset]; }
            else if (type == typeof(bool)) { return BitConverter.ToBoolean(payload, offset); }
            else { throw new Exception("Wrong type"); }
        }

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

            //
            // With array, a field in an event no longer have a fixed offset.
            // Unadjusted offsets are offsets as if all the arrays are empty
            // This list will store the unadjusted offsets to array lengths
            //
            // This is sufficient to get to the actual offsets, once we have
            // the payload, see TryComputeOffset for more details.
            //
            List<int> unadjustedArrayLengthOffsets = new List<int>();
            List<int> arrayElementSizes = new List<int>();
            int offset = 0;
            foreach (KeyValuePair<string, Type> field in dynamicEventSchema.Fields)
            {
                if (schema.ContainsKey(field.Key))
                {
                    DynamicEventSchemas?.Clear();
                    throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a duplicated field named {field.Key}");
                }
                Func<byte[], object>? fieldFetcher = null;

                // The local variable makes sure we capture the value of the offset variable in the lambdas
                int currentOffset = offset;
                if (IsSupportedPrimitiveType(field.Value))
                {
                    fieldFetcher = (payload) => Decode(field.Value, payload, ComputeOffset(payload, unadjustedArrayLengthOffsets, arrayElementSizes, currentOffset));
                    offset += Size(field.Value);
                }
                else if (field.Value.IsArray && IsSupportedPrimitiveType(field.Value.GetElementType()))
                {
                    Type elementType = field.Value.GetElementType();
                    int elementSize = Size(elementType);
                    fieldFetcher = (payload) =>
                    {
                        int unadjustedArrayLengthOffset = ComputeOffset(payload, unadjustedArrayLengthOffsets, arrayElementSizes, currentOffset);
                        int length = (int)payload[unadjustedArrayLengthOffset];
                        Array result = Array.CreateInstance(elementType, length);
                        for (int i = 0; i < length; i++)
                        {
                            result.SetValue(Decode(elementType, payload, unadjustedArrayLengthOffset + 1 + elementSize * i), i);
                        }
                        return result;
                    };
                    unadjustedArrayLengthOffsets.Add(offset);
                    arrayElementSizes.Add(elementSize);
                    offset += 1;
                }
                else
                {
                    DynamicEventSchemas?.Clear();
                    throw new Exception($"Provided event named {dynamicEventSchema.DynamicEventName} has a field named {field.Key} using an unsupported type {field.Value}");
                }
                schema.Add(field.Key, new DynamicEventField { FieldFetcher = fieldFetcher });
            }
            schema.SizeValidator = (payload) => payload.Length == ComputeOffset(payload, unadjustedArrayLengthOffsets, arrayElementSizes, offset);
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
        public required Func<byte[], object> FieldFetcher;
    }

    internal sealed class CompiledSchema : Dictionary<string, DynamicEventField>
    {
        public int MinOccurrence { get; set; }
        public int MaxOccurrence { get; set; }
        public Func<byte[], bool> SizeValidator { get; set; }
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
            if (!schema.SizeValidator(dynamicEvent.Payload))
            {
                throw new Exception($"Event {dynamicEvent.Name} does not have matching size");
            }
            foreach (KeyValuePair<string, DynamicEventField> field in schema)
            {
                object value = field.Value.FieldFetcher(dynamicEvent.Payload);
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
