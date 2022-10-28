namespace FsToolkit.ErrorHandling



#if NETSTANDARD2_0
open System

/// InlineIfLambda doesn't exist until FSharp.Core 6.0.
/// Since we're targeting netstandard2.0 with FSharp.Core 4.7 to keep this libraries' reach
/// we need to create a fake attribute that does nothing instead of having ifdefs in each function argument
[<AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)>]
[<Sealed>]
type internal InlineIfLambdaAttribute() =
    inherit System.Attribute()

open System.Runtime.CompilerServices

// Let all the child libraries have access to this shim as well
[<assembly: InternalsVisibleTo("FsToolkit.ErrorHandling.TaskResult")>]
[<assembly: InternalsVisibleTo("FsToolkit.ErrorHandling.JobResult")>]
[<assembly: InternalsVisibleTo("FsToolkit.ErrorHandling.AsyncSeq")>]
do ()
#endif
