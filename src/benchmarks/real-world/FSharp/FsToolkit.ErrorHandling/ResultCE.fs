namespace FsToolkit.ErrorHandling

open System

[<AutoOpen>]
module ResultCE =

    type ResultBuilder() =
        member inline _.Return(value: 'ok) : Result<'ok, 'error> = Ok value

        member inline _.ReturnFrom(result: Result<'ok, 'error>) : Result<'ok, 'error> = result

        member this.Zero() : Result<unit, 'error> = this.Return()

        member inline _.Bind
            (
                input: Result<'okInput, 'error>,
                [<InlineIfLambda>] binder: 'okInput -> Result<'okOutput, 'error>
            ) : Result<'okOutput, 'error> =
            Result.bind binder input

        member inline _.Delay([<InlineIfLambda>] generator: unit -> Result<'ok, 'error>) : unit -> Result<'ok, 'error> =
            generator

        member inline _.Run([<InlineIfLambda>] generator: unit -> Result<'ok, 'error>) : Result<'ok, 'error> =
            generator ()

        member inline this.Combine
            (
                result: Result<unit, 'error>,
                [<InlineIfLambda>] binder: unit -> Result<'ok, 'error>
            ) : Result<'ok, 'error> =
            this.Bind(result, binder)

        member inline this.TryWith
            (
                [<InlineIfLambda>] generator: unit -> Result<'T, 'TError>,
                [<InlineIfLambda>] handler: exn -> Result<'T, 'TError>
            ) : Result<'T, 'TError> =
            try
                this.Run generator
            with
            | e -> handler e

        member inline this.TryFinally
            (
                [<InlineIfLambda>] generator: unit -> Result<'ok, 'error>,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : Result<'ok, 'error> =
            try
                this.Run generator
            finally
                compensation ()

        member inline this.Using
            (
                resource: 'disposable :> IDisposable,
                binder: 'disposable -> Result<'ok, 'error>
            ) : Result<'ok, 'error> =
            this.TryFinally(
                (fun () -> binder resource),
                (fun () ->
                    if not <| obj.ReferenceEquals(resource, null) then
                        resource.Dispose())
            )

        member inline this.While
            (
                [<InlineIfLambda>] guard: unit -> bool,
                [<InlineIfLambda>] generator: unit -> Result<unit, 'error>
            ) : Result<unit, 'error> =
            if guard () then
                let mutable whileBuilder = Unchecked.defaultof<_>

                whileBuilder <-
                    fun () ->
                        this.Bind(
                            this.Run generator,
                            (fun () ->
                                if guard () then
                                    this.Run whileBuilder
                                else
                                    this.Zero())
                        )

                this.Run whileBuilder
            else
                this.Zero()

        member inline this.For
            (
                sequence: #seq<'T>,
                [<InlineIfLambda>] binder: 'T -> Result<unit, 'TError>
            ) : Result<unit, 'TError> =
            this.Using(
                sequence.GetEnumerator(),
                fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> binder enum.Current))
            )

        member inline _.BindReturn
            (
                x: Result<'okInput, 'error>,
                [<InlineIfLambda>] f: 'okInput -> 'okOutput
            ) : Result<'okOutput, 'error> =
            Result.map f x

        member inline _.MergeSources
            (
                left: Result<'left, 'error>,
                right: Result<'right, 'error>
            ) : Result<'left * 'right, 'error> =
            Result.zip left right

        /// <summary>
        /// Method lets us transform data types into our internal representation.  This is the identity method to recognize the self type.
        ///
        /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        member inline _.Source(result: Result<'ok, 'error>) : Result<'ok, 'error> = result

    let result = ResultBuilder()

[<AutoOpen>]
module ResultCEExtensions =

    type ResultBuilder with
        /// <summary>
        /// Needed to allow `for..in` and `for..do` functionality
        /// </summary>
        member inline _.Source(s: #seq<_>) : #seq<_> = s


// Having Choice<_> members as extensions gives them lower priority in
// overload resolution and allows skipping more type annotations.
[<AutoOpen>]
module ResultCEChoiceExtensions =
    type ResultBuilder with
        /// <summary>
        /// Method lets us transform data types into our internal representation.
        /// </summary>
        /// <returns></returns>
        member inline _.Source(choice: Choice<'ok, 'error>) : Result<'ok, 'error> = Result.ofChoice choice
