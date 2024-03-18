from opentelemetry import trace
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor, ConsoleSpanExporter

__trace_provider_initialized = False
__trace_provider_console_exporter_enabled = False

def setup_trace_provider():
    '''Set up the OpenTelemetry trace provider.'''
    global __trace_provider_initialized
    if __trace_provider_initialized:
        return
    provider = TracerProvider()
    trace.set_tracer_provider(provider)
    __trace_provider_initialized = True

def enable_trace_console_exporter():
    '''Enable the console exporter for trace spans.'''
    global __trace_provider_console_exporter_enabled
    if __trace_provider_console_exporter_enabled:
        return
    provider = trace.get_tracer_provider()
    processor = BatchSpanProcessor(ConsoleSpanExporter())
    provider.add_span_processor(processor)
    __trace_provider_console_exporter_enabled = True