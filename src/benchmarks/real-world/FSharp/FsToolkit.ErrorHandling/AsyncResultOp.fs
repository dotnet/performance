namespace FsToolkit.ErrorHandling.Operator.AsyncResult

open FsToolkit.ErrorHandling

[<AutoOpen>]
module AsyncResult =

    let inline (<!>)
        (([<InlineIfLambda>] mapper: 'okInput -> 'okOutput))
        (input: Async<Result<'okInput, 'error>>)
        : Async<Result<'okOutput, 'error>> =
        AsyncResult.map mapper input

    let inline (<*>)
        (applier: Async<Result<'okInput -> 'okOutput, 'error>>)
        (input: Async<Result<'okInput, 'error>>)
        : Async<Result<'okOutput, 'error>> =
        AsyncResult.apply applier input

    let inline (>>=)
        (input: Async<Result<'input, 'error>>)
        ([<InlineIfLambda>] binder: 'input -> Async<Result<'okOutput, 'error>>)
        : Async<Result<'okOutput, 'error>> =
        AsyncResult.bind binder input
