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
        [TestCase(100000000)]
        [TestCase(1000000000)]
        public void CanCreateOffHeapStorage(int sizeOfIEnumerable)
        {
            var storage = new OffHeapIEnumerable<TestClass>(GetIEnumerableOfTestClass(sizeOfIEnumerable));

            Assert.AreEqual(sizeOfIEnumerable, storage.ToList().Count);
        }
    }
}
