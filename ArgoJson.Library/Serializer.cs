using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArgoJson
{
    public class Serializer
    {
        internal static readonly Dictionary<int, TypeNode> _types;

        static Serializer()
        {
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

            var builder = new StringBuilder(64);
            using (var sw = new StringWriter(builder))
                node._serialize(value, sw);
            
            return builder.ToString();
        }
    }
}
