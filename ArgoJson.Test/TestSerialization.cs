using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArgoJson.Console.Model;

namespace ArgoJson.Test
{
    [TestClass]
    public class TestSerialization
    {
        [TestMethod]
        public void TestSimpleArray()
        {
            var array  = new[] { 0, 1, 2, 3, 4 };
            var result = ArgoJson.Serializer.Serialize(array);

            Assert.AreEqual("[0,1,2,3,4]", result);
        }
        
        [TestMethod]
        public void TestObjects()
        {
            var obj = new TestObject
            {
                Name    = "John Smith",
                Address = "1912 Franklin Ave\nApt. 221",
                Age     = 22
            };

            var result = ArgoJson.Serializer.Serialize(obj);

            Assert.AreEqual("{\"Name\":\"John Smith\",\"Address\":\"1912 Franklin Ave\\nApt. 221\",\"Age\":22}",
                            result);
        }

        //TODO - [TestMethod]
        public void TestAnonymous()
        {
            var obj = new
            {
                Name    = "John Smith",
                Address = "1912 Franklin Ave\nApt. 221",
                Age     = 22
            };

            var asString = obj.ToString();
            var result = ArgoJson.Serializer.Serialize(obj);
        }

        public class TestObject
        {
            public string Name { get; set; }

            public string Address { get; set; }

            public int Age { get; set; }
        }
    }
}