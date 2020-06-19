using System;

namespace SharpReadWriteLock
{
    public interface IReadWriteLock : IDisposable
    {
        ReadWriteLockType Type { get; }
    }
}