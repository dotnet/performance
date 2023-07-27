namespace global

open System.Diagnostics.Tracing

[<EventSource(Guid = "9bb228bd-1033-5cf0-1a56-c2dbbe0ebc86")>]
type PerfLabGenericEventSource() =
    inherit EventSource()

    [<Literal>]
    let MagicConstant = 6666

    static member val Log = new PerfLabGenericEventSource()

    [<Event(6667)>] // MagicConstant + 1
    member this.Startup() = this.WriteEvent(MagicConstant + 1)

    [<Event(6668)>] // MagicConstant + 2
    member this.OnMain() = this.WriteEvent(MagicConstant + 2)