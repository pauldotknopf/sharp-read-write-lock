using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharpReadWriteLock
{
    public class ReadWriteLocker :  IReadWriteLocker
    {
        private readonly Dictionary<object, RefCounted> _locks = new Dictionary<object, RefCounted>();
        
        public async Task<IReadWriteLock> ReadLock(string key, TimeSpan? timeout = null)
        {
            var l = GetOrCreate(key);
            IDisposable session;
            if (timeout.HasValue)
            {
                if (timeout == Timeout.InfiniteTimeSpan)
                {
                    session = await l.ReaderLockAsync();
                }
                else if (timeout == TimeSpan.Zero)
                {
                    // Pass in a cancelled token to have no timeout.
                    try
                    {
                        session = await l.ReaderLockAsync(new CancellationToken(true));
                    }
                    catch (OperationCanceledException)
                    {
                        // Lock couldn't be acquired.
                        // This will unref the lock
                        new Releaser(this, null, key, ReadWriteLockType.Read).Dispose();
                        return null;
                    }
                }
                else
                {
                    var source = new CancellationTokenSource();
                    source.CancelAfter(timeout.Value);
                    session = await l.ReaderLockAsync(source.Token);
                }
            }
            else
            {
                session = await l.ReaderLockAsync();
            }
            return new Releaser(this, session, key, ReadWriteLockType.Read);
        }

        public async Task<IReadWriteLock> WriteLock(string key, TimeSpan? timeout = null)
        {
            var l = GetOrCreate(key);
            IDisposable session;
            if (timeout.HasValue)
            {
                if (timeout == Timeout.InfiniteTimeSpan)
                {
                    session = await l.WriterLockAsync();
                }
                else if (timeout == TimeSpan.Zero)
                {
                    // Pass in a cancelled token to have no timeout.
                    try
                    {
                        session = await l.WriterLockAsync(new CancellationToken(true));
                    }
                    catch (OperationCanceledException)
                    {
                        // Lock couldn't be acquired.
                        // This will unref the lock
                        new Releaser(this, null, key, ReadWriteLockType.Write).Dispose();
                        return null;
                    }
                }
                else
                {
                    var source = new CancellationTokenSource();
                    source.CancelAfter(timeout.Value);
                    session = await l.WriterLockAsync(source.Token);
                }
            }
            else
            {
                session = await l.WriterLockAsync();
            }
            return new Releaser(this, session, key, ReadWriteLockType.Write);
        }
        
        private Nito.AsyncEx.AsyncReaderWriterLock GetOrCreate(object key)
        {
            RefCounted item;
            
            lock (_locks)
            {
                if (_locks.TryGetValue(key, out item))
                {
                    ++item.RefCount;
                }
                else
                {
                    item = new RefCounted();
                    _locks[key] = item;
                }
            }
            
            return item.Value;
        }

        private class RefCounted
        {
            public RefCounted()
            {
                RefCount = 1;
                Value = new Nito.AsyncEx.AsyncReaderWriterLock();
            }

            public int RefCount { get; set; }

            public Nito.AsyncEx.AsyncReaderWriterLock Value { get; }
        }
        
        private class Releaser : IReadWriteLock
        {
            private readonly ReadWriteLocker _locker;
            private readonly IDisposable _lockSession;
            private readonly string _key;
            
            public Releaser(ReadWriteLocker locker, IDisposable lockSession, string key, ReadWriteLockType type)
            {
                _locker = locker;
                _lockSession = lockSession;
                _key = key;
                Type = type;
            }

            public void Dispose()
            {
                lock (_locker._locks)
                {
                    var item = _locker._locks[_key];
                    --item.RefCount;
                    if (item.RefCount == 0)
                        _locker._locks.Remove(_key);
                }
                _lockSession?.Dispose();
            }

            public ReadWriteLockType Type { get; }
        }
    }
}