using System;
using System.Linq;

namespace Benchmarks.Serializers
{
    internal static class SerializerBenchmarks
    {
        internal static Type[] GetTypes()
            => GetOpenGenericBenchmarks()
                .SelectMany(openGeneric => GetViewModels().Select(viewModel => openGeneric.MakeGenericType(viewModel)))
                .ToArray();

        private static Type[] GetOpenGenericBenchmarks()
            => new[]
            {
                typeof(Json_ToString<>),
                typeof(Json_ToStream<>),
                typeof(Json_FromString<>),
                typeof(Json_FromStream<>),
                typeof(Xml_ToStream<>),
                typeof(Xml_FromStream<>),
                typeof(Binary_ToStream<>),
                typeof(Binary_FromStream<>)
            };

        private static Type[] GetViewModels()
            => new[]
            {
                typeof(LoginViewModel),
                typeof(Location),
                typeof(IndexViewModel),
                typeof(MyEventsListerViewModel)
            };
    }
}