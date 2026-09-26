using System.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OffHeapStorage.tests
{
    [TestFixture]
    public class OffHeapStorageTests
    {
        public class TestClass
        {
            public int A {get;set;}
            public string B {get;set;}
        }
        public IEnumerable<TestClass> GetIEnumerableOfTestClass(int size)
        {
            for(int i = 0;i<size;i++){
                yield return new TestClass()
                {
                    A = i,
                    B = i.ToString()
                };
            }
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000)]
        [TestCase(1000000)]
        [TestCase(10000000)]
        //[TestCase(100000000)]
        //[TestCase(1000000000)]
        public void CanCreateOffHeapStorage(int sizeOfIEnumerable)
        {
            var storage = new OffHeapIEnumerable<TestClass>(GetIEnumerableOfTestClass(sizeOfIEnumerable));

            Assert.That(storage.CountIEnumerable(), Is.EqualTo(sizeOfIEnumerable));
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000)]
        [TestCase(1000000)]
        [TestCase(10000000)]
        //[TestCase(100000000)]
        //[TestCase(1000000000)]
        public void CanCreateOnHeapStorage(int sizeOfIEnumerable)
        {

            Assert.That(GetIEnumerableOfTestClass(sizeOfIEnumerable).ToList().Select((x) =>
            {
                Assert.That(x.A, Is.Not.EqualTo(-1));
                return x;
            }).CountIEnumerable(), Is.EqualTo(sizeOfIEnumerable));
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000)]
        [TestCase(1000000)]
        [TestCase(10000000)]
        //[TestCase(100000000)]
        //[TestCase(1000000000)]
        public void SanityCheckWithIEnumerable(int sizeOfIEnumerable)
        {

            Assert.That(GetIEnumerableOfTestClass(sizeOfIEnumerable).CountIEnumerable(), Is.EqualTo(sizeOfIEnumerable));
        }

        public class AllTypes
        {
            public int I { get; set; }
            public bool B { get; set; }
            public decimal M { get; set; }
            public string S { get; set; }
            public float F { get; set; }
            public double D { get; set; }
        }

        [Test]
        public void RoundTripsAllSupportedTypes()
        {
            var input = Enumerable.Range(0, 5000).Select(i => new AllTypes
            {
                I = i,
                B = i % 2 == 0,
                M = i * 1.25m,
                S = i % 7 == 0 ? null : "item " + i,
                F = i / 3f,
                D = i / 7d
            }).ToList();

            var output = new OffHeapIEnumerable<AllTypes>(input).ToList();

            Assert.That(output.Count, Is.EqualTo(input.Count));
            for (int i = 0; i < input.Count; i++)
            {
                Assert.That(output[i].I, Is.EqualTo(input[i].I));
                Assert.That(output[i].B, Is.EqualTo(input[i].B));
                Assert.That(output[i].M, Is.EqualTo(input[i].M));
                Assert.That(output[i].S, Is.EqualTo(input[i].S));
                Assert.That(output[i].F, Is.EqualTo(input[i].F));
                Assert.That(output[i].D, Is.EqualTo(input[i].D));
            }
        }

        [Test]
        public void CanEnumerateMultipleTimes()
        {
            var storage = new OffHeapIEnumerable<TestClass>(GetIEnumerableOfTestClass(100));

            Assert.That(storage.Select(x => x.A), Is.EqualTo(Enumerable.Range(0, 100)));
            Assert.That(storage.Select(x => x.A), Is.EqualTo(Enumerable.Range(0, 100)));
        }

        [Test]
        public void InterleavedEnumeratorsAreIndependent()
        {
            var storage = new OffHeapIEnumerable<TestClass>(GetIEnumerableOfTestClass(1000));

            var pairs = storage.Zip(storage.Skip(1), (a, b) => b.A - a.A).ToList();

            Assert.That(pairs.Count, Is.EqualTo(999));
            Assert.That(pairs, Is.All.EqualTo(1));
        }

        [Test]
        public void EmptyInputProducesEmptyStorage()
        {
            var storage = new OffHeapIEnumerable<TestClass>(Enumerable.Empty<TestClass>());

            Assert.That(storage, Is.Empty);
        }


    }

    public static class IEnumerableExts
    {
        public static int CountIEnumerable(this IEnumerable ienumerable)
        {
            var counter = 0;
            var enumerator = ienumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                counter++;
            }
            return counter;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> iEnumerable, Action<T> action)
        {
            foreach (var obj in iEnumerable)
            {
                action(obj);
                yield return obj;
            }
        } 
    }
}
