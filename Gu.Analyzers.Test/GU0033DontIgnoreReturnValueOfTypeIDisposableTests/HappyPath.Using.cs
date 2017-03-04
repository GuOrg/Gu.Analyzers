namespace Gu.Analyzers.Test.GU0033DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Using : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task WhenCreatingStreamReader()
            {
                var testCode = @"
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            using(var reader = new StreamReader(File.OpenRead(string.Empty)))
			{
			}
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task SampleWithAwait()
            {
                var testCode = @"
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Foo
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}