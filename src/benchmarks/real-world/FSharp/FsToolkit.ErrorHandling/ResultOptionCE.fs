namespace FsToolkit.ErrorHandling

[<AutoOpen>]
module ResultOptionCE =

    type ResultOptionBuilder() =
        member _.Return value = ResultOption.retn value
        member _.ReturnFrom value = value

        member _.Bind(resultOpt, binder) = ResultOption.bind binder resultOpt

        member _.Combine(r1, r2) = r1 |> ResultOption.bind (fun _ -> r2)

        member _.Delay f = f ()


    let resultOption = ResultOptionBuilder()
