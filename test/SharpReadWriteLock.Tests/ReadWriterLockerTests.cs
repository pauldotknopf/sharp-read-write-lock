using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace SharpReadWriteLock.Tests
{
    public class ReadWriterLockerTests
    {
        private readonly IReadWriteLocker _locker;
        
        public ReadWriterLockerTests()
        {
            _locker = new ReadWriteLocker();
        }

        [Fact]
        public async Task Can_get_read_lock()
        {
            var rl = await _locker.ReadLock("test");
            rl.Type.Should().Be(ReadWriteLockType.Read);
            rl.Dispose();
        }

        [Fact]
        public async Task Can_get_write_lock()
        {
            var wl = await _locker.WriteLock("test");
            wl.Type.Should().Be(ReadWriteLockType.Write);
            wl.Dispose();
        }

        [Fact]
        public async Task Cant_get_write_lock_when_read_lock_active()
        {
            var rl = await _locker.ReadLock("test");
            var wl = await _locker.WriteLock("test", TimeSpan.Zero);
            wl.Should().BeNull();
            rl.Dispose();
            wl = await _locker.WriteLock("test", TimeSpan.Zero);
            wl.Should().NotBeNull();
            wl.Dispose();
        }

        [Fact]
        public async Task Cant_get_read_lock_when_write_lock_active()
        {
            var wl = await _locker.WriteLock("test");
            var rl = await _locker.ReadLock("test", TimeSpan.Zero);
            rl.Should().BeNull();
            wl.Dispose();
            rl = await _locker.ReadLock("test", TimeSpan.Zero);
            rl.Should().NotBeNull();
            rl.Dispose();
        }
        
        [Fact]
        public async Task Can_wait_for_readlock_without_timeout()
        {
            var flow = new AutoResetEvent(false);
            string message = null;
            var task1 = Task.Run(async () =>
            {
                using (await _locker.WriteLock("test"))
                {
                    message = "processing message 1";
                    flow.Set();
                    flow.WaitOne();
                }
            });
            flow.WaitOne();
            message.Should().Be("processing message 1");

            var task2 = Task.Run(async () =>
            {
                using (await _locker.ReadLock("test"))
                {
                    message = "processing message 2";
                    flow.Set();
                }
            });

            task2.Wait(TimeSpan.FromSeconds(1)).Should().BeFalse();

            flow.Set();
            flow.WaitOne();
            message.Should().Be("processing message 2");

            task2.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();

            await task1;
            await task2;
            flow.Set();
            flow.Set();
        }
        
        [Fact]
        public async Task Can_wait_for_writelock_without_timeout()
        {
            var flow = new AutoResetEvent(false);
            string message = null;
            var task1 = Task.Run(async () =>
            {
                using (await _locker.WriteLock("test"))
                {
                    message = "processing message 1";
                    flow.Set();
                    flow.WaitOne();
                }
            });
            flow.WaitOne();
            message.Should().Be("processing message 1");

            var task2 = Task.Run(async () =>
            {
                using (await _locker.WriteLock("test"))
                {
                    message = "processing message 2";
                    flow.Set();
                }
            });

            task2.Wait(TimeSpan.FromSeconds(1)).Should().BeFalse();

            flow.Set();
            flow.WaitOne();
            message.Should().Be("processing message 2");

            task2.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();

            await task1;
            await task2;
            flow.Set();
            flow.Set();
        }
    }
}