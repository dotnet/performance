using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;

namespace ScenarioMeasurement.TraceEventParsers;

public class PerfLabGenericEventSourceLTTngProviderParser : TraceEventParser
{
    public static readonly Guid ProviderGuid = new Guid(0x9aef1fe1, 0xcbac, 0x4d70, 0x87, 0x2b, 0x4f, 0x84, 0x89, 0xa6, 0x26, 0xe2);

    public PerfLabGenericEventSourceLTTngProviderParser(TraceEventSource source)
        : base(source)
    { }

    public event Action<StartupTraceData> Startup
    {
        add
        {
            source.RegisterEventTemplate(StartupTemplate(value));
        }
        remove
        {
            source.UnregisterEventTemplate(value, PerfLabValues.StartupEventId, ProviderGuid);
        }
    }

    public event Action<OnMainTraceData> OnMain
    {
        add
        {
            source.RegisterEventTemplate(OnMainTemplate(value));
        }
        remove
        {
            source.UnregisterEventTemplate(value, PerfLabValues.OnMainEventId, ProviderGuid);
        }
    }

    protected override string GetProviderName() => PerfLabValues.LTTngProviderName;

    protected override IEnumerable<CtfEventMapping> EnumerateCtfEventMappings()
    {
        yield return new CtfEventMapping($"{PerfLabValues.LTTngProviderName}:{PerfLabValues.StartupEventName}", ProviderGuid, 0, PerfLabValues.StartupEventId, 0);
        yield return new CtfEventMapping($"{PerfLabValues.LTTngProviderName}:{PerfLabValues.OnMainEventName}", ProviderGuid, 0, PerfLabValues.OnMainEventId, 0);
    }

    static volatile TraceEvent[] s_templates;
    protected override void EnumerateTemplates(Func<string, string, EventFilterResponse> eventsToObserve, Action<TraceEvent> callback)
    {
        if (s_templates == null)
        {
            var templates = new TraceEvent[]
            {
                StartupTemplate(null),
                OnMainTemplate(null),
            };
            s_templates = templates;
        }
        foreach (var template in s_templates)
            if (eventsToObserve == null || eventsToObserve(template.ProviderName, template.EventName) == EventFilterResponse.AcceptEvent)
                callback(template);
    }

    static StartupTraceData StartupTemplate(Action<StartupTraceData> action) => new StartupTraceData(action, PerfLabValues.StartupEventId, 1, PerfLabValues.StartupEventName, Guid.Empty, 0, string.Empty, ProviderGuid, PerfLabValues.LTTngProviderName);
    static OnMainTraceData OnMainTemplate(Action<OnMainTraceData> action) => new OnMainTraceData(action, PerfLabValues.OnMainEventId, 1, PerfLabValues.OnMainEventName, Guid.Empty, 0, string.Empty, ProviderGuid, PerfLabValues.LTTngProviderName);

    public abstract class PerfLabTraceData<TSelf> : TraceEvent
        where TSelf : PerfLabTraceData<TSelf>
    {
        private event Action<TSelf> _target;

        internal PerfLabTraceData(Action<TSelf> target, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            _target = target;
        }

        protected override void Dispatch()
        {
            _target((TSelf)this);
        }

        protected override Delegate Target
        {
            get => _target;
            set => _target = (Action<TSelf>)value;
        }

        public override string[] PayloadNames => Array.Empty<string>();

        public override object PayloadValue(int index) => null;
    }
    public sealed class StartupTraceData : PerfLabTraceData<StartupTraceData>
    {
        public StartupTraceData(Action<StartupTraceData> target, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(target, eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        { }
    }
    public sealed class OnMainTraceData : PerfLabTraceData<OnMainTraceData>
    {
        public OnMainTraceData(Action<OnMainTraceData> target, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(target, eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        { }
    }
}
