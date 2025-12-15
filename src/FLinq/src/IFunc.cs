
namespace Flinq;

public interface IFunc<in TIn, out TOut>
    where TIn : allows ref struct
    where TOut : allows ref struct
{
    TOut Invoke(TIn input);
}

public interface IFunc<in T1, in T2, out TOut>
    where T1 : allows ref struct
    where T2 : allows ref struct
    where TOut : allows ref struct
{
    TOut Invoke(T1 t1, T2 t2);
}