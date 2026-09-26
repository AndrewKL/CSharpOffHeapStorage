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

            Assert.That(spec.PropertyList.Count, Is.EqualTo(5));

            var newObj = spec.Constructor();
            Assert.That(newObj.GetType(), Is.EqualTo(typeof(TestObj)));

            foreach(var prop in spec.PropertyList)
            {
                Assert.That(prop.Getter, Is.Not.Null);
                Assert.That(prop.Setter, Is.Not.Null);
            }
        }

        public class DerivedObj : TestObj
        {
            public double F { get; set; }
            public int ReadOnly { get { return 42; } }
            public long Unsupported { get; set; }
        }

        [Test]
        public void IncludesInheritedAndSkipsReadOnlyOrUnsupportedProperties()
        {
            var spec = new ObjectSerializationInfo(typeof(DerivedObj));

            Assert.That(spec.PropertyList.Count, Is.EqualTo(6));
        }
    }
}
