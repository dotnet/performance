Imports System.Diagnostics.Tracing

<EventSource(Guid:="9bb228bd-1033-5cf0-1a56-c2dbbe0ebc86")>
Class PerfLabGenericEventSource
    Inherits EventSource

    Public Shared ReadOnly Property Log As PerfLabGenericEventSource = New PerfLabGenericEventSource()

    Public Sub Startup()
        WriteEvent(1)
    End Sub

    Public Sub OnMain()
        WriteEvent(2)
    End Sub
End Class
