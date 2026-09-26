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
            var stream = new MemoryTributary();

            stream.Write(data, 0, data.Length);
            Assert.That(stream.Length, Is.EqualTo(data.Length));

            stream.Position = 0;
            var result = new byte[data.Length];
            Assert.That(stream.Read(result, 0, result.Length), Is.EqualTo(data.Length));
            Assert.That(result, Is.EqualTo(data));
        }

        [Test]
        public void ReadAtEndReturnsZero()
        {
            var stream = new MemoryTributary(new byte[] { 1, 2, 3 });
            stream.Position = 3;

            Assert.That(stream.Read(new byte[10], 0, 10), Is.EqualTo(0));
            Assert.That(stream.ReadByte(), Is.EqualTo(-1));
        }

        [Test]
        public void SeekFromEndUsesSignedOffset()
        {
            var stream = new MemoryTributary(new byte[] { 1, 2, 3, 4 });

            Assert.That(stream.Seek(-1, SeekOrigin.End), Is.EqualTo(3));
            Assert.That(stream.ReadByte(), Is.EqualTo(4));
        }

        [Test]
        public void ReadFromStopsWhenSourceIsExhausted()
        {
            var stream = new MemoryTributary();

            stream.ReadFrom(new MemoryStream(new byte[] { 1, 2, 3 }), 100);

            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        }
    }
}
