using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace ArgoJson.Test
{
    [TestClass]
    public class TestDeserialization
    {
        enum ObjectType
        {
            Array,
            Object,
            String,
            Number,
            Property
        }

        [DebuggerDisplay("{Type}: {Index}")]
        class Position
        {
            public readonly int Index;
            public readonly ObjectType Type;
            public readonly List<Position> Children;
            public int _length;

            public Position(int index, ObjectType type)
            {
                Index = index;
                Type  = type;

                switch (type)
                {
                    case ObjectType.Array:
                    case ObjectType.Object:
                    case ObjectType.Property:
                        Children = new List<Position>();
                        break;
                }
            }
        }

        static char[] OpenItem = { '[', '{', '"', '}', ']' };

        [TestMethod]
        public void PlayPen()
        {
            var serialized = "{\"Name\":\"John Smith\",\"Address\":\"1912 Franklin Ave\\nApt. 221\",\"Age\":22}";

            var items = new Stack<Position>(capacity: 32);
            int index = 0;

            Position top = null;

            do
            {
                var parent = top;
                index = serialized.IndexOfAny(OpenItem, index);

                //Position next;
                switch (serialized[index])
                {
                    case '[': // Open array
                        items.Push(top = new Position(index++, ObjectType.Array));
                        continue;

                    case '{': // Open object
                        items.Push(top = new Position(index++, ObjectType.Object));
                        continue;

                    case '"': // Open or close string
                        if (parent.Type == ObjectType.Object)
                        {
                            // Open property
                            items.Push(top = new Position(index, ObjectType.Property));
                            parent.Children.Add(top);
                            parent = top;
                        }

                        // Open a new string
                        items.Push(top = new Position(index++, ObjectType.String));

                        // TODO: Seek to end of string
                         
                        // Close the string
                        top._length = index++ - top.Index;
                        continue;

                    case '}': // Close object
                        break;

                    case ']': // Close array
                        break;
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
