using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ArgoJson
{
    public class Serializer
    {
        internal static readonly Dictionary<int, TypeNode> _types;

        private static readonly AssemblyBuilder _assemblyBuilder;

        internal static readonly ModuleBuilder _assemblyModule;

        static Serializer()
        {
            // Define dynamic assembly
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("ArgoJsonSerialization" + Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.Run
            );

            _assemblyModule = _assemblyBuilder.DefineDynamicModule("Module");

            // Initialize several basic types
            _types = new Dictionary<int, TypeNode>(capacity: 32)
            {
                { typeof(int).GetHashCode(),      new TypeNode(typeof(int)) },
                { typeof(bool).GetHashCode(),     new TypeNode(typeof(bool)) },
                { typeof(double).GetHashCode(),   new TypeNode(typeof(double)) },
                { typeof(float).GetHashCode(),    new TypeNode(typeof(float)) },
                { typeof(string).GetHashCode(),   new TypeNode(typeof(string)) },
                { typeof(Guid).GetHashCode(),     new TypeNode(typeof(Guid)) },
                { typeof(DateTime).GetHashCode(), new TypeNode(typeof(DateTime)) },
            };
        }

        public static string Serialize(object value)
        {
            if (value == null)
                throw new ArgumentNullException("item");
            
            var type     = value.GetType();
            var typeHash = type.GetHashCode();

            TypeNode node;
            if (_types.TryGetValue(typeHash, out node) == false)
            {
                node = new TypeNode(type);
                _types.Add(typeHash, node);
            }

            // TODO - Perform simple heuristics to determine
            // starting size & buffering

            var builder = new StringBuilder(256);
            using (var sw = new StringWriter(builder))
                node._serialize(value, sw);
            
            return builder.ToString();
        }

        public static void Save<Type>(string output)
        {
            var type     = typeof(Type);
            var typeHash = type.GetHashCode();

            TypeNode node;
            if (_types.TryGetValue(typeHash, out node) == false)
            {
                node = new TypeNode(type);
                _types.Add(typeHash, node);
            }

            throw new NotImplementedException();
        }
    }
}
