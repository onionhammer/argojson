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
        #region Fields

        internal static readonly Dictionary<Type, TypeNode> _types;

        private static readonly AssemblyBuilder _assemblyBuilder;

        internal static readonly ModuleBuilder _assemblyModule;

        #endregion

        #region Constructor

        static Serializer()
        {
            // Define dynamic assembly
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("ArgoJsonSerialization" + Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.RunAndSave
            );

            _assemblyModule = _assemblyBuilder.DefineDynamicModule("Module");

            // Initialize several basic types
            _types = new Dictionary<Type, TypeNode>(capacity: 16)
            {
                { typeof(int),      new TypeNode(typeof(int)) },
                { typeof(bool),     new TypeNode(typeof(bool)) },
                { typeof(double),   new TypeNode(typeof(double)) },
                { typeof(float),    new TypeNode(typeof(float)) },
                { typeof(string),   new TypeNode(typeof(string)) },
                { typeof(Guid),     new TypeNode(typeof(Guid)) },
                { typeof(DateTime), new TypeNode(typeof(DateTime)) },
            };
        }

        protected Serializer() { }

        #endregion

        #region Methods

        public static string Serialize(object value)
        {
            if (value == null)
                throw new ArgumentNullException("item");
            
            var type = value.GetType();

            TypeNode node;
            Helpers.GetHandler(type, out node);

            // TODO - Perform simple heuristics to determine
            // starting size & buffering

            var builder = new StringBuilder(256);
            using (var sw = new StringWriter(builder))
                node._serialize(value, sw);
            
            return builder.ToString();
        }

        public static void SaveAssembly(string output)
        {
            _assemblyBuilder.Save(output);
        }

        #endregion
    }
}