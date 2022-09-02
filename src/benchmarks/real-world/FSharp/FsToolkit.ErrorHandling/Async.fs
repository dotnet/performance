namespace FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module Async =

    let inline singleton (value: 'value) : Async<'value> = value |> async.Return
    let inline retn (value: 'value) : Async<'value> = value |> async.Return

    let inline bind ([<InlineIfLambda>] binder: 'input -> Async<'output>) (input: Async<'input>) : Async<'output> =
        async.Bind(input, binder)

    let inline apply (applier: Async<'input -> 'output>) (input: Async<'input>) : Async<'output> =
        bind (fun f' -> bind (fun x' -> singleton (f' x')) input) applier

    let inline map ([<InlineIfLambda>] mapper: 'input -> 'output) (input: Async<'input>) : Async<'output> =
        bind (fun x' -> mapper x' |> singleton) input

    let inline map2
        ([<InlineIfLambda>] mapper: 'input1 -> 'input2 -> 'output)
        (input1: Async<'input1>)
        (input2: Async<'input2>)
        : Async<'output> =
        bind (fun x -> bind (fun y -> mapper x y |> singleton) input2) input1

    let inline map3
        ([<InlineIfLambda>] mapper: 'input1 -> 'input2 -> 'input3 -> 'output)
        (input1: Async<'input1>)
        (input2: Async<'input2>)
        (input3: Async<'input3>)
        : Async<'output> =
        bind (fun x -> bind (fun y -> bind (fun z -> mapper x y z |> singleton) input3) input2) input1


    /// Takes two asyncs and returns a tuple of the pair
    let inline zip (left: Async<'left>) (right: Async<'right>) : Async<'left * 'right> =
        bind (fun l -> bind (fun r -> singleton (l, r)) right) left

module AsyncOperators =

    let inline (<!>) ([<InlineIfLambda>] mapper: 'input -> 'output) (input: Async<'input>) : Async<'output> =
        Async.map mapper input

    let inline (<*>) (applier: Async<'input -> 'output>) (input: Async<'input>) : Async<'output> =
        Async.apply applier input

    let inline (>>=) (input: Async<'input>) ([<InlineIfLambda>] binder: 'input -> Async<'output>) : Async<'output> =
        Async.bind binder input
