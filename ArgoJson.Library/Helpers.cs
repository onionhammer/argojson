﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ArgoJson
{
    internal static class Helpers
    {
        static char[] Illegal = { 
            '"', '\\', '/', '\b', '\f', '\n', '\r', '\t' 
        };

        public static string Escape(string value)
        {
            var result = new StringBuilder(value);
            var index  = 0;
            var offset = 0;

            do
            {
                int found = value.IndexOfAny(Illegal, index);

                if (found > -1)
                {
                    index = found;

                    switch (result[found])
                    {
                        case '\b': result[found] = 'b'; break;
                        case '\f': result[found] = 'f'; break;
                        case '\n': result[found] = 'n'; break;
                        case '\r': result[found] = 'r'; break;
                        case '\t': result[found] = 't'; break;
                    }

                    result.Insert(index + offset++, '\\');
                }
                else break;

            } while (index < result.Length);

            return result.ToString();
        }

        public static bool IsOfGeneric(this Type type, Type interfaceType, out Type subType)
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

        public static MethodInfo GetDispose(Type type)
        {
            var interfaces = type.GetInterfaces();

            for (var i = 0; i < interfaces.Length; ++i)
            {
                var iface = interfaces[i];

                if (iface == typeof(IDisposable))
                    return iface.GetMethod("Dispose");
            }

            throw new NotImplementedException();
        }
    }
}