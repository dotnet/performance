namespace FsToolkit.ErrorHandling

open System.Threading.Tasks
open System

[<AutoOpen>]
module AsyncOptionCE =
    type AsyncOptionBuilder() =

        member inline _.Return(value: 'value) : Async<'value option> = AsyncOption.retn value

        member inline _.ReturnFrom(value: Async<'value option>) : Async<'value option> = value

        member inline _.Zero() : Async<unit option> = async.Return <| option.Zero()

        member inline _.Bind
            (
                input: Async<'input option>,
                [<InlineIfLambda>] binder: 'input -> Async<'output option>
            ) : Async<'output option> =
            AsyncOption.bind binder input

        member inline _.Delay(generator: unit -> Async<'value option>) : Async<'value option> = async.Delay generator

        member inline this.Combine(computation1: Async<_ option>, computation2: Async<_ option>) : Async<_ option> =
            this.Bind(computation1, (fun () -> computation2))

        member inline _.TryWith
            (
                computation: Async<'value option>,
                [<InlineIfLambda>] handler: System.Exception -> Async<'value option>
            ) : Async<_ option> =
            async.TryWith(computation, handler)

        member inline _.TryFinally
            (
                computation: Async<'value option>,
                [<InlineIfLambda>] compensation: unit -> unit
            ) : Async<'value option> =
            async.TryFinally(computation, compensation)

        member inline this.Using
            (
                resource: 'disposable :> IDisposable,
                [<InlineIfLambda>] binder: 'disposable -> Async<'output option>
            ) : Async<'output option> =
            this.TryFinally(
                (binder resource),
                (fun () ->
                    if not <| obj.ReferenceEquals(resource, null) then
                        resource.Dispose())
            )

        member this.While(guard: unit -> bool, computation: Async<unit option>) : Async<unit option> =
            if not <| guard () then
                this.Zero()
            else
                this.Bind(computation, (fun () -> this.While(guard, computation)))

        member inline this.For(sequence: #seq<'input>, binder: 'input -> Async<unit option>) : Async<unit option> =
            this.Using(
                sequence.GetEnumerator(),
                fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> binder enum.Current))
            )

        /// <summary>
        /// Method lets us transform data types into our internal representation. This is the identity method to recognize the self type.
        ///
        /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
        /// </summary>
        member inline _.Source(async: Async<'value option>) : Async<'value option> = async

#if !FABLE_COMPILER
        /// <summary>
        /// Method lets us transform data types into our internal representation.
        /// </summary>
        member inline _.Source(task: Task<'value option>) : Async<'value option> = task |> Async.AwaitTask
#endif

    let asyncOption = AsyncOptionBuilder()


[<AutoOpen>]
// Having members as extensions gives them lower priority in
// overload resolution and allows skipping more type annotations.
module AsyncOptionCEExtensions =

    type AsyncOptionBuilder with
        /// <summary>
        /// Needed to allow `for..in` and `for..do` functionality
        /// </summary>
        member inline _.Source(s: #seq<_>) : #seq<_> = s

        /// <summary>
        /// Method lets us transform data types into our internal representation.
        /// </summary>
        member inline _.Source(r: 'value option) : Async<option<'value>> = Async.singleton r

        /// <summary>
        /// Method lets us transform data types into our internal representation.
        /// </summary>
        member inline _.Source(a: Async<'value>) : Async<option<'value>> = a |> Async.map Some

#if !FABLE_COMPILER
        /// <summary>
        /// Method lets us transform data types into our internal representation.
        /// </summary>
        member inline _.Source(a: Task<'value>) : Async<option<'value>> = a |> Async.AwaitTask |> Async.map Some
#endif
