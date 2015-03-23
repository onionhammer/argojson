using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace ArgoJson.Test
{
    [TestClass]
    public class TestDeserialization
    {
        [DebuggerDisplay("{Character}: {Index}")]
        struct Token
        {
            public readonly char Character;
            public readonly int Index;

            public Token(char character, int index)
            {
                Character = character;
                Index     = index;
            }
        }

        static char[] OpenItem = { '[', '{', '"', ',', ':', '}', ']' };

        [TestMethod]
        public void PlayPen()
        {
            var serialized = "{\"Name\":\"John Smith\",\"Address\":\"1912 Franklin Ave\\nApt. 221\",\"Age\":22}";

            var items = new List<Token>(capacity: 32);
            int index = 0;

            do
            {
                index = serialized.IndexOfAny(OpenItem, index);

                //Position next
                char charAtIndex = serialized[index];
                switch (charAtIndex)
                {
                    default:
                        items.Add(new Token(charAtIndex, index++));
                        continue;
                        
                    case '"': // Open or close string
                        // Open a new string
                        items.Add(new Token('"', index++));

                        // TODO: Seek to end of string
                         
                        // Close the string
                        continue;
                }

            } while (index < serialized.Length);
        }

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
