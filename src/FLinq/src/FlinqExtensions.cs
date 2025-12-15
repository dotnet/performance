using System.Collections;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Engines;

namespace Flinq;

public static class FlinqExtensions
{
    public static int Count<TEnum, T>(this TEnum e)
        where TEnum : IEnumerator<TEnum, T>
        where T : allows ref struct
    {
        return TEnum.Count(e);
    }

    public static int CountFold<TEnum, T>(this TEnum e)
        where TEnum : IEnumerator<TEnum, T>
        where T : allows ref struct
    {
        int count = 0;
        TEnum.Fold(e, count, new CountFunc<T>());
        return count;
    }

    private struct CountFunc<T> : IFunc<int, T, int>
        where T : allows ref struct
    {
        public int Invoke(int accumulator, T item)
        {
            return accumulator + 1;
        }
    }

    extension<TSelf, T>(TSelf e)
        where TSelf : IEnumerator<TSelf, T>
    {
        public void Consume(Consumer consumer)
        {
            T item;
            do
            {
                item = e.TryGetNext(out bool hasMore);
                if (!hasMore)
                {
                    break;
                }
                consumer.Consume(item);
            } while (true);
        }

        public void ConsumeFold(Consumer consumer)
        {
            TSelf.Fold(e, consumer, new ConsumeFunc<T>());
        }

        public List<T> ToList()
        {
            var list = new List<T>();
            TSelf.Fold(e, list, new AddToListFunc<T>());
            return list;
        }
    }

    private struct ConsumeFunc<TItem> : IFunc<Consumer, TItem, Consumer>
    {
        public Consumer Invoke(Consumer accumulator, TItem item)
        {
            accumulator.Consume(item);
            return accumulator;
        }
    }

    private struct AddToListFunc<T> : IFunc<List<T>, T, List<T>>
    {
        public List<T> Invoke(List<T> list, T item)
        {
            list.Add(item);
            return list;
        }
    }

    extension<TSelf, T>(TSelf e)
        where TSelf : IEnumerator<TSelf, T>
        where T : allows ref struct
    {
        public Map<T, TOut, TSelf, TFunc> Map<TOut, TFunc>(TFunc func)
            where TOut : allows ref struct
            where TFunc : IFunc<T, TOut>
        {
            return TSelf.Select<TOut, TFunc>(e, func);
        }

        public Map<T, TOut, TSelf> Map<TOut>(Func<T, TOut> func)
            where TOut : allows ref struct
        {
            return TSelf.Select(e, func);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Filter<T, TSelf, TFunc> Filter<TFunc>(TFunc predicate)
            where TFunc : IFunc<T, bool>
        {
            return TSelf.Where<TFunc>(e, predicate);
        }
    }
}
