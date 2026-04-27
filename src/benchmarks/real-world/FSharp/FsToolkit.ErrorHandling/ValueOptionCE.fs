namespace FsToolkit.ErrorHandling


#if !FABLE_COMPILER
[<AutoOpen>]
module ValueOptionCE =
    open System

    type ValueOptionBuilder() =
        member inline _.Return(x: 'value) : 'value voption = ValueSome x

        member inline _.ReturnFrom(m: 'value voption) : 'value voption = m

        member inline _.Bind
            (
                input: 'input voption,
                [<InlineIfLambda>] binder: 'input -> 'output voption
            ) : 'output voption =
            ValueOption.bind binder input

        // Could not get it to work solely with Source. In loop cases it would potentially match the #seq overload and ask for type annotation
        member inline this.Bind
            (
                input: 'input when 'input: null,
                [<InlineIfLambda>] binder: 'input -> 'output voption
            ) : 'output voption =
            this.Bind(ValueOption.ofObj input, binder)

        member inline this.Zero() : unit voption = this.Return()

        member inline _.Combine
            (
                input: 'input voption,
                [<InlineIfLambda>] binder: 'input -> 'output voption
            ) : 'output voption =
            ValueOption.bind binder input

        member inline this.Combine(input: unit voption, output: 'output voption) : 'output voption =
            this.Bind(input, (fun () -> output))

        member inline _.Delay([<InlineIfLambda>] f: unit -> 'a) = f

        member inline _.Run([<InlineIfLambda>] f: unit -> 'v) = f ()

        member inline this.TryWith([<InlineIfLambda>] m, [<InlineIfLambda>] handler) =
            try
                this.Run m
            with
            | e -> handler e

        member inline this.TryFinally([<InlineIfLambda>] m, [<InlineIfLambda>] compensation) =
            try
                this.Run m
            finally
                compensation ()

        member inline this.Using(resource: 'T :> IDisposable, [<InlineIfLambda>] binder) : _ voption =
            this.TryFinally(
                (fun () -> binder resource),
                (fun () ->
                    if not <| obj.ReferenceEquals(resource, null) then
                        resource.Dispose())
            )

        member inline this.While
            (
                [<InlineIfLambda>] guard: unit -> bool,
                [<InlineIfLambda>] generator: unit -> _ voption
            ) : _ voption =
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

        member inline this.For(sequence: #seq<'T>, [<InlineIfLambda>] binder: 'T -> _ voption) : _ voption =
            this.Using(
                sequence.GetEnumerator(),
                fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> binder enum.Current))
            )

        member inline _.BindReturn(x, [<InlineIfLambda>] f) = ValueOption.map f x

        member inline _.BindReturn(x, [<InlineIfLambda>] f) =
            x |> ValueOption.ofObj |> ValueOption.map f

        member inline _.MergeSources(option1, option2) = ValueOption.zip option1 option2

        /// <summary>
        /// Method lets us transform data types into our internal representation.  This is the identity method to recognize the self type.
        ///
        /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
        /// </summary>
        member inline _.Source(result: _ voption) : _ voption = result


        // /// <summary>
        // /// Method lets us transform data types into our internal representation.
        // /// </summary>
        member inline _.Source(vopt: _ option) : _ voption = vopt |> ValueOption.ofOption

    let voption = ValueOptionBuilder()

[<AutoOpen>]
module ValueOptionExtensionsLower =
    type ValueOptionBuilder with
        member inline _.Source(nullableObj: 'a when 'a: null) = nullableObj |> ValueOption.ofObj
        member inline _.Source(m: string) = m |> ValueOption.ofObj

        member inline _.MergeSources(nullableObj1, option2) =
            ValueOption.zip (ValueOption.ofObj nullableObj1) option2


        member inline _.MergeSources(option1, nullableObj2) =
            ValueOption.zip (option1) (ValueOption.ofObj nullableObj2)


        member inline _.MergeSources(nullableObj1, nullableObj2) =
            ValueOption.zip (ValueOption.ofObj nullableObj1) (ValueOption.ofObj nullableObj2)

[<AutoOpen>]
module ValueOptionExtensions =
    open System

    type ValueOptionBuilder with
        /// <summary>
        /// Needed to allow `for..in` and `for..do` functionality
        /// </summary>
        member inline _.Source(s: #seq<_>) = s

        // /// <summary>
        // /// Method lets us transform data types into our internal representation.
        // /// </summary>
        member inline _.Source(nullable: Nullable<'a>) : 'a voption = nullable |> ValueOption.ofNullable
#endif
