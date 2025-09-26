import functools
from logging import getLogger
from typing import TYPE_CHECKING, Callable, Optional, TypeVar
from typing_extensions import ParamSpec

class TracingStateManager:
    '''A class to manage the state of tracing.'''
    def __init__(self):
        self.trace_provider_initialized = False
        self.trace_provider_console_exporter_enabled = False
    
    def set_initialized(self, value : bool): self.trace_provider_initialized = value
    def set_console_exporter_enabled(self, value : bool): self.trace_provider_console_exporter_enabled = value
    def get_initialized(self): return self.trace_provider_initialized
    def get_console_exporter_enabled(self): return self.trace_provider_console_exporter_enabled

tracing_state_manager = TracingStateManager()

def setup_tracing():
    '''Set up the OpenTelemetry trace provider.'''
    if tracing_state_manager.get_initialized():
        return
    
    try:
        from opentelemetry import trace
        from opentelemetry.sdk.trace import TracerProvider
    except:
        return

    provider = TracerProvider()
    trace.set_tracer_provider(provider)
    tracing_state_manager.set_initialized(True)

def enable_trace_console_exporter():
    '''Enable the console exporter for trace spans.'''
    if tracing_state_manager.get_console_exporter_enabled():
        return
    
    try:
        from opentelemetry import trace
        from opentelemetry.sdk.trace.export import BatchSpanProcessor, ConsoleSpanExporter
    except:
        getLogger().warning('OpenTelemetry not imported. Skipping OpenTelemetry console logger initialization.')
        return

    provider = trace.get_tracer_provider()
    processor = BatchSpanProcessor(ConsoleSpanExporter())
    provider.add_span_processor(processor) # pyright: ignore[reportUnknownMemberType, reportAttributeAccessIssue] -- exists but not in type stubs
    tracing_state_manager.set_console_exporter_enabled(True)

def get_tracer(name: str="dotnet.performance"):
    '''Return a tracer with the specified name.'''
    return AwareTracer(name)

def is_provider_initialized() -> bool:
    '''Return whether the trace provider has been initialized.'''
    return tracing_state_manager.get_initialized()

def is_console_exporter_enabled() -> bool:
    '''Return whether the console exporter has been enabled.'''
    return tracing_state_manager.get_console_exporter_enabled()

P = ParamSpec("P")
R = TypeVar("R")
class AwareTracer:
    """
    A OpenTelemetry aware tracer implementation that is used as a wrapper for OpenTelemetry calls where OpenTelemetry is not guaranteed be installed.
    When not installed, the tracer is a no-op and the decorated functions are executed as if the decorator was not there..
    """
    def __init__(self, name: str = "dotnet.performance") -> None:
        if TYPE_CHECKING:
            from opentelemetry.trace import Tracer # pyright: ignore[reportMissingTypeStubs]
            self._tracer: Optional[Tracer]
            
        try:
            from opentelemetry import trace
        except ImportError:
            self._tracer = None
        else:
            self._tracer = trace.get_tracer(name)

    def start_as_current_span(self, name: str) -> Callable[[Callable[P, R]], Callable[P, R]]:
        """
        Decorator that starts a new span as the current span if OpenTelemetry is imported.
        If OpenTelemetry is not imported, the function is executed without starting a new span.

        Args:
            *top_args: Variable length argument list that will be passed to OpenTelemetry Tracer.start_as_current_span.
            **top_kwargs: Arbitrary keyword arguments.

        Returns:
            The result of executing the decorated function.
        """
        def decorator(func: Callable[P, R]) -> Callable[P, R]:
            # If no tracer, just return the original function unchanged (best for typing & introspection).
            if self._tracer is None:
                return func

            @functools.wraps(func)
            def wrapped(*args: P.args, **kwargs: P.kwargs) -> R:
                if self._tracer is None:
                    return func(*args, **kwargs)
                
                with self._tracer.start_as_current_span(name):
                    return func(*args, **kwargs)

            return wrapped

        return decorator
    