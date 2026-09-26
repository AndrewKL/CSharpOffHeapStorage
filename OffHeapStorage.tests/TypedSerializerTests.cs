using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace OffHeapStorage.tests
{
    [TestFixture]
    public class TypedSerializerTests
    {
        public class Item
        {
            public int I { get; set; }
            public bool B { get; set; }
            public decimal M { get; set; }
            public string S { get; set; }
            public float F { get; set; }
            public double D { get; set; }
            public int ReadOnly { get { return 7; } }
        }

        public class NoParameterlessConstructor
        {
            public NoParameterlessConstructor(int a) { A = a; }
            public int A { get; set; }
        }

        public class NoSerializableProperties
        {
            public long Unsupported { get; set; }
        }

        [Test]
        public void WritesSameBytesAsSerializer()
        {
            var items = Enumerable.Range(0, 100).Select(i => new Item
            {
                I = i, B = i % 3 == 0, M = i * 0.5m, S = i % 5 == 0 ? null : "s" + i, F = i / 2f, D = i / 9d
            }).ToList();

            using var boxed = new MemoryStream();
            using var typed = new MemoryStream();
            var boxedSerializer = new Serializer<Item>(boxed);
            var typedSerializer = new TypedSerializer<Item>(typed);
            foreach (var item in items)
            {
                boxedSerializer.Serialize(item);
                typedSerializer.Serialize(item);
            }

            Assert.That(typed.ToArray(), Is.EqualTo(boxed.ToArray()));
        }

        [Test]
        public void TypeWithoutParameterlessConstructorThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TypedSerializer<NoParameterlessConstructor>(new MemoryStream()));
        }

        [Test]
        public void TypeWithNoSerializablePropertiesWritesNothing()
        {
            using var stream = new MemoryStream();
            new TypedSerializer<NoSerializableProperties>(stream).Serialize(new NoSerializableProperties { Unsupported = 5 });

            Assert.That(stream.Length, Is.EqualTo(0));
        }
    }
}
