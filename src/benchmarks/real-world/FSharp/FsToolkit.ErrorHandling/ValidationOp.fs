namespace FsToolkit.ErrorHandling.Operator.Validation

open FsToolkit.ErrorHandling

[<AutoOpen>]
module Validation =

    let inline (<!>)
        ([<InlineIfLambda>] mapper: 'okInput -> 'okOutput)
        (input: Validation<'okInput, 'error>)
        : Validation<'okOutput, 'error> =
        Result.map mapper input

    let inline (<!^>)
        ([<InlineIfLambda>] mapper: 'okInput -> 'okOutput)
        (input: Result<'okInput, 'error>)
        : Validation<'okOutput, 'error> =
        Result.map mapper (Validation.ofResult (input))

    let inline (<*>)
        (applier: Validation<('okInput -> 'okOutput), 'error>)
        (input: Validation<'okInput, 'error>)
        : Validation<'okOutput, 'error> =
        Validation.apply applier input

    let inline (<*^>)
        (applier: Validation<('okInput -> 'okOutput), 'error>)
        (input: Result<'okInput, 'error>)
        : Validation<'okOutput, 'error> =
        Validation.apply applier (Validation.ofResult input)
