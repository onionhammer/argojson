using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ArgoJson
{
    public class Serializer
    {
        #region Fields

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
        }

        protected Serializer() { }

        #endregion

        #region Methods

        public static string Serialize(object value)
        {
            if (value == null)
                throw new ArgumentNullException("item");
            
            var type    = value.GetType();
            var builder = new StringBuilder(256);

            SerializerNode node;
            SerializerNode.GetHandler(type, out node);

            // TODO - Perform simple heuristics to determine
            // starting size & buffering

            // TODO - Determine if type is anonymous.

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