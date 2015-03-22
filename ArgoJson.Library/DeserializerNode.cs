using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ArgoJson
{
    internal struct DeserializerNode
    {
        #region Fields

        private static readonly AssemblyBuilder _assemblyBuilder;

        private static readonly ModuleBuilder _assemblyModule;

        private static readonly Dictionary<Type, DeserializerNode> _types;

        public readonly Func<TextReader, object> _deserialize;

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

        public DeserializerNode(Type type)
        {
            bool nullable = false;

            reprocess:

            if (type.IsPrimitive)
            {

            }
            else
            {

            }

            throw new NotImplementedException();
        }

        #endregion
    }
}
