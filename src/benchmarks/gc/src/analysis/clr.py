# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from typing import Any, cast, Sequence, Type

from ..commonlib.get_built import get_built_gcperf
from ..commonlib.type_utils import T

from .clr_types import (
    AbstractCtfTraceEventSource,
    AbstractETWTraceEventSource,
    AbstractEventPipeEventSource,
    AbstractTraceEvent,
    AbstractTraceEventDispatcher,
    AbstractTraceEventSource,
    AbstractTraceProcess,
    AbstractTraceLoadedDotNetRuntime,
    AbstractTraceLoadedDotNetRuntimeExtensions,
    AbstractTraceProcessesExtensions,
    AbstractGCIssues,
    AbstractBGCAllocWaitInfo,
    AbstractGCPerHeapHistory,
    AbstractGCStats,
    AbstractPythonnetUtil,
    AbstractServerGcHistory,
    AbstractSymbolReader,
    AbstractThreadWorkSpan,
    AbstractTraceGC,
    AbstractTraceEvents,
    AbstractTraceLog,
    AbstractGCPerHeapHistoryGenData,
    AbstractAnalysis,
    AbstractEtlTrace,
    AbstractFile,
    AbstractBindingFlags,
    AbstractStackSourceSample,
    AbstractJoinAnalysis,
    AbstractTimeSpanUtil,
)


# This class contains no data, but is passed around as proof that we've set up CLR.
class Clr:
    @property
    def _system(self) -> Any:
        # pylint:disable=import-outside-toplevel
        import System  # type: ignore

        return System

    @property
    def _tracing(self) -> Any:
        from Microsoft.Diagnostics import Tracing  # type: ignore

        return Tracing

    @property
    def _symbols(self) -> Any:
        from Microsoft.Diagnostics import Symbols

        return Symbols

    @property
    def _analysis(self) -> Any:
        from Microsoft.Diagnostics.Tracing import Analysis  # type: ignore

        return Analysis

    @property
    def _cap_gc(self) -> Any:
        from Microsoft.Diagnostics.Tracing.Analysis.Cap import GC  # type: ignore

        return GC

    @property
    def _gc(self) -> Any:
        from Microsoft.Diagnostics.Tracing.Analysis import GC  # type: ignore

        return GC

    @property
    def _etlx(self) -> Any:
        from Microsoft.Diagnostics.Tracing import Etlx

        return Etlx

    @property
    def _clr(self) -> Any:
        from Microsoft.Diagnostics.Tracing.Parsers import Clr as ClrParser  # type: ignore

        return ClrParser

    @property
    def _gcperf(self) -> Any:
        # pylint:disable=import-outside-toplevel
        import GCPerf  # type: ignore

        return GCPerf

    @property
    def _io(self) -> Any:
        from System import IO

        return IO

    @property
    def _reflection(self) -> Any:
        from System import Reflection

        return Reflection

    @property
    def _stacks(self) -> Any:
        from Microsoft.Diagnostics.Tracing import Stacks

        return Stacks

    @property
    def SymbolReader(self) -> Type[AbstractSymbolReader]:
        return cast(Type[AbstractSymbolReader], self._symbols.SymbolReader)

    @property
    def CtfTraceEventSource(self) -> Type[AbstractCtfTraceEventSource]:
        return cast(Type[AbstractCtfTraceEventSource], self._tracing.CtfTraceEventSource)

    @property
    def ETWTraceEventSource(self) -> Type[AbstractETWTraceEventSource]:
        return cast(Type[AbstractETWTraceEventSource], self._tracing.ETWTraceEventSource)

    @property
    def EventPipeEventSource(self) -> Type[AbstractEventPipeEventSource]:
        return cast(Type[AbstractEventPipeEventSource], self._tracing.EventPipeEventSource)

    @property
    def TraceEvent(self) -> Type[AbstractTraceEvent]:
        return cast(Type[AbstractTraceEvent], self._tracing.TraceEvent)

    @property
    def TraceEventDispatcher(self) -> Type[AbstractTraceEventDispatcher]:
        return cast(Type[AbstractTraceEventDispatcher], self._tracing.TraceEventDispatcher)

    @property
    def TraceEventSource(self) -> Type[AbstractTraceEventSource]:
        return cast(Type[AbstractTraceEventSource], self._tracing.TraceEventSource)

    @property
    def TraceProcess(self) -> Type[AbstractTraceProcess]:
        return cast(Type[AbstractTraceProcess], self._analysis.TraceProcess)

    @property
    def TraceLoadedDotNetRuntime(self) -> Type[AbstractTraceLoadedDotNetRuntime]:
        return cast(Type[AbstractTraceLoadedDotNetRuntime], self._analysis.TraceLoadedDotNetRuntime)

    @property
    def TraceLoadedDotNetRuntimeExtensions(
        self,
    ) -> Type[AbstractTraceLoadedDotNetRuntimeExtensions]:
        return cast(
            Type[AbstractTraceLoadedDotNetRuntimeExtensions],
            self._analysis.TraceLoadedDotNetRuntimeExtensions,
        )

    @property
    def TraceProcessesExtensions(self) -> Type[AbstractTraceProcessesExtensions]:
        return cast(Type[AbstractTraceProcessesExtensions], self._analysis.TraceProcessesExtensions)

    @property
    def GCIssues(self) -> Type[AbstractGCIssues]:
        return cast(Type[AbstractGCIssues], self._cap_gc.GCIssues)

    @property
    def BGCAllocWaitInfo(self) -> Type[AbstractBGCAllocWaitInfo]:
        return cast(Type[AbstractBGCAllocWaitInfo], self._gc.BGCAllocWaitInfo)

    @property
    def GCPerHeapHistory(self) -> Type[AbstractGCPerHeapHistory]:
        return cast(Type[AbstractGCPerHeapHistory], self._gc.GCPerHeapHistory)

    @property
    def GCStats(self) -> Type[AbstractGCStats]:
        return cast(Type[AbstractGCStats], self._gc.GCStats)

    @property
    def ServerGcHistory(self) -> Type[AbstractServerGcHistory]:
        return cast(Type[AbstractServerGcHistory], self._gc.ServerGcHistory)

    @property
    def ThreadWorkSpan(self) -> Type[AbstractThreadWorkSpan]:
        return cast(Type[AbstractThreadWorkSpan], self._gc.ThreadWorkSpan)

    @property
    def TraceGC(self) -> Type[AbstractTraceGC]:
        return cast(Type[AbstractTraceGC], self._gc.TraceGC)

    @property
    def TraceEvents(self) -> Type[AbstractTraceEvents]:
        return cast(Type[AbstractTraceEvents], self._etlx.TraceEvents)

    @property
    def TraceLog(self) -> Type[AbstractTraceLog]:
        return cast(Type[AbstractTraceLog], self._etlx.TraceLog)

    @property
    def GCPerHeapHistoryGenData(self) -> Type[AbstractGCPerHeapHistoryGenData]:
        return cast(Type[AbstractGCPerHeapHistoryGenData], self._clr.GCPerHeapHistoryGenData)

    @property
    def Analysis(self) -> Type[AbstractAnalysis]:
        return cast(Type[AbstractAnalysis], self._gcperf.Analysis)

    @property
    def EtlTrace(self) -> Type[AbstractEtlTrace]:
        return cast(Type[AbstractEtlTrace], self._gcperf.EtlTrace)

    @property
    def PythonnetUtil(self) -> Type[AbstractPythonnetUtil]:
        return cast(Type[AbstractPythonnetUtil], self._gcperf.PythonnetUtil)

    @property
    def File(self) -> Type[AbstractFile]:
        return cast(Type[AbstractFile], self._io.File)

    @property
    def BindingFlags(self) -> Type[AbstractBindingFlags]:
        return cast(Type[AbstractBindingFlags], self._reflection.BindingFlags)

    @property
    def StackSourceSample(self) -> Type[AbstractStackSourceSample]:
        return cast(Type[AbstractStackSourceSample], self._stacks.StackSourceSample)

    @property
    def Action1(self) -> Any:
        return getattr(self._system, "Action`1")

    @property
    def Array(self) -> Any:
        return getattr(self._system, "Array")

    @property
    def StackSourceCallStackIndex(self) -> Any:
        return self._stacks.StackSourceCallStackIndex

    @property
    def JoinAnalysis(self) -> Type[AbstractJoinAnalysis]:
        return cast(Type[AbstractJoinAnalysis], self._gcperf.JoinAnalysis)

    @property
    def TimeSpanUtil(self) -> Type[AbstractTimeSpanUtil]:
        return cast(Type[AbstractTimeSpanUtil], self._gcperf.TimeSpanUtil)

    def to_array(self, t: Type[T], seq: Sequence[T]) -> Any:
        assert isinstance(seq, list)
        cs_arr = self.Array[t](seq)
        assert cs_arr.Length == len(seq)
        return cs_arr


def get_clr() -> Clr:
    # Import this lazily because pythonnet is hard to install on some machines,
    # and some commands can do without it
    from clr import AddReference

    for path in ("System.Collections", *(str(d) for d in get_built_gcperf())):
        AddReference(path)
    return Clr()


SYMBOL_PATH = ";".join(
    [
        f"SRV*C:\\symbols*{x}"
        for x in [
            f"http://symweb.corp.microsoft.com",
            f"http://msdl.microsoft.com/download/symbols",
            f"https://dotnet.myget.org/F/dotnet-core/symbols",
        ]
    ]
)
