using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ArgoJson
{
    public class TestItem
    {
        static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }

            return true;
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime? Graduated { get; set; }

        //public TestItem Child { get; set; }

        public string[] Checkins { get; set; }

        public override bool Equals(object obj)
        {
            var otherItem = obj as TestItem;

            return this.Id == otherItem.Id 
                && this.Name == otherItem.Name 
                && this.Graduated == otherItem.Graduated
                && ArraysEqual(this.Checkins, otherItem.Checkins);
        }
    }

    internal struct DeserializerNode
    {
        #region Fields

        private static readonly AssemblyBuilder _assemblyBuilder;

        private static readonly ModuleBuilder _assemblyModule;

        private static readonly Dictionary<Type, DeserializerNode> _types;

        public readonly Func<JsonReader, object> _deserialize;

        #endregion

        #region Methods

        public static void GetHandler(Type type, out DeserializerNode node)
        {
            if (_types.TryGetValue(type, out node) == false)
            {
                // Create a new handler for this type as it is not recognized
                node = new DeserializerNode(type);

                // Add a new handler for this type
                _types.Add(type, node);
            }
        }

        #endregion

        #region Constructor

        static DeserializerNode()
        {
            // Initialize assembly module and expression tree dictionary
            _assemblyModule = Helpers.CreateModule(out _assemblyBuilder);
            _types          = new Dictionary<Type, DeserializerNode>(capacity: 16);
        }

        #region Test

        static object DeserializeTestItem(JsonReader reader)
        {
            if (reader.ReadStartObject() == false)
                return null;

            string propertyName;
            Guid value1;
            DateTime value2;

            var result = new TestItem();

            while (reader.ReadPropertyStart(out propertyName))
            {
                switch (propertyName)
                {
                    case "Id": // Read GUID
                        if (reader.ReadGuidValue(out value1))
                            result.Id = value1;
                        continue;

                    case "Graduated": // Read DateTime
                        if (reader.ReadDateValue(out value2))
                            result.Graduated = value2;
                        continue;

                    case "Name": // Read String
                        result.Name = reader.ReadStringValue();
                        continue;

                    case "Checkins": // Read Array of Ints
                        if (reader.ReadStartArray())
                        {
                            var items = new List<string>(capacity: 4);

                            do
                            {
                                items.Add(reader.ReadStringValue());
                            }
                            while(reader.ContinueArray());

                            result.Checkins = items.ToArray();
                        }
                        continue;

                }
            }

            reader.SkipEndObject();

            return result;
        }

        static object DeserializeTestItems(JsonReader reader)
        {
            if (reader.ReadStartArray() == false)
                return null;

            var arrayResult = new List<TestItem>();

            do
            {
                if (reader.ReadStartObject() == false)
                {
                    arrayResult.Add(null);
                }
                else
                {
                    string propertyName;
                    Guid value1;
                    DateTime value2;

                    var result = new TestItem();

                    while (reader.ReadPropertyStart(out propertyName))
                    {
                        switch (propertyName)
                        {
                            case "Id": // Read GUID
                                if (reader.ReadGuidValue(out value1))
                                    result.Id = value1;
                                continue;

                            case "Graduated": // Read DateTime
                                if (reader.ReadDateValue(out value2))
                                    result.Graduated = value2;
                                continue;

                            case "Name": // Read String
                                result.Name = reader.ReadStringValue();
                                continue;

                            case "Checkins": // Read Array of Ints
                                if (reader.ReadStartArray())
                                {
                                    var items = new List<string>(capacity: 4);

                                    do
                                    {
                                        items.Add(reader.ReadStringValue());
                                    }
                                    while (reader.ContinueArray());

                                    result.Checkins = items.ToArray();
                                }
                                continue;

                        }
                    }

                    reader.SkipEndObject();

                    arrayResult.Add(result);
                }

            } while (reader.ContinueArray());

            return arrayResult;
        }

        #endregion

        public DeserializerNode(Type type)
        {
            // TODO - remove line:
            _deserialize = DeserializeTestItem;
//            _deserialize = DeserializeTestItems;

            bool nullable = false;

            reprocess:

            if (type.IsPrimitive)
            {

            }
            else
            {

            }

            //_deserialize = (reader) => null;
        }

        #endregion
    }
}
