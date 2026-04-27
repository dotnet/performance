namespace FsToolkit.ErrorHandling.Operator.ResultOption

open FsToolkit.ErrorHandling

[<AutoOpen>]
module ResultOption =

    let inline (<!>) f x = ResultOption.map f x
    let inline (<*>) f x = ResultOption.apply f x

    let inline (<*^>) f x =
        ResultOption.apply f (Result.map Some x)

    let inline (>>=) x f = ResultOption.bind f x
