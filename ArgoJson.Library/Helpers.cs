using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ArgoJson
{
    public static class Helpers
    {
        #region Fields

        static char[] CharsToEscape = { 
            '"', '\\', '/', '\b', '\f', '\n', '\r', '\t' 
        };

        #endregion

        #region Methods

        public static string Escape(string value)
        {
            var result = new StringBuilder(value);
            var index  = 0;
            var offset = 0;
            int destIndex;

            do
            {
                index = value.IndexOfAny(CharsToEscape, index);

                if (index < 0)
                    break;

                destIndex = index + offset;
                switch (result[destIndex])
                {
                    case '\b': result[destIndex] = 'b'; break;
                    case '\f': result[destIndex] = 'f'; break;
                    case '\n': result[destIndex] = 'n'; break;
                    case '\r': result[destIndex] = 'r'; break;
                    case '\t': result[destIndex] = 't'; break;
                }

                result.Insert(index++ + offset++, '\\');
            } while (index + offset < result.Length);

            return result.ToString();
        }

        internal static bool IsOfGeneric(this Type type, Type interfaceType, out Type subType)
        {
            var interfaces = type.GetInterfaces();

            for (var i = 0; i < interfaces.Length; ++i)
            {
                var iface = interfaces[i];

                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == interfaceType)
                {
                    subType = iface;
                    return true;
                }
            }

            subType = null;
            return false;
        }

        internal static MethodInfo GetDispose(Type type)
        {
            var interfaces = type.GetInterfaces();

            for (var i = 0; i < interfaces.Length; ++i)
            {
                var iface = interfaces[i];

                if (iface == typeof(IDisposable))
                    return iface.GetMethod("Dispose");
            }

            return null;
        }

        internal static ModuleBuilder CreateModule(out AssemblyBuilder assembly)
        {
            // Define dynamic assembly
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("ArgoJsonModule" + Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.RunAndSave
            );

            return assembly.DefineDynamicModule("Module");
        }

        internal static Delegate CompileToType<T>(
            this ModuleBuilder module,
            Type type, Expression<T> expression)
        {
            var typeBuilder = module.DefineType(
                type.Name + "Operation" + Guid.NewGuid().ToString("N"));

            var methodBuilder = typeBuilder.DefineMethod(
                "Operation", MethodAttributes.Public | MethodAttributes.Static);

            expression.CompileToMethod(methodBuilder);

            var serializerType = typeBuilder.CreateType();

            return Delegate.CreateDelegate(
                expression.Type,
                serializerType.GetMethod("Operation")
            );
        }

        #endregion
    }
}