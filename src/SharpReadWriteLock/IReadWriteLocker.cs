using System;
using System.Threading.Tasks;

namespace SharpReadWriteLock
{
    public interface IReadWriteLocker
    {
        Task<IReadWriteLock> ReadLock(string key, TimeSpan? timeout = null);

        Task<IReadWriteLock> WriteLock(string key, TimeSpan? timeout = null);
    }
}