using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroBenchmarks;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Exceptions
{
    [BenchmarkCategory(Categories.CoreCLR)]
    public class Handling
    {
        public enum ExceptionKind { Software,  Hardware, ReflectionSoftware, ReflectionHardware }

        public class Exception1 : Exception { }
        public class Exception2 : Exception { }
        public class Exception3 : Exception { }
        public class Exception4 : Exception { }
        public class Exception5 : Exception { }
        public class Exception6 : Exception { }
        public class Exception7 : Exception { }
        public class Exception8 : Exception { }
        public class Exception9 : Exception { }
        public class Exception10 : Exception { }

        public object AlwaysNull { get; set; }

        private readonly Consumer _consumer = new Consumer();
        private readonly MethodInfo _throwMethod = typeof(Handling).GetMethod(nameof(Throw), BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly object[] _throwMehtodParameterSoft = new object[] { ExceptionKind.Software };
        private readonly object[] _throwMehtodParameterHard = new object[] { ExceptionKind.Hardware };

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Throw(ExceptionKind kind)
        {
            if (kind == ExceptionKind.Software)
            {
                throw new Exception();
            }
            else if (kind == ExceptionKind.Hardware)
            {
                _consumer.Consume(AlwaysNull.GetHashCode()); // this is going to throw a NullReferenceException
            }
            else if (kind == ExceptionKind.ReflectionSoftware)
            {
                _throwMethod.Invoke(this, _throwMehtodParameterSoft);
            }
            else if (kind == ExceptionKind.ReflectionHardware)
            {
                _throwMethod.Invoke(this, _throwMehtodParameterHard);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DoNothing() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool ReturnTrue() => true;

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        [Arguments(ExceptionKind.ReflectionSoftware)]
        [Arguments(ExceptionKind.ReflectionHardware)]
        public Exception ThrowAndCatch(ExceptionKind kind)
        {
            try
            {
                Throw(kind);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        [Arguments(ExceptionKind.ReflectionSoftware)]
        [Arguments(ExceptionKind.ReflectionHardware)]
        public Exception ThrowAndCatchManyCatchBlocks(ExceptionKind kind)
        {
            try
            {
                Throw(kind);
            }
            catch (Exception5 ex)
            {
                return ex;
            }
            catch (Exception4 ex)
            {
                return ex;
            }
            catch (Exception3 ex)
            {
                return ex;
            }
            catch (Exception2 ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                return ex; // actual
            }

            return null;
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception ThrowAndCatchFinally(ExceptionKind kind)
        {
            try
            {
                Throw(kind);
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
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception ThrowAndCatchWhen(ExceptionKind kind)
        {
            try
            {
                Throw(kind);
            }
            catch (Exception ex) when (ReturnTrue())
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception ThrowAndCatchWhenFinally(ExceptionKind kind)
        {
            try
            {
                Throw(kind);
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
        private void Level1(ExceptionKind kind) => Level2(kind);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level2(ExceptionKind kind) => Level3(kind);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level3(ExceptionKind kind) => Level4(kind);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level4(ExceptionKind kind) => Level5(kind);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level5(ExceptionKind kind) => Level6(kind);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level6(ExceptionKind kind) => Level7(kind);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level7(ExceptionKind kind) => Level8(kind);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level8(ExceptionKind kind) => Level9(kind);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level9(ExceptionKind kind) => Level10(kind);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level10(ExceptionKind kind) => Throw(kind);

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        [Arguments(ExceptionKind.ReflectionSoftware)]
        [Arguments(ExceptionKind.ReflectionHardware)]
        public Exception ThrowAndCatchDeep(ExceptionKind kind)
        {
            try
            {
                Level1(kind);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception ThrowAndCatchFinallyDeep(ExceptionKind kind)
        {
            try
            {
                Level1(kind);
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
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception ThrowAndCatchWhenDeep(ExceptionKind kind)
        {
            try
            {
                Level1(kind);
            }
            catch (Exception ex) when (ReturnTrue())
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception ThrowAndCatchWhenFinallyDeep(ExceptionKind kind)
        {
            try
            {
                Level1(kind);
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
        private void ThrowRecursive(ExceptionKind kind, int depth)
        {
            if (depth-- == 0)
            {
                Throw(kind);
            }
            else
            {
                ThrowRecursive(kind, depth);
            }
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        [Arguments(ExceptionKind.ReflectionSoftware)]
        [Arguments(ExceptionKind.ReflectionHardware)]
        public Exception ThrowAndCatchDeepRecursive(ExceptionKind kind)
        {
            try
            {
                ThrowRecursive(kind, 10);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception ThrowAndCatchFinallyDeepRecursive(ExceptionKind kind)
        {
            try
            {
                ThrowRecursive(kind, 10);
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
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception ThrowAndCatchWhenDeepRecursive(ExceptionKind kind)
        {
            try
            {
                ThrowRecursive(kind, 10);
            }
            catch (Exception ex) when (ReturnTrue())
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception ThrowAndCatchWhenFinallyDeepRecursive(ExceptionKind kind)
        {
            try
            {
                ThrowRecursive(kind, 10);
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



        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception MultipleNestedTryCatch_FirstCatches(ExceptionKind kind)
        {
            try
            {
                try
                {
                    try
                    {
                        try
                        {
                            Throw(kind);
                        }
                        catch (Exception ex)
                        {
                            return ex;
                        }
                    }
                    catch (Exception1)
                    {
                        throw;
                    }
                }
                catch (Exception2)
                {
                    throw;
                }
            }
            catch (Exception3)
            {
                throw;
            }

            return null;
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception MultipleNestedTryCatch_LastCatches(ExceptionKind kind)
        {
            try
            {
                try
                {
                    try
                    {
                        try
                        {
                            Throw(kind);
                        }
                        catch (Exception1)
                        {
                            throw;
                        }
                    }
                    catch (Exception2)
                    {
                        throw;
                    }
                }
                catch (Exception3)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception MultipleNestedTryFinally(ExceptionKind kind)
        {
            try
            {
                try
                {
                    try
                    {
                        try
                        {
                            Throw(kind);
                        }
                        finally
                        {
                            DoNothing();
                        }
                    }
                    finally
                    {
                        DoNothing();
                    }
                }
                finally
                {
                    DoNothing();
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level1Rethrow(ExceptionKind kind) { try { Level2Rethrow(kind); } catch (Exception) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level2Rethrow(ExceptionKind kind) { try { Level3Rethrow(kind); } catch (Exception) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level3Rethrow(ExceptionKind kind) { try { Level4Rethrow(kind); } catch (Exception) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level4Rethrow(ExceptionKind kind) { try { Level5Rethrow(kind); } catch (Exception) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level5Rethrow(ExceptionKind kind) { try { Level6Rethrow(kind); } catch (Exception) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level6Rethrow(ExceptionKind kind) { try { Level7Rethrow(kind); } catch (Exception) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level7Rethrow(ExceptionKind kind) { try { Level8Rethrow(kind); } catch (Exception) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level8Rethrow(ExceptionKind kind) { try { Level9Rethrow(kind); } catch (Exception) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level9Rethrow(ExceptionKind kind) { try { Level10Rethrow(kind); } catch (Exception) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level10Rethrow(ExceptionKind kind) { try { Throw(kind); } catch (Exception) { throw; } }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception CatchAndRethrowDeep(ExceptionKind kind)
        {
            try
            {
                Level1Rethrow(kind);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level1ThrowOther(ExceptionKind kind) { try { Level2ThrowOther(kind); } catch (Exception ex) { throw new Exception("level1", ex); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level2ThrowOther(ExceptionKind kind) { try { Level3ThrowOther(kind); } catch (Exception ex) { throw new Exception("level2", ex); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level3ThrowOther(ExceptionKind kind) { try { Level4ThrowOther(kind); } catch (Exception ex) { throw new Exception("level3", ex); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level4ThrowOther(ExceptionKind kind) { try { Level5ThrowOther(kind); } catch (Exception ex) { throw new Exception("level4", ex); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level5ThrowOther(ExceptionKind kind) { try { Level6ThrowOther(kind); } catch (Exception ex) { throw new Exception("level5", ex); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level6ThrowOther(ExceptionKind kind) { try { Level7ThrowOther(kind); } catch (Exception ex) { throw new Exception("level6", ex); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level7ThrowOther(ExceptionKind kind) { try { Level8ThrowOther(kind); } catch (Exception ex) { throw new Exception("level7", ex); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level8ThrowOther(ExceptionKind kind) { try { Level9ThrowOther(kind); } catch (Exception ex) { throw new Exception("level8", ex); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level9ThrowOther(ExceptionKind kind) { try { Level10ThrowOther(kind); } catch (Exception ex) { throw new Exception("level9", ex); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level10ThrowOther(ExceptionKind kind) { try { Throw(kind); } catch (Exception ex) { throw new Exception("level10", ex); } }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception CatchAndThrowOtherDeep(ExceptionKind kind)
        {
            try
            {
                Level1ThrowOther(kind);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level1Finally(ExceptionKind kind) { try { Level2Finally(kind); } finally { DoNothing(); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level2Finally(ExceptionKind kind) { try { Level3Finally(kind); } finally { DoNothing(); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level3Finally(ExceptionKind kind) { try { Level4Finally(kind); } finally { DoNothing(); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level4Finally(ExceptionKind kind) { try { Level5Finally(kind); } finally { DoNothing(); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level5Finally(ExceptionKind kind) { try { Level6Finally(kind); } finally { DoNothing(); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level6Finally(ExceptionKind kind) { try { Level7Finally(kind); } finally { DoNothing(); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level7Finally(ExceptionKind kind) { try { Level8Finally(kind); } finally { DoNothing(); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level8Finally(ExceptionKind kind) { try { Level9Finally(kind); } finally { DoNothing(); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level9Finally(ExceptionKind kind) { try { Level10Finally(kind); } finally { DoNothing(); } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level10Finally(ExceptionKind kind) { try { Throw(kind); } finally { DoNothing(); } }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception TryAndFinallyDeep(ExceptionKind kind)
        {
            try
            {
                Level1Finally(kind);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level1TryDontCatch(ExceptionKind kind) { try { Level2TryDontCatch(kind); } catch (Exception1) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level2TryDontCatch(ExceptionKind kind) { try { Level3TryDontCatch(kind); } catch (Exception2) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level3TryDontCatch(ExceptionKind kind) { try { Level4TryDontCatch(kind); } catch (Exception3) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level4TryDontCatch(ExceptionKind kind) { try { Level5TryDontCatch(kind); } catch (Exception4) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level5TryDontCatch(ExceptionKind kind) { try { Level6TryDontCatch(kind); } catch (Exception5) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level6TryDontCatch(ExceptionKind kind) { try { Level7TryDontCatch(kind); } catch (Exception6) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level7TryDontCatch(ExceptionKind kind) { try { Level8TryDontCatch(kind); } catch (Exception7) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level8TryDontCatch(ExceptionKind kind) { try { Level9TryDontCatch(kind); } catch (Exception8) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level9TryDontCatch(ExceptionKind kind) { try { Level10TryDontCatch(kind); } catch (Exception9) { throw; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Level10TryDontCatch(ExceptionKind kind) { try { Throw(kind); } catch (Exception10) { throw; } }

        [Benchmark]
        [Arguments(ExceptionKind.Software)]
        [Arguments(ExceptionKind.Hardware)]
        public Exception TryAndCatchDeep_CaugtAtTheTop(ExceptionKind kind)
        {
            try
            {
                Level1TryDontCatch(kind);
            }
            catch (Exception ex) // the handler that actualy catches the exception
            {
                return ex;
            }

            return null;
        }
    }
}
