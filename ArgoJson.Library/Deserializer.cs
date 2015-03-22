using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgoJson
{
    public class Deserializer
    {
        #region Constructor

        protected Deserializer() { }

        #endregion

        #region Methods

        public static T Deserialize<T>(string source)
        {
            using (var sr = new StringReader(source))
                return Deserialize<T>(sr); 
        }

        public static T Deserialize<T>(Stream source)
        {
            using (var sr = new StreamReader(source))
                return Deserialize<T>(sr); 
        }

        public static T Deserialize<T>(TextReader source)
        {
            DeserializerNode node;
            DeserializerNode.GetHandler(typeof(T), out node);

            return (T)node._deserialize(source);
        }

        #endregion
    }
}
