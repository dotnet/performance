namespace FsToolkit.ErrorHandling

[<AutoOpen>]
module AsyncResultOptionCE =

    type AsyncResultOptionBuilder() =
        member inline _.Return(value: 'ok) : Async<Result<'ok option, 'error>> = AsyncResultOption.retn value

        member inline _.ReturnFrom(value: Async<Result<'ok option, 'error>>) : Async<Result<'ok option, 'error>> = value

        member inline _.Bind
            (
                input: Async<Result<'okInput option, 'error>>,
                [<InlineIfLambda>] binder: 'okInput -> Async<Result<'okOutput option, 'error>>
            ) : Async<Result<'okOutput option, 'error>> =
            AsyncResultOption.bind binder input

        member inline _.Combine(aro1, aro2) =
            aro1 |> AsyncResultOption.bind (fun _ -> aro2)

        member inline _.Delay([<InlineIfLambda>] f: unit -> Async<'a>) : Async<'a> = async.Delay f

    let asyncResultOption = new AsyncResultOptionBuilder()
