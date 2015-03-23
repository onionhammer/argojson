using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArgoJson.Test
{
    [TestClass]
    public class TestDeserialization
    {
        static readonly char[] OpenItem   = { '[', '{', '"', ',', ':', '}', ']' };
        static readonly char[] OpenString = { '\\', '"' };

        [DebuggerDisplay("{Index}: {Character}")]
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
                switch (serialized[index])
                {
                    default:
                        items.Add(new Token(serialized[index], index++));
                        continue;
                        
                    case '"':
                        // Open a new string
                        items.Add(new Token('"', index++));

                        do
                        {
                            // Seek to end of string
                            index = serialized.IndexOfAny(OpenString, index);

                            switch (serialized[index])
                            {
                                case '"':
                                    // Close string
                                    items.Add(new Token('"', index++));
                                    goto endString;

                                case '\\':
                                    // Increment index by 2
                                    index += 2;
                                    continue;
                            }

                        } while (index < serialized.Length);

                        endString:
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
