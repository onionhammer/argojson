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
            var builder = new StringBuilder(256);

            using (var sw = new StringWriter(builder))
                Serialize(value, sw);
            
            return builder.ToString();
        }

        public static void Serialize(object value, Stream destination)
        {
            using (var sw = new StreamWriter(destination))
                Serialize(value, sw);
        }

        public static void Serialize(object value, TextWriter destination)
        {
            var type = value.GetType();

            SerializerNode node;
            SerializerNode.GetHandler(type, out node);

            node._serialize(value, destination);
        }

        public static void SaveAssembly(string output)
        {
            _assemblyBuilder.Save(output);
        }

        #endregion
    }
}