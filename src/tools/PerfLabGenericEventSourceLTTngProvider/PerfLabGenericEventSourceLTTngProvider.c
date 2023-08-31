#define LTTNG_UST_TRACEPOINT_CREATE_PROBES
#define LTTNG_UST_TRACEPOINT_DEFINE
#include "PerfLabGenericEventSourceLTTngProvider.h"

void emit_startup()
{
    lttng_ust_tracepoint(PerfLabGenericEventSourceLTTngProvider, Startup);
}

void emit_on_main()
{
    lttng_ust_tracepoint(PerfLabGenericEventSourceLTTngProvider, OnMain);
}