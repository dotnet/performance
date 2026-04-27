namespace FsToolkit.ErrorHandling

open System

[<AutoOpen>]
module ValidationCE =
    type ValidationBuilder() =
        member inline _.Return(value: 'ok) : Validation<'ok, 'error> = Validation.ok value

        member inline _.ReturnFrom(result: Validation<'ok, 'error>) : Validation<'ok, 'error> = result

        member inline _.Bind
            (
                result: Validation<'okInput, 'error>,
                [<InlineIfLambda>] binder: 'okInput -> Validation<'okOutput, 'error>
            ) : Validation<'okOutput, 'error> =
            Validation.bind binder result

        member inline this.Zero() : Validation<unit, 'error> = this.Return()

        member inline _.Delay
            ([<InlineIfLambda>] generator: unit -> Validation<'ok, 'error>)
            : unit -> Validation<'ok, 'error> =
            generator

        member inline _.Run([<InlineIfLambda>] generator: unit -> Validation<'ok, 'error>) : Validation<'ok, 'error> =
            generator ()

        member inline this.Combine
            (
                result: Validation<unit, 'error>,
                [<InlineIfLambda>] binder: unit -> Validation<'ok, 'error>
            ) : Validation<'ok, 'error> =
            this.Bind(result, binder)

        member inline this.TryWith
            (
                [<InlineIfLambda>] generator: unit -> Validation<'ok, 'error>,
                [<InlineIfLambda>] handler: exn -> Validation<'ok, 'error>
            ) : Validation<'ok, 'error> =
            try
                this.Run generator
            with
            | e -> handler e

        member inline this.TryFinally
            (
                [<InlineIfLambda>] generator: unit -> Validation<'ok, 'error>,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : Validation<'ok, 'error> =
            try
                this.Run generator
            finally
                compensation ()

        member inline this.Using
            (
                resource: 'disposable :> IDisposable,
                [<InlineIfLambda>] binder: 'disposable -> Validation<'okOutput, 'error>
            ) : Validation<'okOutput, 'error> =
            this.TryFinally(
                (fun () -> binder resource),
                (fun () ->
                    if not <| obj.ReferenceEquals(resource, null) then
                        resource.Dispose())
            )

        member inline this.While
            (
                [<InlineIfLambda>] guard: unit -> bool,
                [<InlineIfLambda>] generator: unit -> Validation<unit, 'error>
            ) : Validation<unit, 'error> =
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
                sequence: #seq<'ok>,
                [<InlineIfLambda>] binder: 'ok -> Validation<unit, 'error>
            ) : Validation<unit, 'error> =
            this.Using(
                sequence.GetEnumerator(),
                fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> binder enum.Current))
            )

        member inline _.BindReturn
            (
                input: Validation<'okInput, 'error>,
                [<InlineIfLambda>] mapper: 'okInput -> 'okOutput
            ) : Validation<'okOutput, 'error> =
            Validation.map mapper input

        member inline _.MergeSources
            (
                left: Validation<'left, 'error>,
                right: Validation<'right, 'error>
            ) : Validation<'left * 'right, 'error> =
            Validation.zip left right

        /// <summary>
        /// Method lets us transform data types into our internal representation.  This is the identity method to recognize the self type.
        ///
        /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        member inline _.Source(result: Validation<'ok, 'error>) : Validation<'ok, 'error> = result

    let validation = ValidationBuilder()

[<AutoOpen>]
module ValidationCEExtensions =

    // Having members as extensions gives them lower priority in
    // overload resolution and allows skipping more type annotations.
    type ValidationBuilder with
        /// <summary>
        /// Needed to allow `for..in` and `for..do` functionality
        /// </summary>
        member inline _.Source(s: #seq<_>) : #seq<_> = s

        /// <summary>
        /// Method lets us transform data types into our internal representation.
        /// </summary>
        member inline _.Source(s: Result<'ok, 'error>) : Validation<'ok, 'error> = Validation.ofResult s

        /// <summary>
        /// Method lets us transform data types into our internal representation.
        /// </summary>
        /// <returns></returns>
        member inline _.Source(choice: Choice<'ok, 'error>) : Validation<'ok, 'error> = Validation.ofChoice choice
