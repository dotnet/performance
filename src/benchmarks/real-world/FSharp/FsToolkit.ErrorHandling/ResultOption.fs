namespace FsToolkit.ErrorHandling


[<RequireQualifiedAccess>]
module ResultOption =

    let map f ro = Result.map (Option.map f) ro

    let bind f ro =
        Result.bind
            (function
            | Some x -> f x
            | None -> Ok None)
            ro

    let retn x = Ok(Some x)

    let apply f x =
        bind (fun f' -> bind (fun x' -> retn (f' x')) x) f

    let map2 f x y = (apply (apply (retn f) x) y)

    let map3 f x y z = apply (map2 f x y) z

    /// Replaces the wrapped value with unit
    let ignore<'ok, 'error> (ro: Result<'ok option, 'error>) = ro |> map ignore<'ok>
