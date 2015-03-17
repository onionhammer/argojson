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
        }
    }
}