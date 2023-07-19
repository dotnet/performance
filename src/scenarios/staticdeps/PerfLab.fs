namespace global

open System.Diagnostics.Tracing

[<EventSource(Guid = "9bb228bd-1033-5cf0-1a56-c2dbbe0ebc86")>]
type PerfLabGenericEventSource() =
    inherit EventSource()

    static member val Log = new PerfLabGenericEventSource()

    member this.Startup() = this.WriteEvent(1)

    member this.OnMain() = this.WriteEvent(2)