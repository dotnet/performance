namespace FsToolkit.ErrorHandling

open System.Threading.Tasks

[<RequireQualifiedAccess>]
module AsyncResult =


    let inline retn (value: 'ok) : Async<Result<'ok, 'error>> = Ok value |> Async.singleton

    let inline ok (value: 'ok) : Async<Result<'ok, 'error>> = retn value

    let inline returnError (error: 'error) : Async<Result<'ok, 'error>> = Error error |> Async.singleton

    let inline error (error: 'error) : Async<Result<'ok, 'error>> = returnError error

    let inline map
        ([<InlineIfLambda>] mapper: 'input -> 'output)
        (input: Async<Result<'input, 'error>>)
        : Async<Result<'output, 'error>> =
        Async.map (Result.map mapper) input

    let inline mapError
        ([<InlineIfLambda>] mapper: 'inputError -> 'outputError)
        (input: Async<Result<'ok, 'inputError>>)
        : Async<Result<'ok, 'outputError>> =
        Async.map (Result.mapError mapper) input

    let inline bind
        ([<InlineIfLambda>] binder: 'input -> Async<Result<'output, 'error>>)
        (input: Async<Result<'input, 'error>>)
        : Async<Result<'output, 'error>> =
        Async.bind (Result.either binder returnError) input

    let inline foldResult
        ([<InlineIfLambda>] onSuccess: 'input -> 'output)
        ([<InlineIfLambda>] onError: 'inputError -> 'output)
        (input: Async<Result<'input, 'inputError>>)
        : Async<'output> =
        Async.map (Result.either onSuccess onError) input

#if !FABLE_COMPILER

    let inline ofTask (aTask: Task<'ok>) : Async<Result<'ok, exn>> =
        async.Delay(fun () ->
            aTask
            |> Async.AwaitTask
            |> Async.Catch
            |> Async.map Result.ofChoice)

    let inline ofTaskAction (aTask: Task) : Async<Result<unit, exn>> =
        async.Delay(fun () ->
            aTask
            |> Async.AwaitTask
            |> Async.Catch
            |> Async.map Result.ofChoice)

#endif


    let inline map2
        ([<InlineIfLambda>] mapper: 'input1 -> 'input2 -> 'output)
        (input1: Async<Result<'input1, 'error>>)
        (input2: Async<Result<'input2, 'error>>)
        : Async<Result<'output, 'error>> =
        Async.map2 (Result.map2 mapper) input1 input2

    let inline map3
        ([<InlineIfLambda>] mapper: 'input1 -> 'input2 -> 'input3 -> 'output)
        (input1: Async<Result<'input1, 'error>>)
        (input2: Async<Result<'input2, 'error>>)
        (input3: Async<Result<'input3, 'error>>)
        : Async<Result<'output, 'error>> =
        Async.map3 (Result.map3 mapper) input1 input2 input3

    let inline apply
        (applier: Async<Result<'input -> 'output, 'error>>)
        (input: Async<Result<'input, 'error>>)
        : Async<Result<'output, 'error>> =
        map2 (fun f x -> f x) applier input


    /// <summary>
    /// Returns <paramref name="input"/> if it is <c>Ok</c>, otherwise returns <paramref name="ifError"/>
    /// </summary>
    /// <param name="ifError">The value to use if <paramref name="input"/> is <c>Error</c></param>
    /// <param name="input">The input result.</param>
    /// <remarks>
    /// </remarks>
    /// <example>
    /// <code>
    ///     AsyncResult.error "First" |> AsyncResult.orElse (AsyncResult.error "Second") // evaluates to Error ("Second")
    ///     AsyncResult.error "First" |> AsyncResult.orElse (AsyncResult.ok "Second") // evaluates to Ok ("Second")
    ///     AsyncResult.ok "First" |> AsyncResult.orElse (AsyncResult.error "Second") // evaluates to Ok ("First")
    ///     AsyncResult.ok "First" |> AsyncResult.orElse (AsyncResult.ok "Second") // evaluates to Ok ("First")
    /// </code>
    /// </example>
    /// <returns>
    /// The result if the result is Ok, else returns <paramref name="ifError"/>.
    /// </returns>
    let inline orElse
        (ifError: Async<Result<'ok, 'errorOutput>>)
        (input: Async<Result<'ok, 'errorInput>>)
        : Async<Result<'ok, 'errorOutput>> =
        Async.bind (Result.either ok (fun _ -> ifError)) input

    /// <summary>
    /// Returns <paramref name="input"/> if it is <c>Ok</c>, otherwise executes <paramref name="ifErrorFunc"/> and returns the result.
    /// </summary>
    /// <param name="ifErrorFunc">A function that provides an alternate result when evaluated.</param>
    /// <param name="input">The input result.</param>
    /// <remarks>
    /// <paramref name="ifErrorFunc"/>  is not executed unless <paramref name="input"/> is an <c>Error</c>.
    /// </remarks>
    /// <example>
    /// <code>
    ///     AsyncResult.error "First" |> AsyncResult.orElseWith (fun _ -> AsyncResult.error "Second") // evaluates to Error ("Second")
    ///     AsyncResult.error "First" |> AsyncResult.orElseWith (fun _ -> AsyncResult.ok "Second") // evaluates to Ok ("Second")
    ///     AsyncResult.ok "First" |> AsyncResult.orElseWith (fun _ -> AsyncResult.error "Second") // evaluates to Ok ("First")
    ///     AsyncResult.ok "First" |> AsyncResult.orElseWith (fun _ -> AsyncResult.ok "Second") // evaluates to Ok ("First")
    /// </code>
    /// </example>
    /// <returns>
    /// The result if the result is Ok, else the result of executing <paramref name="ifErrorFunc"/>.
    /// </returns>
    let inline orElseWith
        ([<InlineIfLambda>] ifErrorFunc: 'errorInput -> Async<Result<'ok, 'errorOutput>>)
        (input: Async<Result<'ok, 'errorInput>>)
        : Async<Result<'ok, 'errorOutput>> =
        Async.bind (Result.either ok ifErrorFunc) input

    /// Replaces the wrapped value with unit
    let inline ignore<'ok, 'error> (value: Async<Result<'ok, 'error>>) : Async<Result<unit, 'error>> =
        value |> map ignore<'ok>

    /// Returns the specified error if the async-wrapped value is false.
    let inline requireTrue (error: 'error) (value: Async<bool>) : Async<Result<unit, 'error>> =
        value |> Async.map (Result.requireTrue error)

    /// Returns the specified error if the async-wrapped value is true.
    let inline requireFalse (error: 'error) (value: Async<bool>) : Async<Result<unit, 'error>> =
        value |> Async.map (Result.requireFalse error)

    // Converts an async-wrapped Option to a Result, using the given error if None.
    let inline requireSome (error: 'error) (value: Async<'ok option>) : Async<Result<'ok, 'error>> =
        value |> Async.map (Result.requireSome error)

    // Converts an async-wrapped Option to a Result, using the given error if Some.
    let inline requireNone (error: 'error) (value: Async<'ok option>) : Async<Result<unit, 'error>> =
        value |> Async.map (Result.requireNone error)

    /// Returns Ok if the async-wrapped value and the provided value are equal, or the specified error if not.
    let inline requireEqual (value1: 'value) (value2: Async<'value>) (error: 'error) : Async<Result<unit, 'error>> =
        value2
        |> Async.map (fun x2' -> Result.requireEqual value1 x2' error)

    /// Returns Ok if the two values are equal, or the specified error if not.
    let inline requireEqualTo (other: 'value) (error: 'error) (this: Async<'value>) : Async<Result<unit, 'error>> =
        this
        |> Async.map (Result.requireEqualTo other error)

    /// Returns Ok if the async-wrapped sequence is empty, or the specified error if not.
    let inline requireEmpty (error: 'error) (values: Async<#seq<'ok>>) : Async<Result<unit, 'error>> =
        values |> Async.map (Result.requireEmpty error)

    /// Returns Ok if the async-wrapped sequence is not-empty, or the specified error if not.
    let inline requireNotEmpty (error: 'error) (values: Async<#seq<'ok>>) : Async<Result<unit, 'error>> =
        values |> Async.map (Result.requireNotEmpty error)

    /// Returns the first item of the async-wrapped sequence if it exists, or the specified
    /// error if the sequence is empty
    let inline requireHead (error: 'error) (values: Async<#seq<'ok>>) : Async<Result<'ok, 'error>> =
        values |> Async.map (Result.requireHead error)

    /// Replaces an error value of an async-wrapped result with a custom error
    /// value.
    let inline setError
        (error: 'errorOutput)
        (asyncResult: Async<Result<'ok, 'errorInput>>)
        : Async<Result<'ok, 'errorOutput>> =
        asyncResult |> Async.map (Result.setError error)

    /// Replaces a unit error value of an async-wrapped result with a custom
    /// error value. Safer than setError since you're not losing any information.
    let inline withError
        (error: 'errorOutput)
        (asyncResult: Async<Result<'ok, unit>>)
        : Async<Result<'ok, 'errorOutput>> =
        asyncResult |> Async.map (Result.withError error)

    /// Extracts the contained value of an async-wrapped result if Ok, otherwise
    /// uses ifError.
    let inline defaultValue (ifError: 'ok) (asyncResult: Async<Result<'ok, 'error>>) : Async<'ok> =
        asyncResult
        |> Async.map (Result.defaultValue ifError)

    /// Extracts the contained value of an async-wrapped result if Error, otherwise
    /// uses ifOk.
    let inline defaultError (ifOk: 'error) (asyncResult: Async<Result<'ok, 'error>>) : Async<'error> =
        asyncResult
        |> Async.map (Result.defaultError ifOk)

    /// Extracts the contained value of an async-wrapped result if Ok, otherwise
    /// evaluates ifErrorThunk and uses the result.
    let inline defaultWith
        ([<InlineIfLambda>] ifErrorThunk: unit -> 'ok)
        (asyncResult: Async<Result<'ok, 'error>>)
        : Async<'ok> =
        asyncResult
        |> Async.map (Result.defaultWith ifErrorThunk)

    /// Same as defaultValue for a result where the Ok value is unit. The name
    /// describes better what is actually happening in this case.
    let inline ignoreError<'error> (result: Async<Result<unit, 'error>>) : Async<unit> = defaultValue () result

    /// If the async-wrapped result is Ok, executes the function on the Ok value.
    /// Passes through the input value.
    let inline tee
        ([<InlineIfLambda>] inspector: 'ok -> unit)
        (asyncResult: Async<Result<'ok, 'error>>)
        : Async<Result<'ok, 'error>> =
        asyncResult |> Async.map (Result.tee inspector)

    /// If the async-wrapped result is Ok and the predicate returns true, executes
    /// the function on the Ok value. Passes through the input value.
    let inline teeIf
        ([<InlineIfLambda>] predicate: 'ok -> bool)
        ([<InlineIfLambda>] inspector: 'ok -> unit)
        (asyncResult: Async<Result<'ok, 'error>>)
        : Async<Result<'ok, 'error>> =
        asyncResult
        |> Async.map (Result.teeIf predicate inspector)

    /// If the async-wrapped result is Error, executes the function on the Error
    /// value. Passes through the input value.
    let inline teeError
        ([<InlineIfLambda>] teeFunction: 'error -> unit)
        (asyncResult: Async<Result<'ok, 'error>>)
        : Async<Result<'ok, 'error>> =
        asyncResult
        |> Async.map (Result.teeError teeFunction)

    /// If the async-wrapped result is Error and the predicate returns true,
    /// executes the function on the Error value. Passes through the input value.
    let inline teeErrorIf
        ([<InlineIfLambda>] predicate: 'error -> bool)
        ([<InlineIfLambda>] teeFunction: 'error -> unit)
        (asyncResult: Async<Result<'ok, 'error>>)
        : Async<Result<'ok, 'error>> =
        asyncResult
        |> Async.map (Result.teeErrorIf predicate teeFunction)


    /// Takes two results and returns a tuple of the pair
    let inline zip
        (left: Async<Result<'leftOk, 'error>>)
        (right: Async<Result<'rightOk, 'error>>)
        : Async<Result<'leftOk * 'rightOk, 'error>> =
        Async.zip left right
        |> Async.map (fun (r1, r2) -> Result.zip r1 r2)

    /// Takes two results and returns a tuple of the error pair
    let inline zipError
        (left: Async<Result<'ok, 'leftError>>)
        (right: Async<Result<'ok, 'rightError>>)
        : Async<Result<'ok, 'leftError * 'rightError>> =
        Async.zip left right
        |> Async.map (fun (r1, r2) -> Result.zipError r1 r2)

    /// Catches exceptions and maps them to the Error case using the provided function.
    let inline catch
        ([<InlineIfLambda>] exnMapper: exn -> 'error)
        (input: Async<Result<'ok, 'error>>)
        : Async<Result<'ok, 'error>> =
        input
        |> Async.Catch
        |> Async.map (function
            | Choice1Of2 (Ok v) -> Ok v
            | Choice1Of2 (Error err) -> Error err
            | Choice2Of2 ex -> Error(exnMapper ex))

    /// Lift Async to AsyncResult
    let inline ofAsync (value: Async<'ok>) : Async<Result<'ok, 'error>> = value |> Async.map Ok

    /// Lift Result to AsyncResult
    let inline ofResult (x: Result<'ok, 'error>) : Async<Result<'ok, 'error>> = x |> Async.singleton
