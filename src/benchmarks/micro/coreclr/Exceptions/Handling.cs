using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;
using System.Runtime.CompilerServices;

namespace Exceptions
{
    [BenchmarkCategory(Categories.CoreCLR)]
    public class Handling
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DoNothing() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool ReturnTrue() => true;

        [Benchmark]
        public Exception ThrowAndCatch()
        {
            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        [Benchmark]
        public Exception ThrowAndCatchFinally()
        {
            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DoNothing();
            }
        }

        [Benchmark]
        public Exception ThrowAndCatchWhen()
        {
            try
            {
                throw new Exception();
            }
            catch (Exception ex) when (ReturnTrue())
            {
                return ex;
            }
        }

        [Benchmark]
        public Exception ThrowAndCatchWhenFinally()
        {
            try
            {
                throw new Exception();
            }
            catch (Exception ex) when (ReturnTrue())
            {
                return ex;
            }
            finally
            {
                DoNothing();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level1() => Level2();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level2() => Level3();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level3() => Level4();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level4() => Level5();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level5() => Level6();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level6() => Level7();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level7() => Level8();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level8() => Level9();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level9() => Level10();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level10() => throw new Exception();

        [Benchmark]
        public Exception ThrowAndCatchDeep()
        {
            try
            {
                Level1();
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        public Exception ThrowAndCatchFinallyDeep()
        {
            try
            {
                Level1();
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DoNothing();
            }

            return null;
        }

        [Benchmark]
        public Exception ThrowAndCatchWhenDeep()
        {
            try
            {
                Level1();
            }
            catch (Exception ex) when (ReturnTrue())
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        public Exception ThrowAndCatchWhenFinallyDeep()
        {
            try
            {
                Level1();
            }
            catch (Exception ex) when (ReturnTrue())
            {
                return ex;
            }
            finally
            {
                DoNothing();
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowRecursive(int depth)
        {
            if (depth-- == 0)
            {
                throw new Exception();
            }
            else
            {
                ThrowRecursive(depth);
            }
        }

        [Benchmark]
        public Exception ThrowAndCatchDeepRecursive()
        {
            try
            {
                ThrowRecursive(10);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        public Exception ThrowAndCatchFinallyDeepRecursive()
        {
            try
            {
                ThrowRecursive(10);
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                DoNothing();
            }

            return null;
        }

        [Benchmark]
        public Exception ThrowAndCatchWhenDeepRecursive()
        {
            try
            {
                ThrowRecursive(10);
            }
            catch (Exception ex) when (ReturnTrue())
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        public Exception ThrowAndCatchWhenFinallyDeepRecursive()
        {
            try
            {
                ThrowRecursive(10);
            }
            catch (Exception ex) when (ReturnTrue())
            {
                return ex;
            }
            finally
            {
                DoNothing();
            }

            return null;
        }
    }
}
