namespace Flinq;

public static class ListExtensions
{
    public static ListEnumerator<T> Flinq<T>(this List<T> list)
    {
        return new ListEnumerator<T>(list);
    }
}

public struct ListEnumerator<T> : IEnumerator<ListEnumerator<T>, T>, IBoxEnumerator<T>
{
    private readonly List<T> _list;
    private int _index;

    public ListEnumerator(List<T> list)
    {
        _list = list;
        _index = -1;
    }

    public static int Count(ListEnumerator<T> @this) => @this._list.Count - @this._index - 1;

    public T TryGetNext(out bool hasMore)
    {
        _index++;
        if (_index < _list.Count)
        {
            hasMore = true;
            return _list[_index];
        }
        else
        {
            hasMore = false;
            return default!;
        }
    }

    static TAcc IEnumerator<ListEnumerator<T>, T>.Fold<TAcc, TFunc>(ListEnumerator<T> @this, TAcc accumulator, TFunc func)
    {
        foreach (var item in @this._list)
        {
            accumulator = func.Invoke(accumulator, item);
        }
        return accumulator;
    }
}