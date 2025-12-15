
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Flinq;

public interface IEnumerator<TSelf, T>
    where TSelf : IEnumerator<TSelf, T>
    where T : allows ref struct
{
    /// <summary>
    /// Returns next element in the sequence if available and sets done to false.
    /// If no more elements are available, sets done to true and returns default.
    /// </summary>
    T TryGetNext(out bool done);

    static virtual int Count(TSelf @this)
    {
        int count = 0;
        bool hasMore;
        do
        {
            @this.TryGetNext(out hasMore);
            if (hasMore)
            {
                count++;
            }
        } while (hasMore);
        return count;
    }

    static virtual TAcc Fold<TAcc, TFunc>(TSelf @this, TAcc accumulator, TFunc func)
        where TAcc : allows ref struct
        where TFunc : IFunc<TAcc, T, TAcc>
    {
        bool hasMore;
        do
        {
            var item = @this.TryGetNext(out hasMore);
            if (hasMore)
            {
                accumulator = func.Invoke(accumulator, item);
            }
        } while (hasMore);
        return accumulator;
    }

    static virtual TAcc Fold<TAcc, TFunc>(TSelf @this, TAcc accumulator, Func<TAcc, T, TAcc> func)
        where TAcc : allows ref struct
    {
        bool hasMore;
        do
        {
            var item = @this.TryGetNext(out hasMore);
            if (hasMore)
            {
                accumulator = func.Invoke(accumulator, item);
            }
        } while (hasMore);
        return accumulator;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static virtual Map<T, TOut, TSelf, TFunc> Select<TOut, TFunc>(TSelf @this, TFunc func)
        where TOut : allows ref struct
        where TFunc : IFunc<T, TOut>
    {
        return new Map<T, TOut, TSelf, TFunc>(@this, func);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static virtual Map<T, TOut, TSelf> Select<TOut>(TSelf @this, Func<T, TOut> func)
        where TOut : allows ref struct
    {
        return new Map<T, TOut, TSelf>(@this, func);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static virtual Filter<T, TSelf, TFunc> Where<TFunc>(TSelf @this, TFunc predicate)
        where TFunc : IFunc<T, bool>
    {
        return new Filter<T, TSelf, TFunc>(@this, predicate);
    }
}

public interface IBoxEnumerator<T>
    : IEnumerator<IBoxEnumerator<T>, T>
    where T : allows ref struct
{
}

public struct Map<TIn, TOut, TEnum, TFunc>
    : IEnumerator<Map<TIn, TOut, TEnum, TFunc>, TOut>
    where TIn : allows ref struct
    where TOut : allows ref struct
    where TEnum : IEnumerator<TEnum, TIn>
    where TFunc : IFunc<TIn, TOut>
{
    private TEnum _source;
    private TFunc _func;

    public Map(TEnum source, TFunc func)
    {
        _source = source;
        _func = func;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut TryGetNext(out bool hasMore)
    {
        var input = _source.TryGetNext(out hasMore);
        if (hasMore)
        {
            return _func.Invoke(input);
        }
        else
        {
            return default!;
        }
    }

    static TAcc IEnumerator<Map<TIn, TOut, TEnum, TFunc>, TOut>.Fold<TAcc, TFoldFunc>(Map<TIn, TOut, TEnum, TFunc> @this, TAcc accumulator, TFoldFunc func)
    {
        return TEnum.Fold(
            @this._source,
            accumulator,
            new MapFoldFunc<TAcc, TFoldFunc>(@this._func, func));
    }

    private struct MapFoldFunc<TAcc, TFoldFunc>(TFunc mapFunc, TFoldFunc foldFunc)
        : IFunc<TAcc, TIn, TAcc>
        where TAcc : allows ref struct
        where TFoldFunc : IFunc<TAcc, TOut, TAcc>
    {
        public TAcc Invoke(TAcc acc, TIn t)
        {
            return foldFunc.Invoke(acc, mapFunc.Invoke(t));
        }
    }
}

public struct Map<TIn, TOut, TEnum>
    : IEnumerator<Map<TIn, TOut, TEnum>, TOut>
    where TIn : allows ref struct
    where TOut : allows ref struct
    where TEnum : IEnumerator<TEnum, TIn>
{
    private TEnum _source;
    private Func<TIn, TOut> _func;

    public Map(TEnum source, Func<TIn, TOut> func)
    {
        _source = source;
        _func = func;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut TryGetNext(out bool hasMore)
    {
        var input = _source.TryGetNext(out hasMore);
        if (hasMore)
        {
            return _func.Invoke(input);
        }
        else
        {
            return default!;
        }
    }

    static TAcc IEnumerator<Map<TIn, TOut, TEnum>, TOut>.Fold<TAcc, TFoldFunc>(Map<TIn, TOut, TEnum> @this, TAcc accumulator, TFoldFunc func)
    {
        return TEnum.Fold(
            @this._source,
            accumulator,
            new MapFoldFunc<TAcc, TFoldFunc>(@this._func, func));
    }

    private struct MapFoldFunc<TAcc, TFoldFunc>(Func<TIn, TOut> mapFunc, TFoldFunc foldFunc)
        : IFunc<TAcc, TIn, TAcc>
        where TAcc : allows ref struct
        where TFoldFunc : IFunc<TAcc, TOut, TAcc>
    {
        public TAcc Invoke(TAcc acc, TIn t)
        {
            return foldFunc.Invoke(acc, mapFunc.Invoke(t));
        }
    }
}

public struct Filter<T, TEnum, TFunc>(TEnum source, TFunc predicate)
    : IEnumerator<Filter<T, TEnum, TFunc>, T>
    where T : allows ref struct
    where TEnum : IEnumerator<TEnum, T>
    where TFunc : IFunc<T, bool>
{
    private TEnum _source = source;
    private TFunc _predicate = predicate;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T TryGetNext(out bool hasMore)
    {
        while (true)
        {
            var item = _source.TryGetNext(out hasMore);
            if (!hasMore)
            {
                return default!;
            }

            if (_predicate.Invoke(item))
            {
                return item;
            }
        }
    }

    static TAcc IEnumerator<Filter<T, TEnum, TFunc>, T>.Fold<TAcc, TFoldFunc>(Filter<T, TEnum, TFunc> @this, TAcc accumulator, TFoldFunc func)
    {
        return TEnum.Fold(
            @this._source,
            accumulator,
            new FilterFoldFunc<TAcc, TFoldFunc>(@this._predicate, func));
    }

    private struct FilterFoldFunc<TAcc, TFoldFunc>(TFunc predicate, TFoldFunc foldFunc)
        : IFunc<TAcc, T, TAcc>
        where TAcc : allows ref struct
        where TFoldFunc : IFunc<TAcc, T, TAcc>
    {
        public TAcc Invoke(TAcc acc, T t)
        {
            return predicate.Invoke(t) ? foldFunc.Invoke(acc, t) : acc;
        }
    }
}