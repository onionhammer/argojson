using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArgoJson.Test
{
    [TestClass]
    public class TestDeserialization
    {
        [TestMethod]
        public void TestObjects()
        {
            var serialized = "{\"Name\":\"John Smith\",\"Address\":\"1912 Franklin Ave\\nApt. 221\",\"Age\":22}";
            var deserialized = Deserializer.Deserialize<TestObject>(serialized);

            Assert.AreEqual(new TestObject
            {
                Name    = "John Smith",
                Address = "1912 Franklin Ave\nApt. 221",
                Age     = 22
            }, deserialized);

        }
    }
}
