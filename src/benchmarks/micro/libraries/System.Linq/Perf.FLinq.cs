// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Flinq;

namespace System.Linq.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.LINQ)]
    [MemoryDiagnoser]
    public class ToListBenchmarks
    {
        private List<Book> _books = null!;
        private CenturyFilter _centuryFilter;

        [GlobalSetup]
        public void Setup()
        {
            _books = new List<Book> {
                new Book { Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Year = 1925 },
                new Book { Title = "To Kill a Mockingbird", Author = "Harper Lee", Year = 1960 },
                new Book { Title = "1984", Author = "George Orwell", Year = 1949 },
                new Book { Title = "Pride and Prejudice", Author = "Jane Austen", Year = 1813 },
                new Book { Title = "The Catcher in the Rye", Author = "J.D. Salinger", Year = 1951 }
            };
            _centuryFilter = new CenturyFilter { _targetCentury = 20 };
        }

        [Benchmark(Baseline = true)]
        public List<int> LinqToList()
        {
            return _books
                .Select(book => (book.Year - 1) / 100 + 1)
                .Where(c => c == 20)
                .ToList();
        }

        [Benchmark]
        public List<int> FlinqToList()
        {
            return _books
                .Flinq()
                .Map<ListEnumerator<Book>, Book, int, CenturyCalculator>(new CenturyCalculator())
                .Filter<Map<Book, int, ListEnumerator<Book>, CenturyCalculator>, int, CenturyFilter>(_centuryFilter)
                .ToList<Filter<int, Map<Book, int, ListEnumerator<Book>, CenturyCalculator>, CenturyFilter>, int>();
        }

        [Benchmark]
        public List<int> ForEachToList()
        {
            var result = new List<int>();
            foreach (var book in _books)
            {
                int century = (book.Year - 1) / 100 + 1;
                if (century == 20)
                {
                    result.Add(century);
                }
            }
            return result;
        }
    }

    public struct CenturyCalculator : IFunc<Book, int>
    {
        public int Invoke(Book book)
        {
            return (book.Year - 1) / 100 + 1;
        }
    }

    public struct CenturyFilter : IFunc<int, bool>
    {
        public int _targetCentury;
        public bool Invoke(int century)
        {
            return century == _targetCentury;
        }
    }

    public class Book
    {
        public string Title = null!;
        public string Author = null!;
        public int Year;
    }
}
