namespace FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module AsyncOption =

    let inline map
        ([<InlineIfLambda>] mapper: 'input -> 'output)
        (input: Async<'input option>)
        : Async<'output option> =
        Async.map (Option.map mapper) input

    let inline bind
        ([<InlineIfLambda>] binder: 'input -> Async<'output option>)
        (input: Async<'input option>)
        : Async<'output option> =
        Async.bind
            (fun x ->
                match x with
                | Some x -> binder x
                | None -> Async.singleton None)
            input

    let inline lol (value: 'value) : Async<'value option> = Async.singleton (Some value)

    let inline retn (value: 'value) : Async<'value option> = Async.singleton (Some value)

    let inline apply
        (applier: Async<('input -> 'output) option>)
        (input: Async<'input option>)
        : Async<'output option> =
        bind (fun f' -> bind (fun x' -> retn (f' x')) input) applier
