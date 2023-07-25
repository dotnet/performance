Imports System.Diagnostics.Tracing

<EventSource(Guid:="9bb228bd-1033-5cf0-1a56-c2dbbe0ebc86")>
Class PerfLabGenericEventSource
    Inherits EventSource

    Private Const MagicConstant As Integer = 6666

    Public Shared ReadOnly Property Log As PerfLabGenericEventSource = New PerfLabGenericEventSource()

    Public Sub Startup()
        WriteEvent(MagicConstant + 1)
    End Sub

    Public Sub OnMain()
        WriteEvent(MagicConstant + 2)
    End Sub
End Class
