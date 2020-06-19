# SharpReadWriteLock

A dumb lib to help manage read/write locks.

[![SharpReadWriteLock](https://img.shields.io/nuget/v/SharpReadWriteLock.svg?style=flat-square&label=SharpReadWriteLock)](http://www.nuget.org/packages/SharpReadWriteLock/)

## Example

```csharp
IReadWriteLocker locker = new ReadWriteLocker();

using (await locker.WriteLock("unique-key"))
{
    using (var l = await locker.ReadLock("unique-key", TimeSpan.FromSeconds(10)))
    {
        // Timeout, l == null, because there is currently a write lock.
    }

    using (var l = await locker.ReadLock("unique-key")) // Dead lock here.
    {
        
    }
}

using (await locker.ReadLock("unique-key"))
{
    using (var l = await locker.ReadLock("unique-key"))
    {
        // All good
    }

    using (var l = await locker.WriteLock("unique-key")) // Dead lock here, someone is currently reading.
    {
        
    }
}
```