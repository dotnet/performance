namespace FsToolkit.ErrorHandling.Operator.AsyncResultOption

open FsToolkit.ErrorHandling

[<AutoOpen>]
module AsyncResultOption =

    let inline (<!>)
        ([<InlineIfLambda>] mapper: 'okInput -> 'okOutput)
        (input: Async<Result<'okInput option, 'error>>)
        : Async<Result<'okOutput option, 'error>> =
        AsyncResultOption.map mapper input

    let inline (<*>)
        (applier: Async<Result<('okInput -> 'okOutput) option, 'error>>)
        (input: Async<Result<'okInput option, 'error>>)
        : Async<Result<'okOutput option, 'error>> =
        AsyncResultOption.apply applier input

    let inline (>>=)
        (input: Async<Result<'okInput option, 'error>>)
        ([<InlineIfLambda>] binder: 'okInput -> Async<Result<'okOutput option, 'error>>)
        : Async<Result<'okOutput option, 'error>> =
        AsyncResultOption.bind binder input
