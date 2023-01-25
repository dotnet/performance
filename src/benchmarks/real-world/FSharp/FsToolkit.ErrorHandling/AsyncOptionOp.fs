namespace FsToolkit.ErrorHandling.Operator.AsyncOption

open FsToolkit.ErrorHandling

[<AutoOpen>]
module AsyncOption =

    let inline (<!>)
        (([<InlineIfLambda>] mapper: 'input -> 'output))
        (input: Async<'input option>)
        : Async<'output option> =
        AsyncOption.map mapper input

    let inline (<*>)
        (applier: Async<('input -> 'output) option>)
        (input: Async<'input option>)
        : Async<'output option> =
        AsyncOption.apply applier input

    let inline (>>=)
        (input: Async<'input option>)
        ([<InlineIfLambda>] binder: 'input -> Async<'output option>)
        : Async<'output option> =
        AsyncOption.bind binder input
