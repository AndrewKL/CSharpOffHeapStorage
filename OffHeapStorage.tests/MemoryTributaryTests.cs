using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace OffHeapStorage.tests
{
    [TestFixture]
    public class MemoryTributaryTests
    {
        [Test]
        public void WriteAndReadAcrossBlockBoundaries()
        {
            var data = Enumerable.Range(0, 200000).Select(i => (byte)(i % 251)).ToArray();
            using var stream = new MemoryTributary(0, 4096);

            stream.Write(data, 0, data.Length);
            Assert.That(stream.Length, Is.EqualTo(data.Length));
            Assert.That(stream.AllocatedBytes, Is.EqualTo(49 * 4096));

            stream.Position = 0;
            var result = new byte[data.Length];
            Assert.That(stream.Read(result, 0, result.Length), Is.EqualTo(data.Length));
            Assert.That(result, Is.EqualTo(data));
        }

        [Test]
        public void ReadAtEndReturnsZero()
        {
            using var stream = new MemoryTributary(new byte[] { 1, 2, 3 });
            stream.Position = 3;

            Assert.That(stream.Read(new byte[10], 0, 10), Is.EqualTo(0));
            Assert.That(stream.ReadByte(), Is.EqualTo(-1));
        }

        [Test]
        public void SeekFromEndUsesSignedOffset()
        {
            using var stream = new MemoryTributary(new byte[] { 1, 2, 3, 4 });

            Assert.That(stream.Seek(-1, SeekOrigin.End), Is.EqualTo(3));
            Assert.That(stream.ReadByte(), Is.EqualTo(4));
        }

        [Test]
        public void ReadFromStopsWhenSourceIsExhausted()
        {
            using var stream = new MemoryTributary();

            stream.ReadFrom(new MemoryStream(new byte[] { 1, 2, 3 }), 100);

            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void LengthConstructorPreallocatesZeroedMemory()
        {
            using var stream = new MemoryTributary(10000, 4096);

            Assert.That(stream.Length, Is.EqualTo(10000));
            Assert.That(stream.Position, Is.EqualTo(0));
            Assert.That(stream.AllocatedBytes, Is.EqualTo(3 * 4096));
            Assert.That(stream.ToArray(), Is.All.EqualTo(0));
        }

        [Test]
        public void ShrinkingFreesBlocksAndRegrowingReadsZeros()
        {
            using var stream = new MemoryTributary(0, 16);
            stream.Write(Enumerable.Repeat((byte)0xFF, 40).ToArray(), 0, 40);

            stream.SetLength(20);
            Assert.That(stream.AllocatedBytes, Is.EqualTo(32));

            stream.SetLength(40);
            var data = stream.ToArray();
            Assert.That(data.Take(20), Is.All.EqualTo(0xFF));
            Assert.That(data.Skip(20), Is.All.EqualTo(0));
        }

        [Test]
        public void UseAfterDisposeThrows()
        {
            var stream = new MemoryTributary(new byte[] { 1, 2, 3 });
            stream.Dispose();

            Assert.That(stream.AllocatedBytes, Is.EqualTo(0));
            Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
            Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
        }
    }
}
