namespace FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module Result =

    let inline map
        ([<InlineIfLambda>] mapper: 'okInput -> 'okOutput)
        (input: Result<'okInput, 'error>)
        : Result<'okOutput, 'error> =
        match input with
        | Ok x -> Ok(mapper x)
        | Error e -> Error e

    let inline mapError
        ([<InlineIfLambda>] errorMapper: 'errorInput -> 'errorOutput)
        (input: Result<'ok, 'errorInput>)
        : Result<'ok, 'errorOutput> =
        match input with
        | Ok x -> Ok x
        | Error e -> Error(errorMapper e)

    let inline bind
        ([<InlineIfLambda>] binder: 'okInput -> Result<'okOutput, 'error>)
        (input: Result<'okInput, 'error>)
        : Result<'okOutput, 'error> =
        match input with
        | Ok x -> binder x
        | Error e -> Error e

    let inline isOk (value: Result<'ok, 'error>) : bool =
        match value with
        | Ok _ -> true
        | Error _ -> false

    let inline isError (value: Result<'ok, 'error>) : bool =
        match value with
        | Ok _ -> false
        | Error _ -> true

    let inline either
        ([<InlineIfLambda>] onOk: 'okInput -> 'output)
        ([<InlineIfLambda>] onError: 'errorInput -> 'output)
        (input: Result<'okInput, 'errorInput>)
        : 'output =
        match input with
        | Ok x -> onOk x
        | Error err -> onError err

    let inline eitherMap
        ([<InlineIfLambda>] onOk: 'okInput -> 'okOutput)
        ([<InlineIfLambda>] onError: 'errorInput -> 'errorOutput)
        (input: Result<'okInput, 'errorInput>)
        : Result<'okOutput, 'errorOutput> =
        match input with
        | Ok x -> Ok(onOk x)
        | Error err -> Error(onError err)

    let inline apply
        (applier: Result<'okInput -> 'okOutput, 'error>)
        (input: Result<'okInput, 'error>)
        : Result<'okOutput, 'error> =
        match (applier, input) with
        | Ok f, Ok x -> Ok(f x)
        | Error e, _
        | _, Error e -> Error e

    let inline map2
        ([<InlineIfLambda>] mapper: 'okInput1 -> 'okInput2 -> 'okOutput)
        (input1: Result<'okInput1, 'error>)
        (input2: Result<'okInput2, 'error>)
        : Result<'okOutput, 'error> =
        match (input1, input2) with
        | Ok x, Ok y -> Ok(mapper x y)
        | Error e, _
        | _, Error e -> Error e


    let inline map3
        ([<InlineIfLambda>] mapper: 'okInput1 -> 'okInput2 -> 'okInput3 -> 'okOutput)
        (input1: Result<'okInput1, 'error>)
        (input2: Result<'okInput2, 'error>)
        (input3: Result<'okInput3, 'error>)
        : Result<'okOutput, 'error> =
        match (input1, input2, input3) with
        | Ok x, Ok y, Ok z -> Ok(mapper x y z)
        | Error e, _, _
        | _, Error e, _
        | _, _, Error e -> Error e

    let inline fold
        ([<InlineIfLambda>] onOk: 'okInput -> 'output)
        ([<InlineIfLambda>] onError: 'errorInput -> 'output)
        (input: Result<'okInput, 'errorInput>)
        : 'output =
        match input with
        | Ok x -> onOk x
        | Error err -> onError err

    let inline ofChoice (input: Choice<'ok, 'error>) : Result<'ok, 'error> =
        match input with
        | Choice1Of2 x -> Ok x
        | Choice2Of2 e -> Error e

    let inline tryCreate (fieldName: string) (x: 'a) : Result< ^b, (string * 'c) > =
        let tryCreate' x =
            (^b: (static member TryCreate: 'a -> Result< ^b, 'c >) x)

        tryCreate' x |> mapError (fun z -> (fieldName, z))


    /// <summary>
    /// Returns <paramref name="result"/> if it is <c>Ok</c>, otherwise returns <paramref name="ifError"/>
    /// </summary>
    /// <param name="ifError">The value to use if <paramref name="result"/> is <c>Error</c></param>
    /// <param name="result">The input result.</param>
    /// <remarks>
    /// </remarks>
    /// <example>
    /// <code>
    ///     Error ("First") |> Result.orElse (Error ("Second")) // evaluates to Error ("Second")
    ///     Error ("First") |> Result.orElseWith (Ok ("Second")) // evaluates to Ok ("Second")
    ///     Ok ("First") |> Result.orElseWith (Error ("Second")) // evaluates to Ok ("First")
    ///     Ok ("First") |> Result.orElseWith (Ok ("Second")) // evaluates to Ok ("First")
    /// </code>
    /// </example>
    /// <returns>
    /// The result if the result is Ok, else returns <paramref name="ifError"/>.
    /// </returns>
    let inline orElse (ifError: Result<'ok, 'errorOutput>) (result: Result<'ok, 'error>) : Result<'ok, 'errorOutput> =
        match result with
        | Ok x -> Ok x
        | Error e -> ifError


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
    ///     Error ("First") |> Result.orElseWith (fun _ -> Error ("Second")) // evaluates to Error ("Second")
    ///     Error ("First") |> Result.orElseWith (fun _ -> Ok ("Second")) // evaluates to Ok ("Second")
    ///     Ok ("First") |> Result.orElseWith (fun _ -> Error ("Second")) // evaluates to Ok ("First")
    ///     Ok ("First") |> Result.orElseWith (fun _ -> Ok ("Second")) // evaluates to Ok ("First")
    /// </code>
    /// </example>
    /// <returns>
    /// The result if the result is Ok, else the result of executing <paramref name="ifErrorFunc"/>.
    /// </returns>
    let inline orElseWith
        ([<InlineIfLambda>] ifErrorFunc: 'error -> Result<'ok, 'errorOutput>)
        (result: Result<'ok, 'error>)
        : Result<'ok, 'errorOutput> =
        match result with
        | Ok x -> Ok x
        | Error e -> ifErrorFunc e

    /// Replaces the wrapped value with unit
    let inline ignore<'ok, 'error> (result: Result<'ok, 'error>) : Result<unit, 'error> =
        match result with
        | Ok _ -> Ok()
        | Error e -> Error e

    /// Returns the specified error if the value is false.
    let inline requireTrue (error: 'error) (value: bool) : Result<unit, 'error> = if value then Ok() else Error error

    /// Returns the specified error if the value is true.
    let inline requireFalse (error: 'error) (value: bool) : Result<unit, 'error> =
        if not value then Ok() else Error error

    /// Converts an Option to a Result, using the given error if None.
    let inline requireSome (error: 'error) (option: 'ok option) : Result<'ok, 'error> =
        match option with
        | Some x -> Ok x
        | None -> Error error

    /// Converts an Option to a Result, using the given error if Some.
    let inline requireNone (error: 'error) (option: 'value option) : Result<unit, 'error> =
        match option with
        | Some _ -> Error error
        | None -> Ok()

    /// Converts a nullable value into a Result, using the given error if null
    let inline requireNotNull (error: 'error) (value: 'ok) : Result<'ok, 'error> =
        match value with
        | null -> Error error
        | nonnull -> Ok nonnull

    /// Returns Ok if the two values are equal, or the specified error if not.
    /// Same as requireEqual, but with a signature that fits piping better than
    /// normal function application.
    let inline requireEqualTo (other: 'value) (error: 'error) (this: 'value) : Result<unit, 'error> =
        if this = other then
            Ok()
        else
            Error error

    /// Returns Ok if the two values are equal, or the specified error if not.
    /// Same as requireEqualTo, but with a signature that fits normal function
    /// application better than piping.
    let inline requireEqual (x1: 'value) (x2: 'value) (error: 'error) : Result<unit, 'error> =
        if x1 = x2 then Ok() else Error error

    /// Returns Ok if the sequence is empty, or the specified error if not.
    let inline requireEmpty (error: 'error) (xs: #seq<'value>) : Result<unit, 'error> =
        if Seq.isEmpty xs then
            Ok()
        else
            Error error

    /// Returns the specified error if the sequence is empty, or Ok if not.
    let inline requireNotEmpty (error: 'error) (xs: #seq<'value>) : Result<unit, 'error> =
        if Seq.isEmpty xs then
            Error error
        else
            Ok()

    /// Returns the first item of the sequence if it exists, or the specified
    /// error if the sequence is empty
    let inline requireHead (error: 'error) (xs: #seq<'ok>) : Result<'ok, 'error> =
        match Seq.tryHead xs with
        | Some x -> Ok x
        | None -> Error error

    /// Replaces an error value with a custom error value.
    let inline setError (error: 'error) (result: Result<'ok, 'errorIgnored>) : Result<'ok, 'error> =
        result |> mapError (fun _ -> error)

    /// Replaces a unit error value with a custom error value. Safer than setError
    /// since you're not losing any information.
    let inline withError (error: 'error) (result: Result<'ok, unit>) : Result<'ok, 'error> =
        result |> mapError (fun () -> error)

    /// Returns the contained value if Ok, otherwise returns ifError.
    let inline defaultValue (ifError: 'ok) (result: Result<'ok, 'error>) : 'ok =
        match result with
        | Ok x -> x
        | Error _ -> ifError

    // Returns the contained value if Error, otherwise returns ifOk.
    let inline defaultError (ifOk: 'error) (result: Result<'ok, 'error>) : 'error =
        match result with
        | Error error -> error
        | Ok _ -> ifOk

    /// Returns the contained value if Ok, otherwise evaluates ifErrorThunk and
    /// returns the result.
    let inline defaultWith ([<InlineIfLambda>] ifErrorThunk: unit -> 'ok) (result: Result<'ok, 'error>) : 'ok =
        match result with
        | Ok x -> x
        | Error _ -> ifErrorThunk ()

    /// Same as defaultValue for a result where the Ok value is unit. The name
    /// describes better what is actually happening in this case.
    let inline ignoreError<'error> (result: Result<unit, 'error>) : unit = defaultValue () result

    /// If the result is Ok and the predicate returns true, executes the function
    /// on the Ok value. Passes through the input value.
    let inline teeIf
        ([<InlineIfLambda>] predicate: 'ok -> bool)
        ([<InlineIfLambda>] inspector: 'ok -> unit)
        (result: Result<'ok, 'error>)
        : Result<'ok, 'error> =
        match result with
        | Ok x -> if predicate x then inspector x
        | Error _ -> ()

        result

    /// If the result is Error and the predicate returns true, executes the
    /// function on the Error value. Passes through the input value.
    let inline teeErrorIf
        ([<InlineIfLambda>] predicate: 'error -> bool)
        ([<InlineIfLambda>] inspector: 'error -> unit)
        (result: Result<'ok, 'error>)
        : Result<'ok, 'error> =
        match result with
        | Ok _ -> ()
        | Error x -> if predicate x then inspector x

        result

    /// If the result is Ok, executes the function on the Ok value. Passes through
    /// the input value.
    let inline tee ([<InlineIfLambda>] inspector: 'ok -> unit) (result: Result<'ok, 'error>) : Result<'ok, 'error> =
        teeIf (fun _ -> true) inspector result

    /// If the result is Error, executes the function on the Error value. Passes
    /// through the input value.
    let inline teeError
        ([<InlineIfLambda>] inspector: 'error -> unit)
        (result: Result<'ok, 'error>)
        : Result<'ok, 'error> =
        teeErrorIf (fun _ -> true) inspector result

    /// Converts a Result<Async<_>,_> to an Async<Result<_,_>>
    let inline sequenceAsync (resAsync: Result<Async<'ok>, 'error>) : Async<Result<'ok, 'error>> = async {
        match resAsync with
        | Ok asnc ->
            let! x = asnc
            return Ok x
        | Error err -> return Error err
    }

    ///
    let inline traverseAsync
        ([<InlineIfLambda>] f: 'okInput -> Async<'okOutput>)
        (res: Result<'okInput, 'error>)
        : Async<Result<'okOutput, 'error>> =
        sequenceAsync ((map f) res)


    /// Returns the Ok value or runs the specified function over the error value.
    let inline valueOr ([<InlineIfLambda>] f: 'error -> 'ok) (res: Result<'ok, 'error>) : 'ok =
        match res with
        | Ok x -> x
        | Error x -> f x

    /// Takes two results and returns a tuple of the pair
    let zip (left: Result<'leftOk, 'error>) (right: Result<'rightOk, 'error>) : Result<'leftOk * 'rightOk, 'error> =
        match left, right with
        | Ok x1res, Ok x2res -> Ok(x1res, x2res)
        | Error e, _ -> Error e
        | _, Error e -> Error e

    /// Takes two results and returns a tuple of the error pair
    let zipError
        (left: Result<'ok, 'leftError>)
        (right: Result<'ok, 'rightError>)
        : Result<'ok, 'leftError * 'rightError> =
        match left, right with
        | Error x1res, Error x2res -> Error(x1res, x2res)
        | Ok e, _ -> Ok e
        | _, Ok e -> Ok e
