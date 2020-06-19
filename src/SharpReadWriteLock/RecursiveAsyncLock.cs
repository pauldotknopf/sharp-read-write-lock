using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SharpReadWriteLock
{
    public class RecursiveAsyncLock
    {
        private readonly AsyncLocal<SemaphoreSlim> _currentSemaphore =
            new AsyncLocal<SemaphoreSlim> { Value = new SemaphoreSlim(1) };

        public async Task<T> DoWithLock<T>(Func<Task<T>> body)
        {
            SemaphoreSlim currentSem = _currentSemaphore.Value;
            await currentSem.WaitAsync();
            var nextSem = new SemaphoreSlim(1);
            _currentSemaphore.Value = nextSem;
            T result;
            
            try
            {
                result = await body();
            }
            finally
            {
                Debug.Assert(nextSem == _currentSemaphore.Value);
                await nextSem.WaitAsync();
                _currentSemaphore.Value = currentSem;
                currentSem.Release();
            }

            return result;
        }

        public async Task DoWithLock(Func<Task> body)
        {
            await DoWithLock(async () =>
            {
                await body();
                return (object)null;
            });
        }
    }
}