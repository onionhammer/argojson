using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArgoJson.Test
{
    [TestClass]
    public class TestStringSerialization
    {
        [TestMethod]
        public void TestEscape()
        {
            var toEscape = "hello\nworld\rthis\tis\n";

            var escaped = Helpers.Escape(toEscape);

            Assert.AreEqual("hello\\nworld\\rthis\\tis\\n", escaped);

            var unescaped = Helpers.Unescape(escaped);

            Assert.AreEqual(toEscape, unescaped);
        }
    }
}
