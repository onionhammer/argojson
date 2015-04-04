using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
        public void TestTestItem()
        {
            var original        = new TestItem {
                Id              = Guid.NewGuid(),
                Graduated       = DateTime.Today.AddYears(-15),
                Name            = "John Smith",
                Checkins        = new[] { 0, 2, 4, 6 },
                //Child         = new TestItem
                //{
                //    Id        = Guid.NewGuid(),
                //    Graduated = DateTime.Today.AddYears(-10),
                //    Name      = "Jane Doe"
                //}
            };

            var serialized   = Serializer.Serialize(original);
            //serialized = "{\"Id\":\"1adee806-f966-43b8-8c30-d0551d147460\",\"Name\":\"John Smith\",\"Graduated\":\"2000-03-28T00:00:00-06:00\"}";
            var deserialized = Deserializer.Deserialize<TestItem>(serialized);
            
            Assert.AreEqual(original, deserialized);
        }

        [TestMethod]
        public void PlayPen()
        {
            var serialized = "{\"Name\":\"John Smith\",\"Address\":\"1912 Franklin Ave\\nApt. 221\",\"Age\":22}";

            var tokens = new List<Token>(capacity: 32);
            int index = 0;

            do
            {
                index = serialized.IndexOfAny(OpenItem, index);

                //Position next
                switch (serialized[index])
                {
                    default:
                        tokens.Add(new Token(serialized[index], index++));
                        continue;
                        
                    case '"':
                        // Open a new string
                        tokens.Add(new Token('"', index++));

                        do
                        {
                            // Seek to end of string
                            index = serialized.IndexOfAny(OpenString, index);

                            switch (serialized[index])
                            {
                                case '"':
                                    // Close string
                                    tokens.Add(new Token('"', index++));
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
