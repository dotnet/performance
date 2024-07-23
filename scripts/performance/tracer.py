from logging import getLogger

class TracingStateManager:
    '''A class to manage the state of tracing.'''
    def __init__(self):
        self.trace_provider_initialized = False
        self.trace_provider_console_exporter_enabled = False
        self.trace_opentelemetry_imported = False
    
    def set_initialized(self, value : bool): self.trace_provider_initialized = value
    def set_console_exporter_enabled(self, value : bool): self.trace_provider_console_exporter_enabled = value
    def set_opentelemetry_imported(self, value : bool): self.trace_opentelemetry_imported = value
    def get_initialized(self): return self.trace_provider_initialized
    def get_console_exporter_enabled(self): return self.trace_provider_console_exporter_enabled
    def get_opentelemetry_imported(self): return self.trace_opentelemetry_imported

tracing_state_manager = TracingStateManager()
try:
    from opentelemetry import trace
    from opentelemetry.sdk.trace import TracerProvider
    from opentelemetry.sdk.trace.export import BatchSpanProcessor, ConsoleSpanExporter
    tracing_state_manager.set_opentelemetry_imported(True)

except ImportError:
    pass

def setup_tracing():
    '''Set up the OpenTelemetry trace provider.'''
    if tracing_state_manager.get_initialized() or not tracing_state_manager.get_opentelemetry_imported():
        return
    provider = TracerProvider()
    trace.set_tracer_provider(provider)
    tracing_state_manager.set_initialized(True)

def enable_trace_console_exporter():
    '''Enable the console exporter for trace spans.'''
    if tracing_state_manager.get_console_exporter_enabled() or not tracing_state_manager.get_opentelemetry_imported():
        if not tracing_state_manager.get_opentelemetry_imported():
            getLogger().warning('OpenTelemetry not imported. Skipping OpenTelemetry console logger initialization.')
        return
    provider = trace.get_tracer_provider()
    processor = BatchSpanProcessor(ConsoleSpanExporter())
    provider.add_span_processor(processor)
    tracing_state_manager.set_console_exporter_enabled(True)

def get_tracer(name="dotnet.performance"):
    '''Return a tracer with the specified name.'''
    return AwareTracer(name)

def is_opentelemetry_imported() -> bool:
    '''Return whether OpenTelemetry has been imported.'''
    return tracing_state_manager.get_opentelemetry_imported()

def is_provider_initialized() -> bool:
    '''Return whether the trace provider has been initialized.'''
    return tracing_state_manager.get_initialized()

def is_console_exporter_enabled() -> bool:
    '''Return whether the console exporter has been enabled.'''
    return tracing_state_manager.get_console_exporter_enabled()

class AwareTracer:
    """
    A OpenTelemetry aware tracer implementation that is used as a wrapper for OpenTelemetry calls where OpenTelemetry is not guaranteed be installed.
    When not installed, the tracer is a no-op and the decorated functions are executed as if the decorator was not there..
    """
    tracer = None
    def __init__(self, name="dotnet.performance"):
        if is_opentelemetry_imported():
            self.tracer = trace.get_tracer(name)

    def start_as_current_span(self, *top_args, **top_kwargs):
        """
        Decorator that starts a new span as the current span if OpenTelemetry is imported.
        If OpenTelemetry is not imported, the function is executed without starting a new span.

        Args:
            *top_args: Variable length argument list that will be passed to OpenTelemetry Tracer.start_as_current_span.
            **top_kwargs: Arbitrary keyword arguments.

        Returns:
            The result of executing the decorated function.
        """
        def decorator(func):
            def wrapper(*args, **kwargs):
                if self.tracer is not None:
                    with self.tracer.start_as_current_span(*top_args, **top_kwargs):
                        return func(*args, **kwargs)
                else:
                    return func(*args, **kwargs)
            return wrapper
        return decorator
    