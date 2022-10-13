namespace FsToolkit.ErrorHandling.Operator.Result

open FsToolkit.ErrorHandling

[<AutoOpen>]
module Result =
    let inline (<!>)
        (([<InlineIfLambda>] mapper: 'okInput -> 'okOutput))
        (input: Result<'okInput, 'error>)
        : Result<'okOutput, 'error> =
        Result.map mapper input

    let inline (<*>)
        (applier: Result<'okInput -> 'okOutput, 'error>)
        (input: Result<'okInput, 'error>)
        : Result<'okOutput, 'error> =
        Result.apply applier input

    let inline (>>=)
        (input: Result<'input, 'error>)
        ([<InlineIfLambda>] binder: 'input -> Result<'okOutput, 'error>)
        : Result<'okOutput, 'error> =
        Result.bind binder input
