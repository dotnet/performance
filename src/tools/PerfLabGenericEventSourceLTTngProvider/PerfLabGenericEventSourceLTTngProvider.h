#undef LTTNG_UST_TRACEPOINT_PROVIDER
#define LTTNG_UST_TRACEPOINT_PROVIDER PerfLabGenericEventSourceLTTngProvider

#undef LTTNG_UST_TRACEPOINT_INCLUDE
#define LTTNG_UST_TRACEPOINT_INCLUDE "./PerfLabGenericEventSourceLTTngProvider.h"

#if !defined(PERFLABGENERICEVENTSOURCELTTNGPROVIDER_H) || defined(LTTNG_UST_TRACEPOINT_HEADER_MULTI_READ)
#define PERFLABGENERICEVENTSOURCELTTNGPROVIDER_H

#include <lttng/tracepoint.h>

LTTNG_UST_TRACEPOINT_EVENT(
    PerfLabGenericEventSourceLTTngProvider,

    Startup,

    LTTNG_UST_TP_ARGS(),

    LTTNG_UST_TP_FIELDS()
)

LTTNG_UST_TRACEPOINT_EVENT(
    PerfLabGenericEventSourceLTTngProvider,

    OnMain,

    LTTNG_UST_TP_ARGS(),

    LTTNG_UST_TP_FIELDS()
)

#endif /* PERFLABGENERICEVENTSOURCELTTNGPROVIDER_H */

#include <lttng/tracepoint-event.h>