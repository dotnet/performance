// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.BenchI)]
public class TreeInsert
{
    private struct Node
    {
        public int A;
        public int L;
        public int R;
    }

    private struct Tree
    {
        public int Root;
        public int NextAvail;
        public Node[] Nodes;
    }

    private Tree _s;

    public TreeInsert()
    {
        _s.Nodes = new Node[10001];
    }

    private void BenchInner(int x)
    {
        /* a tree insertion routine from knuth */
        int i = _s.Root;
        int j = _s.NextAvail;

    L10:
        /* compare */
        if (_s.Nodes[i].A < x)
        {
            if (_s.Nodes[i].L != 0)
            {
                i = _s.Nodes[i].L;
                goto L10;
            }
            else
            {
                _s.Nodes[i].L = j;
                goto L20;
            }
        }
        else
        {
            if (_s.Nodes[i].R != 0)
            {
                i = _s.Nodes[i].R;
                goto L10;
            }
            else
            {
                _s.Nodes[i].R = j;
                goto L20;
            }
        }

    L20:
        /* insert */
        _s.Nodes[j].A = x;
        _s.Nodes[j].L = 0;
        _s.Nodes[j].R = 0;
        _s.NextAvail = j + 1;
    }


    [Benchmark(Description = nameof(TreeInsert))]
    public bool Test()
    {
        _s.Root = 1;
        _s.NextAvail = 2;
        _s.Nodes[1].A = 300;
        _s.Nodes[1].L = 0;
        _s.Nodes[1].R = 0;

        int j = 99999;
        for (int i = 1; i <= 900; i++)
        {
            BenchInner(j & 4095);
            j = j + 33333;
        }

        return (_s.Nodes[500].A == 441);
    }
}
}
