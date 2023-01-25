namespace FsToolkit.ErrorHandling

/// Validation<'a, 'err> is defined as Result<'a, 'err list> meaning you can use many of the functions found in the Result module.
type Validation<'ok, 'error> = Result<'ok, 'error list>

[<RequireQualifiedAccess>]
module Validation =

    let inline ok (value: 'ok) : Validation<'ok, 'error> = Ok value
    let inline error (error: 'error) : Validation<'ok, 'error> = Error [ error ]

    let inline ofResult (result: Result<'ok, 'error>) : Validation<'ok, 'error> = Result.mapError List.singleton result

    let inline ofChoice (choice: Choice<'ok, 'error>) : Validation<'ok, 'error> =
        match choice with
        | Choice1Of2 x -> ok x
        | Choice2Of2 e -> error e

    let inline apply
        (applier: Validation<'okInput -> 'okOutput, 'error>)
        (input: Validation<'okInput, 'error>)
        : Validation<'okOutput, 'error> =
        match applier, input with
        | Ok f, Ok x -> Ok(f x)
        | Error errs, Ok _
        | Ok _, Error errs -> Error errs
        | Error errs1, Error errs2 -> Error(errs1 @ errs2)

    let inline retn (value: 'ok) : Validation<'ok, 'error> = ok value

    let inline returnError (error: 'error) : Validation<'ok, 'error> = Error [ error ]


    /// <summary>
    /// Returns <paramref name="result"/> if it is <c>Ok</c>, otherwise returns <paramref name="ifError"/>
    /// </summary>
    /// <param name="ifError">The value to use if <paramref name="result"/> is <c>Error</c></param>
    /// <param name="result">The input result.</param>
    /// <remarks>
    /// </remarks>
    /// <example>
    /// <code>
    ///     Error (["First"]) |> Validation.orElse (Error (["Second"])) // evaluates to Error (["Second"])
    ///     Error (["First"]) |> Validation.orElseWith (Ok ("Second")) // evaluates to Ok ("Second")
    ///     Ok ("First") |> Validation.orElseWith (Error (["Second"])) // evaluates to Ok ("First")
    ///     Ok ("First") |> Validation.orElseWith (Ok ("Second")) // evaluates to Ok ("First")
    /// </code>
    /// </example>
    /// <returns>
    /// The result if the result is Ok, else returns <paramref name="ifError"/>.
    /// </returns>
    let inline orElse
        (ifError: Validation<'ok, 'errorOutput>)
        (result: Validation<'ok, 'errorInput>)
        : Validation<'ok, 'errorOutput> =
        result |> Result.either ok (fun _ -> ifError)



    /// <summary>
    /// Returns <paramref name="result"/> if it is <c>Ok</c>, otherwise executes <paramref name="ifErrorFunc"/> and returns the result.
    /// </summary>
    /// <param name="ifErrorFunc">A function that provides an alternate result when evaluated.</param>
    /// <param name="result">The input result.</param>
    /// <remarks>
    /// <paramref name="ifErrorFunc"/>  is not executed unless <paramref name="result"/> is an <c>Error</c>.
    /// </remarks>
    /// <example>
    /// <code>
    ///     Error (["First"]) |> Validation.orElseWith (fun _ -> Error (["Second"])) // evaluates to Error (["Second"])
    ///     Error (["First"]) |> Validation.orElseWith (fun _ -> Ok ("Second")) // evaluates to Ok ("Second")
    ///     Ok ("First") |> Validation.orElseWith (fun _ -> Error (["Second"])) // evaluates to Ok ("First")
    ///     Ok ("First") |> Validation.orElseWith (fun _ -> Ok ("Second")) // evaluates to Ok ("First")
    /// </code>
    /// </example>
    /// <returns>
    /// The result if the result is Ok, else the result of executing <paramref name="ifErrorFunc"/>.
    /// </returns>
    let inline orElseWith
        ([<InlineIfLambda>] ifErrorFunc: 'errorInput list -> Validation<'ok, 'errorOutput>)
        (result: Validation<'ok, 'errorInput>)
        : Validation<'ok, 'errorOutput> =
        result |> Result.either ok ifErrorFunc


    let inline map
        ([<InlineIfLambda>] mapper: 'okInput -> 'okOutput)
        (input: Validation<'okInput, 'error>)
        : Validation<'okOutput, 'error> =
        Result.map mapper input

    let inline map2
        ([<InlineIfLambda>] mapper: 'okInput1 -> 'okInput2 -> 'okOutput)
        (input1: Validation<'okInput1, 'error>)
        (input2: Validation<'okInput2, 'error>)
        : Validation<'okOutput, 'error> =
        match input1, input2 with
        | Ok x, Ok y -> Ok(mapper x y)
        | Ok _, Error errs -> Error errs
        | Error errs, Ok _ -> Error errs
        | Error errs1, Error errs2 -> Error(errs1 @ errs2)

    let inline map3
        ([<InlineIfLambda>] mapper: 'okInput1 -> 'okInput2 -> 'okInput3 -> 'okOutput)
        (input1: Validation<'okInput1, 'error>)
        (input2: Validation<'okInput2, 'error>)
        (input3: Validation<'okInput3, 'error>)
        : Validation<'okOutput, 'error> =
        match input1, input2, input3 with
        | Ok x, Ok y, Ok z -> Ok(mapper x y z)
        | Error errs, Ok _, Ok _ -> Error errs
        | Ok _, Error errs, Ok _ -> Error errs
        | Ok _, Ok _, Error errs -> Error errs
        | Error errs1, Error errs2, Ok _ -> Error(errs1 @ errs2)
        | Ok _, Error errs1, Error errs2 -> Error(errs1 @ errs2)
        | Error errs1, Ok _, Error errs2 -> Error(errs1 @ errs2)
        | Error errs1, Error errs2, Error errs3 -> Error(errs1 @ errs2 @ errs3)

    let inline mapError
        ([<InlineIfLambda>] errorMapper: 'errorInput -> 'errorOutput)
        (input: Validation<'ok, 'errorInput>)
        : Validation<'ok, 'errorOutput> =
        Result.mapError (List.map errorMapper) input

    let inline mapErrors
        ([<InlineIfLambda>] errorMapper: 'errorInput list -> 'errorOutput list)
        (input: Validation<'ok, 'errorInput>)
        : Validation<'ok, 'errorOutput> =
        Result.mapError (errorMapper) input

    let inline bind
        ([<InlineIfLambda>] binder: 'okInput -> Validation<'okOutput, 'error>)
        (input: Validation<'okInput, 'error>)
        : Validation<'okOutput, 'error> =
        Result.bind binder input

    let inline zip
        (left: Validation<'left, 'error>)
        (right: Validation<'right, 'error>)
        : Validation<'left * 'right, 'error> =
        match left, right with
        | Ok x1res, Ok x2res -> Ok(x1res, x2res)
        | Error e, Ok _ -> Error e
        | Ok _, Error e -> Error e
        | Error e1, Error e2 -> Error(e1 @ e2)
