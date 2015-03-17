using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArgoJson.Test
{
    [TestClass]
    public class TestArraySerialization
    {
        [TestMethod]
        public void TestSimpleArray()
        {
            var array  = new[] { 0, 1, 2, 3, 4 };
            var result = ArgoJson.Serializer.Serialize(array);

            Assert.AreEqual("[0,1,2,3,4]", result);
        }

        [TestMethod]
        public void TestObject()
        {
            var obj = new
            {
                Name = "John Smith",
                Address = "1912 Franklin Ave\nApt. 221",
                Age = 22
            };

            var result = ArgoJson.Serializer.Serialize(obj);

            //Assert.AreEqual("{\"Name\":\"John Smith\",\"Address\":\"1912 Franklin Ave\\nApt. 221\",}");
        }
    }
}