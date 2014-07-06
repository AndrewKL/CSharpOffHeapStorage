using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace OffHeapStorage.tests
{
    [TestFixture]
    public class ObjectSerializationTests
    {
        public class TestObj
        {
            public int A { get; set; }
            public bool B { get; set; }
            public string C { get; set; }
            public decimal D { get; set; }
            public float E { get; set; }
        }

        

        [Test]
        public void CreateSpecForType()
        {
            var spec = new ObjectSerializationInfo(typeof (TestObj));

            Assert.AreEqual(5,spec.PropertyList.Count);

            var newObj = spec.Constructor();
            Assert.AreEqual(typeof(TestObj),newObj.GetType());

            foreach(var prop in spec.PropertyList)
            {
                Assert.NotNull(prop.Getter);
                Assert.NotNull(prop.Setter);
            }
        }
    }
}
