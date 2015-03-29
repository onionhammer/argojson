using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ArgoJson
{
    public class TestItem
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime? Graduated { get; set; }

        //public TestItem Child { get; set; }
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
            var result = new TestItem();

            reader.ReadStartObject();

            string propertyName;
            while (reader.ReadPropertyStart(out propertyName))
            {
                switch (propertyName)
                {
                    case "Id": // Read GUID
                        Guid value1;
                        if (reader.ReadGuidValue(out value1))
                            result.Id = value1;
                        continue;

                    case "Graduated": // Read DateTime
                        DateTime value2;
                        if (reader.ReadDateValue(out value2))
                            result.Graduated = value2;
                        continue;

                    case "Name": // Read String
                        result.Name = reader.ReadStringValue();
                        continue;
                }
            }

            reader.ReadEndObject();

            return result;
        }

        #endregion

        public DeserializerNode(Type type)
        {
            // TODO - remove line:
            _deserialize = DeserializeTestItem;

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
