using System.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

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

            Assert.AreEqual(sizeOfIEnumerable, storage.CountIEnumerable());
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

            Assert.AreEqual(sizeOfIEnumerable, GetIEnumerableOfTestClass(sizeOfIEnumerable).ToList().Select((x) =>
            {
                Assert.AreNotEqual(-1, x.A);
                return x;
            }).CountIEnumerable());
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

            Assert.AreEqual(sizeOfIEnumerable, GetIEnumerableOfTestClass(sizeOfIEnumerable).CountIEnumerable());
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
